using Microsoft.AspNetCore.Mvc;
using OM_79_HUB.Models;
using OM79.Models.DB; // Update with the correct namespace
using System.Linq;

namespace OM_79_HUB.Components
{
    public class LinkedEditOMViewComponent : ViewComponent
    {
        private readonly OM79Context _context;

        public LinkedEditOMViewComponent(OM79Context context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke(int hubId)
        {
            // Fetch OMTable entries for the given HubId
            var entries = _context.OMTable.Where(entry => entry.HubId == hubId).ToList();

            // Create a view model that includes both entries and the hubId
            var viewModel = new LinkedEditOMViewModel
            {
                HubId = hubId,
                OMTableEntries = entries
            };

            // Return the view with the view model
            return View("~/Views/OM79/_LinkedOMEdit.cshtml", viewModel); // Specify the correct view path here
        }
    }

    // ViewModel to pass both OMTable entries and HubId
    public class LinkedEditOMViewModel
    {
        public int HubId { get; set; }
        public List<OMTable> OMTableEntries { get; set; } = new List<OMTable>(); // Empty list if no entries found
    }
}
