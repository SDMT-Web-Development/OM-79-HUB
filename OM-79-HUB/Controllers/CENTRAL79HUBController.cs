﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using ITfoxtec.Identity.Saml2.Schemas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore; 
using OM_79_HUB.Components;
using OM_79_HUB.Data;
using OM_79_HUB.Models;
using OM_79_HUB.Models.DB.OM79Hub;
using OM_79_HUB.Models.DB.PJ103.ViewModels;
using OM79.Models.DB;
using X.PagedList;

namespace OM_79_HUB.Controllers
{
    public class CENTRAL79HUBController : Controller
    {
        private readonly OM_79_HUBContext _context;
        private readonly OM79Context _OMcontext;
        private readonly Pj103Context _PJcontext;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public CENTRAL79HUBController(OM_79_HUBContext context, OM79Context oM79Context, Pj103Context pj103Context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _OMcontext = oM79Context;
            _PJcontext = pj103Context;
            _httpContextAccessor = httpContextAccessor; 
        }

        //[HttpPost]
        //public async Task<IActionResult> CreateAdminAuto()
        //{
        //     Create a hardcoded AdminData object
        //    var newAdminData = new AdminData
        //    {
        //        FirstName = "Ethan",          // Hardcoded value
        //        LastName = "Johnson",            // Hardcoded value
        //        ENumber = "E122059",           // Hardcoded value
        //        StateEmail = "ethan.m.johnson@wv.gov",  // Hardcoded value
        //        DateEstablished = DateTime.Now,
        //        DistrictAdmin = false,        // Hardcoded value
        //        StatewideAdmin = false,      // Hardcoded value
        //        SystemAdmin = true,         // Hardcoded value
        //        DistrictNumber = null           // Hardcoded value
        //    };

        //     Assuming you have a _context (DbContext) injected into the controller
        //    _context.AdminData.Add(newAdminData);

        //     Save the changes to the database
        //    await _context.SaveChangesAsync();

        //     Return a success response or redirect
        //    return Ok("Admin data created successfully!");
        //}


        [HttpGet]
        public async Task<IActionResult> EditPackage(int? id)
        {
            // Use the id parameter to load the specific package data from the database
            var om = _context.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);

            // If the package data with the given ID is not found, return a 404 Not Found response
            if (om == null)
            {
                return NotFound();
            }

            // Initial check on the OM79Workflow's NextStep and current user's access
            var om79Workflow = await _context.OM79Workflow.FirstOrDefaultAsync(w => w.HubID == id);
            var currentUser = User.Identity.Name;
            if (om79Workflow?.NextStep == "FinishEdits" && om.UserId == currentUser)
            {
                // Bypass further permission checks if this condition is met
                ViewBag.TestUniqueID = id;
                return View(om);
            }

            //Need to only allow someone to access this when the workflow step is currently with this 
            var hub = await _context.CENTRAL79HUB.FirstOrDefaultAsync(h => h.OMId == id);
            var user = User.Identity.Name;
            var validUser = await checkComplexPermission(user, hub);
            if (!validUser)
            {
                return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
            }

            ViewBag.TestUniqueID = id;


            // Return the view with the loaded package data
            return View(om);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustSegmentCount([FromBody] SegmentAdjustmentModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.action) || model.omTableId <= 0)
            {
                return BadRequest("Invalid request data.");
            }

            int? uniqueID = HttpContext.Session.GetInt32("UniqueID");
            if (uniqueID == null)
            {
                return NotFound("OMID not found.");
            }

            try
            {
                // Retrieve the specific PJ103Workflow entry based on the OMTable entry ID passed from the client
                var pj103Workflow = await _OMcontext.PJ103Workflow.FirstOrDefaultAsync(w => w.OMID == model.omTableId);
                if (pj103Workflow == null)
                {
                    return NotFound("PJ103Workflow not found.");
                }

                // Adjust the segment count based on the action
                if (model.action == "increase")
                {
                    pj103Workflow.NumberOfSegments = (pj103Workflow.NumberOfSegments ?? 0) + 1;
                }
                else if (model.action == "decrease" && pj103Workflow.NumberOfSegments > 0)
                {
                    pj103Workflow.NumberOfSegments -= 1;
                }
                else
                {
                    return BadRequest("Invalid action or segment count.");
                }

                await _OMcontext.SaveChangesAsync();

                // Retrieve the current OMTable entry based on the passed ID
                var currentItem = await _OMcontext.OMTable.FirstOrDefaultAsync(w => w.Id == model.omTableId);
                if (currentItem == null)
                {
                    return NotFound("OMTable entry not found.");
                }

                // Retrieve submissions related to the current OMTable entry
                var PJsAttachedToItem = await _PJcontext.Submissions.Where(e => e.OM79Id == currentItem.Id).ToListAsync();

                var requiredCount = pj103Workflow.NumberOfSegments;
                var currentCount = PJsAttachedToItem.Count;

                // Check if the current number of segments meets or exceeds the required number of segments
                if (currentCount >= requiredCount)
                {
                    // Retrieve the CENTRAL79HUB entry
                    var currentHub = await _context.CENTRAL79HUB.FirstOrDefaultAsync(e => e.OMId == currentItem.HubId);
                    if (currentHub == null)
                    {
                        return NotFound("CENTRAL79HUB entry not found.");
                    }

                    // Retrieve all OMTable entries attached to this hub
                    var OMsAttachedToHub = await _OMcontext.OMTable.Where(e => e.HubId == currentHub.OMId).ToListAsync();

                    // Retrieve the OM79Workflow entry related to this hub
                    var currentOmWorkflow = await _context.OM79Workflow.FirstOrDefaultAsync(e => e.HubID == currentHub.OMId);
                    if (currentOmWorkflow == null)
                    {
                        return NotFound("OM79Workflow entry not found.");
                    }

                    var requiredOmCount = currentOmWorkflow.NumberOfItems;
                    var currentOmCount = OMsAttachedToHub.Count;

                    if (currentOmCount >= requiredOmCount)
                    {
                        // Update the next step to "FinishSubmit" if all items and segments are complete
                        //currentOmWorkflow.NextStep = "FinishSubmit";
                        currentOmWorkflow.NextStep = "FinishEdits";

                    }
                    else
                    {
                        // Update the next step to "AddItem" if there are still more items to be added
                        currentOmWorkflow.NextStep = "AddItem";
                    }

                    // Save changes to OM79Workflow
                    await _context.SaveChangesAsync();
                }

                // If the segment count hasn't met the requirement, leave the next step unchanged

                return Ok();
            }
            catch (Exception ex)
            {
                // Log the exception details for debugging
                Console.WriteLine($"Error adjusting segment count: {ex.Message}");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        // Model to handle the segment adjustment
        public class SegmentAdjustmentModel
        {
            public string action { get; set; } // 'increase' or 'decrease'
            public int omTableId { get; set; } // The ID of the OMTable entry
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustItemCount([FromBody] ItemCountAdjustmentModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Action))
            {
                return BadRequest("Invalid request data.");
            }

            int? uniqueID = HttpContext.Session.GetInt32("UniqueID");
            if (uniqueID == null)
            {
                return NotFound("Unique ID not found.");
            }
            
            try
            {
                // Retrieve the OM79Workflow entry based on the HubID stored in the session
                var om79Workflow = await _context.OM79Workflow.FirstOrDefaultAsync(w => w.HubID == uniqueID.Value);
                if (om79Workflow == null)
                {
                    return NotFound("OM79Workflow not found.");
                }

                if (model.Action == "increase")
                {
                    om79Workflow.NumberOfItems = (om79Workflow.NumberOfItems ?? 0) + 1;
                }
                else if (model.Action == "decrease" && (om79Workflow.NumberOfItems ?? 1) > 1)
                {
                    om79Workflow.NumberOfItems = (om79Workflow.NumberOfItems ?? 1) - 1;
                }
                else
                {
                    return BadRequest("Invalid action or item count.");
                }

                // Save changes after modifying the item count
                await _context.SaveChangesAsync();

                // Check if the current number of items matches the required number of items
                var currentCount = await _OMcontext.OMTable.Where(e => e.HubId ==  uniqueID.Value).CountAsync();
                var requiredCount = om79Workflow.NumberOfItems ?? 0; // Assuming RequiredNumberOfItems is stored in the workflow

                if (currentCount >= requiredCount)
                {
                    // Update the next step to "FinishSubmit" if all items are complete
                    //om79Workflow.NextStep = "FinishSubmit";
                    om79Workflow.NextStep = "FinishEdits";

                }
                else
                {
                    // Update the next step to "AddItem" if more items are still needed
                    om79Workflow.NextStep = "AddItem";
                }

                // Save the changes after updating the next step
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                // Log the exception details for debugging
                Console.WriteLine($"Error adjusting item count: {ex.Message}");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        public class ItemCountAdjustmentModel
        {
            public string Action { get; set; }
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmAllChanges(int id)
        {
            try
            {
                // Retrieve the OM79Workflow entry by OMId
                var om79Workflow = await _context.OM79Workflow.FirstOrDefaultAsync(w => w.HubID == id);
                if (om79Workflow == null)
                {
                    return NotFound("OM79Workflow entry not found.");
                }

                // Update the NextStep to "FinishSubmit"
                om79Workflow.NextStep = "FinishSubmit";

                // Save changes to the database
                await _context.SaveChangesAsync();

                // Redirect or return a success response
                return RedirectToAction("Details", new { id = om79Workflow.HubID });
            }
            catch (Exception ex)
            {
                // Log the exception details for debugging
                Console.WriteLine($"Error confirming all changes: {ex.Message}");
                return StatusCode(500, "An internal server error occurred.");
            }
        }
        public void sendMissingUsersEmailToDistrictManager(List<string> missingRoles, int? districtToSendTo)
        {
            // Find the District System Administrator for this district
            var districtAdmins = _context.AdminData
                                 .Where(u => u.DistrictNumber == districtToSendTo && u.DistrictAdmin == true)
                                 .ToList();

            if (!districtAdmins.Any())
            {
                // No district administrators found, exit early
                return;
            }

            foreach (var admin in districtAdmins)
            {
                var automatedEmail = _httpContextAccessor.HttpContext?.Items["AutomatedEmailAddress"]?.ToString() ?? "DOTPJ103Srv@wv.gov";

                var message = new MailMessage
                {
                    From = new MailAddress(automatedEmail)
                };
                message.To.Add(admin.StateEmail);
                message.CC.Add("ethan.m.johnson@wv.gov");

                message.Subject = $"OM79 System Alert for District {districtToSendTo} - Action Required";
                message.Body = $@"
                            <html>
                            <body style='font-family: Arial, sans-serif; color: #333; background-color: #f8f8f8; padding: 20px;'>
                                <div style='max-width: 600px; margin: auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ddd;'>
            
                                    <!-- Header -->
                                    <h2 style='text-align: center; color: #d9534f;'>OM79 System Missing Roles</h2>
                                    <p style='font-size: 14px; text-align: center;'>District {districtToSendTo}</p>
                                    <hr style='border: 0; border-top: 1px solid #ddd;'>

                                    <!-- Greeting -->
                                    <p>Hello <strong>{admin.FirstName}</strong>,</p>

                                    <!-- Introduction -->
                                    <p>The system has detected that the following required roles are currently unassigned in <strong>District {districtToSendTo}</strong>:</p>

                                    <!-- Missing Roles List -->
                                    <ul style='background: #fff3cd; padding: 15px; border-radius: 8px; border-left: 5px solid #d9534f;'>
                                        {string.Join("", missingRoles.Select(role => $"<li><strong>{role}</strong></li>"))}
                                    </ul>

                                    <!-- Action Needed -->
                                    <p>An OM79 entry has just been submitted for review in <strong>District {districtToSendTo}</strong>. However, without these required roles assigned in the system, the review process will be stalled until someone is added to continue the workflow.</p>

                                    <!-- Call to Action: Add Users Link -->
                                    <p style='text-align: center; margin-top: 20px;'>
                                        <a href='https://dotapps.transportation.wv.gov/om79/AccountSystemAndWorkflow/DistrictAccountSystem' 
                                           style='background: #007BFF; color: white; text-decoration: none; padding: 12px 20px; border-radius: 5px; font-size: 16px; display: inline-block;'>
                                           Add Missing User(s)
                                        </a>
                                    </p>

                                    <hr style='border: 0; border-top: 1px solid #ddd; margin-top: 20px;'>

                                    <!-- Footer -->
                                    <p style='font-size: 12px; text-align: center; color: #667;'>Thank you,<br>OM79 Automated System</p>
                                </div>
                            </body>
                            </html>";

                message.IsBodyHtml = true;
                var client = new SmtpClient
                {
                    Host = "10.204.145.32",
                    Port = 25,
                    EnableSsl = false,
                    Credentials = new NetworkCredential("ethan.m.johnson@wv.gov", "trippononemails")
                };
                client.Send(message);
            }
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




                var requiredRoles = new HashSet<string>
                {
                    "Bridge Engineer",
                    "Traffic Engineer",
                    "Maintenance Engineer",
                    "Construction Engineer",
                    "Right Of Way Manager"
                };
                var existingRoles = allDistrictUsers.SelectMany(GetUserRoles).Distinct().ToHashSet();
                var missingRoles = requiredRoles.Except(existingRoles).ToList();
                if (missingRoles.Any()) // If at least one role is missing
                {
                    sendMissingUsersEmailToDistrictManager(missingRoles, districtToSendTo);
                }


                    foreach (var userRoleEntry in userRolesDictionary)
                {
                    var user = userRoleEntry.Value.User;
                    var roles = userRoleEntry.Value.Roles;
                    var roleResponsibilities = new Dictionary<string, string>
                    {
                        { "Bridge Engineer", "Responsible for reviewing OM-79 package and attachments for accuracy and completeness." },
                        { "Traffic Engineer", "Responsible for reviewing OM-79 package and attachments for accuracy and completeness." },
                        { "Maintenance Engineer", "Responsible for reviewing OM-79 package and attachments for accuracy and completeness." },
                        { "Construction Engineer", "Responsible for reviewing OM-79 package and attachments for accuracy and completeness." },
                        { "Right Of Way Manager", "Responsible for reviewing OM-79 package and attachments for accuracy and completeness." }
                    };

                    var roleDetails = string.Join("<br>", roles.Select(role =>
                                                $"<strong>{role}:</strong> {roleResponsibilities.GetValueOrDefault(role, "Responsible for reviewing the OM79 entry.")}"));


                    var automatedEmail = _httpContextAccessor.HttpContext?.Items["AutomatedEmailAddress"]?.ToString() ?? "DOTPJ103Srv@wv.gov";

                    var message = new MailMessage
                    {
                        From = new MailAddress(automatedEmail)
                    };


                    message.To.Add(user.Email);
                    message.CC.Add("ethan.m.johnson@wv.gov");

                    message.Subject = $"OM79 Submission - Review Required for District {user.District}";

                    message.Body = $@"
                            <html>
                            <body style='font-family: Arial, sans-serif; color: #333; background-color: #f8f8f8; padding: 20px;'>
                                <div style='max-width: 600px; margin: auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ddd;'>
                
                                    <!-- Header -->
                                    <h2 style='text-align: center; color: #0056b3;'>OM79 Submission Requires Your Review</h2>
                                    <p style='font-size: 14px; text-align: center;'>District {user.District}</p>
                                    <hr style='border: 0; border-top: 1px solid #ddd;'>

                                    <!-- Greeting -->
                                    <p>Hello <strong>{user.FirstName}</strong>,</p>

                                    <!-- Introduction -->
                                    <p>An OM79 entry has been submitted for review in District {user.District}. You are responsible for reviewing the following sections based on your assigned role(s):</p>

                                    <!-- Role Responsibilities Table -->
                                     <p><strong>Your Responsibilities:</strong></p>
                                    <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF;'>
                                        {string.Join("", roles.Select(role => $"<p><strong style='color: #0056b3;'>{role}:</strong> {roleResponsibilities.GetValueOrDefault(role, "Responsible for reviewing the OM79 entry.")}</p>"))}
                                    </div>


                                    <!-- Call to Action -->
                                    <p style='text-align: center; margin-top: 20px;'>
                                        <a href='https://dotapps.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}' 
                                            style='background: #28a745; color: white; text-decoration: none; padding: 12px 20px; border-radius: 5px; font-size: 16px; display: inline-block;'>
                                            Review & Sign OM79 Entry
                                        </a>
                                    </p>

                                    <!-- Important Note -->
                                    <p style='color: #0056b3; font-weight: bold; text-align: center; margin-top: 15px;'>
                                        The signature form is located at the bottom of the page at the link above.
                                    </p>

                                    <hr style='border: 0; border-top: 1px solid #ddd; margin-top: 20px;'>

                                    <!-- Footer -->
                                    <p style='font-size: 12px; text-align: center; color: #667;'>Note: If you hold multiple roles, you must sign separately for each role.</p>
                                    <p style='font-size: 12px; text-align: center; color: #667;'>Thank you,<br>OM79 Automated System</p>
                                </div>
                            </body>
                            </html>";

                    //message.Subject = $"OM79 Submission - Review Required for District {user.District}";
                    //message.Body = $@"
                    //                <html>
                    //                <body>
                    //                    <p>Hello {user.FirstName},</p>
                    //                    <p>An OM79 entry has been submitted for review in District {user.District}. Based on your assigned role(s), you are responsible for reviewing the following aspects:</p>
                    //                    <p>{roleDetails}</p>
                    //                    <p>Please click the link below to access and sign the OM79 entry:</p>
                    //                    <p><a href='https://dotapps.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}'>Review OM79 Entry</a></p>
                    //                    <p><strong>Note:</strong> If you hold multiple roles, you must sign separately for each role.</p>
                    //                    <p style='color: red; font-weight: bold;'>The signature form is located at the bottom of the page inside the pink box.</p>
                    //                    <p>Thank you,<br>OM79 Automated System</p>
                    //                </body>
                    //                </html>";

                    message.IsBodyHtml = true;


                    var client = new SmtpClient
                    {
                        Host = "10.204.145.32",
                        Port = 25,
                        EnableSsl = false,
                        Credentials = new NetworkCredential("ethan.m.johnson@wv.gov", "trippononemails")
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

            Console.WriteLine($"Current Step: {currentStep}"); // Or use a logging framework
            Console.WriteLine($"Normalized User: {normalizedUser}");
            Console.WriteLine($"Normalized Hub User: {normalizedHubUser}");
        
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
                    isUserAuthorized = await _context.UserData
                        .AnyAsync(u => u.HDS && u.ENumber.ToLower() == normalizedUser);
                    break;

                case "SubmittedToCentralGIS":
                    // Allow access if the current user's ENumber matches a UserData entry where HDS is true
                    isUserAuthorized = await _context.UserData
                        .AnyAsync(u => u.GISManager && u.ENumber.ToLower() == normalizedUser);
                    break;

                case "SubmittedBackToDistrictManager":
                case "SubmittedBackToDistrictManagerFromOperations":
                    // Allow access if the current user's ENumber matches a UserData entry where District matches and they are a DistrictManager
                    isUserAuthorized = await _context.UserData
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
        public async Task<IActionResult> ArchiveOM79(int id, string IDNumber)
        {
            // Validate that the IDNumber is not null or empty
            if (string.IsNullOrWhiteSpace(IDNumber))
            {
                // Add an error message to the ModelState
                ModelState.AddModelError("IDNumber", "The archive link is required.");
                return RedirectToAction(nameof(Index)); // Redirect to an appropriate action
            }

            var om79Entry = await _context.CENTRAL79HUB.FindAsync(id);
            if (om79Entry == null)
            {
                return RedirectToAction(nameof(Index)); // Redirect to an appropriate action
            }

            // Archive the CENTRAL79HUB entry
            om79Entry.IsArchive = true;

            // Set workflow step to Archived
            om79Entry.WorkflowStep = "Archived";

            // Update the IDNumber field with the provided link
            om79Entry.IDNumber = IDNumber;

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





        // GET: CENTRAL79HUB/ArchivedIndexRescinded
        public async Task<IActionResult> ArchivedIndexRescinded(string searchUserId, int? page)
        {
            //Need to only allow for signees to access this
            var currentUser = User.Identity.Name;
            var validUser = await CheckForAccessPermission(currentUser);
            if (!validUser)
            {
                return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
            }

            ViewData["CurrentFilter"] = searchUserId;

            // Filter the records where IsArchive is true and the workflow step is "CancelledRequestArchive"
            var central79HubEntries = from m in _context.CENTRAL79HUB
                                      where m.IsArchive == true && m.WorkflowStep == "CancelledRequestArchive"
                                      select m;

            if (!String.IsNullOrEmpty(searchUserId))
            {
                central79HubEntries = central79HubEntries.Where(s => s.UserId.ToLower().Contains(searchUserId.ToLower()));
            }

            int pageNumber = (page ?? 1);
            var pagedCentral79HubEntries = await central79HubEntries.ToPagedListAsync(pageNumber, 50);

            return View(pagedCentral79HubEntries);
        }

        private async Task<bool> CheckForAccessPermission(string currentUser)
        {
            if (string.IsNullOrEmpty(currentUser))
            {
                return false;
            }

            // Normalize the ENumber (assuming case-insensitive)
            string normalizedENumber = currentUser.Replace("EXECUTIVE\\", "").Trim().ToLower();

            // Check in AdminData
            bool isAdmin = await _context.AdminData
                .AnyAsync(a => a.ENumber.ToLower() == normalizedENumber);

            if (isAdmin)
            {
                return true;
            }

            // Check in UserData
            bool isUser = await _context.UserData
                .AnyAsync(u => u.ENumber.ToLower() == normalizedENumber);

            return isUser;
        }

        // GET: CENTRAL79HUB/ArchivedIndex
        public async Task<IActionResult> ArchivedIndex(string searchUserId, int? page)
        {
            //Need to only allow for signees to access this
            var currentUser = User.Identity.Name;
            var validUser = await CheckForAccessPermission(currentUser);
            if (!validUser)
            {
                return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
            }
            
            ViewData["CurrentFilter"] = searchUserId;

            // Filter the records where IsArchive is true
            var central79HubEntries = from m in _context.CENTRAL79HUB
                                      where m.IsArchive == true && m.WorkflowStep != "CancelledRequestArchive"
                                      select m;

            if (!String.IsNullOrEmpty(searchUserId))
            {
                central79HubEntries = central79HubEntries.Where(s => s.UserId.Contains(searchUserId));
            }

            int pageNumber = (page ?? 1);
            var pagedCentral79HubEntries = await central79HubEntries.ToPagedListAsync(pageNumber, 50);

            return View(pagedCentral79HubEntries);
        }



        //// GET: CENTRAL79HUB/SignIndex
        //public async Task<IActionResult> SignIndex(int? page)
        //{
        //    // Check if the user has permission to access this page
        //    var currentUser = User.Identity.Name;
        //    var validUser = await CheckForAccessPermission(currentUser);
        //    if (!validUser)
        //    {
        //        return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
        //    }

        //    // Normalize user ID and retrieve all roles
        //    var normalizedUser = currentUser.Replace("EXECUTIVE\\", "").Trim();
        //    var roles = await _context.UserData.Where(e => e.ENumber == normalizedUser).ToListAsync();
        //    List<string> userRoles = new List<string>();
        //    List<int?> userDistricts = new List<int?>();


        //    foreach (var role in roles)
        //    {
        //        if (role.BridgeEngineer)
        //        {
        //            userRoles.Add("BridgeEngineer");
        //            userDistricts.Add(role.District);
        //            Console.WriteLine("Role: BridgeEngineer, District: " + role.District);
        //        }
        //        if (role.TrafficEngineer)
        //        {
        //            userRoles.Add("TrafficEngineer");
        //            userDistricts.Add(role.District);
        //            Console.WriteLine("Role: TrafficEngineer, District: " + role.District);
        //        }
        //        if (role.MaintenanceEngineer)
        //        {
        //            userRoles.Add("MaintenanceEngineer");
        //            userDistricts.Add(role.District);
        //            Console.WriteLine("Role: MaintenanceEngineer, District: " + role.District);
        //        }
        //        if (role.ConstructionEngineer)
        //        {
        //            userRoles.Add("ConstructionEngineer");
        //            userDistricts.Add(role.District);
        //            Console.WriteLine("Role: ConstructionEngineer, District: " + role.District);
        //        }
        //        if (role.RightOfWayManager)
        //        {
        //            userRoles.Add("RightOfWayManager");
        //            userDistricts.Add(role.District);
        //            Console.WriteLine("Role: RightOfWayManager, District: " + role.District);
        //        }
        //        if (role.DistrictManager)
        //        {
        //            userRoles.Add("DistrictManager");
        //            userDistricts.Add(role.District);
        //            Console.WriteLine("Role: DistrictManager, District: " + role.District);
        //        }

        //        if (role.HDS)
        //        {
        //            userRoles.Add("HDS");
        //            Console.WriteLine("Role: HDS");
        //        }
        //        if (role.GISManager)
        //        {
        //            userRoles.Add("GISManager");
        //            Console.WriteLine("Role: GISManager");
        //        }
        //        if (role.Chief)
        //        {
        //            userRoles.Add("Chief");
        //            Console.WriteLine("Role: Chief");
        //        }
        //        if (role.RegionalEngineer)
        //        {
        //            userRoles.Add("RegionalEngineer");
        //            Console.WriteLine("Role: RegionalEngineer, Districts: " + role.DistrictsForRegionalEngineer);

        //            // Parse the comma-separated list of districts for RegionalEngineer
        //            if (!string.IsNullOrEmpty(role.DistrictsForRegionalEngineer))
        //            {
        //                var districts = role.DistrictsForRegionalEngineer.Split(',')
        //                                    .Select(d => int.TryParse(d.Trim(), out int district) ? (int?)district : null)
        //                                    .Where(d => d.HasValue)
        //                                    .ToList();
        //                userDistricts.AddRange(districts);
        //            }
        //        }

        //        if (role.DirectorOfOperations)
        //        {
        //            userRoles.Add("DirectorOfOperations");
        //            Console.WriteLine("Role: DirectorOfOperations");
        //        }
        //        if (role.DeputySecretary)
        //        {
        //            userRoles.Add("DeputySecretary");
        //            Console.WriteLine("Role: DeputySecretary");
        //        }
        //    }


        //    // Build a query for entries the user should see
        //    var entriesQuery = _context.CENTRAL79HUB.AsQueryable();

        //    // Filter entries based on roles and districts
        //    entriesQuery = entriesQuery.Where(e =>
        //        (e.WorkflowStep == "SubmittedToDistrict" && userDistricts.Contains(e.District)) ||
        //        (e.WorkflowStep == "SubmittedToDistrictManager" && userRoles.Contains("DistrictManager")) ||
        //        (e.WorkflowStep == "SubmittedBackToDistrictManagerFromOperations" && userRoles.Contains("DistrictManager")) ||
        //        (e.WorkflowStep == "SubmittedBackToDistrictManager" && userRoles.Contains("DistrictManager")) ||
        //        (e.WorkflowStep == "SubmittedToCentralHDS" && userRoles.Contains("HDS")) ||
        //        (e.WorkflowStep == "SubmittedToCentralGIS" && userRoles.Contains("GISManager")) ||
        //        (e.WorkflowStep == "SubmittedToRegionalEngineer" && userRoles.Contains("RegionalEngineer")) ||
        //        (e.WorkflowStep == "SubmittedToDirectorOfOperations" && userRoles.Contains("DirectorOfOperations")) ||
        //        (e.WorkflowStep == "SubmittedToCentralChief" && userRoles.Contains("Chief"))
        //    );

        //    // Order entries to group them appropriately

        //    // Paginate the results with 50 records per page
        //    int pageNumber = page ?? 1;
        //    var pagedEntries = await entriesQuery.ToPagedListAsync(pageNumber, 50);

        //    // Return the view with the paginated results
        //    return View(pagedEntries);
        //}

        public async Task<IActionResult> SignIndex(int? page)
        {
            var currentUser = User.Identity.Name;
            var validUser = await CheckForAccessPermission(currentUser);
            if (!validUser)
            {
                return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
            }

            var normalizedUser = currentUser.Replace("EXECUTIVE\\", "").Trim();
            var roles = await _context.UserData.Where(e => e.ENumber == normalizedUser).ToListAsync();
            List<UserRole> userRoles = new List<UserRole>();

            foreach (var role in roles)
            {
                if (role.BridgeEngineer)
                    userRoles.Add(new UserRole("BridgeEngineer", new List<int?> { role.District }));
                if (role.TrafficEngineer)
                    userRoles.Add(new UserRole("TrafficEngineer", new List<int?> { role.District }));
                if (role.MaintenanceEngineer)
                    userRoles.Add(new UserRole("MaintenanceEngineer", new List<int?> { role.District }));
                if (role.ConstructionEngineer)
                    userRoles.Add(new UserRole("ConstructionEngineer", new List<int?> { role.District }));
                if (role.RightOfWayManager)
                    userRoles.Add(new UserRole("RightOfWayManager", new List<int?> { role.District }));
                if (role.DistrictManager)
                    userRoles.Add(new UserRole("DistrictManager", new List<int?> { role.District }));

                if (role.RegionalEngineer)
                {
                    var districts = role.DistrictsForRegionalEngineer?.Split(',')
                                       .Select(d => int.TryParse(d.Trim(), out int district) ? (int?)district : null)
                                       .Where(d => d.HasValue)
                                       .ToList() ?? new List<int?>();

                    userRoles.Add(new UserRole("RegionalEngineer", districts));
                }

                if (role.HDS) userRoles.Add(new UserRole("HDS"));
                if (role.GISManager) userRoles.Add(new UserRole("GISManager"));
                if (role.Chief) userRoles.Add(new UserRole("Chief"));
                if (role.DirectorOfOperations) userRoles.Add(new UserRole("DirectorOfOperations"));
                if (role.DeputySecretary) userRoles.Add(new UserRole("DeputySecretary"));
            }

            if (!userRoles.Any())
            {
                return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
            }

            // Collect all entries
            var allEntries = new List<CENTRAL79HUB>();

            int pageNumber = page ?? 1;
            int pageSize = 50;

            // For roles with district-based permissions
            var districtRoles = new[] { "BridgeEngineer", "TrafficEngineer", "MaintenanceEngineer", "ConstructionEngineer", "RightOfWayManager", "DistrictManager", "RegionalEngineer" };

            var userDistricts = userRoles
                .Where(r => districtRoles.Contains(r.RoleName))
                .SelectMany(r => r.Districts)
                .ToList();

            // Build a queryable to collect entries based on roles
            IQueryable<CENTRAL79HUB> query = _context.CENTRAL79HUB.Where(entry => false); // Start with an empty query

            foreach (var role in userRoles)
            {
                switch (role.RoleName)
                {
                    case "BridgeEngineer":
                    case "TrafficEngineer":
                    case "MaintenanceEngineer":
                    case "ConstructionEngineer":
                    case "RightOfWayManager":
                        query = query.Union(
                            _context.CENTRAL79HUB.Where(entry => entry.WorkflowStep == "SubmittedToDistrict" && role.Districts.Contains(entry.District))
                        );
                        break;

                    case "DistrictManager":
                        query = query.Union(
                            _context.CENTRAL79HUB.Where(entry =>
                                (entry.WorkflowStep == "SubmittedToDistrictManager" || entry.WorkflowStep == "SubmittedBackToDistrictManager" || entry.WorkflowStep == "SubmittedBackToDistrictManagerFromOperations")
                                && role.Districts.Contains(entry.District))
                        );
                        break;

                    case "HDS":
                        query = query.Union(
                            _context.CENTRAL79HUB.Where(entry => entry.WorkflowStep == "SubmittedToCentralHDS")
                        );
                        break;

                    case "GISManager":
                        query = query.Union(
                            _context.CENTRAL79HUB.Where(entry => entry.WorkflowStep == "SubmittedToCentralGIS")
                        );
                        break;

                    case "RegionalEngineer":
                        query = query.Union(
                            _context.CENTRAL79HUB.Where(entry => entry.WorkflowStep == "SubmittedToRegionalEngineer" && role.Districts.Contains(entry.District))
                        );
                        break;

                    case "DirectorOfOperations":
                        query = query.Union(
                            _context.CENTRAL79HUB.Where(entry => entry.WorkflowStep == "SubmittedToDirectorOfOperations")
                        );
                        break;

                    case "Chief":
                        query = query.Union(
                            _context.CENTRAL79HUB.Where(entry => entry.WorkflowStep == "SubmittedToCentralChief")
                        );
                        break;

                        // Add additional roles as needed
                }
            }

            // Execute the query and get distinct entries
            var distinctEntries = await query.Distinct().ToListAsync();

            // Paginate the combined entries
            var pagedEntries = distinctEntries.OrderBy(e => e.OMId).ToPagedList(pageNumber, pageSize);

            // Return the paged list to the view
            return View(pagedEntries);
        }







        public class UserRole
        {
            public string RoleName { get; set; }
            public List<int?> Districts { get; set; }

            public UserRole(string roleName, List<int?> districts = null)
            {
                RoleName = roleName;
                Districts = districts ?? new List<int?>();
            }
        }



        // GET: CENTRAL79HUB
        public async Task<IActionResult> Index(string searchUserId, int? page)
        {
            //Need to only allow for signees to access this
            var currentUser = User.Identity.Name;
            var validUser = await CheckForAccessPermission(currentUser);
            if (!validUser)
            {
                return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
            }

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
        public async Task<IActionResult> MyIndex(int? page)
        {
            // Store the current logged-in user's identity
            string currentUserId = User.Identity.Name;

            // Filter the records where the UserId matches the current user
            var central79HubEntries = from m in _context.CENTRAL79HUB
                                      where m.UserId == currentUserId
                                      select m;

            // Map WorkflowStep to an integer for ordering
            central79HubEntries = central79HubEntries.OrderBy(entry =>
                entry.WorkflowStep == null || entry.WorkflowStep == "NotStarted" ? 1 :
                entry.WorkflowStep == "RestartFromDistrict" || entry.WorkflowStep == "RestartFromDistrictManager" ? 2 :
                entry.WorkflowStep == "SubmittedToDistrict" || entry.WorkflowStep == "SubmittedToDistrictManager" ? 3 :
                entry.WorkflowStep == "SubmittedToCentralHDS" || entry.WorkflowStep == "SubmittedToCentralGIS" ||
                entry.WorkflowStep == "SubmittedBackToDistrictManager" || entry.WorkflowStep == "SubmittedBackToDistrictManagerFromOperations" ||
                entry.WorkflowStep == "SubmittedToRegionalEngineer" || entry.WorkflowStep == "SubmittedToDirectorOfOperations" ||
                entry.WorkflowStep == "SubmittedToCentralChief" ? 4 :
                entry.WorkflowStep == "CancelledRequestArchive" ? 5 :
                entry.WorkflowStep == "Finalized" ? 6 :
                7 // Default value for any other WorkflowStep
            );


            // Set the page number, defaulting to 1 if no page is provided
            int pageNumber = page ?? 1;

            // Paginate the results with 50 records per page
            var pagedCentral79HubEntries = await central79HubEntries.ToPagedListAsync(pageNumber, 50);

            // Return the view with the paginated results
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

            // Check if the current user has access permission
            var currentUser = User.Identity.Name;
            var validUser = await CheckForAccessPermission(currentUser);

            // Allow access if the user has permission or is the user associated with the CENTRAL79HUB entry
            if (!validUser && currentUser != cENTRAL79HUB.UserId)
            {
                return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
            }


            HttpContext.Session.SetInt32("UniqueID", id.Value);

            ViewBag.TestUniqueID = id;

            return View(cENTRAL79HUB);
        }

        // GET: CENTRAL79HUB/Create
        public IActionResult Create()
        {
            HttpContext.Session.Remove("UniqueID");
            Dropdowns();
            return View();
            
        }

        //New account system this are not needed"         public IActionResult AdminCreate(), post
        /*
        // GET: CENTRAL79HUB/Admin
        public IActionResult AdminCreate()
        {
            Dropdowns();
            return View();

        }
        */
        /*
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
        */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OMId,UserId,Otherbox,County,District,IDNumber,RouteID, EmailSubmit")] CENTRAL79HUB cENTRAL79HUB, int NumberOfItems)
        {
            string userIdentity = User.Identity.Name;
            cENTRAL79HUB.UserId = userIdentity;

            if (ModelState.IsValid)
            {
                cENTRAL79HUB.IsSubmitted = false;
                cENTRAL79HUB.DateSubmitted = DateTime.Now;
                cENTRAL79HUB.IsArchive = false;
                cENTRAL79HUB.WorkflowStep = "NotStarted";
                cENTRAL79HUB.Edited = false;
                cENTRAL79HUB.HasGISReviewed = false;


                // Generate the SmartID
                string currentYear = DateTime.Now.Year.ToString();
                int district = cENTRAL79HUB.District ?? 0; // Use 0 if District is null (handle accordingly)

                // Count how many OM79 entries exist for this district and year
                int countForDistrictAndYear = await _context.CENTRAL79HUB
                    .Where(h => h.District == district && h.DateSubmitted.HasValue && h.DateSubmitted.Value.Year == DateTime.Now.Year)
                    .CountAsync();

                // Generate the new SmartID
                string uniqueCounter = (countForDistrictAndYear + 1).ToString("D4"); // Ensures four digits with leading zeros
                cENTRAL79HUB.SmartID = $"OM79-{currentYear}-{district}-{uniqueCounter}";




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


            // Initial check on the OM79Workflow's NextStep and current user's access
            var om = await _context.CENTRAL79HUB.FirstOrDefaultAsync(e => e.OMId == id);
            // If the package data with the given ID is not found, return a 404 Not Found response
            if (om == null)
            {
                return NotFound();
            }
            var om79Workflow = await _context.OM79Workflow.FirstOrDefaultAsync(w => w.HubID == id);
            var currentUser = User.Identity.Name;
            if (om79Workflow?.NextStep == "FinishEdits" && om.UserId == currentUser)
            {
                // Bypass further permission checks if this condition is met
                ViewBag.TestUniqueID = id;
                return View(om);
            }

            // Need to only allow someone to access this when the workflow step is currently with this 
            var hub = await _context.CENTRAL79HUB.FirstOrDefaultAsync(h => h.OMId == id);
            var user = User.Identity.Name;
            var validUser = await checkComplexPermission(user, hub);
            if (!validUser)
            {
                return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
            }


            var cENTRAL79HUB = await _context.CENTRAL79HUB.FindAsync(id);
            if (cENTRAL79HUB == null)
            {
                return NotFound();
            }
            return View(cENTRAL79HUB);
        }

        // POST: CENTRAL79HUB/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OMId,RouteID,EmailSubmit,Otherbox")] CENTRAL79HUB cENTRAL79HUB)
        {
            if (id != cENTRAL79HUB.OMId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Retrieve the existing entity from the database
                var existingEntity = await _context.CENTRAL79HUB.FindAsync(id);
                if (existingEntity == null)
                {
                    return NotFound();
                }

                // Update only the fields that were edited in the form
                existingEntity.RouteID = cENTRAL79HUB.RouteID;
                existingEntity.EmailSubmit = cENTRAL79HUB.EmailSubmit;
                existingEntity.Otherbox = cENTRAL79HUB.Otherbox;

                // Save changes
                _context.Update(existingEntity);
                await _context.SaveChangesAsync();

                // Redirect to the EditPackage action
                return RedirectToAction("EditPackage", new { id = cENTRAL79HUB.OMId });
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


            //Need to only allow someone to access this when the workflow step is currently with this 
            var hub = await _context.CENTRAL79HUB.FirstOrDefaultAsync(h => h.OMId == id);
            var user = User.Identity.Name;
            var validUser = await checkComplexPermission(user, hub);
            if (!validUser)
            {
                return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
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
            //Need to only allow someone to access this when the workflow step is currently with this 
            var hub = await _context.CENTRAL79HUB.FirstOrDefaultAsync(h => h.OMId == id);
            var user = User.Identity.Name;
            var validUser = await checkComplexPermission(user, hub);
            if (!validUser)
            {
                return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
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

        //// Same as signOMhub, but for central signatures
        //public async Task<IActionResult> SignOMHubCentral()
        //{
        //    var app = Request.Form["apradio"];
        //    var den = Request.Form["denradio"];
        //    var hubkey = int.Parse(Request.Form["HubKey"]); // Define hubkey here
        //    bool isCentralHDS = false;
        //    bool isCentralGIS = false;
        //    bool isCentralChief = false;
        //    var omEntry = _context.CENTRAL79HUB.FirstOrDefault(e => e.OMId ==  hubkey);     

        //    if (app.FirstOrDefault() == "approve")
        //    {
        //        var signature = new SignatureData();
        //        signature.HubKey = int.Parse(Request.Form["HubKey"]);
        //        signature.IsApprove = true;
        //        signature.IsDenied = false;
        //        signature.Comments = Request.Form["commentsmodal"];
        //        signature.Signatures = Request.Form["signaturemodal"];
        //        signature.SigType = Request.Form["sigtype"];
        //        signature.ENumber = HttpContext.User.Identity.Name;
        //        signature.DateSubmitted = DateTime.Now;
        //        signature.IsCurrentSig = true;


        //        _context.Add(signature);
        //        await _context.SaveChangesAsync();


        //        if (signature.SigType == "HDS" && omEntry.WorkflowStep == "SubmittedToCentralHDS")
        //        {
        //            // The HDS user just did their first approval
        //            omEntry.WorkflowStep = "SubmittedToCentralGIS";
        //            await _context.SaveChangesAsync();
        //            SendWorkflowEmailToGISManagers(hubkey); // Send email to GIS Managers
        //        }
        //        else if (signature.SigType == "GIS Manager" && omEntry.WorkflowStep == "SubmittedToCentralGIS")
        //        {
        //            // The GIS user just did their approval
        //            omEntry.WorkflowStep = "SubmittedToCentralSecondHDS";
        //            await _context.SaveChangesAsync();
        //            SendWorkflowEmailToSecondHDS(hubkey); // Send email to second HDS
        //        }
        //        else if (signature.SigType == "HDS" && omEntry.WorkflowStep == "SubmittedToCentralSecondHDS")
        //        {
        //            // The HDS user just did their second approval
        //            omEntry.WorkflowStep = "SubmittedToCentralChief";
        //            await _context.SaveChangesAsync();
        //            SendWorkflowEmailToChief(hubkey); // Send email to Chief
        //        }
        //        else if (signature.SigType == "Chief" && omEntry.WorkflowStep == "SubmittedToCentralChief")
        //        {
        //            // The chief just submitted their approval
        //            omEntry.WorkflowStep = "SubmittedToCentralThirdHDS";
        //            await _context.SaveChangesAsync();
        //            SendWorkflowEmailToThirdHDS(hubkey); // Send email to third HDS
        //        }
        //        else if (signature.SigType == "HDS" && omEntry.WorkflowStep == "SubmittedToCentralThirdHDS")
        //        {
        //            // The HDS just did their third approval
        //            omEntry.WorkflowStep = "SubmittedToCentralLRS";
        //            await _context.SaveChangesAsync();
        //            SendWorkflowEmailToLRS(hubkey); // Send email to LRS
        //        }
        //        else if (signature.SigType == "LRS" && omEntry.WorkflowStep == "SubmittedToCentralLRS")
        //        {
        //            // At this point the workflow should be completed
        //            // The LRS just submitted their confirmation
        //            omEntry.WorkflowStep = "Finalized";
        //            await _context.SaveChangesAsync();
        //        }

        //    }


        //    if (den.FirstOrDefault() == "deny")
        //    {
        //        var signature = new SignatureData();
        //        signature.HubKey = int.Parse(Request.Form["HubKey"]);
        //        signature.IsApprove = false;
        //        signature.IsDenied = true;
        //        signature.Comments = Request.Form["commentsmodal"];
        //        signature.Signatures = Request.Form["signaturemodal"];
        //        signature.SigType = Request.Form["sigtype"];
        //        signature.ENumber = HttpContext.User.Identity.Name;
        //        signature.DateSubmitted = DateTime.Now;
        //        signature.IsCurrentSig = true;

        //        _context.Add(signature);
        //        await _context.SaveChangesAsync();

        //    }
        //    return RedirectToAction(nameof(Details), new { id = hubkey });
        //}



        //public void SendWorkflowEmailToGISManagers(int id)
        //{
        //    try
        //    {
        //        var omEntry = _context.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
        //        if (omEntry == null)
        //        {
        //            Console.WriteLine("OM Entry not found.");
        //            return;
        //        }

        //        // Find all GIS Managers
        //        var gisManagers = _context.UserData.Where(e => e.GISManager).ToList();
        //        if (!gisManagers.Any())
        //        {
        //            Console.WriteLine("GIS Managers not found.");
        //            return;
        //        }

        //        // Compose the email
        //        var message = new MailMessage
        //        {
        //            From = new MailAddress("DOTPJ103Srv@wv.gov"),
        //            Subject = $"OM79 Entry Ready for GIS Manager Review",
        //            Body = $"Hello,<br><br>" +
        //                   $"An OM79 entry has been reviewed by the HDS and is now awaiting your approval. As the GIS Manager, your review is crucial before the entry can be forwarded to the next step.<br><br>" +
        //                   $"Please click the link below to review and sign the OM79 entry:<br><br>" +
        //                   $"<a href='https://dotappstest.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}'>Review OM79 Entry</a><br><br>" +
        //                   $"Thank you for your attention to this matter.<br><br>" +
        //                   $"Best regards,<br>" +
        //                   $"OM79 Automated System",
        //            IsBodyHtml = true
        //        };

        //        // Add all GIS Managers to the recipient list
        //        foreach (var gisManager in gisManagers)
        //        {
        //            message.To.Add(gisManager.Email);
        //        }

        //        // Send the email
        //        var client = new SmtpClient
        //        {
        //            Host = "10.204.145.32",
        //            Port = 25,
        //            EnableSsl = false,
        //            Credentials = new NetworkCredential("richard.a.tucker@wv.gov", "trippononemails")
        //        };

        //        client.Send(message);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
        //    }
        //}

        //public void SendWorkflowEmailToSecondHDS(int id)
        //{
        //    try
        //    {
        //        var omEntry = _context.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
        //        if (omEntry == null)
        //        {
        //            Console.WriteLine("OM Entry not found.");
        //            return;
        //        }

        //        // Find all HDS users
        //        var hdsUsers = _context.UserData.Where(e => e.HDS).ToList();
        //        if (!hdsUsers.Any())
        //        {
        //            Console.WriteLine("HDS users not found.");
        //            return;
        //        }

        //        // Compose the email
        //        var message = new MailMessage
        //        {
        //            From = new MailAddress("DOTPJ103Srv@wv.gov"),
        //            Subject = $"OM79 Entry Ready for Second HDS Review",
        //            Body = $"Hello,<br><br>" +
        //                   $"An OM79 entry has been reviewed by the GIS Manager and is now awaiting your second approval. As the HDS, your review is crucial before the entry can be forwarded to the Chief.<br><br>" +
        //                   $"Please click the link below to review and sign the OM79 entry:<br><br>" +
        //                   $"<a href='https://dotappstest.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}'>Review OM79 Entry</a><br><br>" +
        //                   $"Thank you for your attention to this matter.<br><br>" +
        //                   $"Best regards,<br>" +
        //                   $"OM79 Automated System",
        //            IsBodyHtml = true
        //        };

        //        // Add all HDS users to the recipient list
        //        foreach (var hdsUser in hdsUsers)
        //        {
        //            message.To.Add(hdsUser.Email);
        //        }

        //        // Send the email
        //        var client = new SmtpClient
        //        {
        //            Host = "10.204.145.32",
        //            Port = 25,
        //            EnableSsl = false,
        //            Credentials = new NetworkCredential("richard.a.tucker@wv.gov", "trippononemails")
        //        };

        //        client.Send(message);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
        //    }
        //}

        //public void SendWorkflowEmailToChief(int id)
        //{
        //    try
        //    {
        //        var omEntry = _context.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
        //        if (omEntry == null)
        //        {
        //            Console.WriteLine("OM Entry not found.");
        //            return;
        //        }

        //        // Find the Chief
        //        var chief = _context.UserData.FirstOrDefault(e => e.Chief);
        //        if (chief == null)
        //        {
        //            Console.WriteLine("Chief not found.");
        //            return;
        //        }

        //        // Compose the email
        //        var message = new MailMessage
        //        {
        //            From = new MailAddress("DOTPJ103Srv@wv.gov"),
        //            Subject = $"OM79 Entry Ready for Chief Review",
        //            Body = $"Hello {chief.FirstName},<br><br>" +
        //                   $"An OM79 entry has been reviewed by the HDS and is now awaiting your approval. As the Chief, your review is crucial before the entry can be finalized.<br><br>" +
        //                   $"Please click the link below to review and sign the OM79 entry:<br><br>" +
        //                   $"<a href='https://dotappstest.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}'>Review OM79 Entry</a><br><br>" +
        //                   $"Thank you for your attention to this matter.<br><br>" +
        //                   $"Best regards,<br>" +
        //                   $"OM79 Automated System",
        //            IsBodyHtml = true
        //        };

        //        message.To.Add(chief.Email);

        //        // Send the email
        //        var client = new SmtpClient
        //        {
        //            Host = "10.204.145.32",
        //            Port = 25,
        //            EnableSsl = false,
        //            Credentials = new NetworkCredential("richard.a.tucker@wv.gov", "trippononemails")
        //        };

        //        client.Send(message);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
        //    }
        //}

        //public void SendWorkflowEmailToThirdHDS(int id)
        //{
        //    try
        //    {
        //        var omEntry = _context.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
        //        if (omEntry == null)
        //        {
        //            Console.WriteLine("OM Entry not found.");
        //            return;
        //        }

        //        // Find all HDS users
        //        var hdsUsers = _context.UserData.Where(e => e.HDS).ToList();
        //        if (!hdsUsers.Any())
        //        {
        //            Console.WriteLine("HDS users not found.");
        //            return;
        //        }

        //        // Compose the email
        //        var message = new MailMessage
        //        {
        //            From = new MailAddress("DOTPJ103Srv@wv.gov"),
        //            Subject = $"OM79 Entry Ready for Third HDS Review",
        //            Body = $"Hello,<br><br>" +
        //                   $"An OM79 entry has been reviewed by the Chief and is now awaiting your third approval. As the HDS, your review is crucial before the entry can be forwarded to the LRS.<br><br>" +
        //                   $"Please click the link below to review and sign the OM79 entry:<br><br>" +
        //                   $"<a href='https://dotappstest.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}'>Review OM79 Entry</a><br><br>" +
        //                   $"Thank you for your attention to this matter.<br><br>" +
        //                   $"Best regards,<br>" +
        //                   $"OM79 Automated System",
        //            IsBodyHtml = true
        //        };

        //        // Add all HDS users to the recipient list
        //        foreach (var hdsUser in hdsUsers)
        //        {
        //            message.To.Add(hdsUser.Email);
        //        }

        //        // Send the email
        //        var client = new SmtpClient
        //        {
        //            Host = "10.204.145.32",
        //            Port = 25,
        //            EnableSsl = false,
        //            Credentials = new NetworkCredential("richard.a.tucker@wv.gov", "trippononemails")
        //        };

        //        client.Send(message);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
        //    }
        //}
        //public void SendWorkflowEmailToLRS(int id)
        //{
        //    try
        //    {
        //        var omEntry = _context.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
        //        if (omEntry == null)
        //        {
        //            Console.WriteLine("OM Entry not found.");
        //            return;
        //        }

        //        // Find all LRS users
        //        var lrsUsers = _context.UserData.Where(e => e.LRS).ToList();
        //        if (!lrsUsers.Any())
        //        {
        //            Console.WriteLine("LRS users not found.");
        //            return;
        //        }

        //        // Compose the email
        //        var message = new MailMessage
        //        {
        //            From = new MailAddress("DOTPJ103Srv@wv.gov"),
        //            Subject = $"OM79 Entry Ready for LRS Uploading",
        //            Body = $"Hello,<br><br>" +
        //                   $"An OM79 entry has been reviewed by the HDS and is now awaiting your confirmation. As the LRS, your action is crucial to finalize the entry.<br><br>" +
        //                   $"Please click the link below to review and finalize the OM79 entry:<br><br>" +
        //                   $"<a href='https://dotappstest.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}'>Review OM79 Entry</a><br><br>" +
        //                   $"Thank you for your attention to this matter.<br><br>" +
        //                   $"Best regards,<br>" +
        //                   $"OM79 Automated System",
        //            IsBodyHtml = true
        //        };

        //        // Add all LRS users to the recipient list
        //        foreach (var lrsUser in lrsUsers)
        //        {
        //            message.To.Add(lrsUser.Email);
        //        }

        //        // Send the email
        //        var client = new SmtpClient
        //        {
        //            Host = "10.204.145.32",
        //            Port = 25,
        //            EnableSsl = false,
        //            Credentials = new NetworkCredential("richard.a.tucker@wv.gov", "trippononemails")
        //        };

        //        client.Send(message);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
        //    }
        //}


        //public async Task<IActionResult> SignOMHub()
        //{
        //    var app = Request.Form["apradio"];
        //    var den = Request.Form["denradio"];
        //    var hubkey = int.Parse(Request.Form["HubKey"]); // Define hubkey here
        //    bool isDistrictManagerSigning = false;



        //    if (app.FirstOrDefault() == "approve")
        //    {
        //        var signature = new SignatureData();
        //        signature.HubKey = int.Parse(Request.Form["HubKey"]);
        //        signature.IsApprove = true;
        //        signature.IsDenied = false;
        //        signature.Comments = Request.Form["commentsmodal"];
        //        signature.Signatures = Request.Form["signaturemodal"];
        //        signature.SigType = Request.Form["sigtype"];
        //        signature.ENumber = HttpContext.User.Identity.Name;
        //        signature.DateSubmitted = DateTime.Now;
        //        signature.IsCurrentSig = true;


        //        _context.Add(signature);
        //        await _context.SaveChangesAsync();

        //        if (signature.SigType == "District Manager")
        //        {
        //            isDistrictManagerSigning = true;
        //        }

        //        // Update the workflow step if approved
        //        var omEntry = _context.CENTRAL79HUB.FirstOrDefault(e => e.OMId == signature.HubKey);
        //        if (omEntry != null && omEntry.WorkflowStep == "SubmittedToDistrict")
        //        {
        //            var allSignatures = _context.SignatureData.Where(e => e.HubKey == omEntry.OMId).ToList();

        //            // List of required roles
        //            var requiredRoles = new List<string>
        //            {
        //                "Bridge Engineer",
        //                "Traffic Engineer",
        //                "Maintenance Engineer",
        //                "Construction Engineer",
        //                "Right Of Way Manager"
        //            };

        //            // Check if all required roles have signatures
        //            bool allRolesSigned = requiredRoles.All(role => allSignatures.Any(sig => sig.SigType == role));

        //            if (allRolesSigned)
        //            {
        //                bool allRolesApproved = requiredRoles.All(role => allSignatures.Any(sig => sig.SigType == role && sig.IsApprove));
        //                if (allRolesApproved)
        //                {
        //                    omEntry.WorkflowStep = "SubmittedToDistrictManager";

        //                    Console.WriteLine("============================================");
        //                    Console.WriteLine("============================================");
        //                    Console.WriteLine("============================================");
        //                    Console.WriteLine("============================================");
        //                    Console.WriteLine("============================================");
        //                    Console.WriteLine("==========All Signatures should be done here");
        //                    Console.WriteLine("============================================");
        //                    Console.WriteLine("============================================");
        //                    Console.WriteLine("============================================");
        //                    Console.WriteLine("============================================");
        //                    Console.WriteLine("============================================");
        //                    Console.WriteLine("============================================");
        //                    Console.WriteLine("============================================");
        //                    Console.WriteLine("============================================");
        //                    Console.WriteLine("============================================");
        //                    await _context.SaveChangesAsync();


        //                    /////////// Send District Manager Email Here
        //                    ///

        //                    sendInitialWorkflowEmailToDistrictManager(hubkey);
        //                }
        //                else
        //                {
        //                    // This is the case where everyone from the district has signed, but one or more district users declined the OM79, need to send it back to the initial user with an email
        //                    // send all active signature's comments to the user and restart the workflow and allow them to edit the items, segments, and om79 

        //                    sendDistrictUserDenialEmail(hubkey);

        //                    foreach (var signatures in allSignatures)
        //                    {
        //                        signature.IsCurrentSig = false;
        //                    }
        //                    omEntry.WorkflowStep = "RestartFromDistrict";
        //                    omEntry.IsSubmitted = false;


        //                    await _context.SaveChangesAsync();
        //                }
        //            }
        //            await _context.SaveChangesAsync();
        //        }


        //        // Check if the District Manager has just signed SubmittedToDistrictManager
        //        if (omEntry != null && omEntry.WorkflowStep == "SubmittedToDistrictManager" && isDistrictManagerSigning)
        //        {
        //            // This means the district manager has just signed and approved the OM79 entry to be sent to the central office
        //            Console.WriteLine("District Manager has just signed and approved the OM79 entry.");

        //            omEntry.WorkflowStep = "SubmittedToCentralHDS";
        //            await _context.SaveChangesAsync();

        //            // Send email to HDS
        //            SendWorkflowEmailToHDS(hubkey);


        //        }


        //    }

        //    if (den.FirstOrDefault() == "deny")
        //    {
        //        var signature = new SignatureData();
        //        signature.HubKey = int.Parse(Request.Form["HubKey"]);
        //        signature.IsApprove = false;
        //        signature.IsDenied = true;
        //        signature.Comments = Request.Form["commentsmodal"];
        //        signature.Signatures = Request.Form["signaturemodal"];
        //        signature.SigType = Request.Form["sigtype"];
        //        signature.ENumber = HttpContext.User.Identity.Name;
        //        signature.DateSubmitted = DateTime.Now;
        //        signature.IsCurrentSig = true;

        //        _context.Add(signature);
        //        await _context.SaveChangesAsync();




        //        if (signature.SigType == "District Manager")
        //        {
        //            isDistrictManagerSigning = true;
        //        }


        //        // Update the workflow step if denied
        //        var omEntry = _context.CENTRAL79HUB.FirstOrDefault(e => e.OMId == signature.HubKey);
        //        if (omEntry != null && omEntry.WorkflowStep == "SubmittedToDistrict")
        //        {
        //            var allSignatures = _context.SignatureData.Where(e => e.HubKey == omEntry.OMId && e.IsCurrentSig == true).ToList();

        //            // List of required roles
        //            var requiredRoles = new List<string>
        //            {
        //                "Bridge Engineer",
        //                "Traffic Engineer",
        //                "Maintenance Engineer",
        //                "Construction Engineer",
        //                "Right Of Way Manager"
        //            };

        //            // Check if all required roles have signatures
        //            bool allRolesSigned = requiredRoles.All(role => allSignatures.Any(sig => sig.SigType == role));

        //            if (allRolesSigned)
        //            {
        //                // This is the case where everyone from the district has signed, but one or more district users declined the OM79, need to send it back to the initial user with an email
        //                // Maybe send all active signature's comments to the user and restart the workflow and allow them to edit the items, segments, and om79 

        //                sendDistrictUserDenialEmail(hubkey);

        //                foreach (var signatures in allSignatures)
        //                {
        //                    signature.IsCurrentSig = false;
        //                }
        //                omEntry.WorkflowStep = "RestartFromDistrict";
        //                omEntry.IsSubmitted = false;


        //                await _context.SaveChangesAsync();
        //            }
        //        }




        //        // Check if the District Manager has just signed
        //        if (omEntry != null && omEntry.WorkflowStep == "SubmittedToDistrictManager" && isDistrictManagerSigning)
        //        {
        //            // This means the district manager has just signed and denied the OM79 entry to be sent to the central office
        //            Console.WriteLine("District Manager has just signed and denied the OM79 entry.");


        //            sendDistrictManagerDenialEmail(hubkey);
        //            var allSignatures = _context.SignatureData.Where(e => e.HubKey == omEntry.OMId && e.IsCurrentSig == true).ToList();

        //            foreach (var signatures in allSignatures)
        //            {
        //                signature.IsCurrentSig = false;
        //            }
        //            omEntry.WorkflowStep = "RestartFromDistrictManager";
        //            omEntry.IsSubmitted = false;


        //            await _context.SaveChangesAsync();                    
        //        }
        //    }

        //    return RedirectToAction(nameof(Details), new { id = hubkey });
        //}

        public async Task<IActionResult> SignOMHub()
        {
            var decision = Request.Form["decision"];
            var hubkey = int.Parse(Request.Form["HubKey"]); // Define hubkey here
            bool isDistrictManagerSigning = false;

            var signature = new SignatureData
            {
                HubKey = hubkey,
                IsApprove = decision == "approve",
                IsDenied = decision == "deny",
                Comments = Request.Form["commentsmodal"],
                Signatures = Request.Form["signaturemodal"],
                SigType = Request.Form["sigtype"],
                ENumber = HttpContext.User.Identity.Name,
                DateSubmitted = DateTime.Now,
                IsCurrentSig = true,
            };

            _context.Add(signature);
            await _context.SaveChangesAsync();

            var omEntry = _context.CENTRAL79HUB.FirstOrDefault(e => e.OMId == hubkey);
            if (omEntry == null)
            {
                return NotFound("OM Entry not found");
            }

            if (signature.SigType == "District Manager")
            {
                isDistrictManagerSigning = true;
            }

            if (decision == "approve")
            {
                await HandleApproval(omEntry, signature, isDistrictManagerSigning);
            }
            else if (decision == "deny")
            {
                await HandleDenial(omEntry, signature, isDistrictManagerSigning);
            }

            return RedirectToAction(nameof(Details), new { id = hubkey });
        }

        private async Task HandleApproval(CENTRAL79HUB omEntry, SignatureData signature, bool isDistrictManagerSigning)
        {
            if (omEntry.WorkflowStep == "SubmittedToDistrict")
            {
                var allSignatures = _context.SignatureData.Where(e => e.HubKey == omEntry.OMId && e.IsCurrentSig == true).ToList();
                var requiredRoles = new List<string>
                    {
                        "Bridge Engineer",
                        "Traffic Engineer",
                        "Maintenance Engineer",
                        "Construction Engineer",
                        "Right Of Way Manager"
                    };

                bool allRolesSigned = requiredRoles.All(role => allSignatures.Any(sig => sig.SigType == role));
                bool allRolesApproved = requiredRoles.All(role => allSignatures.Any(sig => sig.SigType == role && sig.IsApprove));

                if (allRolesSigned)
                {
                    if (allRolesApproved)
                    {
                        omEntry.WorkflowStep = "SubmittedToDistrictManager";
                        await _context.SaveChangesAsync();
                        sendInitialWorkflowEmailToDistrictManager(signature.HubKey ?? 0); // Convert nullable int to int
                    }
                    else
                    {
                        sendDistrictUserDenialEmail(signature.HubKey ?? 0); // Convert nullable int to int
                        InvalidateSignatures(allSignatures);
                        //omEntry.IsSubmitted = false;
                        omEntry.WorkflowStep = "RestartFromDistrict";
                        await _context.SaveChangesAsync();
                    }
                }
            }

            if (omEntry.WorkflowStep == "SubmittedToDistrictManager" && isDistrictManagerSigning)
            {
                omEntry.WorkflowStep = "SubmittedToCentralHDS";
                await _context.SaveChangesAsync();
                SendWorkflowEmailToHDS(signature.HubKey ?? 0); // Convert nullable int to int
            }
        }

        private async Task HandleDenial(CENTRAL79HUB omEntry, SignatureData signature, bool isDistrictManagerSigning)
        {
            if (omEntry.WorkflowStep == "SubmittedToDistrict")
            {
                var allSignatures = _context.SignatureData.Where(e => e.HubKey == omEntry.OMId && e.IsCurrentSig == true).ToList();
                var requiredRoles = new List<string>
        {
            "Bridge Engineer",
            "Traffic Engineer",
            "Maintenance Engineer",
            "Construction Engineer",
            "Right Of Way Manager"
        };

                bool allRolesSigned = requiredRoles.All(role => allSignatures.Any(sig => sig.SigType == role));

                if (allRolesSigned)
                {
                    sendDistrictUserDenialEmail(signature.HubKey ?? 0); // Convert nullable int to int
                    InvalidateSignatures(allSignatures);
                    omEntry.WorkflowStep = "RestartFromDistrict";
                   //omEntry.IsSubmitted = false;
                    await _context.SaveChangesAsync();
                }
            }

            if (omEntry.WorkflowStep == "SubmittedToDistrictManager" && isDistrictManagerSigning)
            {
                sendDistrictManagerDenialEmail(signature.HubKey ?? 0); // Convert nullable int to int
                var allSignatures = _context.SignatureData.Where(e => e.HubKey == omEntry.OMId && e.IsCurrentSig == true).ToList();
                InvalidateSignatures(allSignatures);
                omEntry.WorkflowStep = "RestartFromDistrictManager";
                await _context.SaveChangesAsync();
            }
        }

        private void InvalidateSignatures(List<SignatureData> signatures)
        {
            foreach (var signature in signatures)
            {
                signature.IsCurrentSig = false;
                _context.Update(signature);  // Ensure that the entity state is updated
            }
        }


        public void SendWorkflowEmailToHDS(int id)
        {
            try
            {
                var omEntry = _context.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
                if (omEntry == null)
                {
                    Console.WriteLine("OM Entry not found.");
                    return;
                }

                // Find all HDS users
                var hdsUsers = _context.UserData.Where(e => e.HDS).ToList();
                if (!hdsUsers.Any())
                {
                    Console.WriteLine("HDS users not found.");
                    return;
                }

                // Construct the Review Link (Replace with actual URL)
                string reviewLink = $"https://dotapps.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}";

                // Construct the email body
                string emailBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; color: #333; background-color: #f8f8f8; padding: 20px;'>
                            <div style='max-width: 600px; margin: auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ddd;'>

                                <!-- Header -->
                                <h2 style='text-align: center; color: #0056b3;'>OM79 Submission Requires Your Review</h2>
                                <p style='font-size: 14px; text-align: center;'>Entry ID: <strong>{omEntry.SmartID}</strong> | District: <strong>{omEntry.District}</strong></p>
                                <hr style='border: 0; border-top: 1px solid #ddd;'>

                                <!-- Greeting -->
                                <p>Hello,</p>

                                <!-- Explanation -->
                                <p>An OM79 entry has been submitted from district {omEntry.District} for review. You are responsible for reviewing the following based on your assigned role(s): </p>
                                <p><strong>Your Responsibilities:</strong></p>
                                <ul style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF; list-style-type: none;'>
                                    <li><strong>HDS:</strong> Responsible for reviewing OM-79 package and attachments for accuracy and completeness.</li>
                                </ul>

                                <!-- Call to Action -->
                                <p style='text-align: center; margin-top: 20px;'>
                                    <a href='{reviewLink}' 
                                        style='background: #28a745; color: white; text-decoration: none; padding: 12px 20px; border-radius: 5px; font-size: 16px; display: inline-block;'>
                                        Review & Sign OM79 Entry
                                    </a>
                                </p>
                                <p style='color: #0056b3; font-weight: bold; text-align: center; margin-top: 15px;'>
                                        The signature form is located at the bottom of the page at the link above.
                                </p>
                                <hr style='border: 0; border-top: 1px solid #ddd; margin-top: 20px;'>

                                <!-- Footer -->
                                <p style='font-size: 12px; text-align: center; color: #667;'>Thank you,<br>OM79 Automated System</p>
                            </div>
                        </body>
                        </html>";

                // Create the email
                var automatedEmail = _httpContextAccessor.HttpContext?.Items["AutomatedEmailAddress"]?.ToString() ?? "DOTPJ103Srv@wv.gov";

                var message = new MailMessage
                {
                    From = new MailAddress(automatedEmail),
                    Subject = $"OM79 Submission - HDS Review required",
                    Body = emailBody,
                    IsBodyHtml = true
                };
               
                // Add all HDS users to the To field
                foreach (var hdsUser in hdsUsers)
                {
                    message.To.Add(hdsUser.Email);
                }
                message.CC.Add("ethan.m.johnson@wv.gov");


                var client = new SmtpClient
                {
                    Host = "10.204.145.32",
                    Port = 25,
                    EnableSsl = false,
                    Credentials = new NetworkCredential("ethan.m.johnson@wv.gov", "trippononemails")
                };

                client.Send(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending HDS email: {ex.Message}");
            }
        }


        //public void SendWorkflowEmailToHDS(int id)
        //{
        //    try
        //    {
        //        var omEntry = _context.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
        //        if (omEntry == null)
        //        {
        //            Console.WriteLine("OM Entry not found.");
        //            return;
        //        }

        //        // Find all HDS users
        //        var hdsUsers = _context.UserData.Where(e => e.HDS).ToList();
        //        if (!hdsUsers.Any())
        //        {
        //            Console.WriteLine("HDS users not found.");
        //            return;
        //        }
        //        string reviewLink = $"https://dotapps.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}";
        //        // Compose the email
        //        var message = new MailMessage
        //        {
        //            From = new MailAddress("DOTPJ103Srv@wv.gov"),
        //            Subject = $"OM79 Entry Ready for HDS Review",
        //            Body = $@"
        //                    <html>
        //                    <body style='font-family: Arial, sans-serif; color: #333; background-color: #f8f8f8; padding: 20px;'>
        //                        <div style='max-width: 600px; margin: auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ddd;'>

        //                            <!-- Header -->
        //                            <h2 style='text-align: center; color: #0056b3;'>OM79 Entry Requires HDS Review</h2>
        //                            <p style='font-size: 14px; text-align: center;'>Entry ID: <strong>{omEntry.OMId}</strong></p>
        //                            <hr style='border: 0; border-top: 1px solid #ddd;'>

        //                            <!-- Greeting -->
        //                            <p>Hello,</p>

        //                            <!-- Explanation -->
        //                            <p>An OM79 entry has been reviewed by the District Manager and is now awaiting your approval. As the HDS, your review is crucial before the entry can proceed to the next step.</p>

        //                            <!-- Call to Action -->
        //                            <p style='text-align: center; margin-top: 20px;'>
        //                                <a href='{reviewLink}' 
        //                                    style='background: #28a745; color: white; text-decoration: none; padding: 12px 20px; border-radius: 5px; font-size: 16px; display: inline-block;'>
        //                                    ✅ Review & Sign OM79 Entry
        //                                </a>
        //                            </p>

        //                            <!-- Important Note -->
        //                            <p style='color: #0056b3; font-weight: bold; text-align: center; margin-top: 15px;'>
        //                                Please review the entry at your earliest convenience to avoid workflow delays.
        //                            </p>

        //                            <hr style='border: 0; border-top: 1px solid #ddd; margin-top: 20px;'>

        //                            <!-- Footer -->
        //                            <p style='font-size: 12px; text-align: center; color: #666;'>Thank you for your attention to this matter.</p>
        //                            <p style='font-size: 12px; text-align: center; color: #666;'>Best regards,<br>OM79 Automated System</p>

        //                        </div>
        //                    </body>
        //                    </html>"
        //            IsBodyHtml = true
        //        };

        //        // Add all HDS users to the recipient list
        //        foreach (var hdsUser in hdsUsers)
        //        {
        //            message.To.Add(hdsUser.Email);
        //        }
        //        message.CC.Add("ethan.m.johnson@wv.gov");

        //        // Send the email
        //        var client = new SmtpClient
        //        {
        //            Host = "10.204.145.32",
        //            Port = 25,
        //            EnableSsl = false,
        //            Credentials = new NetworkCredential("ethan.m.johnson@wv.gov", "trippononemails")
        //        };

        //        client.Send(message);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
        //    }
        //}


        public void sendDistrictManagerDenialEmail(int id)
        {
            try
            {
                var omEntry = _context.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
                if (omEntry == null)
                {
                    Console.WriteLine("OM Entry not found.");
                    return;
                }

                var initialUserEmail = omEntry.EmailSubmit;
                var signatures = _context.SignatureData
                    .Where(e => e.HubKey == omEntry.OMId && e.IsCurrentSig == true && e.SigType == "District Manager")
                    .ToList();

                // Construct the Review Link
                string reviewLink = $"https://dotapps.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}";

                // Construct the email body
                string emailBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; color: #333; background-color: #f8f8f8; padding: 20px;'>
                            <div style='max-width: 600px; margin: auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ddd;'>

                                <!-- Header -->
                                <h2 style='text-align: center; color: #d9534f;'>OM79 Submission Denied</h2>
                                <p style='font-size: 14px; text-align: center;'>Entry ID: <strong>{omEntry.SmartID}</strong> | District: <strong>{omEntry.District}</strong></p>
                                <hr style='border: 0; border-top: 1px solid #ddd;'>

                                <!-- Greeting -->
                                <p>Hello,</p>

                                <!-- Explanation -->
                                <p>The District Manager has denied an OM79 entry that you submitted. Below are their comments:</p>

                                <!-- Comments Section -->
                                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #d9534f;'>
                                    {string.Join("", signatures.Select(signature => $@"
                                        <div style='background: white; padding: 10px; border-radius: 8px; box-shadow: 0px 0px 5px #ddd; margin-bottom: 10px;'>
                                            <p><strong>District Manager:</strong> {signature.Signatures} 
                                                <span style='color:{(signature.IsApprove ? "green" : "red")};'>
                                                    {(signature.IsApprove ? "✅ Approved" : "❌ Denied")}
                                                </span>
                                            </p>
                                            <p><strong>Comments:</strong> {signature.Comments}</p>
                                        </div>
                                    "))}
                                </div>

                                <!-- Action Needed -->
                                <p>Please review the requested changes/concerns from your District Manager and update the entry accordingly.</p>

                                <!-- Call to Action -->
                                <p style='text-align: center; margin-top: 20px;'>
                                    <a href='{reviewLink}' 
                                        style='background: #dc3545; color: white; text-decoration: none; padding: 12px 20px; border-radius: 5px; font-size: 16px; display: inline-block;'>
                                        Review & Update OM79 Entry
                                    </a>
                                </p>

                                <hr style='border: 0; border-top: 1px solid #ddd; margin-top: 20px;'>

                                <!-- Footer -->
                                <p style='font-size: 12px; text-align: center; color: #667;'>Thank you,<br>OM79 Automated System</p>
                            </div>
                        </body>
                        </html>";

                // Compose the email
                var automatedEmail = _httpContextAccessor.HttpContext?.Items["AutomatedEmailAddress"]?.ToString() ?? "DOTPJ103Srv@wv.gov";

                var message = new MailMessage
                {
                    From = new MailAddress(automatedEmail),
                    Subject = "OM79 Entry Denial Notification",
                    Body = emailBody,
                    IsBodyHtml = true
                };
                message.CC.Add("ethan.m.johnson@wv.gov");
                message.To.Add(initialUserEmail);

                var client = new SmtpClient
                {
                    Host = "10.204.145.32",
                    Port = 25,
                    EnableSsl = false,
                    Credentials = new NetworkCredential("ethan.m.johnson@wv.gov", "trippononemails")
                };
                client.Send(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
            }
        }
        public void sendDistrictUserDenialEmail(int id)
        {
            try
            {
                var omEntry = _context.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
                if (omEntry == null)
                {
                    Console.WriteLine("OM Entry not found.");
                    return;
                }

                var initialUserEmail = omEntry.EmailSubmit;
                var signatures = _context.SignatureData
                    .Where(e => e.HubKey == omEntry.OMId && e.IsCurrentSig == true)
                    .ToList();

                // Construct the Review Link
                string reviewLink = $"https://dotapps.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}";

                // Construct the email body
                string emailBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; color: #333; background-color: #f8f8f8; padding: 20px;'>
                        <div style='max-width: 600px; margin: auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ddd;'>

                            <!-- Header -->
                            <h2 style='text-align: center; color: #d9534f;'>OM79 Submission Denied</h2>
                            <p style='font-size: 14px; text-align: center;'>Entry ID: <strong>{omEntry.SmartID}</strong> | District: <strong>{omEntry.District}</strong></p>
                            <hr style='border: 0; border-top: 1px solid #ddd;'>

                            <!-- Greeting -->
                            <p>Hello,</p>

                            <!-- Explanation -->
                            <p>One or more of the district-level users has denied an OM79 entry that you submitted. Below are their comments:</p>

                            <!-- Comments Section -->
                            <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #d9534f;'>
                                {string.Join("", signatures.Select(signature => $@"
                                    <div style='background: white; padding: 10px; border-radius: 8px; box-shadow: 0px 0px 5px #ddd; margin-bottom: 10px;'>
                                        <p><strong>{signature.SigType}:</strong> {signature.Signatures} 
                                            <span style='color:{(signature.IsApprove ? "green" : "red")};'>
                                                {(signature.IsApprove ? "✅ Approved" : "❌ Denied")}
                                            </span>
                                        </p>
                                        <p><strong>Comments:</strong> {signature.Comments}</p>
                                    </div>
                                "))}
                            </div>

                            <!-- Action Needed -->
                            <p>Please review the requested changes/concerns from your district and update the entry accordingly.</p>

                            <!-- Call to Action -->
                            <p style='text-align: center; margin-top: 20px;'>
                                <a href='{reviewLink}' 
                                    style='background: #dc3545; color: white; text-decoration: none; padding: 12px 20px; border-radius: 5px; font-size: 16px; display: inline-block;'>
                                    Review & Update OM79 Entry
                                </a>
                            </p>                            

                            <hr style='border: 0; border-top: 1px solid #ddd; margin-top: 20px;'>

                            <!-- Footer -->
                            <p style='font-size: 12px; text-align: center; color: #667;'>Thank you,<br>OM79 Automated System</p>
                        </div>
                    </body>
                    </html>";
                // Compose the email
                var automatedEmail = _httpContextAccessor.HttpContext?.Items["AutomatedEmailAddress"]?.ToString() ?? "DOTPJ103Srv@wv.gov";

                var message = new MailMessage
                {
                    From = new MailAddress(automatedEmail),
                    Subject = "OM79 Submission Denied Notification",
                    Body = emailBody,
                    IsBodyHtml = true
                };

                message.To.Add(initialUserEmail);
                message.CC.Add("ethan.m.johnson@wv.gov");

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
                string reviewLink = $"https://dotapps.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}";

                // Compose the email
                var automatedEmail = _httpContextAccessor.HttpContext?.Items["AutomatedEmailAddress"]?.ToString() ?? "DOTPJ103Srv@wv.gov";

                var message = new MailMessage
                {
                    From = new MailAddress(automatedEmail),
                    Subject = $"OM79 Submission - Review Required for District {districtManager.District}",
                    Body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; color: #333; background-color: #f8f8f8; padding: 20px;'>
                        <div style='max-width: 600px; margin: auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ddd;'>

                            <!-- Header -->
                            <h2 style='text-align: center; color: #0056b3;'>OM79 Submission Requires Your Review</h2>
                            <p style='font-size: 14px; text-align: center;'>Entry ID: <strong>{omEntry.SmartID}</strong> | District: <strong>{districtManager.District}</strong></p>
                            <hr style='border: 0; border-top: 1px solid #ddd;'>

                            <!-- Greeting -->
                            <p>Hello {districtManager.FirstName},</p>

                            <!-- Explanation -->
                            <p>An OM79 entry has been submitted from district {omEntry.District} for review. You are responsible for reviewing the following based on your assigned role(s): </p>
                            <!-- Responsibilities Section -->
                            <p><strong>Your Responsibilities:</strong></p>
                            <ul style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF; list-style-type: none;'>
                                <li><strong>District Manager:</strong> Responsible for reviewing OM-79 package and attachments for accuracy and completeness.</li>
                            </ul>

                            <!-- Call to Action -->
                            <p style='text-align: center; margin-top: 20px;'>
                                <a href='{reviewLink}' 
                                   style='background: #28a745; color: white; text-decoration: none; padding: 12px 20px; border-radius: 5px; font-size: 16px; display: inline-block;'>
                                   Review & Sign OM79 Entry
                                </a>
                            </p>

                            <!-- Important Note -->
                            <p style='color: #0056b3; font-weight: bold; text-align: center; margin-top: 15px;'>
                                The signature form is located at the bottom of the page at the link above.
                            </p>

                            <hr style='border: 0; border-top: 1px solid #ddd; margin-top: 20px;'>

                            <!-- Footer -->
                            <p style='font-size: 12px; text-align: center; color: #667;'>Thank you,<br>OM79 Automated System</p>
                        </div>
                    </body>
                    </html>",
                    IsBodyHtml = true
                };

                message.To.Add(districtManager.Email);
                message.CC.Add("ethan.m.johnson@wv.gov");

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
