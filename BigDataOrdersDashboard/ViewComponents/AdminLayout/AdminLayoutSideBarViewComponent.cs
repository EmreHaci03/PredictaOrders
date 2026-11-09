using Microsoft.AspNetCore.Mvc;

namespace BigDataOrdersDashboard.ViewComponents.Layout
{
    public class AdminLayoutSideBarViewComponent:ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
