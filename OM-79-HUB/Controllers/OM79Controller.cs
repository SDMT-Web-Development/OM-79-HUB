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
           // oMTable.HubId = uniqueID;

            return View(oMTable);
        }

        // POST: OMTables/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id, DistrictNumber, County, SubmissionDate, Routing, RoadChangeType, Otherbox, RouteAssignment, RightOfWayWidth, Railroad, DOTAARNumber, RequestedBy, Comments, AdjacentProperty, APHouses, APBusinesses, APSchools, APOther, APOtherIdentify, Attachments, DESignature, Preparer, RequestedByName, Route, SubRoute, CoDate, CoDateTwo, RAddition, RRedesignation, RMapCorrection, RAbandonment, RInventoryRemoval, RAmend, RRescind, ROther, RightOther, HubId, SignSystem, ProjectNumber, RouteNumber, SubRouteNumber, DateComplete, StartingMilePoint, EndingMilePoint, MaintOrg, YearOfSurvey, BridgeInv, RailroadInv, RailroadAmount, BridgeAmount, BridgeNumbers")] OMTable oMTable, List<IFormFile> attachments)
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
               /* foreach (var attachmentFile in attachments)
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
                }*/

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
            List<SelectListItem> TFDropdown = new()
        {
            new SelectListItem { Text = "Yes", Value = "True" },
            new SelectListItem { Text = "No", Value = "False" }

        };
            TFDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.TFDropdown = TFDropdown;
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

            List<SelectListItem> SignDropdown = new()
            {
                new SelectListItem {Text = "Interstate", Value = "Interstate"},
                new SelectListItem {Text = "US", Value = "US"},
                new SelectListItem {Text = "WV", Value = "WV"},
                new SelectListItem {Text = "CO", Value = "CO"},
                new SelectListItem {Text = "State Park and Forest Road", Value = "State Park and Forest Road"},
                new SelectListItem {Text = "FANS", Value = "FANS"},
                new SelectListItem {Text = "HARP", Value = "HARP"},


            };
            SignDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.SignDropdown = SignDropdown;

            List<SelectListItem> DOHDropdown = new()
            {
                new SelectListItem {Text = "0103", Value = "0103"},
                new SelectListItem {Text = "0108", Value = "0108"},
                new SelectListItem {Text = "0120", Value = "0120"},
                new SelectListItem {Text = "0121", Value = "0121"},
                new SelectListItem {Text = "0122", Value = "0122"},
                new SelectListItem {Text = "0123", Value = "0123"},
                new SelectListItem {Text = "0124", Value = "0124"},
                new SelectListItem {Text = "0127", Value = "0127"},
                new SelectListItem {Text = "0140", Value = "0140"},
                new SelectListItem {Text = "0164", Value = "0164"},
                new SelectListItem {Text = "0167", Value = "0167"},
                new SelectListItem {Text = "0171", Value = "0171"},
                new SelectListItem {Text = "0172", Value = "0172"},
                new SelectListItem {Text = "0173", Value = "0173"},
                new SelectListItem {Text = "0174", Value = "0174"},
                new SelectListItem {Text = "0182", Value = "0182"},
                new SelectListItem {Text = "0206", Value = "0206"},
                new SelectListItem {Text = "0222", Value = "0222"},
                new SelectListItem {Text = "0223", Value = "0223"},
                new SelectListItem {Text = "0230", Value = "0230"},
                new SelectListItem {Text = "0250", Value = "0250"},
                new SelectListItem {Text = "0264", Value = "0264"},
                new SelectListItem {Text = "0271", Value = "0271"},
                new SelectListItem {Text = "0281", Value = "0281"},
                new SelectListItem {Text = "0282", Value = "0282"},
                new SelectListItem {Text = "0307", Value = "0307"},
                new SelectListItem {Text = "0318", Value = "0318"},
                new SelectListItem {Text = "0337", Value = "0337"},
                new SelectListItem {Text = "0343", Value = "0343"},
                new SelectListItem {Text = "0344", Value = "0344"},
                new SelectListItem {Text = "0353", Value = "0353"},
                new SelectListItem {Text = "0354", Value = "0354"},
                new SelectListItem {Text = "0371", Value = "0371"},
                new SelectListItem {Text = "0372", Value = "0372"},
                new SelectListItem {Text = "0382", Value = "0382"},
                new SelectListItem {Text = "0383", Value = "0383"},
                new SelectListItem {Text = "0409", Value = "0409"},
                new SelectListItem {Text = "0417", Value = "0417"},
                new SelectListItem {Text = "0425", Value = "0425"},
                new SelectListItem {Text = "0431", Value = "0431"},
                new SelectListItem {Text = "0439", Value = "0439"},
                new SelectListItem {Text = "0446", Value = "0446"},
                new SelectListItem {Text = "0471", Value = "0471"},
                new SelectListItem {Text = "0472", Value = "0472"},
                new SelectListItem {Text = "0473", Value = "0473"},
                new SelectListItem {Text = "0482", Value = "0482"},
                new SelectListItem {Text = "0502", Value = "0502"},
                new SelectListItem {Text = "0512", Value = "0512"},
                new SelectListItem {Text = "0514", Value = "0514"},
                new SelectListItem {Text = "0516", Value = "0516"},
                new SelectListItem {Text = "0519", Value = "0519"},
                new SelectListItem {Text = "0529", Value = "0529"},
                new SelectListItem {Text = "0533", Value = "0533"},
                new SelectListItem {Text = "0564", Value = "0564"},
                new SelectListItem {Text = "0571", Value = "0571"},
                new SelectListItem {Text = "0582", Value = "0582"},
                new SelectListItem {Text = "0605", Value = "0605"},
                new SelectListItem {Text = "0615", Value = "0615"},
                new SelectListItem {Text = "0626", Value = "0626"},
                new SelectListItem {Text = "0635", Value = "0635"},
                new SelectListItem {Text = "0648", Value = "0648"},
                new SelectListItem {Text = "0652", Value = "0652"},
                new SelectListItem {Text = "0671", Value = "0671"},
                new SelectListItem {Text = "0701", Value = "0701"},
                new SelectListItem {Text = "0704", Value = "0704"},
                new SelectListItem {Text = "0711", Value = "0711"},
                new SelectListItem {Text = "0721", Value = "0721"},
                new SelectListItem {Text = "0749", Value = "0749"},
                new SelectListItem {Text = "0751", Value = "0751"},
                new SelectListItem {Text = "0771", Value = "0771"},
                new SelectListItem {Text = "0772", Value = "0772"},
                new SelectListItem {Text = "0782", Value = "0782"},
                new SelectListItem {Text = "0836", Value = "0836"},
                new SelectListItem {Text = "0838", Value = "0838"},
                new SelectListItem {Text = "0842", Value = "0842"},
                new SelectListItem {Text = "0847", Value = "0847"},
                new SelectListItem {Text = "0882", Value = "0882"},
                new SelectListItem {Text = "0910", Value = "0910"},
                new SelectListItem {Text = "0913", Value = "0913"},
                new SelectListItem {Text = "0932", Value = "0932"},
                new SelectListItem {Text = "0934", Value = "0934"},
                new SelectListItem {Text = "0945", Value = "0945"},
                new SelectListItem {Text = "0971", Value = "0971"},
                new SelectListItem {Text = "0982", Value = "0982"},
                new SelectListItem {Text = "0983", Value = "0983"},
                new SelectListItem {Text = "1024", Value = "1024"},
                new SelectListItem {Text = "1028", Value = "1028"},
                new SelectListItem {Text = "1041", Value = "1041"},
                new SelectListItem {Text = "1055", Value = "1055"},
                new SelectListItem {Text = "1064", Value = "1064"},
                new SelectListItem {Text = "1071", Value = "1071"},
                new SelectListItem {Text = "1072", Value = "1072"},
                new SelectListItem {Text = "1082", Value = "1082"}

            };
            DOHDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select Org Below" });
            ViewBag.DOHDropdown = DOHDropdown;
        }



        /*
        public IActionResult LinkedOM(int hubId)
        {
            Console.WriteLine(hubId);
            var entries = _context.OMTable.Where(entry => entry.HubId == hubId);
            // var entries = IEnumerable.GetEntriesByHubId(hubId);
            // Redirect to the Index action of the Home controller
            return PartialView("_LinkedOM", entries);
        }
        */
        public IEnumerable<OMTable> GetEntriesByUserId(int userid)
        {
            return _context.OMTable.Where(entry => entry.HubId == userid).ToList();
        }
    }


}
