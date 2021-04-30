using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using MiniDemo.Audit;
using MiniDemo.Data;
using MiniDemo.Helpers;
using MiniDemo.Tenant;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MiniDemo.EntityFrameworkCore
{
    public abstract class MiniDbContext<TDbContext> : DbContext, IEfCoreDbContext
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
        protected MiniDbContext(DbContextOptions<TDbContext> options, IServiceProvider serviceProvider)
            : base(options)
        {
            ServiceProvider = serviceProvider;
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            try
            {
                foreach (var entry in ChangeTracker.Entries().ToList())
                {
                    ApplyConcepts(entry);
                }
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


        protected virtual void ApplyConcepts(EntityEntry entry)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    ApplyConceptsForAddedEntity(entry);
                    break;
                case EntityState.Modified:
                    ApplyConceptsForModifiedEntity(entry);
                    break;
                case EntityState.Deleted:
                    ApplyConceptsForDeletedEntity(entry);
                    break;
            }

        }

        #region 添加

        protected virtual void ApplyConceptsForAddedEntity(EntityEntry entry)
        {
            SetCreationAuditProperties(entry);
        }


        protected virtual void SetCreationAuditProperties(EntityEntry entry)
        {
            AuditPropertySetter?.SetCreationProperties(entry.Entity);
        }

        #endregion

        #region 修改

        protected virtual void ApplyConceptsForModifiedEntity(EntityEntry entry)
        {
            SetModificationAuditProperties(entry);

            if (entry.Entity is ISoftDelete && entry.Entity.As<ISoftDelete>().IsDeleted)
            {
                SetDeletionAuditProperties(entry);
            }
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
        protected virtual void ApplyConceptsForDeletedEntity(EntityEntry entry)
        {
            if (TryCancelDeletionForSoftDelete(entry))
            {
                SetDeletionAuditProperties(entry);
            }

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
