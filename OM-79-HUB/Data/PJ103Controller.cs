using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PJ103V3.Models.DB;

namespace OM_79_HUB.Data
{
    public class PJ103Controller : Controller
    {
        private readonly Pj103Context _context;

        public PJ103Controller(Pj103Context context)
        {
            _context = context;
        }

        // GET: PJ103
        public async Task<IActionResult> Index()
        {
              return _context.Submissions != null ? 
                          View(await _context.Submissions.ToListAsync()) :
                          Problem("Entity set 'Pj103Context.Submissions'  is null.");
        }

        // GET: PJ103/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Submissions == null)
            {
                return NotFound();
            }

            var submission = await _context.Submissions
                .FirstOrDefaultAsync(m => m.SubmissionID == id);
            if (submission == null)
            {
                return NotFound();
            }

            return View(submission);
        }

        // GET: PJ103/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: PJ103/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SubmissionID,ProjectKey,ReportDate,County,RouteNumber,SubRouteNumber,ProjectNumber,DateComplete,NatureOfChange,MaintOrg,YearOfSurvey,UserID,OtherBox,SignSystem,StartingMilePoint,EndingMilePoint,RailroadInv,BridgeInv")] Submission submission)
        {
            if (ModelState.IsValid)
            {
                _context.Add(submission);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(submission);
        }

        // GET: PJ103/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Submissions == null)
            {
                return NotFound();
            }

            var submission = await _context.Submissions.FindAsync(id);
            if (submission == null)
            {
                return NotFound();
            }
            return View(submission);
        }

        // POST: PJ103/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("SubmissionID,ProjectKey,ReportDate,County,RouteNumber,SubRouteNumber,ProjectNumber,DateComplete,NatureOfChange,MaintOrg,YearOfSurvey,UserID,OtherBox,SignSystem,StartingMilePoint,EndingMilePoint,RailroadInv,BridgeInv")] Submission submission)
        {
            if (id != submission.SubmissionID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(submission);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubmissionExists(submission.SubmissionID))
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
            return View(submission);
        }

        // GET: PJ103/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Submissions == null)
            {
                return NotFound();
            }

            var submission = await _context.Submissions
                .FirstOrDefaultAsync(m => m.SubmissionID == id);
            if (submission == null)
            {
                return NotFound();
            }

            return View(submission);
        }

        // POST: PJ103/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Submissions == null)
            {
                return Problem("Entity set 'Pj103Context.Submissions'  is null.");
            }
            var submission = await _context.Submissions.FindAsync(id);
            if (submission != null)
            {
                _context.Submissions.Remove(submission);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SubmissionExists(int id)
        {
          return (_context.Submissions?.Any(e => e.SubmissionID == id)).GetValueOrDefault();
        }
    }
}
