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
using OM79.Models.DB;
using X.PagedList;

namespace OM_79_HUB.Controllers
{
    public class CENTRAL79HUBController : Controller
    {
        private readonly OM_79_HUBContext _context;
        private readonly OM79Context _OMcontext;

        public CENTRAL79HUBController(OM_79_HUBContext context, OM79Context oM79Context)
        {
            _context = context;
            _OMcontext = oM79Context;
        }



        //
        //
        //
        //
        //
        // This is where we will start the email/signing system
        //
        //
        //
        //
        [HttpPost]
        public async Task<IActionResult> FinishSubmit(int id)
        {
            // Retrieve the CENTRAL79HUB entry
            var om79 = await _context.CENTRAL79HUB.FindAsync(id);
            if (om79 == null)
            {
                return NotFound();
            }

            // Set IsSubmitted to true
            om79.IsSubmitted = true;

            // Retrieve the OM79Workflow entry using HubID
            var om79Workflow = await _context.OM79Workflow.FirstOrDefaultAsync(w => w.HubID == id);
            if (om79Workflow != null)
            {
                // Set the next step to "Submitted"
                om79Workflow.NextStep = "Submitted";
            }

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Redirect to an appropriate action, such as the index page
            return RedirectToAction("Details", new { id = om79.OMId });
        }


        [HttpPost]
        public async Task<IActionResult> ArchiveOM79(int id)
        {
            var om79 = await _context.CENTRAL79HUB.FindAsync(id);
            if (om79 == null)
            {
                return NotFound();
            }
            // Archive the CENTRAL79HUB entry
            om79.IsArchive = true;

            // Find and archive related OMTable entries
            var relatedOMTables = await _OMcontext.OMTable.Where(t => t.HubId == id).ToListAsync();
            foreach (var item in relatedOMTables)
            {
                item.IsArchive = true;
            }

            // Save changes to both contexts
            await _context.SaveChangesAsync();
            await _OMcontext.SaveChangesAsync();

            return RedirectToAction(nameof(Index)); // Redirect to an appropriate action
        }

        // GET: CENTRAL79HUB/ArchivedIndex
        public async Task<IActionResult> ArchivedIndex(string searchUserId, int? page)
        {
            ViewData["CurrentFilter"] = searchUserId;

            // Filter the records where IsArchive is true
            var central79HubEntries = from m in _context.CENTRAL79HUB
                                      where m.IsArchive == true
                                      select m;

            if (!String.IsNullOrEmpty(searchUserId))
            {
                central79HubEntries = central79HubEntries.Where(s => s.UserId.Contains(searchUserId));
            }

            int pageNumber = (page ?? 1);
            var pagedCentral79HubEntries = await central79HubEntries.ToPagedListAsync(pageNumber, 50);

            return View(pagedCentral79HubEntries);
        }





        // GET: CENTRAL79HUB
        public async Task<IActionResult> Index(string searchUserId, int? page)
        {
            ViewData["CurrentFilter"] = searchUserId;

            // Filter the records where IsArchive is not true
            var central79HubEntries = from m in _context.CENTRAL79HUB
                                      where m.IsArchive == false || m.IsArchive == null
                                      select m;

            if (!String.IsNullOrEmpty(searchUserId))
            {
                central79HubEntries = central79HubEntries.Where(s => s.UserId.Contains(searchUserId));
            }

            int pageNumber = (page ?? 1);
            var pagedCentral79HubEntries = await central79HubEntries.ToPagedListAsync(pageNumber, 50);

            return View(pagedCentral79HubEntries);
        }



        // GET: CENTRAL79HUB
        //public async Task<IActionResult> Index()
        //{
        //    if (_context.CENTRAL79HUB == null)
        //    {
        //        return Problem("Entity set 'OM_79_HUBContext.CENTRAL79HUB' is null.");
        //    }

        //    var items = await _context.CENTRAL79HUB
        //                              .Where(x => x.IsArchive == false || x.IsArchive == null)
        //                              .ToListAsync();

        //    return View(items);
        //}

        public async Task<IActionResult> AdminIndex()
        {
            return _context.CENTRAL79HUB != null ?
                        View(await _context.UserData.ToListAsync()) :
                        Problem("Entity set 'OM_79_HUBContext.UserData'  is null.");
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


            HttpContext.Session.SetInt32("UniqueID", id.Value);

            ViewBag.TestUniqueID = id;

            return View(cENTRAL79HUB);
        }

        // GET: CENTRAL79HUB/Create
        public IActionResult Create()
        {

            Dropdowns();
            return View();
            
        }

        // GET: CENTRAL79HUB/Admin
        public IActionResult AdminCreate()
        {
            Dropdowns();
            return View();

        }
        // POST: CENTRAL79HUB/Admin
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminCreate([Bind("ENumber,UserKey, FirstName, LastName, CRU, CRA, HDS, LRS, GISManager, Chief, DistrictReview, District, Email, BridgeEngineer, TrafficEngineer, MaintenanceEngineer, ConstructionEngineer, RightOfWayManager, DistrictManager")] UserData userdata)
        {
            if (ModelState.IsValid)
            {
                _context.Add(userdata);
                await _context.SaveChangesAsync();
               
                return RedirectToAction("Index");
               
            }
            return View(userdata);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OMId,UserId,Otherbox,County,District,IDNumber,RouteID")] CENTRAL79HUB cENTRAL79HUB, int NumberOfItems)
        {
            string userIdentity = User.Identity.Name;
            cENTRAL79HUB.UserId = userIdentity;

            if (ModelState.IsValid)
            {
                cENTRAL79HUB.IsSubmitted = false;
                cENTRAL79HUB.DateSubmitted = DateTime.Now;
                cENTRAL79HUB.IsArchive = false;
                _context.Add(cENTRAL79HUB);
                await _context.SaveChangesAsync();

                int uniqueID = cENTRAL79HUB.OMId;
                string countyValue = cENTRAL79HUB.County;


                HttpContext.Session.SetInt32("UniqueID", uniqueID);

                // Create a new OM79Workflow entry with the provided number of items and the newly created key from CENTRAL79HUB
                OM79Workflow om79Workflow = new OM79Workflow
                {
                    HubID = uniqueID,
                    NumberOfItems = NumberOfItems,
                    NextStep = "AddFirstItem"
                };
                _context.OM79Workflow.Add(om79Workflow);
                await _context.SaveChangesAsync();

                // Redirect to the Create action of OM79 controller with uniqueID and countyValue
                //return RedirectToAction("Create", "OM79", new { uniqueID = uniqueID, county = countyValue });
                return RedirectToAction("Details", "CENTRAL79HUB", new { id = uniqueID }); //ChangeLater

            }

            return View(cENTRAL79HUB);
        }

        //// POST: CENTRAL79HUB/Create
        //// To protect from overposting attacks, enable the specific properties you want to bind to.
        //// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create([Bind("OMId,UserId,Otherbox,County,District")] CENTRAL79HUB cENTRAL79HUB, int NumberOfItems)
        //{
        //    string userIdentity = User.Identity.Name;
        //    cENTRAL79HUB.UserId = userIdentity;

        //    if (ModelState.IsValid)
        //    {
        //        cENTRAL79HUB.DateSubmitted = DateTime.Now;
        //        _context.Add(cENTRAL79HUB);
        //        await _context.SaveChangesAsync();

        //        int uniqueID = cENTRAL79HUB.OMId;
        //        string countyValue = cENTRAL79HUB.County;

        //        // Redirect to the Create action of OM79 controller with uniqueID and countyValue
        //        return RedirectToAction("Create", "OM79", new { uniqueID = uniqueID, county = countyValue });
        //    }

        //    return View(cENTRAL79HUB);
        //}

        // GET: CENTRAL79HUB/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            Dropdowns();
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
        public async Task<IActionResult> Edit(int id, [Bind("OMId,UserId,Otherbox,County,District")] CENTRAL79HUB cENTRAL79HUB)
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
                signature.DateSubmitted = DateTime.Now;

                
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
                signature.DateSubmitted = DateTime.Now;



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
