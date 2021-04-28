using Microsoft.EntityFrameworkCore;
using MiniDemo.Data;
using MiniDemo.EntityFrameworkCore;
using MiniDemo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniDemo.AppMigrations
{
    public class TestDbContext : MiniDbContext<TestDbContext>
    {
        public TestDbContext(DbContextOptions<TestDbContext> options,IServiceProvider serviceProvider)
           : base(options,serviceProvider)
        {
        }

        public DbSet<Test> Tests { get; set; }


    }
}
