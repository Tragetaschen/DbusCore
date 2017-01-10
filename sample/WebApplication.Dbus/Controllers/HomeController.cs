using Dbus;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace WebApplication.Dbus.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index([FromServices] Task<IOrgFreedesktopDbus> orgFreedesktopDbusTask)
        {
            using (var orgFreedesktopDbus = await orgFreedesktopDbusTask)
            {
                var names = await orgFreedesktopDbus.ListNamesAsync();
                return View(names);
            }
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
