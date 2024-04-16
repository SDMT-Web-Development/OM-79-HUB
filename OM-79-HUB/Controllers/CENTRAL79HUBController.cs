using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OM_79_HUB.Data;
using OM_79_HUB.Models;
using OM_79_HUB.Models.DB.OM79Hub;

namespace OM_79_HUB.Controllers
{
    public class CENTRAL79HUBController : Controller
    {
        private readonly OM_79_HUBContext _context;

        public CENTRAL79HUBController(OM_79_HUBContext context)
        {
            _context = context;
        }

        // GET: CENTRAL79HUB
        public async Task<IActionResult> Index()
        {
              return _context.CENTRAL79HUB != null ? 
                          View(await _context.CENTRAL79HUB.ToListAsync()) :
                          Problem("Entity set 'OM_79_HUBContext.CENTRAL79HUB'  is null.");
        }

        // GET: CENTRAL79HUB/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.CENTRAL79HUB == null)
            {
                return NotFound();
            }

            var cENTRAL79HUB = await _context.CENTRAL79HUB
                .FirstOrDefaultAsync(m => m.OMId == id);
            if (cENTRAL79HUB == null)
            {
                return NotFound();
            }
            
            ViewBag.TestUniqueID = id;

            return View(cENTRAL79HUB);
        }

        // GET: CENTRAL79HUB/Create
        public IActionResult Create()
        {
            Dropdowns();
            return View();
            
        }

        // POST: CENTRAL79HUB/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OMId,UserId,Otherbox")] CENTRAL79HUB cENTRAL79HUB)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cENTRAL79HUB);
                await _context.SaveChangesAsync();
                int uniqueID = cENTRAL79HUB.OMId;

                return RedirectToAction("Create", "OM79", new { uniqueID = uniqueID });
                // return Redirect ($"https://dotappstest.transportation.wv.gov/OM79/OMTables/Create?uniqueID={uniqueID}");
                // return Redirect($"https://dotappstest.transportation.wv.gov/OM79?uniqueID={uniqueID}");
            }
            return View(cENTRAL79HUB);
        }

        // GET: CENTRAL79HUB/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.CENTRAL79HUB == null)
            {
                return NotFound();
            }

            var cENTRAL79HUB = await _context.CENTRAL79HUB.FindAsync(id);
            if (cENTRAL79HUB == null)
            {
                return NotFound();
            }
            return View(cENTRAL79HUB);
        }

        // POST: CENTRAL79HUB/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OMId,USERID,CBOX")] CENTRAL79HUB cENTRAL79HUB)
        {
            if (id != cENTRAL79HUB.OMId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cENTRAL79HUB);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CENTRAL79HUBExists(cENTRAL79HUB.OMId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(cENTRAL79HUB);
        }

        // GET: CENTRAL79HUB/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.CENTRAL79HUB == null)
            {
                return NotFound();
            }

            var cENTRAL79HUB = await _context.CENTRAL79HUB
                .FirstOrDefaultAsync(m => m.OMId == id);
            if (cENTRAL79HUB == null)
            {
                return NotFound();
            }

            return View(cENTRAL79HUB);
        }

        // POST: CENTRAL79HUB/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.CENTRAL79HUB == null)
            {
                return Problem("Entity set 'OM_79_HUBContext.CENTRAL79HUB'  is null.");
            }
            var cENTRAL79HUB = await _context.CENTRAL79HUB.FindAsync(id);
            if (cENTRAL79HUB != null)
            {
                _context.CENTRAL79HUB.Remove(cENTRAL79HUB);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CENTRAL79HUBExists(int id)
        {
          return (_context.CENTRAL79HUB?.Any(e => e.OMId == id)).GetValueOrDefault();
        }

        public  async Task<IActionResult> SignOMHub()
        {
            var app = Request.Form["apradio"];
            var den = Request.Form["denradio"];
            if(app.FirstOrDefault() == "approve")
            {
                var signature = new SignatureData();
                signature.HubKey = int.Parse(Request.Form["HubKey"]);
                signature.IsApprove = true;
                signature.IsDenied = false;
                signature.Comments = Request.Form["commentsmodal"];
                signature.Signatures = Request.Form["signaturemodal"];
                signature.SigType = Request.Form["sigtype"];
                signature.ENumber = HttpContext.User.Identity.Name;


                
                _context.Add(signature);
                await _context.SaveChangesAsync();
            }
            if (den.FirstOrDefault() == "deny")
            {
                var signature = new SignatureData();
                signature.HubKey = int.Parse(Request.Form["HubKey"]);
                signature.IsApprove = false;
                signature.IsDenied = true;
                signature.Comments = Request.Form["commentsmodal"];
                signature.Signatures = Request.Form["signaturemodal"];
                signature.SigType = Request.Form["sigtype"];
                signature.ENumber = HttpContext.User.Identity.Name;



                _context.Add(signature);
                await _context.SaveChangesAsync();
            }
            var hubkey = int.Parse(Request.Form["HubKey"]);

            return RedirectToAction(nameof(Details), new { id = hubkey });
        }
        public void Dropdowns ()
        {
            List<SelectListItem> CountyDropdown = new()
        {
                new SelectListItem {Text = "Barbour", Value = "Barbour"},
                new SelectListItem {Text = "Berkeley", Value = "Berkeley"},
                new SelectListItem {Text = "Boone", Value = "Boone"},
                new SelectListItem {Text = "Braxton", Value = "Braxton"},
                new SelectListItem {Text = "Brooke", Value = "Brooke"},
                new SelectListItem {Text = "Cabell", Value = "Cabell"},
                new SelectListItem {Text = "Calhoun", Value = "Calhoun"},
                new SelectListItem {Text = "Clay", Value = "Clay"},
                new SelectListItem {Text = "Doddridge", Value = "Doddridge"},
                new SelectListItem {Text = "Fayette", Value = "Fayette"},
                new SelectListItem {Text = "Gilmer", Value = "Gilmer"},
                new SelectListItem {Text = "Grant", Value = "Grant"},
                new SelectListItem {Text = "Greenbrier", Value = "Greenbrier"},
                new SelectListItem {Text = "Hampshire", Value = "Hampshire"},
                new SelectListItem {Text = "Hancock", Value = "Hancock"},
                new SelectListItem {Text = "Hardy", Value = "Hardy"},
                new SelectListItem {Text = "Harrison", Value = "Harrison"},
                new SelectListItem {Text = "Jackson", Value = "Jackson"},
                new SelectListItem {Text = "Jefferson", Value = "Jefferson"},
                new SelectListItem {Text = "Kanawha", Value = "Kanawha"},
                new SelectListItem {Text = "Lewis", Value = "Lewis"},
                new SelectListItem {Text = "Lincoln", Value = "Lincoln"},
                new SelectListItem {Text = "Logan", Value = "Logan"},
                new SelectListItem {Text = "McDowell", Value = "McDowell"},
                new SelectListItem {Text = "Marion", Value = "Marion"},
                new SelectListItem {Text = "Marshall", Value = "Marshall"},
                new SelectListItem {Text = "Mason", Value = "Mason"},
                new SelectListItem {Text = "Mercer", Value = "Mercer"},
                new SelectListItem {Text = "Mineral", Value = "Mineral"},
                new SelectListItem {Text = "Mingo", Value = "Mingo"},
                new SelectListItem {Text = "Monongalia", Value = "Monongalia"},
                new SelectListItem {Text = "Monroe", Value = "Monroe"},
                new SelectListItem {Text = "Morgan", Value = "Morgan"},
                new SelectListItem {Text = "Nicholas", Value = "Nicholas"},
                new SelectListItem {Text = "Ohio", Value = "Ohio"},
                new SelectListItem {Text = "Pendleton", Value = "Pendleton"},
                new SelectListItem {Text = "Pleasants", Value = "Pleasants"},
                new SelectListItem {Text = "Pocahontas", Value = "Pocahontas"},
                new SelectListItem {Text = "Preston", Value = "Preston"},
                new SelectListItem {Text = "Putnam", Value = "Putnam"},
                new SelectListItem {Text = "Raleigh", Value = "Raleigh"},
                new SelectListItem {Text = "Randolph", Value = "Randolph"},
                new SelectListItem {Text = "Ritchie", Value = "Ritchie"},
                new SelectListItem {Text = "Roane", Value = "Roane"},
                new SelectListItem {Text = "Summers", Value = "Summers"},
                new SelectListItem {Text = "Taylor", Value = "Taylor"},
                new SelectListItem {Text = "Tucker", Value = "Tucker"},
                new SelectListItem {Text = "Tyler", Value = "Tyler"},
                new SelectListItem {Text = "Upshur", Value = "Upshur"},
                new SelectListItem {Text = "Wayne", Value = "Wayne"},
                new SelectListItem {Text = "Webster", Value = "Webster"},
                new SelectListItem {Text = "Wetzel", Value = "Wetzel"},
                new SelectListItem {Text = "Wirt", Value = "Wirt"},
                new SelectListItem {Text = "Wood", Value = "Wood"},
                new SelectListItem {Text = "Wyoming", Value = "Wyoming"}
        };
            CountyDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.CountyDropdown = CountyDropdown;


            List<SelectListItem> DDropdown = new()
        {
            new SelectListItem { Text = "1", Value = "1" },
            new SelectListItem { Text = "2", Value = "2" },
            new SelectListItem { Text = "3", Value = "3" },
            new SelectListItem { Text = "4", Value = "4" },
            new SelectListItem { Text = "5", Value = "5" },
            new SelectListItem { Text = "6", Value = "6" },
            new SelectListItem { Text = "7", Value = "7" },
            new SelectListItem { Text = "8", Value = "8" },
            new SelectListItem { Text = "9", Value = "9" },
            new SelectListItem { Text = "10", Value = "10" },

        };
            DDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.DDropdown = DDropdown;
        }
    }
}
