using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniDemo.AppMigrations;
using MiniDemo.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MiniDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IServiceProvider _serviceProvider;

        public HomeController(ILogger<HomeController> logger,IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task Test()
        {
            var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
            optionsBuilder.UseSqlServer("Server=DESKTOP-HIO90M8\\SQLEXPRESS;Database=MiniDemo;Trusted_Connection=True;MultipleActiveResultSets=true;");
            using (var test=new TestDbContext(optionsBuilder.Options,_serviceProvider))
            {
                var t = new Models.Test()
                {
                    Id = Guid.NewGuid(),
                    Content = "666"
                };
                test.Tests.Add(t);

                await test.SaveChangesAsync();

                test.Tests.Remove(t);

                await test.SaveChangesAsync();
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
