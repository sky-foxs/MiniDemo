using Microsoft.EntityFrameworkCore;
using MiniDemo.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniDemo.Helpers
{
    public static class ModelBuilderExtensions
    {

        private const string ModelDatabaseProviderAnnotationKey = "_DatabaseProvider";
        public static void SetDatabaseProvider(
            this ModelBuilder modelBuilder,
            EfCoreDatabaseProvider databaseProvider)
        {
            modelBuilder.Model.SetAnnotation(ModelDatabaseProviderAnnotationKey, databaseProvider);
        }
    }
}
