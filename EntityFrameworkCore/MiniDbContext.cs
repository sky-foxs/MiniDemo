using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MiniDemo.Data;
using MiniDemo.DependencyInjection;
using MiniDemo.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MiniDemo.Helpers;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.ComponentModel.DataAnnotations.Schema;
using MiniDemo.Audit;
using MiniDemo.Tenant;

namespace MiniDemo.EntityFrameworkCore
{
    public abstract class MiniDbContext<TDbContext> : DbContext,IEfCoreDbContext
        where TDbContext : DbContext
    {
       // public ILazyServiceProvider LazyServiceProvider { get; set; }
        public IServiceProvider ServiceProvider { get; set; }

        protected virtual Guid? CurrentTenantId => CurrentTenant?.Id;

        protected virtual bool IsMultiTenantFilterEnabled => DataFilter?.IsEnabled<IMultiTenant>() ?? false;
        protected virtual bool IsSoftDeleteFilterEnabled => DataFilter?.IsEnabled<ISoftDelete>() ?? false;

        public IDataFilter DataFilter => (IDataFilter)ServiceProvider.GetService(typeof(IDataFilter));

        public ICurrentTenant CurrentTenant => (ICurrentTenant)ServiceProvider.GetService(typeof(ICurrentTenant));

        public IAuditPropertySetter AuditPropertySetter => (IAuditPropertySetter)ServiceProvider.GetService(typeof(IAuditPropertySetter));

        private static readonly MethodInfo ConfigureBasePropertiesMethodInfo
           = typeof(MiniDbContext<TDbContext>)
               .GetMethod(
                   nameof(ConfigureBaseProperties),
                   BindingFlags.Instance | BindingFlags.NonPublic
               );
        protected MiniDbContext(DbContextOptions<TDbContext> options,IServiceProvider serviceProvider)
            : base(options)
        {
            ServiceProvider = serviceProvider;
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            try
            {
                var changeReport = ApplyAbpConcepts();

                var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new Exception(ex.Message, ex);
            }
            finally
            {
                ChangeTracker.AutoDetectChangesEnabled = true;
            }
        }
        public virtual Task<int> SaveChangesOnDbContextAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            TrySetDatabaseProvider(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                ConfigureBasePropertiesMethodInfo
                    .MakeGenericMethod(entityType.ClrType)
                    .Invoke(this, new object[] { modelBuilder, entityType });

            }
        }

        protected virtual EntityChangeReport ApplyAbpConcepts()
        {
            var changeReport = new EntityChangeReport();

            foreach (var entry in ChangeTracker.Entries().ToList())
            {
                ApplyConcepts(entry, changeReport);
            }

            return changeReport;
        }

        protected virtual void ApplyConcepts(EntityEntry entry, EntityChangeReport changeReport)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    ApplyConceptsForAddedEntity(entry, changeReport);
                    break;
                case EntityState.Modified:
                    ApplyConceptsForModifiedEntity(entry, changeReport);
                    break;
                case EntityState.Deleted:
                    ApplyConceptsForDeletedEntity(entry, changeReport);
                    break;
            }


            AddDomainEvents(changeReport, entry.Entity);
        }

        #region 添加

        protected virtual void ApplyConceptsForAddedEntity(EntityEntry entry, EntityChangeReport changeReport)
        {
            CheckAndSetId(entry);
            SetConcurrencyStampIfNull(entry);
            SetCreationAuditProperties(entry);
            changeReport.ChangedEntities.Add(new EntityChangeEntry(entry.Entity, EntityChangeType.Created));
        }
        protected virtual void CheckAndSetId(EntityEntry entry)
        {
            if (entry.Entity is IEntity<Guid> entityWithGuidId)
            {
                TrySetGuidId(entry, entityWithGuidId);
            }
        }

        protected virtual void TrySetGuidId(EntityEntry entry, IEntity<Guid> entity)
        {
            if (entity.Id != default)
            {
                return;
            }

            var idProperty = entry.Property("Id").Metadata.PropertyInfo;

            //Check for DatabaseGeneratedAttribute
            var dbGeneratedAttr = ReflectionHelper
                .GetSingleAttributeOrDefault<DatabaseGeneratedAttribute>(
                    idProperty
                );

            if (dbGeneratedAttr != null && dbGeneratedAttr.DatabaseGeneratedOption != DatabaseGeneratedOption.None)
            {
                return;
            }

            EntityHelper.TrySetId(
                entity,
                () => Guid.NewGuid(),
                true
            );
        }

        protected virtual void SetConcurrencyStampIfNull(EntityEntry entry)
        {
            var entity = entry.Entity as IHasConcurrencyStamp;
            if (entity == null)
            {
                return;
            }

            if (entity.ConcurrencyStamp != null)
            {
                return;
            }

            entity.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        }

        protected virtual void SetCreationAuditProperties(EntityEntry entry)
        {
            AuditPropertySetter?.SetCreationProperties(entry.Entity);
        }

        #endregion

        #region 修改

        protected virtual void ApplyConceptsForModifiedEntity(EntityEntry entry, EntityChangeReport changeReport)
        {
            UpdateConcurrencyStamp(entry);
            SetModificationAuditProperties(entry);

            if (entry.Entity is ISoftDelete && entry.Entity.As<ISoftDelete>().IsDeleted)
            {
                SetDeletionAuditProperties(entry);
                changeReport.ChangedEntities.Add(new EntityChangeEntry(entry.Entity, EntityChangeType.Deleted));
            }
            else
            {
                changeReport.ChangedEntities.Add(new EntityChangeEntry(entry.Entity, EntityChangeType.Updated));
            }
        }
        protected virtual void UpdateConcurrencyStamp(EntityEntry entry)
        {
            var entity = entry.Entity as IHasConcurrencyStamp;
            if (entity == null)
            {
                return;
            }

            Entry(entity).Property(x => x.ConcurrencyStamp).OriginalValue = entity.ConcurrencyStamp;
            entity.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        }

        protected virtual void SetModificationAuditProperties(EntityEntry entry)
        {
            AuditPropertySetter?.SetModificationProperties(entry.Entity);
        }

        protected virtual void SetDeletionAuditProperties(EntityEntry entry)
        {
            AuditPropertySetter?.SetDeletionProperties(entry.Entity);
        }

        #endregion

        #region 删除
        protected virtual void ApplyConceptsForDeletedEntity(EntityEntry entry, EntityChangeReport changeReport)
        {
            if (TryCancelDeletionForSoftDelete(entry))
            {
                UpdateConcurrencyStamp(entry);
                SetDeletionAuditProperties(entry);
            }

            changeReport.ChangedEntities.Add(new EntityChangeEntry(entry.Entity, EntityChangeType.Deleted));
        }
        protected virtual bool TryCancelDeletionForSoftDelete(EntityEntry entry)
        {
            if (!(entry.Entity is ISoftDelete))
            {
                return false;
            }

            entry.Reload();
            entry.State = EntityState.Modified;
            entry.Entity.As<ISoftDelete>().IsDeleted = true;
            return true;
        }

        #endregion

        protected virtual void AddDomainEvents(EntityChangeReport changeReport, object entityAsObj)
        {
            var generatesDomainEventsEntity = entityAsObj as IGeneratesDomainEvents;
            if (generatesDomainEventsEntity == null)
            {
                return;
            }

            var localEvents = generatesDomainEventsEntity.GetLocalEvents()?.ToArray();
            if (localEvents != null && localEvents.Any())
            {
                changeReport.DomainEvents.AddRange(localEvents.Select(eventData => new DomainEventEntry(entityAsObj, eventData)));
                generatesDomainEventsEntity.ClearLocalEvents();
            }

            var distributedEvents = generatesDomainEventsEntity.GetDistributedEvents()?.ToArray();
            if (distributedEvents != null && distributedEvents.Any())
            {
                changeReport.DistributedEvents.AddRange(distributedEvents.Select(eventData => new DomainEventEntry(entityAsObj, eventData)));
                generatesDomainEventsEntity.ClearDistributedEvents();
            }
        }

        #region 配置

        protected virtual void TrySetDatabaseProvider(ModelBuilder modelBuilder)
        {
            var provider = GetDatabaseProviderOrNull(modelBuilder);
            if (provider != null)
            {
                modelBuilder.SetDatabaseProvider(provider.Value);
            }
        }
        protected virtual EfCoreDatabaseProvider? GetDatabaseProviderOrNull(ModelBuilder modelBuilder)
        {
            switch (Database.ProviderName)
            {
                case "Microsoft.EntityFrameworkCore.SqlServer":
                    return EfCoreDatabaseProvider.SqlServer;
                case "Npgsql.EntityFrameworkCore.PostgreSQL":
                    return EfCoreDatabaseProvider.PostgreSql;
                case "Pomelo.EntityFrameworkCore.MySql":
                    return EfCoreDatabaseProvider.MySql;
                case "Oracle.EntityFrameworkCore":
                case "Devart.Data.Oracle.Entity.EFCore":
                    return EfCoreDatabaseProvider.Oracle;
                case "Microsoft.EntityFrameworkCore.Sqlite":
                    return EfCoreDatabaseProvider.Sqlite;
                case "Microsoft.EntityFrameworkCore.InMemory":
                    return EfCoreDatabaseProvider.InMemory;
                case "FirebirdSql.EntityFrameworkCore.Firebird":
                    return EfCoreDatabaseProvider.Firebird;
                case "Microsoft.EntityFrameworkCore.Cosmos":
                    return EfCoreDatabaseProvider.Cosmos;
                default:
                    return null;
            }
        }

        protected virtual void ConfigureBaseProperties<TEntity>(ModelBuilder modelBuilder, IMutableEntityType mutableEntityType)
           where TEntity : class
        {
            if (mutableEntityType.IsOwned())
            {
                return;
            }

            if (!typeof(IEntity).IsAssignableFrom(typeof(TEntity)))
            {
                return;
            }

            //to do
            modelBuilder.Entity<TEntity>().ConfigureByConvention();

            ConfigureGlobalFilters<TEntity>(modelBuilder, mutableEntityType);
        }

        #endregion

        #region 过滤

        /// <summary>
        /// 将数据过滤语句加到查询中
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="modelBuilder"></param>
        /// <param name="mutableEntityType"></param>
        protected virtual void ConfigureGlobalFilters<TEntity>(ModelBuilder modelBuilder, IMutableEntityType mutableEntityType)
           where TEntity : class
        {
            if (mutableEntityType.BaseType == null && ShouldFilterEntity<TEntity>(mutableEntityType))
            {
                var filterExpression = CreateFilterExpression<TEntity>();
                if (filterExpression != null)
                {
                    modelBuilder.Entity<TEntity>().HasQueryFilter(filterExpression);
                }
            }
        }

        protected virtual bool ShouldFilterEntity<TEntity>(IMutableEntityType entityType) where TEntity : class
        {
            if (typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity)))
            {
                return true;
            }

            if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 返回数据过滤表达式
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        protected virtual Expression<Func<TEntity, bool>> CreateFilterExpression<TEntity>()
           where TEntity : class
        {
            Expression<Func<TEntity, bool>> expression = null;

            if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
            {
                expression = e => !IsSoftDeleteFilterEnabled || !EF.Property<bool>(e, "IsDeleted");
            }

            if (typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity)))
            {
                Expression<Func<TEntity, bool>> multiTenantFilter = e => !IsMultiTenantFilterEnabled || EF.Property<Guid>(e, "TenantId") == CurrentTenantId;
                expression = expression == null ? multiTenantFilter : CombineExpressions(expression, multiTenantFilter);
            }

            return expression;
        }

        /// <summary>
        /// 将expression1与expression2合并到一个表达式返回
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression1"></param>
        /// <param name="expression2"></param>
        /// <returns></returns>
        protected virtual Expression<Func<T, bool>> CombineExpressions<T>(Expression<Func<T, bool>> expression1, Expression<Func<T, bool>> expression2)
        {
            var parameter = Expression.Parameter(typeof(T));

            var leftVisitor = new ReplaceExpressionVisitor(expression1.Parameters[0], parameter);
            var left = leftVisitor.Visit(expression1.Body);

            var rightVisitor = new ReplaceExpressionVisitor(expression2.Parameters[0], parameter);
            var right = rightVisitor.Visit(expression2.Body);

            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left, right), parameter);
        }
        class ReplaceExpressionVisitor : ExpressionVisitor
        {
            private readonly Expression _oldValue;
            private readonly Expression _newValue;

            public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
            {
                _oldValue = oldValue;
                _newValue = newValue;
            }

            public override Expression Visit(Expression node)
            {
                if (node == _oldValue)
                {
                    return _newValue;
                }

                return base.Visit(node);
            }
        }

        #endregion
    }
}
