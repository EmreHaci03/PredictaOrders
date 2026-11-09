using Microsoft.AspNetCore.Mvc;

namespace BigDataOrdersDashboard.ViewComponents.Layout
{
    public class AdminLayoutHeadViewComponent:ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
