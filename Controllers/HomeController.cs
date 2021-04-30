using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniDemo.AppMigrations;
using MiniDemo.Data;
using MiniDemo.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MiniDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDataFilter _dataFilter;

        public HomeController(ILogger<HomeController> logger, IServiceProvider serviceProvider, IDataFilter dataFilter)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _dataFilter = dataFilter;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<JsonResult> Test()
        {
            var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
            optionsBuilder.UseSqlServer("Server=DESKTOP-HIO90M8\\SQLEXPRESS;Database=MiniDemo;Trusted_Connection=True;MultipleActiveResultSets=true;");
            using (var test = new TestDbContext(optionsBuilder.Options, _serviceProvider))
            {
                var t = new Test() { Id = Guid.NewGuid(), Content = DateTime.Now.ToString() };

                test.Tests.Add(t);

                await test.SaveChangesAsync();

                t.Content = DateTime.Now.ToString();
                test.Tests.Update(t);

                await test.SaveChangesAsync();



                using (_dataFilter.Disable<ISoftDelete>())
                {
                    return Json(await test.Tests.Where(m => m.Content != null).ToListAsync());
                }
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
