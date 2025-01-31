using Microsoft.AspNetCore.Mvc;
using OM_79_HUB.Data;
using OM79.Models.DB;

namespace OM_79_HUB.Components
{
    [ViewComponent(Name = "LinkedSignatures")]
    public class LinkedSignatureViewComponent : ViewComponent
    {
        private readonly OM_79_HUBContext _context;

        public LinkedSignatureViewComponent(OM_79_HUBContext ctx) => _context = ctx;

        public IViewComponentResult Invoke(int hubID)
        {
            var entries = _context.SignatureData
                .Where(entry => entry.HubKey == hubID && entry.IsCurrentSig == true)
                .GroupBy(s => s.SigType) // Group by role
                .Select(g => g.OrderByDescending(s => s.DateSubmitted).FirstOrDefault()) // Get the most recent signature per role
                .ToList();
            return View("_linkedSignatures.cshtml",entries);
        }
    }
}
