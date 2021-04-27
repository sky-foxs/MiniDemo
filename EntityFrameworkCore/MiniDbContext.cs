using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MiniDemo.Data;
using MiniDemo.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MiniDemo.EntityFrameworkCore
{
    public abstract class MiniDbContext<TDbContext> : DbContext,IEfCoreDbContext
        where TDbContext : DbContext
    {
        public ILazyServiceProvider LazyServiceProvider { get; set; }

        public IDataFilter DataFilter => LazyServiceProvider.LazyGetRequiredService<IDataFilter>();

        protected virtual bool IsSoftDeleteFilterEnabled => DataFilter?.IsEnabled<ISoftDelete>() ?? false;

        private static readonly MethodInfo ConfigureBasePropertiesMethodInfo
           = typeof(MiniDbContext<TDbContext>)
               .GetMethod(
                   nameof(ConfigureBaseProperties),
                   BindingFlags.Instance | BindingFlags.NonPublic
               );

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

            modelBuilder.Entity<TEntity>().ConfigureByConvention();

            ConfigureGlobalFilters<TEntity>(modelBuilder, mutableEntityType);
        }
    }
}
