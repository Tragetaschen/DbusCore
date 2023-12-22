using Dbus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WebApplication.Dbus.Models;

namespace WebApplication.Dbus.Controllers;

public class HomeController : Controller
{
    public async Task<IActionResult> Index(
         [FromServices] Task<IOrgFreedesktopDbus> orgFreedesktopDbusTask,
         CancellationToken cancellationToken
    )
    {
        var orgFreedesktopDbus = await orgFreedesktopDbusTask;
        var names = await orgFreedesktopDbus.ListNamesAsync(cancellationToken);
        return View(names);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
