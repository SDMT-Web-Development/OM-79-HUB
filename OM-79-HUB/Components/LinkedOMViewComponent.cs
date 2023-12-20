using Microsoft.AspNetCore.Mvc;
using OM79.Models.DB; // Update with the correct namespace
using System.Linq;

namespace OM_79_HUB.Components
{
    public class LinkedOMViewComponent : ViewComponent
    {
        private readonly OM79Context _context;

        public LinkedOMViewComponent(OM79Context context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke(int hubId)
        {
            var entries = _context.OMTable.Where(entry => entry.HubId == hubId).ToList();
            return View("~/Views/OM79/_LinkedOM.cshtml", entries); // Specify the correct view path here
        }

    }
}