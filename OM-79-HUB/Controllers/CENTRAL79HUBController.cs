using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Mail;
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
            om79.WorkflowStep = "SubmittedToDistrict";
            // Retrieve the OM79Workflow entry using HubID
            var om79Workflow = await _context.OM79Workflow.FirstOrDefaultAsync(w => w.HubID == id);
            if (om79Workflow != null)
            {
                // Set the next step to "Submitted"
                om79Workflow.NextStep = "Submitted";
            }

            // Save changes to the database
            await _context.SaveChangesAsync();


            sendInitialWorkflowEmailToDistrictUsers(id);


            // Redirect to an appropriate action, such as the index page
            return RedirectToAction("Details", new { id = om79.OMId });
        }

        public void sendInitialWorkflowEmailToDistrictUsers(int id)
        {
            try
            {
                var omEntry = _context.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
                var districtToSendTo = omEntry.District;

                var allDistrictUsers = _context.UserData
                    .Where(e => e.District == districtToSendTo && !e.DistrictManager)
                    .ToList();

                // Group roles by user
                var userRolesDictionary = allDistrictUsers
                    .GroupBy(u => u.ENumber)
                    .ToDictionary(
                        g => g.Key,
                        g => new
                        {
                            User = g.First(),
                            Roles = g.SelectMany(GetUserRoles).Distinct().ToList()
                        });

                foreach (var userRoleEntry in userRolesDictionary)
                {
                    var user = userRoleEntry.Value.User;
                    var roles = string.Join(", ", userRoleEntry.Value.Roles);

                    var message = new MailMessage
                    {
                        From = new MailAddress("DOTPJ103Srv@wv.gov")
                    };
                    message.To.Add(user.Email);
                    message.Subject = $"OM79 Submitted For District [{user.District}] Review";
                    message.Body = $"Hello {user.FirstName},<br><br>" +
                                   $"An OM79 entry has been submitted from your district. You are currently listed for the following role(s) in the system and are responsible for reviewing and signing the OM79 in district {user.District}:<br><br>" +
                                   $"{string.Join("<br>", userRoleEntry.Value.Roles)}.<br><br>" +
                                   $"Please click the link below to review and sign the OM79 entry:<br><br>" +
                                   $"<a href='https://dotappstest.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}'>Review OM79 Entry</a><br><br>" +
                                   $"Note: If you hold multiple roles within your district, you will need to sign for each role separately using the link above.<br><br>" +
                                   $"Thank you,<br>" +
                                   $"OM79 Automated System";
                    message.IsBodyHtml = true;

                    var client = new SmtpClient
                    {
                        Host = "10.204.145.32",
                        Port = 25,
                        EnableSsl = false,
                        Credentials = new NetworkCredential("richard.a.tucker@wv.gov", "trippononemails")
                    };
                    client.Send(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
            }
        }

        private IEnumerable<string> GetUserRoles(UserData user)
        {
            if (user.BridgeEngineer) yield return "Bridge Engineer";
            if (user.TrafficEngineer) yield return "Traffic Engineer";
            if (user.MaintenanceEngineer) yield return "Maintenance Engineer";
            if (user.ConstructionEngineer) yield return "Construction Engineer";
            if (user.RightOfWayManager) yield return "Right Of Way Manager";
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
            var hubkey = int.Parse(Request.Form["HubKey"]); // Define hubkey here
            bool isDistrictManagerSigning = false;


            if (app.FirstOrDefault() == "approve")
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
                signature.IsCurrentSig = true;


                _context.Add(signature);
                await _context.SaveChangesAsync();

                if (signature.SigType == "District Manager")
                {
                    isDistrictManagerSigning = true;
                }

                // Update the workflow step if approved
                var omEntry = _context.CENTRAL79HUB.FirstOrDefault(e => e.OMId == signature.HubKey);
                if (omEntry != null && omEntry.WorkflowStep == "SubmittedToDistrict")
                {
                    var allSignatures = _context.SignatureData.Where(e => e.HubKey == omEntry.OMId).ToList();

                    // List of required roles
                    var requiredRoles = new List<string>
                    {
                        "Bridge Engineer",
                        "Traffic Engineer",
                        "Maintenance Engineer",
                        "Construction Engineer",
                        "Right Of Way Manager"
                    };

                    // Check if all required roles have signatures
                    bool allRolesSigned = requiredRoles.All(role => allSignatures.Any(sig => sig.SigType == role));
                    
                    if (allRolesSigned)
                    {
                        bool allRolesApproved = requiredRoles.All(role => allSignatures.Any(sig => sig.SigType == role && sig.IsApprove));
                        if (allRolesApproved)
                        {
                            omEntry.WorkflowStep = "SubmittedToDistrictManager";

                            Console.WriteLine("============================================");
                            Console.WriteLine("============================================");
                            Console.WriteLine("============================================");
                            Console.WriteLine("============================================");
                            Console.WriteLine("============================================");
                            Console.WriteLine("============All Signatures should be done here=========================");
                            Console.WriteLine("============================================");
                            Console.WriteLine("============================================");
                            Console.WriteLine("============================================");
                            Console.WriteLine("============================================");
                            Console.WriteLine("============================================");
                            Console.WriteLine("============================================");
                            Console.WriteLine("============================================");
                            Console.WriteLine("============================================");
                            Console.WriteLine("============================================");
                            await _context.SaveChangesAsync();


                            /////////// Send District Manager Email Here
                            ///

                            sendInitialWorkflowEmailToDistrictManager(hubkey);
                        }
                        else
                        {
                            // This is the case where everyone from the district has signed, but one or more district users declined the OM79, need to send it back to the initial user with an email
                            // Maybe send all active signature's comments to the user and restart the workflow and allow them to edit the items, segments, and om79 
                            omEntry.WorkflowStep = "Restart";
                            await _context.SaveChangesAsync();
                        }
                    }
                    await _context.SaveChangesAsync();
                }


                // Check if the District Manager has just signed
                if (omEntry != null && omEntry.WorkflowStep == "SubmittedToDistrictManager" && isDistrictManagerSigning)
                {
                    // This means the district manager has just signed and approved the OM79 entry to be sent to the central office
                    Console.WriteLine("District Manager has just signed and approved the OM79 entry.");
                    Console.WriteLine("District Manager has just signed and approved the OM79 entry.");
                    Console.WriteLine("District Manager has just signed and approved the OM79 entry.");
                    Console.WriteLine("District Manager has just signed and approved the OM79 entry.");
                    Console.WriteLine("District Manager has just signed and approved the OM79 entry.");
                    Console.WriteLine("District Manager has just signed and approved the OM79 entry.");
                    Console.WriteLine("District Manager has just signed and approved the OM79 entry.");
                    Console.WriteLine("District Manager has just signed and approved the OM79 entry.");
                    Console.WriteLine("District Manager has just signed and approved the OM79 entry.");
                    Console.WriteLine("District Manager has just signed and approved the OM79 entry.");
                    Console.WriteLine("District Manager has just signed and approved the OM79 entry.");

                    omEntry.WorkflowStep = "SubmittedToCentralHDS";
                    await _context.SaveChangesAsync();

                    // You can add additional logic here if needed
                }

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





                // Update the workflow step if denied
                var omEntry = _context.CENTRAL79HUB.FirstOrDefault(e => e.OMId == signature.HubKey);
                if (omEntry != null && omEntry.WorkflowStep == "SubmittedToDistrict")
                {
                    var allSignatures = _context.SignatureData.Where(e => e.HubKey == omEntry.OMId).ToList();

                    // List of required roles
                    var requiredRoles = new List<string>
                    {
                        "Bridge Engineer",
                        "Traffic Engineer",
                        "Maintenance Engineer",
                        "Construction Engineer",
                        "Right Of Way Manager"
                    };

                    // Check if all required roles have signatures
                    bool allRolesSigned = requiredRoles.All(role => allSignatures.Any(sig => sig.SigType == role));

                    if (allRolesSigned)
                    {
                        // This is the case where everyone from the district has signed, but one or more district users declined the OM79, need to send it back to the initial user with an email
                        // Maybe send all active signature's comments to the user and restart the workflow and allow them to edit the items, segments, and om79 
                        omEntry.WorkflowStep = "Restart";
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return RedirectToAction(nameof(Details), new { id = hubkey });
        }


        public void sendInitialWorkflowEmailToDistrictManager(int id)
        {
            try
            {
                var omEntry = _context.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
                if (omEntry == null)
                {
                    Console.WriteLine("OM Entry not found.");
                    return;
                }

                var districtToSendTo = omEntry.District;

                // Find the district manager
                var districtManager = _context.UserData.FirstOrDefault(e => e.District == districtToSendTo && e.DistrictManager);
                if (districtManager == null)
                {
                    Console.WriteLine("District Manager not found.");
                    return;
                }

                // Compose the email
                var message = new MailMessage
                {
                    From = new MailAddress("DOTPJ103Srv@wv.gov"),
                    Subject = $"OM79 Entry Ready for District [{districtManager.District}] Manager Review",
                    Body = $"Hello {districtManager.FirstName},<br><br>" +
                           $"An OM79 entry from your district has been reviewed and is now awaiting your approval. As the District Manager, your review is crucial before the entry can be forwarded to the central office.<br><br>" +
                           $"Please click the link below to review and sign the OM79 entry:<br><br>" +
                           $"<a href='https://dotappstest.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}'>Review OM79 Entry</a><br><br>" +
                           $"Thank you for your attention to this matter.<br><br>" +
                           $"Best regards,<br>" +
                           $"OM79 Automated System",
                    IsBodyHtml = true
                };

                message.To.Add(districtManager.Email);

                // Send the email
                var client = new SmtpClient
                {
                    Host = "10.204.145.32",
                    Port = 25,
                    EnableSsl = false,
                    Credentials = new NetworkCredential("richard.a.tucker@wv.gov", "trippononemails")
                };

                client.Send(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
            }
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
