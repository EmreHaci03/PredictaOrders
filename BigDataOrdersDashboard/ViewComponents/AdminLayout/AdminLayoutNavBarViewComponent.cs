using Microsoft.AspNetCore.Mvc;

namespace BigDataOrdersDashboard.ViewComponents.Layout
{
    public class AdminLayoutNavBarViewComponent:ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();  
        }
    }
}
