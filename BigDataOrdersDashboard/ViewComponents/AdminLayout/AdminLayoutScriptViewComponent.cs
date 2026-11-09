using Microsoft.AspNetCore.Mvc;

namespace BigDataOrdersDashboard.ViewComponents.Layout
{
    public class AdminLayoutScriptViewComponent:ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
