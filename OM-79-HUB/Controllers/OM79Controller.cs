using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OM_79_HUB.Models;
using OM79.Models.DB;


namespace OM_79_HUB.Data
{
    public class OM79Controller : Controller
    {
        private readonly OM79Context _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public OM79Controller(OM79Context context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: OMTables
        public async Task<IActionResult> Index()
        {
            return _context.OMTable != null ?
                        View(await _context.OMTable.ToListAsync()) :
                        Problem("Entity set 'OM79Context.OMTable'  is null.");
        }

        // GET: OMTables/Details/5
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

        // GET: OMTables/Create
        public IActionResult Create([FromQuery] int uniqueID, OMTable oMTable)
        {
            DropDowns();
            Console.WriteLine(uniqueID);
            TempData["UniqueID"] = uniqueID; // Store uniqueID in TempData


            ViewBag.TestUniqueID = uniqueID;
            oMTable.HubId = uniqueID;

            return View(oMTable);
        }

        // POST: OMTables/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OMTable oMTable, List<IFormFile> attachments)
        {

            Console.WriteLine("------------------------------------------------------------------------------------------");
            Console.WriteLine("------------------------------------------------------------------------------------------");

            Console.WriteLine(oMTable.HubId);

            Console.WriteLine("------------------------------------------------------------------------------------------");
            Console.WriteLine("------------------------------------------------------------------------------------------");

            if (ModelState.IsValid)
            {
                _context.Add(oMTable);

                await _context.SaveChangesAsync();
                // int wowee = Int32.Parse(uniqueID);

                //    oMTable.HubId = wowee;
                oMTable.SubmissionDate = DateTime.Now;
                int unique79ID = oMTable.Id;

                //Console.WriteLine("UniqueID " + $"Query Parameter: {HttpContext.Request.Query["uniqueID"]}");
                /*   if (int.TryParse(HttpContext.Request.Query["uniqueID"], out int uniqueID))
                   {
                       // Use the uniqueID as needed
                       // For example, you can pass it to a view or use it in your logic
                       oMTable.HubId = uniqueID;
                       await _context.SaveChangesAsync();
                       // Your further logic...
                   }
                   else
                   {
                       // Handle the case when the uniqueID is not present or not a valid integer
                       return BadRequest("Invalid or missing uniqueID");
                   }
                   */
                if (TempData["UniqueID"] is int uniqueID)
                {
                    oMTable.HubId = uniqueID; // Set the uniqueID to your model's property
                    await _context.SaveChangesAsync();
                }


                // Save the attachments
                foreach (var attachmentFile in attachments)
                {
                    if (attachmentFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "OMAttachments");
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + attachmentFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await attachmentFile.CopyToAsync(fileStream);
                        }

                        var attachment = new Attachments
                        {
                            FileName = attachmentFile.FileName,
                            FilePath = filePath,
                            SubmissionID = oMTable.Id // Use the SubmissionID from the saved submission
                        };
                        _context.Attachments.Add(attachment);
                    }
                }

                // Save all changes to the database
                await _context.SaveChangesAsync();
                return RedirectToAction("Create", "PJ103", new { uniqueID = unique79ID });
                //return Redirect($"https://dotappstest.transportation.wv.gov/PJ-103/Submissions/Create?uniqueID={unique79ID}");
            }
            return View(oMTable);
        }








        /**
        // GET: OMTables/Create
        public IActionResult Create()
        {
            DropDowns();
            return View();
        }

        // POST: OMTables/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OMTable oMTable, List<IFormFile> attachments)
        {
            if (ModelState.IsValid)
            {
                _context.Add(oMTable);
                await _context.SaveChangesAsync();
                oMTable.SubmissionDate= DateTime.Now;
                

                // Save the attachments
                foreach (var attachmentFile in attachments)
                {
                    if (attachmentFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "OMAttachments");
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + attachmentFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await attachmentFile.CopyToAsync(fileStream);
                        }

                        var attachment = new Attachments
                        {
                            FileName = attachmentFile.FileName,
                            FilePath = filePath,
                            SubmissionID = oMTable.Id // Use the SubmissionID from the saved submission
                        };
                        _context.Attachments.Add(attachment);
                    }
                }

                // Save all changes to the database
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(oMTable);
        }

        // GET: OMTables/Edit/5
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
        */


        // POST: OMTables/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DistrictNumber,County,SubmissionDate,Routing,RoadChangeType,Otherbox,RouteAssignment,RightOfWayWidth,Railroad,DOTAARNumber,RequestedBy,Comments,AdjacentProperty,APHouses,APBusinesses,APSchools,APOther,APOtherIdentify,Attachments,DESignature,Preparer")] OMTable oMTable)
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

        // GET: OMTables/Delete/5
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

        // POST: OMTables/Delete/5
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
        public void DropDowns()
        {
            List<SelectListItem> RoutingDropdown = new()
        {

            new SelectListItem { Text = "HO", Value = "HO" },
            new SelectListItem { Text = "TI", Value = "TI" },
            new SelectListItem { Text = "OM", Value = "OM" }
        };
            RoutingDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.RoutingDropdown = RoutingDropdown;

            List<SelectListItem> RCTDropdown = new()
        {
            new SelectListItem { Text = "Addition (PJ-103 Required)", Value = "Addition" },
            new SelectListItem { Text = "Redesignation (PJ-103 Required)", Value = "Redesignation" },
            new SelectListItem { Text = "Map Correction", Value = "Map Correction" },
            new SelectListItem { Text = "Abandonment", Value = "Abandonment" },
            new SelectListItem { Text = "Inventory Removal", Value = "Inventory Removal" },
            new SelectListItem { Text = "Amend", Value = "Amend" },
            new SelectListItem { Text = "Rescind", Value = "Rescind" },
            new SelectListItem {Text = "Other", Value ="Other"}
        };
            RCTDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.RCTDropdown = RCTDropdown;

            List<SelectListItem> RADropdown = new()
        {
            new SelectListItem { Text = "New", Value = "New" },
            new SelectListItem { Text = "Current", Value = "Current" },
            new SelectListItem { Text = "Not Available", Value = "N/A"}

        };
            RADropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.RADropdown = RADropdown;

            List<SelectListItem> ROWDropdown = new()
        {
            new SelectListItem { Text = "20'", Value = "20" },
            new SelectListItem { Text = "30'", Value = "30" },
            new SelectListItem { Text = "40'", Value = "40" },
            new SelectListItem { Text = "50'", Value = "50" },
            new SelectListItem { Text = "60'", Value = "60" },
            new SelectListItem { Text = "Traveled Way", Value = "Traveled Way" },
            new SelectListItem { Text = "Other", Value = "Other" },
            new SelectListItem { Text = "Not Available", Value = "N/A" }

        };
            ROWDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.ROWDropdown = ROWDropdown;

            List<SelectListItem> YNDropdown = new()
        {
            new SelectListItem { Text = "Yes", Value = "Yes" },
            new SelectListItem { Text = "No", Value = "No" }

        };
            YNDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.YNDropdown = YNDropdown;
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

            List<SelectListItem> RequestDropdown = new()
        {
            new SelectListItem { Text = "Citizens", Value = "Citizens" },
            new SelectListItem { Text = "Municipality", Value = "Municipality" },
            new SelectListItem { Text = "District", Value = "District" },
            new SelectListItem { Text = "Developer", Value = "Developer" },
            new SelectListItem { Text = "HOA", Value = "HOA" },
            new SelectListItem {Text = "Other", Value ="Other"},
        };
            RequestDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.RequestDropdown = RequestDropdown;
        }




        public IActionResult LinkedOM(int hubId)
        {
            Console.WriteLine(hubId);
            var entries = _context.OMTable.Where(entry => entry.HubId == hubId);
            // var entries = IEnumerable.GetEntriesByHubId(hubId);
            // Redirect to the Index action of the Home controller
            return PartialView("_LinkedOM", entries);
        }
        
        public IEnumerable<OMTable> GetEntriesByUserId(int userid)
        {
            return _context.OMTable.Where(entry => entry.HubId == userid).ToList();
        }
    }


}
