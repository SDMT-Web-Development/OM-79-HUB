﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PJ103V3.Models.DB;
using PJ103V3.Models.ViewModels;

using OM_79_HUB.DTOs;
using OM_79_HUB.Models;
using OM79.Models.DB;
using OM_79_HUB.Models.DB.OM79Hub;
using Microsoft.AspNetCore.SignalR;

namespace OM_79_HUB.Data
{
    public class PJ103Controller : Controller
    {
        private readonly Data.Pj103Context _context;
        private readonly OM79Context _oM79Context;
        private readonly OM_79_HUBContext _hubContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public PJ103Controller(Data.Pj103Context context, IWebHostEnvironment webHostEnvironment, OM79Context oM79Context, OM_79_HUBContext hubContext, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _oM79Context = oM79Context;
            _hubContext = hubContext;   
            _httpContextAccessor = httpContextAccessor;
        }
        // GET: Submissions

        //Taking this out 
        /*
        public async Task<IActionResult> Index()
        {
            // Define the list of admin enums
            List<string> AdminEnums = new List<string> { "EXECUTIVE\\E073953", "EXECUTIVE\\E115477", "EXECUTIVE\\E006518", "EXECUTIVE\\E107097",
                "EXECUTIVE\\E029937", "EXECUTIVE\\E039503", "EXECUTIVE\\E101121", "EXECUTIVE\\E122059" };

            // Get the user's identity name
            string plz = User.Identity.Name;

            if (AdminEnums.Contains(plz))
            {
                // User is an admin, show the Index view
                if (_context.Submissions != null)
                {
                    return View(await _context.Submissions.ToListAsync());
                }
                else
                {
                    return Problem("Entity set 'Pj103Context.Submissions' is null.");
                }
            }
            else
            {
                // User is not an admin, redirect to the Home/Index action
                return RedirectToAction("Index", "Home");
            }
        }



        /*   {
     List<string> AdminEnums = new List<string>()
    { "E073953", "E115477"};
        string plz = User.Identity.Name;
        if (AdminEnums.Contains(plz))
        {
            return _context.Submissions != null ?
                        View(await _context.Submissions.ToListAsync()) :
                        Problem("Entity set 'Pj103Context.Submissions'  is null.");
        }
        else
        {
            return RedirectToAction(nameof(RedirectToHomeIndex));
        }
    } */





        public async Task<IActionResult> Details(int? id, AclvlDTOs dto)
        {
            if (id == null || _context.Submissions == null)
            {
                return NotFound();
            }
            var viewmodel = new Aclvl();

            // Mapping fields from submission to viewmodel
            var submission = await _context.Submissions.FirstOrDefaultAsync(m => m.SubmissionID == id);

            //Need to only allow for signees to access this
            var currentUser = User.Identity.Name;
            var validUser = await CheckForAccessPermission(currentUser);
            var omTable = await _oM79Context.OMTable.FirstOrDefaultAsync(e => e.Id == submission.OM79Id);
            var hub = await _hubContext.CENTRAL79HUB.FirstOrDefaultAsync(e => e.OMId == omTable.HubId);

            if (!(validUser) && (currentUser != hub.UserId))
            {
                return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
            }

        
            if (submission != null)
            {
                viewmodel.SubmissionID = submission.SubmissionID;
                viewmodel.ProjectKey = submission.ProjectKey;
                viewmodel.ReportDate = submission.ReportDate;
                viewmodel.County = submission.County;
                viewmodel.RouteNumber = submission.RouteNumber;
                viewmodel.SubRouteNumber = submission.SubRouteNumber;
                viewmodel.ProjectNumber = submission.ProjectNumber;
                viewmodel.DateComplete = submission.DateComplete;
                viewmodel.NatureOfChange = submission.NatureOfChange;
                viewmodel.MaintOrg = submission.MaintOrg;
                //   viewmodel.YearOfSurvey = submission.YearOfSurvey;
                viewmodel.UserID = submission.UserID;
                viewmodel.OtherBox = submission.OtherBox;
                viewmodel.SignSystem = submission.SignSystem;
                viewmodel.StartingMilePoint = submission.StartingMilePoint;
                viewmodel.EndingMilePoint = submission.EndingMilePoint;


                viewmodel.RailroadInv = submission.RailroadInv;
            }

            // Mapping fields from routeinfo to viewmodel
            var routeinfo = await _context.RouteInfo.FirstOrDefaultAsync(m => m.SubmissionID == id);
            if (routeinfo != null)
            {
                viewmodel.ID = routeinfo.ID;
                viewmodel.SubmissionID = routeinfo.SubmissionID;
                //viewmodel.SurfaceType1 = routeinfo.SurfaceType1;
                //viewmodel.Depth = routeinfo.Depth;
                //viewmodel.SurfaceWidth = routeinfo.SurfaceWidth;
                //viewmodel.GradeWidth = routeinfo.GradeWidth;
                //viewmodel.YearBuilt = routeinfo.YearBuilt;
                //viewmodel.SubmissionIDUAB = routeinfo.SubmissionIDUAB;
                //    viewmodel.SurfaceBeginMile = routeinfo.SurfaceBeginMile;
                //    viewmodel.SurfaceEndMile = routeinfo.SurfaceEndMile;
                viewmodel.AccessControl = routeinfo.AccessControl;
                viewmodel.ThroughLanes = routeinfo.ThroughLanes;
                viewmodel.CounterPeakLanes = routeinfo.CounterPeakLanes;
                viewmodel.PeakLanes = routeinfo.PeakLanes;
                viewmodel.ReverseLanes = routeinfo.ReverseLanes;
                viewmodel.LaneWidth = routeinfo.LaneWidth;
                viewmodel.MedianWidth = routeinfo.MedianWidth;
                viewmodel.PavementWidth = routeinfo.PavementWidth;
                viewmodel.SpecialSys = routeinfo.SpecialSys;
                viewmodel.FacilityType = routeinfo.FacilityType;
                viewmodel.FederalAid = routeinfo.FederalAid;
                viewmodel.FedForestHighway = routeinfo.FedForestHighway;
                viewmodel.MedianType = routeinfo.MedianType;
                viewmodel.NHS = routeinfo.NHS;
                viewmodel.TruckRoute = routeinfo.TruckRoute;
                viewmodel.GovIDOwnership = routeinfo.GovIDOwnership;
                viewmodel.WVlegalClass = routeinfo.WVlegalClass;
                viewmodel.FunctionalClass = routeinfo.FunctionalClass;
                viewmodel.SurfaceTypeN = routeinfo.SurfaceTypeN;
            }

            // Mapping fields from bridgerr to viewmodel
            var bridgerr = await _context.BridgeRR.FirstOrDefaultAsync(m => m.RailKey == (int)id);
            if (bridgerr != null)
            {
                viewmodel.Id = bridgerr.Id;
                viewmodel.BridgeNumber = bridgerr.BridgeNumber;
                viewmodel.StationBeginMP = bridgerr.StationBeginMP;
                viewmodel.StationEndMP = bridgerr.StationEndMP;
                viewmodel.CrossingName = bridgerr.CrossingName;
                //viewmodel.SubMaterial = bridgerr.SubMaterial;
                //viewmodel.SuperMaterial = bridgerr.SuperMaterial;
                //viewmodel.FloorMaterial = bridgerr.FloorMaterial;
                //viewmodel.ArchMaterial = bridgerr.ArchMaterial;
                //viewmodel.TotalLength = bridgerr.TotalLength?.ToString();
                //viewmodel.ClearanceRoadway = bridgerr.ClearanceRoadway?.ToString();
                //viewmodel.ClearanceSidewalkRight = bridgerr.ClearanceSidewalkRight?.ToString();
                //viewmodel.ClearanceSidewalkLeft = bridgerr.ClearanceSidewalkLeft?.ToString();
                //viewmodel.ClearanceStreamble = bridgerr.ClearanceStreambed?.ToString();
                //viewmodel.ClearancePortal = bridgerr.ClearancePortal?.ToString();
                //viewmodel.ClearanceAboveWater = bridgerr.ClearanceAboveWater?.ToString();
                //viewmodel.PostedLoadLimits = bridgerr.PostedLoadLimits;
                //viewmodel.ConstructionDate = bridgerr.ConstructionDate;
                //viewmodel.WhomBuilt = bridgerr.WhomBuilt;
                //viewmodel.HistoricalBridge = bridgerr.HistoricalBridge;
                viewmodel.BridgeSurfaceType = bridgerr.BridgeSurfaceType;
                viewmodel.BridgeWidth = bridgerr.BridgeWidth;
                viewmodel.RailRoadMP = bridgerr.RailRoadMP;
                viewmodel.BridgeName = bridgerr.BridgeName;
                viewmodel.RailKey = bridgerr.RailKey;
            }

            return View(viewmodel);
        }
        /*
        // GET: Submissions/Details/5
        public async Task<IActionResult> Details(int? id, AclvlDTOs dto)
        {
            if (id == null || _context.Submissions == null)
            {
                return NotFound();
            }
            var viewmodel = new Aclvl();
            var submission = await _context.Submissions
                .FirstOrDefaultAsync(m => m.SubmissionID == id);
           // var span = await _context.Spans
            //    .FirstOrDefaultAsync(m => m.SubmissionID == id);
           var routeinfo = await _context.RouteInfo
               .FirstOrDefaultAsync(m => m.SubmissionID == id);

            var bridgerr = await _context.BridgeRR
                .FirstOrDefaultAsync(m => m.RailKey == id);



            //Mapping fields from submission to viewmodel
            viewmodel.SubmissionID = submission.SubmissionID;
            viewmodel.ProjectKey = submission.ProjectKey;
            viewmodel.ReportDate = submission.ReportDate;
            viewmodel.County = submission.County;
            viewmodel.RouteNumber = submission.RouteNumber;
            viewmodel.SubRouteNumber = submission.SubRouteNumber;
            viewmodel.ProjectNumber = submission.ProjectNumber;
            viewmodel.DateComplete = submission.DateComplete;
            viewmodel.NatureOfChange = submission.NatureOfChange;
            viewmodel.MaintOrg = submission.MaintOrg;
            viewmodel.YearOfSurvey = submission.YearOfSurvey;
            viewmodel.UserID = submission.UserID;
            viewmodel.OtherBox = submission.OtherBox;
            viewmodel.SignSystem = submission.SignSystem;
            viewmodel.StartingMilePoint = submission.StartingMilePoint;
            viewmodel.EndingMilePoint = submission.EndingMilePoint;
            viewmodel.RailroadInv = submission.RailroadInv;

            //Mapping fields from routeinfo to viewmodel
            viewmodel.ID = routeinfo.ID;
            viewmodel.SubmissionID = routeinfo.SubmissionID;
            viewmodel.SurfaceType1 = routeinfo.SurfaceType1;
            viewmodel.Depth = routeinfo.Depth;
            viewmodel.SurfaceWidth = routeinfo.SurfaceWidth;
            viewmodel.GradeWidth = routeinfo.GradeWidth;
            viewmodel.YearBuilt = routeinfo.YearBuilt;
            viewmodel.SubmissionIDUAB = routeinfo.SubmissionIDUAB;
            viewmodel.StartingMilePoint = routeinfo.SurfaceBeginMile;
            viewmodel.EndingMilePoint = routeinfo.SurfaceEndMile;
            viewmodel.AccessControl = routeinfo.AccessControl;
            viewmodel.ThroughLanes = routeinfo.ThroughLanes;
            viewmodel.CounterPeakLanes = routeinfo.CounterPeakLanes;
            viewmodel.PeakLanes = routeinfo.PeakLanes;
            viewmodel.ReverseLanes = routeinfo.ReverseLanes;
            viewmodel.LaneWidth = routeinfo.LaneWidth;
            viewmodel.MedianWidth = routeinfo.MedianWidth;
            viewmodel.PavementWidth = routeinfo.PavementWidth;
            viewmodel.SpecialSys = routeinfo.SpecialSys;
            viewmodel.FacilityType = routeinfo.FacilityType;
            viewmodel.FederalAid = routeinfo.FederalAid;
            viewmodel.FedForestHighway = routeinfo.FedForestHighway;
            viewmodel.MedianType = routeinfo.MedianType;
            viewmodel.NHS = routeinfo.NHS;
            viewmodel.TruckRoute = routeinfo.TruckRoute;
            viewmodel.GovIDOwnership = routeinfo.GovIDOwnership;
            viewmodel.WVlegalClass = routeinfo.WVlegalClass;
            viewmodel.FunctionalClass = routeinfo.FunctionalClass;


            //Mapping fields from bridgerr to viewmodel
            viewmodel.Id = bridgerr.Id;
            viewmodel.BridgeNumber = bridgerr.BridgeNumber;
            viewmodel.StationBeginMP = bridgerr.StationBeginMP;
            viewmodel.StationEndMP = bridgerr.StationEndMP;
            viewmodel.CrossingName = bridgerr.CrossingName;
            viewmodel.SubMaterial = bridgerr.SubMaterial;
            viewmodel.SuperMaterial = bridgerr.SuperMaterial;
            viewmodel.FloorMaterial = bridgerr.FloorMaterial;
            viewmodel.ArchMaterial = bridgerr.ArchMaterial;
            viewmodel.TotalLength = bridgerr.TotalLength?.ToString(); // Use null-conditional operator
            viewmodel.ClearanceRoadway = bridgerr.ClearanceRoadway?.ToString(); // Use null-conditional operator
            viewmodel.ClearanceSidewalkRight = bridgerr.ClearanceSidewalkRight?.ToString(); // Use null-conditional operator
            viewmodel.ClearanceSidewalkLeft = bridgerr.ClearanceSidewalkLeft?.ToString(); // Use null-conditional operator
            viewmodel.ClearanceStreamble = bridgerr.ClearanceStreambed?.ToString(); // Use null-conditional operator
            viewmodel.ClearancePortal = bridgerr.ClearancePortal?.ToString(); // Use null-conditional operator
            viewmodel.ClearanceAboveWater = bridgerr.ClearanceAboveWater?.ToString(); // Use null-conditional operator
            viewmodel.PostedLoadLimits = bridgerr.PostedLoadLimits;
            viewmodel.ConstructionDate = bridgerr.ConstructionDate;
            viewmodel.WhomBuilt = bridgerr.WhomBuilt;
            viewmodel.HistoricalBridge = bridgerr.HistoricalBridge;
            viewmodel.BridgeSurfaceType = bridgerr.BridgeSurfaceType;
            viewmodel.BridgeWidth = bridgerr.BridgeWidth;
            viewmodel.RailRoadMP = bridgerr.RailRoadMP;
            viewmodel.BridgeName = bridgerr.BridgeName;
            viewmodel.RailKey = bridgerr.RailKey;


            if (submission == null)
            {
                return NotFound();
            }

            return View(viewmodel);
        }
        */
        // GET: Submissions/Create
        public async Task<IActionResult> CreateAsync([FromQuery] int uniqueID, int HubID)
        {
            ViewBag.testUniqueID = uniqueID;
            ViewBag.testHubID = HubID;

            
            // Retrieve the parent OMTable entry
            var parentItem = await _oM79Context.OMTable.FirstOrDefaultAsync(m => m.Id == uniqueID);

            if (parentItem == null)
            {
                return NotFound();
            }

            ViewBag.StartingMilePoint = parentItem.StartingMilePoint;
            ViewBag.EndingMilePoint = parentItem.EndingMilePoint;


            Console.WriteLine("= ===================================================");
            Console.WriteLine("= ===================================================");
            Console.WriteLine("= ===================================================");
            Console.WriteLine("= ======================Start       "+ parentItem.StartingMilePoint +"================End         " + parentItem.EndingMilePoint +"=============");
            Console.WriteLine("= ===================================================");
            Console.WriteLine("= ===================================================");


            TempData["PageStatus"] = "GoingToCreatePage"; // Set TempData for navigation status
            Console.WriteLine(ViewBag.testUniqueID);
            DropDowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Submission submission, AclvlDTOs dto, List<IFormFile> attachments, int testHubID)
        {
            if (ModelState.IsValid)
            {
                // Save the submission first to get an ID
                _context.Add(submission);
                await _context.SaveChangesAsync();

                // Update additional properties and sync tables after getting SubmissionID
                submission.DateComplete = DateTime.Now;
                dto.SubmissionID = submission.SubmissionID;
                synctables(submission.SubmissionID);



                await _context.SaveChangesAsync();


                Console.WriteLine("----------------------------------------");
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("----------------------------------------");

                var om79ID = submission.OM79Id;
                Console.WriteLine($"OM79ID: {om79ID}");

                var om79 = await _oM79Context.OMTable.FirstOrDefaultAsync(o => o.Id == om79ID);
                Console.WriteLine($"OMTable entry: {om79?.Id}, {om79?.RoadChangeType}");

                var pjWorkflow = await _oM79Context.PJ103Workflow.FirstOrDefaultAsync(o => o.OMID == om79ID);
                Console.WriteLine($"PJ103Workflow entry: {pjWorkflow?.PJ103WorkflowID}, {pjWorkflow?.NumberOfSegments}");


               

                var hub = await _hubContext.CENTRAL79HUB.FirstOrDefaultAsync(o => o.OMId == om79.HubId);
                Console.WriteLine($"CENTRAL79HUB entry: {hub?.OMId}, {hub?.County}");

                var omWorkflow = await _hubContext.OM79Workflow.FirstOrDefaultAsync(o => o.HubID == hub.OMId);
                Console.WriteLine($"OM79Workflow entry: {omWorkflow?.HubID}, {omWorkflow?.NextStep}");

                Console.WriteLine("----------------------------------------");
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("----------------------------------------");
                if (pjWorkflow == null || hub.IsSubmitted == true)
                {
                    //They are editing the package here
                    return RedirectToAction("EditPackage", "CENTRAL79HUB", new { id = hub.OMId });
                }


                // Get the list of PJ103 segments
                var pj103Segments = await _context.Submissions.Where(p => p.OM79Id == om79.Id).ToListAsync();
                Console.WriteLine($"Number of PJ103 segments: {pj103Segments.Count}");
                foreach (var segment in pj103Segments)
                {
                    Console.WriteLine($"PJ103 Segment ID: {segment.SubmissionID}");
                }

                // Get the list of OM79Attached items
                var om79Attached = await _oM79Context.OMTable.Where(o => o.HubId == om79.HubId).ToListAsync();
                Console.WriteLine($"Number of OM79 attached items: {om79Attached.Count}");


                // Compare the count of PJ103 segments with pjWorkflow.NumberOfSegments
                if (pj103Segments.Count < pjWorkflow.NumberOfSegments || pj103Segments.Count == 0)
                {
                    return RedirectToAction("Details", "CENTRAL79HUB", new { id = hub.OMId });
                }
                else
                {
                    if(om79Attached.Count < omWorkflow.NumberOfItems)
                    {
                        // Update om79Workflow.NextStep to "AddItem"
                        omWorkflow.NextStep = "AddItem";
                        await _hubContext.SaveChangesAsync();
                        return RedirectToAction("Details", "CENTRAL79HUB", new { id = hub.OMId });
                    }
                    else
                    {
                        omWorkflow.NextStep = "FinishEdits";
                        await _hubContext.SaveChangesAsync();   
                        return RedirectToAction("Details", "CENTRAL79HUB", new { id = hub.OMId });
                    }
                }
            }

              void synctables(int ID)
              {
                  _context.BridgeRR.Add(dto.ToBridgeRR());
                  _context.RouteInfo.Add(dto.ToRouteInfo());
              }

              return View(submission);
          }
        /*
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Submission submission, AclvlDTOs dto, List<IFormFile> attachments)
        {
            if (ModelState.IsValid)
            {
                // Save the submission first to get an ID
                _context.Add(submission);
                await _context.SaveChangesAsync();

                // Update additional properties and sync tables after getting SubmissionID
                submission.DateComplete = DateTime.Now;
                dto.SubmissionID = submission.SubmissionID;
                synctables(submission.SubmissionID);

                // Save the attachments
                foreach (var attachmentFile in attachments)
                {
                    var filePath = "~/wwwroot/PJAttachments"; // Save your file and get the path
                    var attachment = new Attachments
                    {
                        FileName = attachmentFile.FileName,
                        FilePath = filePath,
                        SubmissionID = submission.SubmissionID  // Use the SubmissionID from the saved submission
                    };
                    _context.Attachments.Add(attachment);
                }

                // Save all changes to the database
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            void synctables(int ID)
            {
                _context.BridgeRR.Add(dto.ToBridgeRR());
                _context.RouteInfo.Add(dto.ToRouteInfo());
            }

            return View(submission);
        }
        */


        /*
        // POST: Submissions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //   Create([Bind("SubmissionID,ProjectKey,ReportDate,County,RouteNumber,SubRouteNumber,ProjectNumber,DateComplete,NatureOfChange,MilesOfNewRoad,MaintOrg,YearOfSurvey,AccessControl,ThroughLanes,CounterPeakLanes,PeakLanes,ReverseLanes,LaneWidth,MedianWidth,PavementWidth,SpecialSys,FacilityType,FederalAid,FedForestHighway,MedianType,NHS,TruckRoute,GovIDOwnership,WVlegalClass,FunctionalClass,BridgeNumber,BridgeLocation,StationFrom,StationTo,CrossingName,WeightLimit,SubMaterial,SuperMaterial,FloorMaterial,ArchMaterial,TotalLength,ClearanceRoadway,ClearanceSidewalkRight,ClearanceSidewalkLeft,ClearanceStreamble,ClearancePortal,ClearanceAboveWater,PostedLoadLimits,ConstructionDate,WhomBuilt,HistoricalBridge,UserID,OtherBox,StartingMilePoint,EndingMilePoint,RailroadInv,NumberOfSpans,SignSystem")] Submission submission, AclvlDTOs dto)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Submission submission, AclvlDTOs dto)
        {
            if (ModelState.IsValid)
            {
                _context.Add(submission);
                await _context.SaveChangesAsync();
                submission.DateComplete= DateTime.Now;
                dto.SubmissionID = submission.SubmissionID;
                synctables(submission.SubmissionID);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            void synctables(int ID)
            {
                _context.BridgeRR.Add(dto.ToBridgeRR());
                // Create related objects from dto and add them to the context
                //    _context.Spans.Add(dto.ToSpan());
                _context.RouteInfo.Add(dto.ToRouteInfo());
               // _context.BridgeRR.Add(dto.ToBridgeRR());


            }
            return View(submission);
        }
        */





        // GET: Submissions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Submissions == null)
            {
                return NotFound();
            }

            // Find the submission
            var submission = await _context.Submissions
                .FirstOrDefaultAsync(s => s.SubmissionID == id);

            if (submission == null)
            {
                return NotFound();
            }

            // Fetch the related RouteInfo entity using its foreign key (SubmissionID)
            var routeInfo = await _context.RouteInfo
                .FirstOrDefaultAsync(r => r.SubmissionID == submission.SubmissionID);

            // Fetch the related OMTable entity using the OM79Id from the submission
            var omTable = await _oM79Context.OMTable
                .FirstOrDefaultAsync(om => om.Id == submission.OM79Id);

            // Map data from the Submission entity to the Aclvl view model
            var aclvlViewModel = new Aclvl
            {
                SubmissionID = submission.SubmissionID,
                OM79ID = submission.OM79Id,
                County = submission.County,
                RouteNumber = submission.RouteNumber,
                SubRouteNumber = submission.SubRouteNumber,
                ProjectNumber = submission.ProjectNumber,
                DateComplete = submission.DateComplete,
                NatureOfChange = submission.NatureOfChange,
                StartingMilePoint = submission.StartingMilePoint,
                EndingMilePoint = submission.EndingMilePoint,
                SignSystem = submission.SignSystem,
                RailroadInv = submission.RailroadInv,
                BridgeInv = submission.BridgeInv,
                OtherBox = submission.OtherBox,
                // Map fields from RouteInfo if it exists
                AccessControl = routeInfo?.AccessControl,
                ThroughLanes = routeInfo?.ThroughLanes,
                CounterPeakLanes = routeInfo?.CounterPeakLanes,
                PeakLanes = routeInfo?.PeakLanes,
                ReverseLanes = routeInfo?.ReverseLanes,
                LaneWidth = routeInfo?.LaneWidth,
                GradeWidth = routeInfo?.GradeWidth,
                MedianWidth = routeInfo?.MedianWidth,
                PavementWidth = routeInfo?.PavementWidth,
                SpecialSys = routeInfo?.SpecialSys,
                FacilityType = routeInfo?.FacilityType,
                FederalAid = routeInfo?.FederalAid,
                FedForestHighway = routeInfo?.FedForestHighway,
                MedianType = routeInfo?.MedianType,
                NHS = routeInfo?.NHS,
                TruckRoute = routeInfo?.TruckRoute,
                GovIDOwnership = routeInfo?.GovIDOwnership,
                WVlegalClass = routeInfo?.WVlegalClass,
                FunctionalClass = routeInfo?.FunctionalClass,
                SurfaceTypeN = routeInfo?.SurfaceTypeN,
                MPSegmentStart = routeInfo?.MPSegmentStart,
                MPSegmentEnd = routeInfo?.MPSegmentEnd
            };

            // Pass the HubId (OMId) from OMTable if available
            ViewBag.OMId = omTable?.HubId;

            // Populate dropdowns using the DropDowns method
            DropDowns();

            return View(aclvlViewModel);
        }
        private async Task<bool> CheckForAccessPermission(string? currentUser)
        {
            if (string.IsNullOrEmpty(currentUser))
            {
                return false;
            }

            // Normalize the ENumber (assuming case-insensitive)
            string normalizedENumber = currentUser.Replace("EXECUTIVE\\", "").Trim().ToLower();

            // Check in AdminData
            bool isAdmin = await _hubContext.AdminData
                .AnyAsync(a => a.ENumber.ToLower() == normalizedENumber);

            if (isAdmin)
            {
                return true;
            }

            // Check in UserData
            bool isUser = await _hubContext.UserData
                .AnyAsync(u => u.ENumber.ToLower() == normalizedENumber);

            return isUser;
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
                    isUserAuthorized = await _hubContext.UserData
                        .AnyAsync(u => u.HDS && u.ENumber.ToLower() == normalizedUser);
                    break;

                case "SubmittedToCentralGIS":
                    // Allow access if the current user's ENumber matches a UserData entry where HDS is true
                    isUserAuthorized = await _hubContext.UserData
                        .AnyAsync(u => u.GISManager && u.ENumber.ToLower() == normalizedUser);
                    break;

                case "SubmittedBackToDistrictManager":
                case "SubmittedBackToDistrictManagerFromOperations":
                    // Allow access if the current user's ENumber matches a UserData entry where District matches and they are a DistrictManager
                    isUserAuthorized = await _hubContext.UserData
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



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Aclvl aclvlViewModel)
        {
            if (ModelState.IsValid)
            {
                var user = User.Identity.Name;

                var oMTable = await _oM79Context.OMTable.FirstOrDefaultAsync(e => e.Id == aclvlViewModel.OM79ID);
                if (oMTable == null)
                {
                    Console.WriteLine($"OMTable not found for OM79ID {aclvlViewModel.OM79ID}");
                    return NotFound();
                }

                var hub = await _hubContext.CENTRAL79HUB.FirstOrDefaultAsync(e => e.OMId == oMTable.HubId);
                if (hub == null)
                {
                    Console.WriteLine($"CENTRAL79HUB entry not found for OMId {oMTable.HubId}");
                    return NotFound();
                }

                var validUser = await checkComplexPermission(user, hub);
                if (!validUser)
                {
                    return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
                }

                try
                {
                    var submission = await _context.Submissions.FirstOrDefaultAsync(s => s.SubmissionID == id);
                    if (submission == null)
                    {
                        Console.WriteLine($"Submission not found for ID {id}");
                        return NotFound();
                    }

                    // Map ViewModel to Submission entity
                    submission.County = aclvlViewModel.County;
                    submission.RouteNumber = aclvlViewModel.RouteNumber;
                    submission.SubRouteNumber = aclvlViewModel.SubRouteNumber;
                    submission.ProjectNumber = aclvlViewModel.ProjectNumber;
                    submission.DateComplete = aclvlViewModel.DateComplete;
                    submission.NatureOfChange = aclvlViewModel.NatureOfChange;
                    submission.StartingMilePoint = aclvlViewModel.StartingMilePoint;
                    submission.EndingMilePoint = aclvlViewModel.EndingMilePoint;
                    submission.SignSystem = aclvlViewModel.SignSystem;
                    submission.RailroadInv = aclvlViewModel.RailroadInv;
                    submission.BridgeInv = aclvlViewModel.BridgeInv;
                    submission.OtherBox = aclvlViewModel.OtherBox;

                    var routeInfo = await _context.RouteInfo.FirstOrDefaultAsync(r => r.SubmissionID == id);
                    if (routeInfo == null)
                    {
                        Console.WriteLine($"RouteInfo not found for SubmissionID {id}, creating new entry.");
                        routeInfo = new RouteInfo { SubmissionID = id };
                        _context.RouteInfo.Add(routeInfo);
                    }

                    // Map ViewModel to RouteInfo entity
                    routeInfo.AccessControl = aclvlViewModel.AccessControl;
                    routeInfo.ThroughLanes = aclvlViewModel.ThroughLanes;
                    routeInfo.CounterPeakLanes = aclvlViewModel.CounterPeakLanes;
                    routeInfo.PeakLanes = aclvlViewModel.PeakLanes;
                    routeInfo.ReverseLanes = aclvlViewModel.ReverseLanes;
                    routeInfo.LaneWidth = aclvlViewModel.LaneWidth;
                    routeInfo.MedianWidth = aclvlViewModel.MedianWidth;
                    routeInfo.GradeWidth = aclvlViewModel.GradeWidth;
                    routeInfo.PavementWidth = aclvlViewModel.PavementWidth;
                    routeInfo.SpecialSys = aclvlViewModel.SpecialSys;
                    routeInfo.FacilityType = aclvlViewModel.FacilityType;
                    routeInfo.FederalAid = aclvlViewModel.FederalAid;
                    routeInfo.FedForestHighway = aclvlViewModel.FedForestHighway;
                    routeInfo.MedianType = aclvlViewModel.MedianType;
                    routeInfo.NHS = aclvlViewModel.NHS;
                    routeInfo.TruckRoute = aclvlViewModel.TruckRoute;
                    routeInfo.GovIDOwnership = aclvlViewModel.GovIDOwnership;
                    routeInfo.WVlegalClass = aclvlViewModel.WVlegalClass;
                    routeInfo.FunctionalClass = aclvlViewModel.FunctionalClass;
                    routeInfo.SurfaceTypeN = aclvlViewModel.SurfaceTypeN;
                    routeInfo.MPSegmentStart = aclvlViewModel.MPSegmentStart;
                    routeInfo.MPSegmentEnd = aclvlViewModel.MPSegmentEnd;

                    _context.Update(submission);
                    _context.Update(routeInfo);
                    await _context.SaveChangesAsync();

                    var item = await _oM79Context.OMTable.FirstOrDefaultAsync(e => e.Id == submission.OM79Id);
                    if (item == null)
                    {
                        Console.WriteLine($"OMTable entry not found for OM79ID {submission.OM79Id}");
                        return NotFound();
                    }

                    return RedirectToAction("EditPackage", "CENTRAL79HUB", new { id = item.HubId });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    Console.WriteLine($"Concurrency exception: {ex.Message}");
                    throw;
                }
            }

            DropDowns();
            return View(aclvlViewModel);
        }


        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, Aclvl aclvlViewModel)
        //{
        //    /*
        //    if (id != aclvlViewModel.SubmissionID)
        //    {
        //        return NotFound();
        //    }
        //    */
        //    if (ModelState.IsValid)
        //    {
        //        //Need to only allow someone to access this when the workflow step is currently with this 
        //        var oMTable = await _oM79Context.OMTable.FirstOrDefaultAsync(e => e.Id == aclvlViewModel.OM79ID);
        //        var hub = await _hubContext.CENTRAL79HUB.FirstOrDefaultAsync(e => e.OMId == oMTable.HubId);
        //        var user = User.Identity.Name;
        //        var validUser = await checkComplexPermission(user, hub);
        //        if (!validUser)
        //        {
        //            return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
        //        }
        //        try
        //        {
        //            // Retrieve the existing Submission entity from the database
        //            var submission = await _context.Submissions
        //                .FirstOrDefaultAsync(s => s.SubmissionID == id);

        //            if (submission == null)
        //            {
        //                return NotFound();
        //            }

        //            // Map data from the ViewModel back to the Submission entity
        //            submission.County = aclvlViewModel.County;
        //            submission.RouteNumber = aclvlViewModel.RouteNumber;
        //            submission.SubRouteNumber = aclvlViewModel.SubRouteNumber;
        //            submission.ProjectNumber = aclvlViewModel.ProjectNumber;
        //            submission.DateComplete = aclvlViewModel.DateComplete;
        //            submission.NatureOfChange = aclvlViewModel.NatureOfChange;
        //            submission.StartingMilePoint = aclvlViewModel.StartingMilePoint;
        //            submission.EndingMilePoint = aclvlViewModel.EndingMilePoint;
        //            submission.SignSystem = aclvlViewModel.SignSystem;
        //            submission.RailroadInv = aclvlViewModel.RailroadInv;
        //            submission.BridgeInv = aclvlViewModel.BridgeInv;
        //            submission.OtherBox = aclvlViewModel.OtherBox;
        //            submission.DateComplete = aclvlViewModel.DateComplete;
        //            // Map any other properties as needed

        //            // Retrieve the related RouteInfo entity
        //            var routeInfo = await _context.RouteInfo
        //                .FirstOrDefaultAsync(r => r.SubmissionID == id);

        //            if (routeInfo == null)
        //            {
        //                // If RouteInfo doesn't exist, create a new one
        //                routeInfo = new RouteInfo
        //                {
        //                    SubmissionID = id
        //                };
        //                _context.RouteInfo.Add(routeInfo);
        //            }

        //            // Map data from the ViewModel back to the RouteInfo entity
        //            routeInfo.AccessControl = aclvlViewModel.AccessControl;
        //            routeInfo.ThroughLanes = aclvlViewModel.ThroughLanes;
        //            routeInfo.CounterPeakLanes = aclvlViewModel.CounterPeakLanes;
        //            routeInfo.PeakLanes = aclvlViewModel.PeakLanes;
        //            routeInfo.ReverseLanes = aclvlViewModel.ReverseLanes;
        //            routeInfo.LaneWidth = aclvlViewModel.LaneWidth;
        //            routeInfo.MedianWidth = aclvlViewModel.MedianWidth;
        //            routeInfo.GradeWidth = aclvlViewModel.GradeWidth;
        //            routeInfo.PavementWidth = aclvlViewModel.PavementWidth;
        //            routeInfo.SpecialSys = aclvlViewModel.SpecialSys;
        //            routeInfo.FacilityType = aclvlViewModel.FacilityType;
        //            routeInfo.FederalAid = aclvlViewModel.FederalAid;
        //            routeInfo.FedForestHighway = aclvlViewModel.FedForestHighway;
        //            routeInfo.MedianType = aclvlViewModel.MedianType;
        //            routeInfo.NHS = aclvlViewModel.NHS;
        //            routeInfo.TruckRoute = aclvlViewModel.TruckRoute;
        //            routeInfo.GovIDOwnership = aclvlViewModel.GovIDOwnership;
        //            routeInfo.WVlegalClass = aclvlViewModel.WVlegalClass;
        //            routeInfo.FunctionalClass = aclvlViewModel.FunctionalClass;
        //            routeInfo.SurfaceTypeN = aclvlViewModel.SurfaceTypeN;
        //            routeInfo.MPSegmentStart = aclvlViewModel.MPSegmentStart;
        //            routeInfo.MPSegmentEnd = aclvlViewModel.MPSegmentEnd;
        //            // Map any other properties as needed

        //            // Update the entities in the context
        //            _context.Update(submission);
        //            _context.Update(routeInfo);

        //            // Save all changes to the database
        //            await _context.SaveChangesAsync();

        //            var item = await _oM79Context.OMTable.FirstOrDefaultAsync(e => e.Id == submission.OM79Id);

        //            if (item == null)
        //            {
        //                return NotFound();
        //            }

        //            var hubID = item.HubId;

        //            return RedirectToAction("EditPackage", "CENTRAL79HUB", new { id = hubID });
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            if (!aclvlViewModel.SubmissionID.HasValue || !SubmissionExists(aclvlViewModel.SubmissionID.Value))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //    }

        //    // If ModelState is invalid, re-populate dropdowns and return the view
        //    DropDowns();
        //    return View(aclvlViewModel);
        //}


        // GET: Submissions/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Submissions == null)
            {
                return NotFound();
            }

            // Fetch the submission entry by ID
            var submission = await _context.Submissions
                .FirstOrDefaultAsync(m => m.SubmissionID == id);
            if (submission == null)
            {
                return NotFound();
            }

            // Delete related BridgeRR entry
            var bridgeRR = await _context.BridgeRR
                .FirstOrDefaultAsync(b => b.RailKey == id);
            if (bridgeRR != null)
            {
                _context.BridgeRR.Remove(bridgeRR);
            }

            // Delete related RouteInfo entry
            var routeInfo = await _context.RouteInfo
                .FirstOrDefaultAsync(r => r.SubmissionID == id);
            if (routeInfo != null)
            {
                _context.RouteInfo.Remove(routeInfo);
            }

            // Finally, delete the Submission entry
            _context.Submissions.Remove(submission);

            // Save all changes
            await _context.SaveChangesAsync();

            // Redirect back to the specific EditPackage page
            var hubId = _oM79Context.OMTable
                .Where(o => o.Id == submission.OM79Id)
                .Select(o => o.HubId)
                .FirstOrDefault();

            return RedirectToAction("EditPackage", "CENTRAL79HUB", new { id = hubId });
        }



        private bool SubmissionExists(int id)
        {
            return (_context.Submissions?.Any(e => e.SubmissionID == id)).GetValueOrDefault();
        }

        public void DropDowns()
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

            List<SelectListItem> NatureDropdown = new()
            {
                   new SelectListItem {Text = "Addition", Value = "Addition"},
                  // new SelectListItem {Text = "Abandonment", Value = "Abandonment"},
                   new SelectListItem {Text = "Redesignation", Value = "Redesignation"},

            };
            NatureDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select Change Below" });
            ViewBag.NatureDropdown = NatureDropdown;

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

            List<SelectListItem> RRDropdown = new()
            {
                new SelectListItem {Text = "Yes", Value = "True"},
                new SelectListItem {Text = "No", Value = "False"},
            };
            RRDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.RRDropdown = RRDropdown;
            List<SelectListItem> BridgeDropdown = new()
            {
                new SelectListItem {Text = "Yes", Value = "True"},
                new SelectListItem {Text = "No", Value = "False"},
            };
            BridgeDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.BridgeDropdown = BridgeDropdown;

            List<SelectListItem> AccessDropdown = new()
            {
                new SelectListItem {Text = "No Control", Value = "No Control"},
                new SelectListItem {Text = "Limited Control", Value = "Limited Control"},
                new SelectListItem {Text = "Full Control", Value = "Full Control"},

            };
            AccessDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.AccessDropdown = AccessDropdown;

            List<SelectListItem> ReverseDropdown = new()
            {
                new SelectListItem {Text = "No Reversible Lanes", Value = "No Reversible Lanes"},
                new SelectListItem {Text = "One Reversible Lane", Value = "One Reversible Lane"},
                new SelectListItem {Text = "Two Reversible Lanes", Value = "Two Reversible Lanes"},
                new SelectListItem {Text = "More than Two Reversible Lanes", Value = "More than Two Reversible Lanes"},

            };
            ReverseDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.ReverseDropdown = ReverseDropdown;

            List<SelectListItem> SpecialDropdown = new()
            {
                new SelectListItem {Text = "Not on a Special System", Value = "Not on a Special System"},
                new SelectListItem {Text = "National Forest Highway System", Value = "National Forest Highway System"},
                new SelectListItem {Text = "Appalachian Development Highway", Value = "Appalachian Development Highway"},
                new SelectListItem {Text = "ARC Access Road", Value = "ARC Access Road"},
                new SelectListItem {Text = "Priority Primary", Value = "Priority Primary"},

            };
            SpecialDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.SpecialDropdown = SpecialDropdown;

            List<SelectListItem> FacilityDropdown = new()
            {
                new SelectListItem {Text = "One-Way Roadway", Value = "One-Way Roadway"},
                new SelectListItem {Text = "Two-Way Roadway", Value = "Two-Way Roadway"},
                new SelectListItem {Text = "One-Way Structure", Value = "One-Way Structure"},
                new SelectListItem {Text = "Two-Way Structure", Value = "Two-Way Structure"},
                new SelectListItem {Text = "Couplet", Value = "Couplet"},
                new SelectListItem {Text = "Non-Mainline", Value = "Non-Mainline"},
                new SelectListItem {Text = "Non-Inventory", Value = "Non-Inventory"},
                new SelectListItem {Text = "Planned/Unbuilt", Value = "Planned/Unbuilt"},

            };
            FacilityDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.FacilityDropdown = FacilityDropdown;

            List<SelectListItem> FederalDropdown = new()
            {
                new SelectListItem {Text = "Interstate", Value = "Interstate"},
                new SelectListItem {Text = "NHS", Value = "NHS"},
                new SelectListItem {Text = "STP", Value = "STP"},
                new SelectListItem {Text = "Inter-modal Connectors", Value = "Inter-modal Connectors"},
                new SelectListItem {Text = "Non-Federal-Aid", Value = "Non-Federal-Aid"},

            };
            FederalDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.FederalDropdown = FederalDropdown;

            List<SelectListItem> FedForestDropdown = new()
            {
                new SelectListItem {Text = "Not in a National Forest", Value = "Not in a National Forest"},
                new SelectListItem {Text = "Is a Federal Forest Highway", Value = "Is a Federal Forest Highway"},

            };
            FedForestDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.FedForestDropdown = FedForestDropdown;

            List<SelectListItem> MedianDropdown = new()
            {
                new SelectListItem {Text = "None", Value = "None"},
                new SelectListItem {Text = "Unprotected", Value = "Unprotected"},
                new SelectListItem {Text = "Curbed", Value = "Curbed"},
                new SelectListItem {Text = "Positive Barrier", Value = "Positive Barrier"},
                new SelectListItem {Text = "Positive Barrier - Flex", Value = "Positive Barrier - Flex"},
                new SelectListItem {Text = "Positive Barrier - Semi-Rigid", Value = "Positive Barrier - Semi-Rigid"},
                new SelectListItem {Text = "Positive Barrier - Rigid", Value = "Positive Barrier - Rigid"},

            };
            MedianDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.MedianDropdown = MedianDropdown;

            List<SelectListItem> NHSDropdown = new()
            {
                new SelectListItem {Text = "Not NHS", Value = "Not NHS"},
                new SelectListItem {Text = "On NHS - Not Connector", Value = "On NHS - Not Connector"},
                new SelectListItem {Text = "Major Airport Connector", Value = "Major Airport Connector"},
                new SelectListItem {Text = "Major Port Facility Connector", Value = "Major Port Facility Connector"},
                new SelectListItem {Text = "Major Amtrak Station", Value = "Major Amtrak Station"},
                new SelectListItem {Text = "Major Rail/Truck Terminal", Value = "Major Rail/Truck Terminal"},
                new SelectListItem {Text = "Major Inner-City Bus Terminal", Value = "Major Inner-City Bus Terminal"},
                new SelectListItem {Text = "Major Pub Transit Multi-Modal Pass Term", Value = "Major Pub Transit Multi-Modal Pass Term"},
                new SelectListItem {Text = "Major Pipeline Terminal", Value = "Major Pipeline Terminal"},
                new SelectListItem {Text = "Major Ferry Terminal", Value = "Major Ferry Terminal"},

            };
            NHSDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.NHSDropdown = NHSDropdown;

            List<SelectListItem> TruckDropdown = new()
            {
                new SelectListItem {Text = "Part of National Truck Network", Value = "Part of National Truck Network"},
                new SelectListItem {Text = "Truck Routes Designated State", Value = "Truck Routes Designated State"},
                new SelectListItem {Text = "Prohibited During Specific Hours", Value = "Prohibited During Specific Hours"},
                new SelectListItem {Text = "No Restriction on Use", Value = "No Restriction on Use"},

            };
            TruckDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.TruckDropdown = TruckDropdown;

            List<SelectListItem> GovDropdown = new()
            {
                new SelectListItem {Text = "State Highway Agency", Value = "State Highway Agency"},
                new SelectListItem {Text = "City or Municipal Highway Agency", Value = "City or Municipal Highway Agency"},
                new SelectListItem {Text = "State Park, Forest or Reservation Agency", Value = "State Park, Forest or Reservation Agency"},
                new SelectListItem {Text = "State Toll Authority", Value = "State Toll Authority"},
                new SelectListItem {Text = "Other (Please note in additional comments.)", Value = "Other"},

            };
            GovDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.GovDropdown = GovDropdown;

            List<SelectListItem> LegalDropdown = new()
            {
                new SelectListItem {Text = "Expressway", Value = "Expressway"},
                new SelectListItem {Text = "Trunk Line", Value = "Trunk Line"},
                new SelectListItem {Text = "Feeder", Value = "Feeder"},
                new SelectListItem {Text = "Essential Arterial", Value = "Essential Arterial"},
                new SelectListItem {Text = "Collector", Value = "Collector"},
                new SelectListItem {Text = "Land Access", Value = "Land Access"},
                new SelectListItem {Text = "Local Roads not Classified as Essential Arterial, Collector, or Land Access", Value = "Local Roads not Classified as Essential Arterial, Collector, or Land Access"},

            };
            LegalDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.LegalDropdown = LegalDropdown;

            List<SelectListItem> FuncDropdown = new()
            {
                new SelectListItem {Text = "RURAL: Principle Arterial - Interstate", Value = "RURAL: Principle Arterial - Interstate"},
                new SelectListItem {Text = "RURAL: Principle Arterial - Other", Value = "RURAL: Principle Arterial - Other"},
                new SelectListItem {Text = "RURAL: Principle Arterial - Freeways or Expressways", Value = "RURAL: Principle Arterial - Freeways or Expressways"},
                new SelectListItem {Text = "RURAL: Minor - Arterial", Value = "RURAL: Minor - Arterial"},
                new SelectListItem {Text = "RURAL: Major Collector", Value = "RURAL: Major Collector"},
                new SelectListItem {Text = "RURAL: Minor Collector", Value = "RURAL: Minor Collector"},
                new SelectListItem {Text = "RURAL: Local", Value = "RURAL: Local"},
                new SelectListItem {Text = "URBAN: Principle Arterial - Interstate", Value = "URBAN: Principle Arterial - Interstate"},
                new SelectListItem {Text = "URBAN: Principle Arterial - Other", Value = "URBAN: Principle Arterial - Other"},
                new SelectListItem {Text = "URBAN: Principle Arterial - Freeways or Expressways", Value = "URBAN: Principle Arterial - Freeways or Expressways"},
                new SelectListItem {Text = "URBAN: Minor - Arterial", Value = "URBAN: Minor - Arterial"},
                new SelectListItem {Text = "URBAN: Collector", Value = "URBAN: Collector"},
                new SelectListItem {Text = "URBAN: Minor Collector", Value = "URBAN: Minor Collector"},
                new SelectListItem {Text = "URBAN: Local", Value = "URBAN: Local"},

            };
            FuncDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.FuncDropdown = FuncDropdown;

            List<SelectListItem> SurfaceTypeDropdown = new()
            {
                new SelectListItem {Text = "Primitive", Value = "Primitive"},
                new SelectListItem {Text = "Unimproved", Value = "Unimproved"},
                new SelectListItem {Text = "Graded & Drained", Value = "Graded & Drained"},
                new SelectListItem {Text = "Soil Surface - Dirt", Value = "Soil Surface - Dirt"},
                new SelectListItem {Text = "Gravel or Stone", Value = "Gravel or Stone"},
                new SelectListItem {Text = "Bit. Surface Treated", Value = "Bit. Surface Treated"},
                new SelectListItem {Text = "Mixed Bit. < 7\" Combined Thickness", Value = "Mixed Bit. < 7\" Combined Thickness"},
                new SelectListItem {Text = "Mixed Bit. > 7\" Combined Thickness", Value = "Mixed Bit. > 7\" Combined Thickness"},
                new SelectListItem {Text = "Bit. Penet. > 7\" Combined Thickness", Value = "Bit. Penet. > 7\" Combined Thickness"},
                new SelectListItem {Text = "Bit. Penet. < 7\" Combined Thickness", Value = "Bit. Penet. < 7\" Combined Thickness"},
                new SelectListItem {Text = "Asphaltic Concrete", Value = "Asphaltic Concrete"},
                new SelectListItem {Text = "Concrete", Value = "Concrete"},
                new SelectListItem {Text = "Brick", Value = "Brick"},
            };
            SurfaceTypeDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
            ViewBag.SurfaceTypeDropdown = SurfaceTypeDropdown;
            /*     List<SelectListItem> SpanDropdown = new()
                 {
                     new SelectListItem {Text = "0", Value = "0"},
                     new SelectListItem {Text = "1", Value = "1"},
                     new SelectListItem {Text = "2", Value = "2"},
                     new SelectListItem {Text = "3", Value = "3"},
                     new SelectListItem {Text = "4", Value = "4"},
                     new SelectListItem {Text = "5", Value = "5"},
                     new SelectListItem {Text = "6", Value = "6"},
                     new SelectListItem {Text = "7", Value = "7"},
                     new SelectListItem {Text = "8", Value = "8"},
                     new SelectListItem {Text = "9", Value = "9"},
                     new SelectListItem {Text = "10", Value = "10"},
                     new SelectListItem {Text = "11", Value = "11"},
                     new SelectListItem {Text = "12", Value = "12"},
                     new SelectListItem {Text = "13", Value = "13"},
                     new SelectListItem {Text = "14", Value = "14"},
                     new SelectListItem {Text = "15", Value = "15"},
                     new SelectListItem {Text = "16", Value = "16"},
                     new SelectListItem {Text = "17", Value = "17"},
                     new SelectListItem {Text = "18", Value = "18"},
                     new SelectListItem {Text = "19", Value = "19"},
                     new SelectListItem {Text = "20", Value = "20"},
                     new SelectListItem {Text = "21", Value = "21"},
                     new SelectListItem {Text = "22", Value = "22"},
                     new SelectListItem {Text = "23", Value = "23"},
                     new SelectListItem {Text = "24", Value = "24"},
                     new SelectListItem {Text = "25", Value = "25"},
                     new SelectListItem {Text = "26", Value = "26"},
                     new SelectListItem {Text = "27", Value = "27"},
                     new SelectListItem {Text = "28", Value = "28"},
                     new SelectListItem {Text = "29", Value = "29"},
                     new SelectListItem {Text = "30", Value = "30"}
                 };
                 SpanDropdown.Insert(0, new SelectListItem { Value = "", Text = "Select" });
                 ViewBag.SpanDropdown = SpanDropdown;
            */
        }
        // public IActionResult GetUnitAsBuiltPartial()
        // {
        // Logic to fetch data or perform any other operations based on the selected value

        //  return PartialView("_UnitAsBuilt");

        public IActionResult GetUnitAsBuiltPartial()
        {
            // Logic to fetch data or perform any other operations based on the selected value
            // For example, create a new instance of the ViewModel and pass it to the partial view

            DropDowns();
            return PartialView("_UnitAsBuilt");
        }
        // }
        public IActionResult GetBridgeRRPartial()
        {
            // Logic to fetch data or perform any other operations based on the selected value
            DropDowns();
            return PartialView("_BridgeRR");
        }
        public IActionResult GetRRPartial()
        {
            // Logic to fetch data or perform any other operations based on the selected value
            DropDowns();
            return PartialView("_Railroad");
        }
        public IActionResult RedirectToHomeIndex()
        {
            // Redirect to the Index action of the Home controller
            return RedirectToAction("Index", "Home");
        }


        /*  public IActionResult GetSpanPartial(string NumberOfSpans)
          {
              ViewBag.NumberOfSpans = NumberOfSpans;
              Console.WriteLine(ViewBag.NumberOfSpans);
              return PartialView("_Span");
          }
        */
    }
}