using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OM79.Models.DB;

namespace OM_79_HUB.Controllers
{
    public class OM79Controller : Controller
    {
        private readonly OM79Context _context;

        public OM79Controller(OM79Context context)
        {
            _context = context;
        }

        // GET: OM79
        public async Task<IActionResult> Index()
        {
              return _context.OMTable != null ? 
                          View(await _context.OMTable.ToListAsync()) :
                          Problem("Entity set 'OM79Context.OMTable'  is null.");
        }

        // GET: OM79/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.OMTable == null)
            {
                return NotFound();
            }

            var oMTable = await _context.OMTable
                .FirstOrDefaultAsync(m => m.Id == id);
            if (oMTable == null)
            {
                return NotFound();
            }

            return View(oMTable);
        }

        // GET: OM79/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: OM79/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DistrictNumber,County,SubmissionDate,Routing,RoadChangeType,Otherbox,RouteAssignment,RightOfWayWidth,Railroad,DOTAARNumber,RequestedBy,Comments,AdjacentProperty,APHouses,APBusinesses,APSchools,APOther,APOtherIdentify,Attachments,DESignature,Preparer,RequestedByName,Route,SubRoute,CoDate,CoDateTwo,RAddition,RRedesignation,RMapCorrection,RAbandonment,RInventoryRemoval,RAmend,RRescind,ROther,RightOther,HubId")] OMTable oMTable)
        {
            if (ModelState.IsValid)
            {
                _context.Add(oMTable);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(oMTable);
        }

        // GET: OM79/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.OMTable == null)
            {
                return NotFound();
            }

            var oMTable = await _context.OMTable.FindAsync(id);
            if (oMTable == null)
            {
                return NotFound();
            }
            return View(oMTable);
        }

        // POST: OM79/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DistrictNumber,County,SubmissionDate,Routing,RoadChangeType,Otherbox,RouteAssignment,RightOfWayWidth,Railroad,DOTAARNumber,RequestedBy,Comments,AdjacentProperty,APHouses,APBusinesses,APSchools,APOther,APOtherIdentify,Attachments,DESignature,Preparer,RequestedByName,Route,SubRoute,CoDate,CoDateTwo,RAddition,RRedesignation,RMapCorrection,RAbandonment,RInventoryRemoval,RAmend,RRescind,ROther,RightOther,HubId")] OMTable oMTable)
        {
            if (id != oMTable.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(oMTable);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OMTableExists(oMTable.Id))
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
            return View(oMTable);
        }

        // GET: OM79/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.OMTable == null)
            {
                return NotFound();
            }

            var oMTable = await _context.OMTable
                .FirstOrDefaultAsync(m => m.Id == id);
            if (oMTable == null)
            {
                return NotFound();
            }

            return View(oMTable);
        }

        // POST: OM79/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.OMTable == null)
            {
                return Problem("Entity set 'OM79Context.OMTable'  is null.");
            }
            var oMTable = await _context.OMTable.FindAsync(id);
            if (oMTable != null)
            {
                _context.OMTable.Remove(oMTable);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OMTableExists(int id)
        {
          return (_context.OMTable?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
