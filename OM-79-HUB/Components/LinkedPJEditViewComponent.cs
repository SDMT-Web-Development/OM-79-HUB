using Microsoft.AspNetCore.Mvc;
using OM_79_HUB.Data;

namespace OM_79_HUB.Components
{
    public class LinkedPJEditViewComponent : ViewComponent
    {
        private readonly Pj103Context _context;

        public LinkedPJEditViewComponent(Pj103Context context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke(int PJhubID)
        {
            var entries = _context.Submissions.Where(entry => entry.OM79Id == PJhubID).ToList();
            return View("~/Views/PJ103/_LinkedPJEdit.cshtml", entries);
        }
    }
}