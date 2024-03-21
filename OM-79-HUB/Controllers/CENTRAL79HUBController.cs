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
    }
}
