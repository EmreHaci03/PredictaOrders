using Microsoft.AspNetCore.Mvc;

namespace BigDataOrdersDashboard.ViewComponents.Layout
{
    public class AdminLayoutRightSideBarViewComponent:ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
