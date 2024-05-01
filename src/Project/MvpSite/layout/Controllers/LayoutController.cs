using Microsoft.AspNetCore.Mvc;

namespace Mvp.Project.MvpSite.Controllers;

public class LayoutController : Controller
{
    public IActionResult Index()
    {
        return Content("testing");
    }
}