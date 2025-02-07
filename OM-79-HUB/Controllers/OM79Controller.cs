﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarfBuzzSharp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OM_79_HUB.Models;
using OM_79_HUB.Models.DB.OM79;
using OM_79_HUB.Models.DB.OM79Hub;
using OM79.Models.DB;
using X.PagedList;




namespace OM_79_HUB.Data
{
    public class OM79Controller : Controller
    {
        private readonly OM79Context _context;
        private readonly OM_79_HUBContext _context2;
        private readonly Pj103Context _pjContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public OM79Controller(OM79Context context, OM_79_HUBContext context2, Pj103Context pjContext, IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _context2 = context2;
            _pjContext = pjContext;
            _webHostEnvironment = webHostEnvironment;
            _httpContextAccessor = httpContextAccessor;
        }
        //// GET: OMTables
        //public async Task<IActionResult> Index(string searchRouteIDB, string sortOrder)
        //{
        //    ViewData["SubmissionDateSortParm"] = String.IsNullOrEmpty(sortOrder) ? "date_desc" : "";

        //    var omTables = from m in _context.OMTable
        //                   select m;

        //    if (!String.IsNullOrEmpty(searchRouteIDB))
        //    {
        //        omTables = omTables.Where(s => s.RouteIDB.Contains(searchRouteIDB));
        //    }

        //    switch (sortOrder)
        //    {
        //        case "date_desc":
        //            omTables = omTables.OrderByDescending(s => s.SubmissionDate);
        //            break;
        //        default:
        //            omTables = omTables.OrderBy(s => s.SubmissionDate);
        //            break;
        //    }

        //    return View(await omTables.ToListAsync());
        //}
        private async Task<bool> CheckForAccessPermission(string? currentUser)
        {
            if (string.IsNullOrEmpty(currentUser))
            {
                return false;
            }

            // Normalize the ENumber (assuming case-insensitive)
            string normalizedENumber = currentUser.Replace("EXECUTIVE\\", "").Trim().ToLower();

            // Check in AdminData
            bool isAdmin = await _context2.AdminData
                .AnyAsync(a => a.ENumber.ToLower() == normalizedENumber);

            if (isAdmin)
            {
                return true;
            }

            // Check in UserData
            bool isUser = await _context2.UserData
                .AnyAsync(u => u.ENumber.ToLower() == normalizedENumber);

            return isUser;
        }

        public async Task<IActionResult> Index(string searchRouteIDB, string sortOrder, int? page)
        {
            //Need to only allow for signees to access this
            var currentUser = User.Identity.Name;
            var validUser = await CheckForAccessPermission(currentUser);
            if (!validUser)
            {
                return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
            }

            ViewData["CurrentSort"] = sortOrder;
            ViewData["SubmissionDateSortParm"] = String.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewData["CurrentFilter"] = searchRouteIDB;

            // Filter the records where IsArchive is not true
            var omTables = from m in _context.OMTable
                           where m.IsArchive != true
                           select m;

            if (!String.IsNullOrEmpty(searchRouteIDB))
            {
                omTables = omTables.Where(s => s.RouteIDB.Contains(searchRouteIDB));
            }

            switch (sortOrder)
            {
                case "date_desc":
                    omTables = omTables.OrderByDescending(s => s.SubmissionDate);
                    break;
                default:
                    omTables = omTables.OrderBy(s => s.SubmissionDate);
                    break;
            }

            int pageNumber = (page ?? 1);
            var pagedOmTables = await omTables.ToPagedListAsync(pageNumber, 50);

            return View(pagedOmTables);
        }



        public async Task<IActionResult> ArchivedIndex(string searchRouteIDB, string sortOrder, int? page)
        {
            //Need to only allow for signees to access this
            var currentUser = User.Identity.Name;
            var validUser = await CheckForAccessPermission(currentUser);
            if (!validUser)
            {
                return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
            }

            ViewData["CurrentSort"] = sortOrder;
            ViewData["SubmissionDateSortParm"] = String.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewData["CurrentFilter"] = searchRouteIDB;

            // First, get all the HubIds from CENTRAL79HUB where WorkflowStep is not "CancelledRequestArchive"
            var validHubIds = await _context2.CENTRAL79HUB
                                              .Where(h => h.WorkflowStep != "CancelledRequestArchive")
                                              .Select(h => h.OMId)  // Assuming OMId is the HubId in OMTable
                                              .ToListAsync();

            // Now fetch the OMTable entries using _context where IsArchive is not true and HubId is in the validHubIds list
            var omTables = from m in _context.OMTable
                           where m.IsArchive == true && m.HubId.HasValue && validHubIds.Contains(m.HubId.Value)
                           select m;


            if (!String.IsNullOrEmpty(searchRouteIDB))
            {
                omTables = omTables.Where(s => s.RouteIDB.Contains(searchRouteIDB));
            }

            switch (sortOrder)
            {
                case "date_desc":
                    omTables = omTables.OrderByDescending(s => s.SubmissionDate);
                    break;
                default:
                    omTables = omTables.OrderBy(s => s.SubmissionDate);
                    break;
            }

            int pageNumber = (page ?? 1);
            var pagedOmTables = await omTables.ToPagedListAsync(pageNumber, 50);

            return View(pagedOmTables);
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

            var hub = _context2.CENTRAL79HUB.FirstOrDefault(e => e.OMId == oMTable.HubId);
            if (hub == null)
            {
                return NotFound();
            }
            //Need to only allow for signees to access this
            var currentUser = User.Identity.Name;
            var validUser = await CheckForAccessPermission(currentUser);

            Console.WriteLine($"Current User: '{currentUser}'");
            Console.WriteLine($"Hub UserId: '{hub.UserId}'");
            
            if (!(validUser) && (currentUser != hub.UserId))
            {
                return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
            }



            return View(oMTable);
        }

        
        // GET: OMTables/Create
        public IActionResult Create([FromQuery] int uniqueID, string county, OMTable oMTable, CENTRAL79HUB central79hub)
        {
            DropDowns(); // Assuming this method populates dropdowns or other data for the view

            // Retrieve CENTRAL79HUB record based on uniqueID
            CENTRAL79HUB central79Hub = _context2.CENTRAL79HUB.FirstOrDefault(c => c.OMId == uniqueID);

            if (central79Hub != null)
            {
                // Retrieve county name from central79Hub
                string countyName = central79Hub.County;

                // Check if countyName exists in CountyMappings dictionary
                if (CountyMappings.ContainsKey(countyName))
                {
                    // Map county name to numeric code
                    int countyCode = CountyMappings[countyName];

                    // Store uniqueID and county code in TempData
                    TempData["UniqueID"] = uniqueID;
                    TempData["CountyCode"] = countyCode;
                    TempData["PageStatus"] = "GoingToCreatePage"; // Set TempData for navigation status

                    ViewBag.TestUniqueID = uniqueID; // Pass uniqueID to the view via ViewBag
                    ViewBag.CountyCode = countyCode; // Pass county code to the view via ViewBag
                }
            }

            return View(oMTable); // Return the view with oMTable object
        }


        // POST: OMTables/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id, DistrictNumber, County, SubmissionDate, Routing, RoadChangeType, Otherbox, RouteAssignment, RightOfWayWidth, Railroad, DOTAARNumber, RequestedBy, Comments, AdjacentProperty, APHouses, APBusinesses, APSchools, APOther, APOtherIdentify, Attachments, DESignature, Preparer, RequestedByName, Route, SubRoute, CoDate, CoDateTwo, RAddition, RRedesignation, RMapCorrection, RAbandonment, RInventoryRemoval, RAmend, RRescind, ROther, RightOther, HubId, SignSystem, ProjectNumber, RouteNumber, SubRouteNumber, DateComplete, StartingMilePoint, EndingMilePoint, MaintOrg, YearOfSurvey, BridgeInv, RailroadInv, RailroadAmount, BridgeAmount, BridgeNumbers, Supplemental")] OMTable oMTable, CENTRAL79HUB central79hub, List<IFormFile> attachments, String Datsubmit, int? segmentCount)
        {
            // Optional: Log initial information
            Console.WriteLine("------------------------------------------------------------------------------------------");
            Console.WriteLine("Starting OMTables/Create Action...");
            Console.WriteLine($"Received Datsubmit: {Datsubmit}");
            Console.WriteLine("------------------------------------------------------------------------------------------");
            var districtToCounties = new Dictionary<string, List<string>>()
                {
                    {"1", new List<string>{"Boone", "Clay", "Kanawha", "Mason", "Putnam"}},
                    {"2", new List<string>{"Cabell", "Lincoln", "Logan", "Mingo", "Wayne"}},
                    {"3", new List<string>{"Calhoun", "Jackson", "Pleasants", "Ritchie", "Roane", "Wirt", "Wood"}},
                    {"4", new List<string>{"Doddridge", "Harrison", "Marion", "Monongalia", "Preston", "Taylor"}},
                    {"5", new List<string>{"Berkeley", "Grant", "Hampshire", "Hardy", "Jefferson", "Mineral", "Morgan"}},
                    {"6", new List<string>{"Brooke", "Hancock", "Marshall", "Ohio", "Tyler", "Wetzel"}},
                    {"7", new List<string>{"Barbour", "Braxton", "Gilmer", "Lewis", "Upshur", "Webster"}},
                    {"8", new List<string>{"Pendleton", "Pocahontas", "Randolph", "Tucker"}},
                    {"9", new List<string>{"Fayette", "Greenbrier", "Monroe", "Nicholas", "Summers"}},
                    {"10", new List<string>{"McDowell", "Mercer", "Raleigh", "Wyoming"}}
                };
            if (Request.Form.TryGetValue("StartingMilePoint", out var startingMilePointValue) && decimal.TryParse(startingMilePointValue, out var startingMilePoint))
            {
                oMTable.StartingMilePoint = startingMilePoint;
            }
            else
            {
                ModelState.AddModelError("StartingMilePoint", "Starting Mile Point is required and must be a valid number.");
            }

            if (Request.Form.TryGetValue("EndingMilePoint", out var endingMilePointValue) &&
                decimal.TryParse(endingMilePointValue, out var endingMilePoint))
            {
                oMTable.EndingMilePoint = endingMilePoint;
            }
            else
            {
                ModelState.AddModelError("EndingMilePoint", "Ending Mile Point is required and must be a valid number.");
            }
            if (!ModelState.IsValid)
            {
                foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(modelError.ErrorMessage);
                }
                return View(oMTable);
            }

            try
            {
                if (ModelState.IsValid)
                {
                    // Ensure countyCode is correctly set from TempData
                    if (TempData["CountyCode"] is int countyCode)
                    {
                        // Set countyCode to oMTable if it's not already set
                        if (oMTable.County == null)
                        {
                            oMTable.County = CountyMappings.FirstOrDefault(x => x.Value == countyCode).Key;
                        }
                        // Map the county to the district number
                        foreach (var district in districtToCounties)
                        {
                            if (district.Value.Contains(oMTable.County))
                            {
                                oMTable.DistrictNumber = int.Parse(district.Key);
                                break; // District found, break the loop
                            }
                        }
                        string paddedRoute = (oMTable.Route ?? 0).ToString("D4");
                        string paddedSubRoute = (oMTable.SubRoute ?? 0).ToString("D2");
                        // Add OMTable to _context and save changes
                        _context.Add(oMTable);
                        await _context.SaveChangesAsync();

                        // Set SubmissionDate and get unique79ID
                        oMTable.SubmissionDate = DateTime.Now;
                        int unique79ID = oMTable.Id;
                        oMTable.IsArchive = false;
                        // Set HubId from TempData if available
                        if (TempData["UniqueID"] is int uniqueID)
                        {
                            oMTable.HubId = uniqueID;
                            await _context.SaveChangesAsync(); // Save changes to _context
                        }

                        if (!string.IsNullOrEmpty(oMTable.SignSystem) && SSMappings.TryGetValue(oMTable.SignSystem, out int signSystemInt))
                        {
                            // Construct routeIDB using the mapped integer for SignSystem
                            string routeIDB = $"{countyCode}{signSystemInt}{paddedRoute}{paddedSubRoute}{oMTable.Supplemental}";
                            oMTable.RouteIDB = routeIDB;
                        }
                        else
                        {
                            // Handle case where mapping is not found, if needed
                            ModelState.AddModelError("SignSystem", "Invalid SignSystem value.");
                            return View(oMTable);
                        }

                        // Create folder for attachments based on unique79ID
                        var uploadsRootFolder = Path.Combine(_webHostEnvironment.WebRootPath, "OMAttachments");
                        var uniqueFolderName = $"OM79-{unique79ID}-Attachments";
                        var uniqueFolderPath = Path.Combine(uploadsRootFolder, uniqueFolderName);

                        // Ensure the directory exists, create if not
                        if (!Directory.Exists(uniqueFolderPath))
                        {
                            Directory.CreateDirectory(uniqueFolderPath);
                        }

                        // Save attachments to unique folder
                        foreach (var attachmentFile in attachments)
                        {
                            if (attachmentFile.Length > 0)
                            {
                                var uniqueFileName = Guid.NewGuid().ToString() + "_" + attachmentFile.FileName;
                                var filePath = Path.Combine(uniqueFolderPath, uniqueFileName);

                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await attachmentFile.CopyToAsync(fileStream);
                                }

                                // Create new Attachment record
                                var attachment = new Attachments
                                {
                                    FileName = attachmentFile.FileName,
                                    FilePath = filePath,
                                    SubmissionID = oMTable.Id
                                };
                                _context.Attachments.Add(attachment);
                            }
                        }

                        // Save changes to _context after processing attachments
                        await _context.SaveChangesAsync();



                        //TODO: Need to pull the isSubmitted variable here
                        //Need to skip the workflow stuff if someone is using the edit page to create a OM79:::
                        // Skip workflow steps if isSubmitted is true

                        // Retrieve the omhub object associated with the current oMTable.HubId
                        var omhub = await _context2.CENTRAL79HUB.FirstOrDefaultAsync(o => o.OMId == oMTable.HubId);

                        // Check if omhub is not null and if IsSubmitted is true
                        if (omhub?.IsSubmitted == true)
                        {
                            return RedirectToAction("EditPackage", "CENTRAL79HUB", new { id = oMTable.HubId });
                        }




                        var OM79workflowForNextStep = await _context2.OM79Workflow.FirstOrDefaultAsync(c => c.HubID == oMTable.HubId);


                        Console.WriteLine("-----------------------------------------------------");
                        Console.WriteLine("-----------------------------------------------------");
                        Console.WriteLine("---------------------------change type-------------------");
                        Console.WriteLine("-----------------------------------------------------");
                        Console.WriteLine("-----------------------------------------------------");
                        Console.WriteLine("-----------------------------------------------------");
                        Console.WriteLine("--------------" + oMTable.RoadChangeType  + "--------------------------");
                        Console.WriteLine("-----------------------------------------------------");
                        Console.WriteLine("-----------------------------------------------------");
                        Console.WriteLine("-----------------------------------------------------");
                        Console.WriteLine("-----------------------------------------------------");
                        Console.WriteLine("-----------------------------------------------------");


                        if (oMTable.RoadChangeType == "Addition" || oMTable.RoadChangeType == "Redesignation")
                        {
                            PJ103Workflow pJ103Workflow = new PJ103Workflow
                            {
                                OMID = oMTable.Id,
                                NumberOfSegments = segmentCount
                            };
                            _context.PJ103Workflow.Add(pJ103Workflow);  
                            await _context.SaveChangesAsync();


                            var OM79workflow = await _context2.OM79Workflow.FirstOrDefaultAsync(c => c.HubID == oMTable.HubId);
                            OM79workflowForNextStep.NextStep = "AddSegment";
                            await _context2.SaveChangesAsync();

                            return RedirectToAction("Details", "CENTRAL79HUB", new { id = oMTable.HubId });
                        }



                        var items = await _context.OMTable.Where(c => c.HubId == oMTable.HubId).ToListAsync();

                        if (items.Count >= OM79workflowForNextStep.NumberOfItems)
                        {
                            OM79workflowForNextStep.NextStep = "FinishEdits";
                            await _context2.SaveChangesAsync();
                            return RedirectToAction("Details", "CENTRAL79HUB", new { id = oMTable.HubId });
                        }
                        else
                        {
                            OM79workflowForNextStep.NextStep = "AddItem";
                            await _context2.SaveChangesAsync();
                            return RedirectToAction("Details", "CENTRAL79HUB", new { id = oMTable.HubId });
                        }
                        /*
                        // Determine action based on Datsubmit value
                        if (Datsubmit == "Save and Create PJ103 Segment")
                        {
                            return RedirectToAction("Create", "PJ103", new { uniqueID = unique79ID });
                        }
                        else if (Datsubmit == "Save")
                        {
                            return RedirectToAction("Details", "CENTRAL79HUB", new { id = oMTable.HubId });
                        }
                        else if (Datsubmit == "Save and Create Additional Item")
                        {
                            return RedirectToAction("Create", "OM79", new { uniqueID = oMTable.HubId });
                        }
                        */
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or handle the exception as needed
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
            }

            // Return the view with oMTable if ModelState is not valid or an exception occurred
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
        */
        // GET: OMTables/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            Console.WriteLine("Edit GET called with id: " + id);

            DropDowns(); // Ensure dropdowns are populated
            Console.WriteLine("Dropdowns populated");

            if (id == null || _context.OMTable == null)
            {
                Console.WriteLine("ID is null or OMTable is null");
                return NotFound();
            }


            var oMTable = await _context.OMTable.FindAsync(id);
            if (oMTable == null)
            {
                Console.WriteLine("OMTable with id " + id + " not found in the database.");
                return NotFound();
            }




            // Initial check on the OM79Workflow's NextStep and current user's access
            var om = await _context2.CENTRAL79HUB.FirstOrDefaultAsync(e => e.OMId == oMTable.HubId);
            // If the package data with the given ID is not found, return a 404 Not Found response
            if (om == null)
            {
                return NotFound();
            }
            var om79Workflow = await _context2.OM79Workflow.FirstOrDefaultAsync(w => w.HubID == om.OMId);
            var currentUser = User.Identity.Name;
            if (om79Workflow?.NextStep == "FinishEdits" && om.UserId == currentUser)
            {
                ViewBag.OMId = oMTable.HubId;

                // Retain the DateComplete field but exclude it from the form
                ViewBag.DateComplete = oMTable.DateComplete;
                Console.WriteLine("DateComplete retained: " + oMTable.DateComplete);

                return View(oMTable);
            }




            //Need to only allow someone to access this when the workflow step is currently with this 
            var hub = await _context2.CENTRAL79HUB.FirstOrDefaultAsync(e => e.OMId == oMTable.HubId);
            var user = User.Identity.Name;

            var validUser = await checkComplexPermission(user, hub);
            if (!validUser)
            {
                return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
            }

            ViewBag.OMId = oMTable.HubId;

            // Retain the DateComplete field but exclude it from the form
            ViewBag.DateComplete = oMTable.DateComplete;
            Console.WriteLine("DateComplete retained: " + oMTable.DateComplete);

            return View(oMTable);
        }


        // POST: OMTables/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DistrictNumber,County,SubmissionDate,Routing,RoadChangeType,Otherbox,RouteAssignment,RightOfWayWidth,RightOther,Railroad,DOTAARNumber,RequestedBy,RequestedByName,Comments,AdjacentProperty,APHouses,APBusinesses,APSchools,APOther,APOtherIdentify,Attachments,DESignature,Preparer,Route,SubRoute,CoDate,CoDateTwo,HubId,SignSystem,ProjectNumber,RouteNumber,SubRouteNumber,StartingMilePoint,EndingMilePoint,MaintOrg,YearOfSurvey,BridgeInv,RailroadInv,RailroadAmount,BridgeAmount,BridgeNumbers,Supplemental,RAddition,RRedesignation,RMapCorrection,RAbandonment,RInventoryRemoval,RAmend,RRescind,ROther,IsArchive,RouteIDB")] OMTable oMTable)
        {
            Console.WriteLine("Edit POST called with id: " + id);

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState is not valid");
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Count > 0)
                    {
                        Console.WriteLine($"Error in {state.Key}: {state.Value.Errors.First().ErrorMessage}");
                    }
                }
            }
            else
            {
                Console.WriteLine("ModelState is valid");
            }

            try
            {
                // Retain the DateComplete field from the database
                var existingOMTable = await _context.OMTable.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
                if (existingOMTable != null)
                {
                    oMTable.DateComplete = existingOMTable.DateComplete;
                    Console.WriteLine("Retained DateComplete from DB: " + oMTable.DateComplete);
                }
                else
                {
                    Console.WriteLine("Existing OMTable entry not found in DB.");
                }

                // Update the entry
                _context.Update(oMTable);
                await _context.SaveChangesAsync();
                Console.WriteLine("Successfully updated OMTable with id: " + id);

                // Redirect to EditPackage with HubId after successful edit
                return RedirectToAction("EditPackage", "CENTRAL79HUB", new { id = oMTable.HubId });
            }
            catch (DbUpdateConcurrencyException)
            {
                Console.WriteLine("DbUpdateConcurrencyException occurred.");
                // Perform the existence check directly here
                if (!_context.OMTable.Any(e => e.Id == oMTable.Id))
                {
                    Console.WriteLine("OMTable not found in DB during concurrency check.");
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // If ModelState is invalid, re-populate dropdowns
            Console.WriteLine("ModelState is invalid, re-populating dropdowns.");
            DropDowns();
            return View(oMTable);
        }

        private async Task<bool> checkComplexPermission(string currentUser, CENTRAL79HUB passedHub)
        {
            if (string.IsNullOrEmpty(currentUser) || passedHub.OMId == null)
            {
                return false;
            }

            var currentStep = passedHub.WorkflowStep;
            var isUserAuthorized = false;

            // Normalize the current user's E-number
            string normalizedUser = currentUser.Replace("EXECUTIVE\\", "").Trim().ToLower();
            var normalizedHubUser = passedHub.UserId.Replace("EXECUTIVE\\", "").Trim().ToLower();
            
            var district = passedHub.District;

            switch (currentStep)
            {
                case "RestartFromDistrict":
                case "RestartFromDistrictManager":
                    // Only the user associated with the hub entry can access this
                    isUserAuthorized = normalizedUser == normalizedHubUser;
                    break;

                case "SubmittedToDistrict":
                    isUserAuthorized = false;
                    break;

                case "SubmittedToDistrictManager":
                    isUserAuthorized = false;
                    break;

                case "SubmittedToCentralHDS":
                    // Allow access if the current user's ENumber matches a UserData entry where HDS is true
                    isUserAuthorized = await _context2.UserData
                        .AnyAsync(u => u.HDS && u.ENumber.ToLower() == normalizedUser);
                    break;

                case "SubmittedToCentralGIS":
                    // Allow access if the current user's ENumber matches a UserData entry where HDS is true
                    isUserAuthorized = await _context2.UserData
                        .AnyAsync(u => u.GISManager && u.ENumber.ToLower() == normalizedUser);
                    break;

                case "SubmittedBackToDistrictManager":
                case "SubmittedBackToDistrictManagerFromOperations":
                    // Allow access if the current user's ENumber matches a UserData entry where District matches and they are a DistrictManager
                    isUserAuthorized = await _context2.UserData
                        .AnyAsync(u => u.District == district && u.DistrictManager && u.ENumber.ToLower() == normalizedUser);
                    break;

                case "SubmittedToRegionalEngineer":
                    isUserAuthorized = false;
                    break;

                case "SubmittedToDirectorOfOperations":
                    isUserAuthorized = false;
                    break;

                case "SubmittedToCentralChief":
                    isUserAuthorized = false;
                    break;

                case "CancelledRequestArchive":
                case "Finalized":
                    // Typically, no edits or deletes allowed once archived or finalized.
                    isUserAuthorized = false;
                    break;

                default:
                    // Any other cases not explicitly handled above
                    isUserAuthorized = false;
                    break;
            }

            return isUserAuthorized;
        }




        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.OMTable == null)
            {
                return NotFound();
            }

            // Find the OMTable entry by id
            var oMTable = await _context.OMTable.FirstOrDefaultAsync(m => m.Id == id);
            if (oMTable == null)
            {
                return NotFound();
            }

            // Fetch the CENTRAL79HUB entry and validate workflow step and permissions
            var hub = await _context2.CENTRAL79HUB.FirstOrDefaultAsync(e => e.OMId == oMTable.HubId);
            if (hub == null)
            {
                return NotFound();
            }

            var om79Workflow = await _context2.OM79Workflow.FirstOrDefaultAsync(w => w.HubID == hub.OMId);
            var currentUser = User.Identity.Name;

            // Check if user can override permissions
            if (om79Workflow?.NextStep == "FinishEdits" && hub.UserId == currentUser)
            {
                await DeleteRelatedEntities(id.Value);
                _context.OMTable.Remove(oMTable);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "OM79 item and related records deleted successfully.";
                return RedirectToAction("EditPackage", "CENTRAL79HUB", new { id = oMTable.HubId });
            }

            // Run the complex permission check if override is not applicable
            var validUser = await checkComplexPermission(currentUser, hub);
            if (!validUser)
            {
                return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
            }

            // Delete related entities and OMTable
            await DeleteRelatedEntities(id.Value);
            _context.OMTable.Remove(oMTable);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "OM79 item and related records deleted successfully.";
            return RedirectToAction("EditPackage", "CENTRAL79HUB", new { id = oMTable.HubId });
        }

        // Helper method to delete related entities
        private async Task DeleteRelatedEntities(int omId)
        {
            // Delete PJ103Workflow entries
            var workflows = await _context.PJ103Workflow.Where(w => w.OMID == omId).ToListAsync();
            if (workflows.Any())
            {
                _context.PJ103Workflow.RemoveRange(workflows);
                await _context.SaveChangesAsync();
            }

            // Delete Submissions and related data
            var submissions = await _pjContext.Submissions.Where(s => s.OM79Id == omId).ToListAsync();
            if (submissions.Any())
            {
                foreach (var submission in submissions)
                {
                    // Delete Attachments
                    var attachments = await _context.Attachments.Where(a => a.SubmissionID == submission.SubmissionID).ToListAsync();
                    if (attachments.Any())
                    {
                        _context.Attachments.RemoveRange(attachments);
                    }

                    // Delete RouteInfo
                    var routeInfo = await _pjContext.RouteInfo.FirstOrDefaultAsync(r => r.SubmissionID == submission.SubmissionID);
                    if (routeInfo != null)
                    {
                        _pjContext.RouteInfo.Remove(routeInfo);
                    }

                    // Delete BridgeRR
                    var bridgeRR = await _pjContext.BridgeRR.FirstOrDefaultAsync(b => b.RailKey == omId);
                    if (bridgeRR != null)
                    {
                        _pjContext.BridgeRR.Remove(bridgeRR);
                    }

                    // Remove the Submission entry
                    _pjContext.Submissions.Remove(submission);
                }

                await _pjContext.SaveChangesAsync();
            }
        }



        /*
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
        */
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

            List<SelectListItem> SuppDropdown = new()
            {
                new SelectListItem {Text = "00 Not Applicable", Value = "00"},
                new SelectListItem {Text = "01 Alternate", Value = "01"},
                new SelectListItem {Text = "02 Wye", Value = "02"},
                new SelectListItem {Text = "03 Spur", Value = "03"},
                new SelectListItem {Text = "04 North", Value = "04"},
                new SelectListItem {Text = "05 South", Value = "05"},
                new SelectListItem {Text = "06 East", Value = "06"},
                new SelectListItem {Text = "07 West", Value = "07"},
                new SelectListItem {Text = "08 Business", Value = "08"},
                new SelectListItem {Text = "09 North Bound (Business)", Value = "09"},
                new SelectListItem {Text = "10 South Bound (Business)", Value = "10"},
                new SelectListItem {Text = "11 East Bound (Business)", Value = "11"},
                new SelectListItem {Text = "12 West Bound (Business)", Value = "12"},
                new SelectListItem {Text = "13 Truck Route", Value = "13"},
                new SelectListItem {Text = "14 Bypass", Value = "14"},
                new SelectListItem {Text = "15 Loop", Value = "15"},
                new SelectListItem {Text = "16 Toll", Value = "16"},
                new SelectListItem {Text = "21 Footbridge", Value = "21"},
                new SelectListItem {Text = "22 Historical Bridge", Value = "22"},
                new SelectListItem {Text = "23 Connector", Value = "23"},




            };
            SuppDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.SuppDropdown = SuppDropdown;

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

        private Dictionary<string, int> CountyMappings = new Dictionary<string, int>
{
    { "Barbour", 01 },
    { "Berkeley", 02 },
    { "Boone", 03 },
    { "Braxton", 04 },
    { "Brooke", 05 },
    { "Cabell", 06 },
    { "Calhoun", 07 },
    { "Clay", 08 },
    { "Doddridge", 09 },
    { "Fayette", 10 },
    { "Gilmer", 11 },
    { "Grant", 12 },
    { "Greenbrier", 13 },
    { "Hampshire", 14 },
    { "Hancock", 15 },
    { "Hardy", 16 },
    { "Harrison", 17 },
    { "Jackson", 18 },
    { "Jefferson", 19 },
    { "Kanawha", 20 },
    { "Lewis", 21 },
    { "Lincoln", 22 },
    { "Logan", 23 },
    { "McDowell", 24 },
    { "Marion", 25 },
    { "Marshall", 26 },
    { "Mason", 27 },
    { "Mercer", 28 },
    { "Mineral", 29 },
    { "Mingo", 30 },
    { "Monongalia", 31 },
    { "Monroe", 32 },
    { "Morgan", 33 },
    { "Nicholas", 34 },
    { "Ohio", 35 },
    { "Pendleton", 36 },
    { "Pleasants", 37 },
    { "Pocahontas", 38 },
    { "Preston", 39 },
    { "Putnam", 40 },
    { "Raleigh", 41 },
    { "Randolph", 42 },
    { "Ritchie", 43 },
    { "Roane", 44 },
    { "Summers", 45 },
    { "Taylor", 46 },
    { "Tucker", 47 },
    { "Tyler", 48 },
    { "Upshur", 49 },
    { "Wayne", 50 },
    { "Webster", 51 },
    { "Wetzel", 52 },
    { "Wirt", 53 },
    { "Wood", 54 },
    { "Wyoming", 55 }
};
        private Dictionary<string, int> SSMappings = new Dictionary<string, int>
        {
            {"Interstate", 1 },
            {"US", 2 },
            {"WV", 3 },
            {"CO", 4 },
            {"State Park and Forest Road", 6 },
            {"FANS", 7 },
            {"HARP", 8 }


        };
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
