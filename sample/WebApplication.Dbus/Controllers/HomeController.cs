using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication.Dbus.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View(new[] {
                ":1.1",
                ":1.2",
                "org.freedesktop.Dbus",
            });
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
