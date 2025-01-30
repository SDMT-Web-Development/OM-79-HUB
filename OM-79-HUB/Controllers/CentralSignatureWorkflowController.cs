using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OM_79_HUB.Data;
using OM_79_HUB.Models.DB.OM79Hub;
using OM79.Models.DB;
using SkiaSharp;
using System.Net.Mail;
using System.Net; 

namespace OM_79_HUB.Controllers
{
    public class CentralSignatureWorkflowController : Controller
    {
        private readonly Pj103Context _pj103Context;
        private readonly OM79Context _om79Context;
        private readonly OM_79_HUBContext _hubContext;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CentralSignatureWorkflowController(Pj103Context context, IWebHostEnvironment webHostEnvironment, OM79Context om79Context, OM_79_HUBContext hubContext)
        {
            _webHostEnvironment = webHostEnvironment;
            _hubContext = hubContext;
            _om79Context = om79Context;
            _pj103Context = context;
        }

        /*This is the functionality for the HDS user signing the OM79*/
        #region SignOMHubCentralHDS

       

        public async Task<IActionResult> SignOMCentralHDS()
        {
            var hubkey = int.Parse(Request.Form["HubKey"]); // Define hubkey here
            var decision = Request.Form["decisionHDS"];
            var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == hubkey);

            if (omEntry == null)
            {
                return NotFound("OM Entry not found");
            }

            if (decision == "approve")
            {
                var signature = new SignatureData
                {
                    HubKey = hubkey,
                    IsApprove = true,
                    IsDenied = false,
                    IsEdited = false,
                    Comments = Request.Form["commentsmodal"],
                    Signatures = Request.Form["signaturemodal"],
                    SigType = "HDS",
                    ENumber = HttpContext.User.Identity.Name,
                    DateSubmitted = DateTime.Now,
                    IsCurrentSig = true,
                };
                _hubContext.Add(signature);
                await _hubContext.SaveChangesAsync();

                // Need to figure out how to know if this has ever been to the GIS manager, using : HasGISReviewed (in omEntry)
                bool everEdited = omEntry.Edited ?? false;
                bool hasBeenReviewedAtLeastOnceByGIS = omEntry.HasGISReviewed ?? false;

                // Approval from the HDS leads to one of the following two roles: GIS Manager || District Manager

                // If this is the first Approval by HDS and it has not yet been to GIS
                // Go to GIS manager for them to approve/edit for the first time
                if (!hasBeenReviewedAtLeastOnceByGIS)
                {
                    //Should hit this on first approval from HDS user
                    //Email the GISManager(s) with the comments from the HDS user
                    //Update the workflow step here
                    omEntry.WorkflowStep = "SubmittedToCentralGIS";
                    await _hubContext.SaveChangesAsync();

                    await SendWorkflowEmailToGISManagers(hubkey, Request.Form["commentsmodal"]);
                }

                // If GIS has reviewed it already then it can go to the district manager and there have been edits
                // Go to District manager for them to approve the changes by one or both of the following users: HDS, GIS
                if (everEdited && hasBeenReviewedAtLeastOnceByGIS)
                {
                    omEntry.WorkflowStep = "SubmittedBackToDistrictManager";
                    await _hubContext.SaveChangesAsync();
                    await SendWorkflowEmailToDistrictManagerWithApprovedFromHDS(hubkey, Request.Form["commentsmodal"]);
                }
            }

            if (decision == "edited")
            {
                var signature = new SignatureData
                {
                    HubKey = hubkey,
                    IsApprove = false,
                    IsDenied = false,
                    IsEdited = true,
                    Comments = Request.Form["commentsmodal"],
                    Signatures = Request.Form["signaturemodal"],
                    SigType = "HDS",
                    ENumber = HttpContext.User.Identity.Name,
                    DateSubmitted = DateTime.Now,
                    IsCurrentSig = true,
                };
                _hubContext.Add(signature);
                await _hubContext.SaveChangesAsync();

                // So we know the HDS user just edited the OM79
                // Meaning that the only next step for the OM79 is to go to the GIS Manager(s)
                // Notify the GIS manager that there is a OM79 ready for review, and that the HDS user did make edits
                omEntry.WorkflowStep = "SubmittedToCentralGIS";
                omEntry.Edited = true;
                await _hubContext.SaveChangesAsync();
                await SendWorkflowEmailToGISManagersWithEditsFromHDS(hubkey, Request.Form["commentsmodal"]);
            }

            return RedirectToAction("Details", "Central79Hub", new { id = hubkey });
        }



        private async Task SendWorkflowEmailToGISManagersWithEditsFromHDS(int id, string comments)
        {
            try
            {
                var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
                if (omEntry == null)
                {
                    Console.WriteLine("OM Entry not found.");
                    return;
                }

                var gisManagers = _hubContext.UserData.Where(e => e.GISManager).ToList();
                if (!gisManagers.Any())
                {
                    Console.WriteLine("GIS Managers not found.");
                    return;
                }

                string reviewLink = $"https://dotapps.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}";

                // Construct the email body
                string emailBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; color: #333; background-color: #f8f8f8; padding: 20px;'>
                            <div style='max-width: 600px; margin: auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ddd;'>

                                <!-- Header -->
                                <h2 style='text-align: center; color: #0056b3;'>OM79 Submission Requires GIS Review</h2>
                                <p style='font-size: 14px; text-align: center;'>Entry ID: <strong>{omEntry.SmartID}</strong></p>
                                <hr style='border: 0; border-top: 1px solid #ddd;'>

                                <!-- Greeting -->
                                <p>Hello,</p>

                                <!-- Explanation -->
                                <p>An OM79 entry has been edited by the HDS user and is now awaiting your review. Please review the comments and make necessary approvals or edits.</p>

                                <!-- Role Responsibilities -->
                                <p><strong>Your Review Options:</strong></p>
                                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF;'>
                                    <p><strong style='color: #0056b3;'>Approve:</strong> Approving the entry will send it back to the District Manager for a Revision Review.</p>
                                    <p><strong style='color: #0056b3;'>Edit:</strong> Editing the entry will send it back to the HDS user for a Revision Review.</p>
                                </div>

                                <!-- Comments Section -->
                                <p><strong>Comments from HDS:</strong></p>
                                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF;'>
                                    <p>{comments}</p>
                                </div>

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

                // Compose the email
                var message = new MailMessage
                {
                    From = new MailAddress("DOTPJ103Srv@wv.gov"),
                    Subject = "OM79 Entry Ready for GIS Manager Review",
                    Body = emailBody,
                    IsBodyHtml = true
                };

                foreach (var gisManager in gisManagers)
                {
                    message.To.Add(gisManager.Email);
                                        message.CC.Add("ethan.m.johnson@wv.gov");

                }

                using (var client = new SmtpClient
                {
                    Host = "10.204.145.32",
                    Port = 25,
                    EnableSsl = false,
                    Credentials = new NetworkCredential("ethan.m.johnson@wv.gov", "trippononemails")
                })
                {
                    await client.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
            }
        }

        private async Task SendWorkflowEmailToDistrictManagerWithApprovedFromHDS(int id, string comments)
        {
            try
            {
                var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
                if (omEntry == null)
                {
                    Console.WriteLine("OM Entry not found.");
                    return;
                }

                var districtManagers = _hubContext.UserData.Where(e => e.DistrictManager && e.District == omEntry.District).ToList();
                if (!districtManagers.Any())
                {
                    Console.WriteLine("District Managers not found.");
                    return;
                }

                var HDSGISsig = _hubContext.SignatureData
                    .Where(e => e.HubKey == id && e.IsCurrentSig == true && (e.SigType == "HDS" || e.SigType == "GIS Manager"))
                    .ToList();

                var hdsComments = HDSGISsig.Where(e => e.SigType == "HDS")
                    .Select(e => new { e.Comments, e.DateSubmitted })
                    .ToList();

                var gisComments = HDSGISsig.Where(e => e.SigType == "GIS Manager")
                    .Select(e => new { e.Comments, e.DateSubmitted })
                    .ToList();

                var hdsCommentsHtml = string.Join("", hdsComments.Select(c => $@"
                        <div style='background: #f8f9fa; padding: 10px; border-radius: 8px; border-left: 5px solid #007BFF; margin-bottom: 10px;'>
                            <p><strong>Date Signed:</strong> {c.DateSubmitted}</p>
                            <p><strong>Comments:</strong> {c.Comments}</p>
                        </div>
                    "));

                var gisCommentsHtml = string.Join("", gisComments.Select(c => $@"
                        <div style='background: #f8f9fa; padding: 10px; border-radius: 8px; border-left: 5px solid #007BFF; margin-bottom: 10px;'>
                            <p><strong>Date Signed:</strong> {c.DateSubmitted}</p>
                            <p><strong>Comments:</strong> {c.Comments}</p>
                        </div>
                    "));


                string reviewLink = $"https://dotapps.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}";

                // Construct the email body
                string emailBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; color: #333; background-color: #f8f8f8; padding: 20px;'>
                            <div style='max-width: 600px; margin: auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ddd;'>

                                <!-- Header -->
                                <h2 style='text-align: center; color: #0056b3;'>OM79 Submission Requires District Manager Review</h2>
                                <p style='font-size: 14px; text-align: center;'>Entry ID: <strong>{omEntry.SmartID}</strong> | District: <strong>{omEntry.District}</strong></p>
                                <hr style='border: 0; border-top: 1px solid #ddd;'>

                                <!-- Greeting -->
                                <p>Hello,</p>

                                <!-- Explanation -->
                                <p>An OM79 entry from your district has been reviewed and edited by the HDS user(s) and/or the LRS Manager(s) from the central office. The updated version is now ready for your approval or further edits.</p>

                                <!-- Role Responsibilities -->
                                <p><strong>Your Review Options:</strong></p>
                                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF;'>
                                    <p><strong style='color: #0056b3;'>Approve:</strong> Approving the changes made by the central office will forward the OM79 to the Regional Engineer.</p>
                                    <p><strong style='color: #0056b3;'>Edit:</strong> Making additional edits to the OM79 will send it back to the HDS user, restarting the workflow.</p>
                                </div>

                                <!-- Comments Section -->
                                <p><strong>GIS Manager Comments:</strong></p>
                                {gisCommentsHtml}

                                <p><strong>HDS User Comments:</strong></p>
                                {hdsCommentsHtml}

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

                // Compose the email
                var message = new MailMessage
                {
                    From = new MailAddress("DOTPJ103Srv@wv.gov"),
                    Subject = "OM79 Entry Ready for District Manager Revision Review",
                    Body = emailBody,
                    IsBodyHtml = true
                };

                // Construct the email body

                foreach (var districtManager in districtManagers)
                {
                    message.To.Add(districtManager.Email);
                    message.CC.Add("ethan.m.johnson@wv.gov");
                }

                using (var client = new SmtpClient
                {
                    Host = "10.204.145.32",
                    Port = 25,
                    EnableSsl = false,
                    Credentials = new NetworkCredential("ethan.m.johnson@wv.gov", "trippononemails")
                })
                {
                    await client.SendMailAsync(message);    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
            }
        }


        private async Task SendWorkflowEmailToGISManagers(int id, string comments)
        {
            try
            {
                var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
                if (omEntry == null)
                {
                    Console.WriteLine("OM Entry not found.");
                    return;
                }

                var gisManagers = _hubContext.UserData.Where(e => e.GISManager).ToList();
                if (!gisManagers.Any())
                {
                    Console.WriteLine("GIS Managers not found.");
                    return;
                }

                string reviewLink = $"https://dotapps.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}";

                // Construct the email body
                string emailBody = $@"
                            <html>
                            <body style='font-family: Arial, sans-serif; color: #333; background-color: #f8f8f8; padding: 20px;'>
                                <div style='max-width: 600px; margin: auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ddd;'>

                                    <!-- Header -->
                                    <h2 style='text-align: center; color: #0056b3;'>OM79 Submission Requires GIS Review</h2>
                                    <p style='font-size: 14px; text-align: center;'>Entry ID: <strong>{omEntry.SmartID}</strong></p>
                                    <hr style='border: 0; border-top: 1px solid #ddd;'>

                                    <!-- Greeting -->
                                    <p>Hello,</p>

                                    <!-- Explanation -->
                                    <p>An OM79 entry has been reviewed by the HDS and is now awaiting your approval. As the GIS Manager, your review is crucial before the entry can proceed to the next step.</p>

                                    <!-- Role Responsibilities -->
                                    <p><strong>Your Review Options:</strong></p>
                                    <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF;'>
                                        <p><strong style='color: #0056b3;'>Approve:</strong> Approving the entry will send it to the Regional Engineer.</p>
                                        <p><strong style='color: #0056b3;'>Edit:</strong> Editing the entry will send it back to the HDS for another revision review.</p>
                                    </div>

                                    <!-- Comments Section -->
                                    <p><strong>Comments from HDS:</strong></p>
                                    <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF;'>
                                        <p>{comments}</p>
                                    </div>

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

                // Compose the email
                var message = new MailMessage
                {
                    From = new MailAddress("DOTPJ103Srv@wv.gov"),
                    Subject = "OM79 Entry Ready for GIS Manager Review",
                    Body = emailBody,
                    IsBodyHtml = true
                };

                foreach (var gisManager in gisManagers)
                {
                    message.To.Add(gisManager.Email);
                    message.CC.Add("ethan.m.johnson@wv.gov");

                }

                using (var client = new SmtpClient
                {
                    Host = "10.204.145.32",
                    Port = 25,
                    EnableSsl = false,
                    Credentials = new NetworkCredential("ethan.m.johnson@wv.gov", "trippononemails")
                })
                {
                    await client.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
            }
        }

        #endregion


        /*This is the functionality for the GIS Manager signing the OM79*/
        #region SignOMCentralGIS
        public async Task<IActionResult> SignOMCentralGIS()
        {
            var hubkey = int.Parse(Request.Form["HubKey"]); // Define hubkey here
            var decision = Request.Form["decisionGIS"];
            var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == hubkey);

            if (omEntry == null)
            {
                return NotFound("OM Entry not found");
            }

            // The GIS manager has just approved the OM79
            if (decision == "approve")
            {
                var signature = new SignatureData
                {
                    HubKey = hubkey,
                    IsApprove = true,
                    IsDenied = false,
                    IsEdited = false,
                    Comments = Request.Form["commentsmodal"],
                    Signatures = Request.Form["signaturemodal"],
                    SigType = "GIS Manager",
                    ENumber = HttpContext.User.Identity.Name,
                    DateSubmitted = DateTime.Now,
                    IsCurrentSig = true,
                };
                _hubContext.Add(signature);
                await _hubContext.SaveChangesAsync();

                // GIS manager has touched it at least once now
                if (omEntry.HasGISReviewed == false || omEntry.HasGISReviewed == null)
                {
                    omEntry.HasGISReviewed = true;
                    await _hubContext.SaveChangesAsync();
                }

                // There are two possible places this workflow can go to: District Manager or Regional Engineer
                // District Manager: If there have been edits made by either the HDS user, or the GIS Manager
                // Regional Engineer: If there have been NO edits made to the OM79
                bool everEdited = omEntry.Edited ?? false;

                if (everEdited)
                {
                    // Send email to the DistrictManager
                    omEntry.WorkflowStep = "SubmittedBackToDistrictManager";
                    await _hubContext.SaveChangesAsync();
                    await SendWorkflowEmailToDistrictManagerWithApprovalFromGISManager(hubkey, Request.Form["commentsmodal"]);

                    await _hubContext.SaveChangesAsync();
                }

                if (!everEdited)
                {
                    // Send email to the regionalEngineer
                    omEntry.WorkflowStep = "SubmittedToRegionalEngineer";
                    await _hubContext.SaveChangesAsync();
                    await SendWorkflowEmailToRegionalEngineerWithApprovalFromGISManagerOrDistrictManager(hubkey, Request.Form["commentsmodal"]);

                    // Can keep all of the signatures active here because there have been no edits made
                }
            }

            // The GIS Manager has just signed the OM79, but it has been edited
            if (decision == "edited")
            {
                var signature = new SignatureData
                {
                    HubKey = hubkey,
                    IsApprove = false,
                    IsDenied = false,
                    IsEdited = true,
                    Comments = Request.Form["commentsmodal"],
                    Signatures = Request.Form["signaturemodal"],
                    SigType = "GIS Manager",
                    ENumber = HttpContext.User.Identity.Name,
                    DateSubmitted = DateTime.Now,
                    IsCurrentSig = true,
                };
                _hubContext.Add(signature);
                await _hubContext.SaveChangesAsync();

                // GIS manager has touched it at least once now
                if (omEntry.HasGISReviewed == false || omEntry.HasGISReviewed == null)
                {
                    omEntry.HasGISReviewed = true;
                    await _hubContext.SaveChangesAsync();
                }

                // So we know the GIS manager just edited the OM79
                // Meaning that the only next step for the OM79 is to go to the HDS user(s)
                // Notify the GIS manager that there is a OM79 ready for review, and that the HDS user did make edits
                omEntry.WorkflowStep = "SubmittedToCentralHDS";
                omEntry.Edited = true;
                omEntry.HasGISReviewed = true;
                await _hubContext.SaveChangesAsync();
                await SendWorkflowEmailToHDSWithEditsFromGISManager(hubkey, Request.Form["commentsmodal"]);
            }
            return RedirectToAction("Details", "Central79Hub", new { id = hubkey });
        }

        //public async Task<IActionResult> SignOMCentralGIS()
        //{
        //    var hubkey = int.Parse(Request.Form["HubKey"]); // Define hubkey here
        //    var approved = Request.Form["apradio"];
        //    var edited = Request.Form["editradio"];
        //    var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == hubkey);

        //    if (omEntry == null)
        //    {
        //        return NotFound("OM Entry not found");
        //    }

        //    //The GIS manager has just approved the OM79
        //    if (approved.FirstOrDefault() == "approve")
        //    {
        //        var signature = new SignatureData
        //        {
        //            HubKey = hubkey,
        //            IsApprove = true,
        //            IsDenied = false,
        //            IsEdited = false,
        //            Comments = Request.Form["commentsmodal"],
        //            Signatures = Request.Form["signaturemodal"],
        //            SigType = "GIS Manager",                    
        //            ENumber = HttpContext.User.Identity.Name,
        //            DateSubmitted = DateTime.Now,
        //            IsCurrentSig = true,
        //        };
        //        _hubContext.Add(signature);
        //        await _hubContext.SaveChangesAsync();

        //        // GIS manager has touched it at least once now:::
        //        if (omEntry.HasGISReviewed == false || omEntry.HasGISReviewed == null)
        //        {
        //            omEntry.HasGISReviewed = true;
        //            await _hubContext.SaveChangesAsync();
        //        }

        //        // There are two possible places this workflow can go to: District Manager or Regional Engineer
        //        // District Manager: If there have been edits made by either the HDS user, or the GIS Manager
        //        // Regional Engineer: If there have been NO edits made to the OM79
        //        bool everEdited = omEntry.Edited ?? false;

        //        if (everEdited)
        //        {
        //            //send email to the DistrictManager
        //            omEntry.WorkflowStep = "SubmittedBackToDistrictManager";
        //            await _hubContext.SaveChangesAsync();
        //            await SendWorkflowEmailToDistrictManagerWithApprovalFromGISManager(hubkey, Request.Form["commentsmodal"]);

        //            //Maybe put this into the District Manager: Approve / Edit signature field and only deactivate them when the District manager edits the OM79 and sends it back to the HDS user
        //            //// Deactivate the current HDS & GIS Manager Signatures here
        //            //var currentSignatures = _hubContext.SignatureData
        //            //    .Where(e => e.HubKey == hubkey && e.IsCurrentSig == true && (e.SigType == "HDS" || e.SigType == "GIS Manager"))
        //            //    .ToList();

        //            //foreach (var signatureToDeactivate in currentSignatures)
        //            //{
        //            //    signatureToDeactivate.IsCurrentSig = false;
        //            //}

        //            await _hubContext.SaveChangesAsync();
        //        }

        //        if (!everEdited)
        //        {
        //            //Send email to the regionalEngineer
        //            omEntry.WorkflowStep = "SubmittedToRegionalEngineer";
        //            await _hubContext.SaveChangesAsync();
        //            await SendWorkflowEmailToRegionalEngineerWithApprovalFromGISManagerOrDistrictManager(hubkey, Request.Form["commentsmodal"]);

        //            //Can keep all of the signatures active here becuase there have been no edits made
        //        }
        //    }

        //    // The GIS Manager has just signed the OM79, but it has been edited
        //    if (edited.FirstOrDefault() == "edited")
        //    {
        //        var signature = new SignatureData
        //        {
        //            HubKey = hubkey,
        //            IsApprove = false,
        //            IsDenied = false,
        //            IsEdited = true,
        //            Comments = Request.Form["commentsmodal"],
        //            Signatures = Request.Form["signaturemodal"],
        //            SigType = "GIS Manager",
        //            ENumber = HttpContext.User.Identity.Name,
        //            DateSubmitted = DateTime.Now,
        //            IsCurrentSig = true,
        //        };
        //        _hubContext.Add(signature);
        //        await _hubContext.SaveChangesAsync();

        //        // GIS manager has touched it at least once now:::
        //        if (omEntry.HasGISReviewed == false || omEntry.HasGISReviewed == null)
        //        {
        //            omEntry.HasGISReviewed = true;
        //            await _hubContext.SaveChangesAsync();
        //        }

        //        //!!!!!!! Might need to change the initial HDS signature to IsActive = false;?

        //        // So we know the GIS manager just edited the OM79
        //        // Meaning that the only next step for the OM79 is to go to the HDS user(s)
        //        // Notify the GIS manager that there is a OM79 ready for review, and that the HDS user did make edits
        //        omEntry.WorkflowStep = "SubmittedToCentralHDS";
        //        omEntry.Edited = true;
        //        omEntry.HasGISReviewed = true;
        //        await _hubContext.SaveChangesAsync();
        //        await SendWorkflowEmailToHDSWithEditsFromGISManager(hubkey, Request.Form["commentsmodal"]);

        //    }
        //    return RedirectToAction("Details", "Central79Hub", new { id = hubkey });
        //}


        private async Task SendWorkflowEmailToDistrictManagerWithApprovalFromGISManager(int id, string comments)
        {
            try
            {
                var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
                if (omEntry == null)
                {
                    Console.WriteLine("OM Entry not found.");
                    return;
                }

                var districtManagers = _hubContext.UserData.Where(e => e.DistrictManager && e.District == omEntry.District).ToList();
                if (!districtManagers.Any())
                {
                    Console.WriteLine("District Managers not found.");
                    return;
                }

                var HDSGISsig = _hubContext.SignatureData
                    .Where(e => e.HubKey == id && e.IsCurrentSig == true && (e.SigType == "HDS" || e.SigType == "GIS Manager"))
                    .ToList();

                var hdsComments = HDSGISsig.Where(e => e.SigType == "HDS")
                    .Select(e => new { e.Comments, e.DateSubmitted })
                    .ToList();

                var gisComments = HDSGISsig.Where(e => e.SigType == "GIS Manager")
                    .Select(e => new { e.Comments, e.DateSubmitted })
                    .ToList();

                var hdsCommentsHtml = string.Join("", hdsComments.Select(c => $@"
                        <div style='background: #f8f9fa; padding: 10px; border-radius: 8px; border-left: 5px solid #007BFF; margin-bottom: 10px;'>
                            <p><strong>Date Signed:</strong> {c.DateSubmitted}</p>
                            <p><strong>Comments:</strong> {c.Comments}</p>
                        </div>
                    "));

                var gisCommentsHtml = string.Join("", gisComments.Select(c => $@"
                        <div style='background: #f8f9fa; padding: 10px; border-radius: 8px; border-left: 5px solid #007BFF; margin-bottom: 10px;'>
                            <p><strong>Date Signed:</strong> {c.DateSubmitted}</p>
                            <p><strong>Comments:</strong> {c.Comments}</p>
                        </div>
                    "));

                string reviewLink = $"https://dotapps.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}";

                // Construct the email body
                string emailBody = $@"
                            <html>
                            <body style='font-family: Arial, sans-serif; color: #333; background-color: #f8f8f8; padding: 20px;'>
                                <div style='max-width: 600px; margin: auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ddd;'>

                                    <!-- Header -->
                                    <h2 style='text-align: center; color: #0056b3;'>OM79 Submission Requires District Manager Review</h2>
                                    <p style='font-size: 14px; text-align: center;'>Entry ID: <strong>{omEntry.SmartID}</strong> | District: <strong>{omEntry.District}</strong></p>
                                    <hr style='border: 0; border-top: 1px solid #ddd;'>

                                    <!-- Greeting -->
                                    <p>Hello,</p>

                                    <!-- Explanation -->
                                    <p>An OM79 entry from your district has been reviewed and edited by the HDS user(s) and/or the GIS Manager(s) from the central office. It is now ready for your approval or further edits.</p>

                                    <!-- Role Responsibilities -->
                                    <p><strong>Your Review Options:</strong></p>
                                    <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF;'>
                                        <p><strong style='color: #0056b3;'>Approve:</strong> Approving the changes made by the central office will forward the OM79 to the Regional Engineer.</p>
                                        <p><strong style='color: #0056b3;'>Making Edits:</strong> Making additional edits to the OM79 will send it back to the HDS user, restarting the workflow.</p>
                                    </div>

                                    <!-- Comments Section -->
                                    <p><strong>GIS Manager Comments:</strong></p>
                                    {gisCommentsHtml}

                                    <p><strong>HDS User Comments:</strong></p>
                                    {hdsCommentsHtml}

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
                            </html>";

                // Compose the email
                var message = new MailMessage
                {
                    From = new MailAddress("DOTPJ103Srv@wv.gov"),
                    Subject = "OM79 Entry Ready for District Manager Revision Review",
                    Body = emailBody,
                    IsBodyHtml = true
                };

                foreach (var districtManager in districtManagers)
                {
                    message.To.Add(districtManager.Email);
                    message.CC.Add("ethan.m.johnson@wv.gov");

                }

                using (var client = new SmtpClient
                {
                    Host = "10.204.145.32",
                    Port = 25,
                    EnableSsl = false,
                    Credentials = new NetworkCredential("ethan.m.johnson@wv.gov", "trippononemails")
                })
                {
                    await client.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
            }
        }


        private async Task SendWorkflowEmailToRegionalEngineerWithApprovalFromGISManagerOrDistrictManager(int id, string comments)
        {
            try
            {
                var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
                if (omEntry == null)
                {
                    Console.WriteLine("OM Entry not found.");
                    return;
                }

                var omEntryDistrict = omEntry.District?.ToString();  // Convert the nullable int to string
                var regionalEngineers = _hubContext.UserData.Where(e => e.RegionalEngineer).ToList();
                if (!regionalEngineers.Any())
                {
                    Console.WriteLine("Regional Engineers not found.");
                    return;
                }

                string reviewLink = $"https://dotapps.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}";

                // Construct the email body
                string emailBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; color: #333; background-color: #f8f8f8; padding: 20px;'>
                            <div style='max-width: 600px; margin: auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ddd;'>

                                <!-- Header -->
                                <h2 style='text-align: center; color: #0056b3;'>OM79 Submission Requires Regional Engineer Review</h2>
                                <p style='font-size: 14px; text-align: center;'>Entry ID: <strong>{omEntry.SmartID}</strong> | District: <strong>{omEntryDistrict}</strong></p>
                                <hr style='border: 0; border-top: 1px solid #ddd;'>

                                <!-- Greeting -->
                                <p>Hello,</p>

                                <!-- Explanation -->
                                <p>An OM79 entry has been approved by the District, HDS user(s), and GIS Manager(s) and is now awaiting your review. As the Regional Engineer, your approval is crucial for finalizing this entry.</p>

                                <!-- Role Responsibilities -->
                                <p><strong>Your Review Options:</strong></p>
                                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF;'>
                                    <p><strong style='color: #0056b3;'>Approve:</strong> Approving this entry will forward the OM79 to the Director of Operations.</p>
                                    <p><strong style='color: #0056b3;'>Deny:</strong> Denying this entry will send it back to the District Manager with your requested changes. The District Manager can then either perform the requested changes or close the request.</p>
                                </div>

                                <!-- Comments Section -->
                                <p><strong>Additional Notes:</strong></p>
                                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF;'>
                                    <p>{comments}</p>
                                </div>

                                <!-- Call to Action -->
                                <p style='text-align: center; margin-top: 20px;'>
                                    <a href='{reviewLink}' 
                                       style='background: #28a745; color: white; text-decoration: none; padding: 12px 20px; border-radius: 5px; font-size: 16px; display: inline-block;'>
                                       Review & Take Action on OM79 Entry
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
                        </html>";

                // Compose the email
                var message = new MailMessage
                {
                    From = new MailAddress("DOTPJ103Srv@wv.gov"),
                    Subject = $"OM79 Entry Ready for Regional Engineer Review, From District [{omEntryDistrict}]",
                    Body = emailBody,
                    IsBodyHtml = true
                };

                foreach (var regionalEngineer in regionalEngineers)
                {
                    if (regionalEngineer.DistrictsForRegionalEngineer != null)
                    {
                        var districts = regionalEngineer.DistrictsForRegionalEngineer.Split(',')
                            .Select(d => d.Trim()).ToList();

                        if (districts.Contains(omEntryDistrict))
                        {
                            message.To.Add(regionalEngineer.Email);
                            message.CC.Add("ethan.m.johnson@wv.gov");

                        }
                    }
                }

                using (var client = new SmtpClient
                {
                    Host = "10.204.145.32",
                    Port = 25,
                    EnableSsl = false,
                    Credentials = new NetworkCredential("ethan.m.johnson@wv.gov", "trippononemails")
                })
                {
                    await client.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
            }
        }


        private async Task SendWorkflowEmailToHDSWithEditsFromGISManager(int id, string comments)
        {
            try
            {
                var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
                if (omEntry == null)
                {
                    Console.WriteLine("OM Entry not found.");
                    return;
                }

                var hdsUsers = _hubContext.UserData.Where(e => e.HDS).ToList();
                if (!hdsUsers.Any())
                {
                    Console.WriteLine("HDS Users not found.");
                    return;
                }

                string reviewLink = $"https://dotapps.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}";

                // Construct the email body
                string emailBody = $@"
                            <html>
                            <body style='font-family: Arial, sans-serif; color: #333; background-color: #f8f8f8; padding: 20px;'>
                                <div style='max-width: 600px; margin: auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ddd;'>

                                    <!-- Header -->
                                    <h2 style='text-align: center; color: #0056b3;'>OM79 Submission Requires Your Review</h2>
                                    <p style='font-size: 14px; text-align: center;'>Entry ID: <strong>{omEntry.SmartID}</strong></p>
                                    <hr style='border: 0; border-top: 1px solid #ddd;'>

                                    <!-- Greeting -->
                                    <p>Hello,</p>

                                    <!-- Explanation -->
                                    <p>An OM79 entry has been edited by the GIS Manager and is now awaiting your review. As an HDS user, your review is crucial for finalizing this entry.</p>

                                    <!-- Role Responsibilities -->
                                    <p><strong>Your Review Options:</strong></p>
                                    <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF;'>
                                        <p><strong style='color: #0056b3;'>Approve:</strong> Approving the changes will forward the OM79 back to the District Manager for review of the revisions.</p>
                                        <p><strong style='color: #0056b3;'>Edit:</strong> Making additional edits will send the OM79 back to the GIS Manager for further review.</p>
                                    </div>

                                    <!-- Comments Section -->
                                    <p><strong>Comments from GIS Manager:</strong></p>
                                    <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF;'>
                                        <p>{comments}</p>
                                    </div>

                                    <!-- Call to Action -->
                                    <p style='text-align: center; margin-top: 20px;'>
                                        <a href='{reviewLink}' 
                                           style='background: #28a745; color: white; text-decoration: none; padding: 12px 20px; border-radius: 5px; font-size: 16px; display: inline-block;'>
                                           Review & Sign OM79
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
                            </html>";

                // Compose the email
                var message = new MailMessage
                {
                    From = new MailAddress("DOTPJ103Srv@wv.gov"),
                    Subject = "OM79 Entry Has Been Edited By GIS Manager",
                    Body = emailBody,
                    IsBodyHtml = true
                };

                foreach (var hdsUser in hdsUsers)
                {
                    message.To.Add(hdsUser.Email);
                    message.CC.Add("ethan.m.johnson@wv.gov");

                }

                using (var client = new SmtpClient
                {
                    Host = "10.204.145.32",
                    Port = 25,
                    EnableSsl = false,
                    Credentials = new NetworkCredential("ethan.m.johnson@wv.gov", "trippononemails")
                })
                {
                    await client.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
            }
        }




        #endregion


        /*This is the functionality for the District Manager Reviewing/Editing the OM79, ie There were edits made by the central office (HDS & GIS),
         * and the district manager needs to review the changes before it is sent to operations (Regional Engineer, Director of Operation, Chief Engineer of Operations)*/
        #region SignOMCentralDistrictManager



        public async Task<IActionResult> SignOMCentralDistrictManager()
        {
            var hubkey = int.Parse(Request.Form["HubKey"]); // Define hubkey here
            var decision = Request.Form["decisionDistrict"];
            var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == hubkey);

            if (omEntry == null)
            {
                return NotFound("OM Entry not found");
            }

            // The District Manager has just approved the revised version of the OM79
            if (decision == "approve")
            {
                var signature = new SignatureData
                {
                    HubKey = hubkey,
                    IsApprove = true,
                    IsDenied = false,
                    IsEdited = false,
                    Comments = Request.Form["commentsmodal"],
                    Signatures = Request.Form["signaturemodal"],
                    SigType = "District Manager",
                    ENumber = HttpContext.User.Identity.Name,
                    DateSubmitted = DateTime.Now,
                    IsCurrentSig = true,
                };
                _hubContext.Add(signature);
                await _hubContext.SaveChangesAsync();

                // When the District Manager approves the changes made by the HDS and GIS Managers
                // Send email to the Regional Engineer that is over their district
                // Using the same workflow email as the one that gets sent when there are two approvals on the first run from the HDS user and GIS manager

                omEntry.WorkflowStep = "SubmittedToRegionalEngineer";
                await _hubContext.SaveChangesAsync();  // Save all changes in one call
                await SendWorkflowEmailToRegionalEngineerWithApprovalFromGISManagerOrDistrictManager(hubkey, Request.Form["commentsmodal"]);
            }

            // The District Manager has just edited the revised version of the OM79
            if (decision == "edited")
            {
                var signature = new SignatureData
                {
                    HubKey = hubkey,
                    IsApprove = false,
                    IsDenied = false,
                    IsEdited = true,
                    Comments = Request.Form["commentsmodal"],
                    Signatures = Request.Form["signaturemodal"],
                    SigType = "District Manager",
                    ENumber = HttpContext.User.Identity.Name,
                    DateSubmitted = DateTime.Now,
                    IsCurrentSig = true,
                };
                _hubContext.Add(signature);
                await _hubContext.SaveChangesAsync();

                // When the District Manager edits the changes made by the HDS and GIS Managers
                // Send email to the HDS user from the central office
                // Need to turn all current central office signatures to inactive
                // Restart Central Office Workflow
                omEntry.Edited = false;
                omEntry.HasGISReviewed = false;
                omEntry.WorkflowStep = "SubmittedToCentralHDS";

                var allCentralOfficeSignatures = _hubContext.SignatureData
                    .Where(e => e.HubKey == hubkey && e.IsCurrentSig == true && (e.SigType == "HDS" || e.SigType == "GIS Manager"))
                    .ToList();

                foreach (var sig in allCentralOfficeSignatures)
                {
                    sig.IsCurrentSig = false;
                    _hubContext.Update(sig);  // Ensure that the entity state is updated
                }
                await _hubContext.SaveChangesAsync();  // Save all changes in one call

                await SendWorkflowEmailToHDSWithEditsFromDistrictManager(hubkey, Request.Form["commentsmodal"]);
            }

            return RedirectToAction("Details", "Central79Hub", new { id = hubkey });
        }


        //public async Task<IActionResult> SignOMCentralDistrictManager()
        //{
        //    var hubkey = int.Parse(Request.Form["HubKey"]); // Define hubkey here
        //    var approved = Request.Form["apradio"];
        //    var edited = Request.Form["editradio"];
        //    var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == hubkey);

        //    if (omEntry == null)
        //    {
        //        return NotFound("OM Entry not found");
        //    }
        //    //The District Manager has just approved the revised version of the OM79
        //    if (approved.FirstOrDefault() == "approve")
        //    {
        //        var signature = new SignatureData
        //        {
        //            HubKey = hubkey,
        //            IsApprove = true,
        //            IsDenied = false,
        //            IsEdited = false,
        //            Comments = Request.Form["commentsmodal"],
        //            Signatures = Request.Form["signaturemodal"],
        //            SigType = "District Manager",
        //            ENumber = HttpContext.User.Identity.Name,
        //            DateSubmitted = DateTime.Now,
        //            IsCurrentSig = true,
        //        };
        //        _hubContext.Add(signature);
        //        await _hubContext.SaveChangesAsync();


        //        //When the District Manager approves the changes made by the HDS and GIS Managers
        //        //Send email to the Regional Engineer that is over their district
        //        //Using the same workflow email as the one that gets sent when there are two approvals on the first run from the HDS user and GIS manager

        //        omEntry.WorkflowStep = "SubmittedToRegionalEngineer";
        //        await _hubContext.SaveChangesAsync();  // Save all changes in one call
        //        await SendWorkflowEmailToRegionalEngineerWithApprovalFromGISManagerOrDistrictManager(hubkey, Request.Form["commentsmodal"]);

        //    }


        //    //The District Manager has just edited the revised version of the OM79
        //    if (edited.FirstOrDefault() == "edited")
        //    {
        //        var signature = new SignatureData
        //        {
        //            HubKey = hubkey,
        //            IsApprove = false,
        //            IsDenied = false,
        //            IsEdited = true,
        //            Comments = Request.Form["commentsmodal"],
        //            Signatures = Request.Form["signaturemodal"],
        //            SigType = "District Manager",
        //            ENumber = HttpContext.User.Identity.Name,
        //            DateSubmitted = DateTime.Now,
        //            IsCurrentSig = true,
        //        };
        //        _hubContext.Add(signature);
        //        await _hubContext.SaveChangesAsync();


        //        //When the District Manager edits the changes made by the HDS and GIS Managers
        //        //Send email to the HDS user from the central office
        //        // !!!! Need to turn all current central office signatures to inactive. 
        //        // Restart Central Office Workflow


        //        omEntry.HasGISReviewed = false;
        //        omEntry.WorkflowStep = "SubmittedToCentralHDS";

        //        var allCentralOfficeSignatures = _hubContext.SignatureData
        //                           .Where(e => e.HubKey == hubkey && e.IsCurrentSig == true && (e.SigType == "HDS" || e.SigType == "GIS Manager"))
        //                           .ToList();

        //        foreach(var sig in allCentralOfficeSignatures)
        //        {
        //            sig.IsCurrentSig = false;
        //            _hubContext.Update(sig);  // Ensure that the entity state is updated
        //        }
        //        await _hubContext.SaveChangesAsync();  // Save all changes in one call

        //        await SendWorkflowEmailToHDSWithEditsFromDistrictManager(hubkey, Request.Form["commentsmodal"]);
        //    }
        //    return RedirectToAction("Details", "Central79Hub", new { id = hubkey });
        //}

        private async Task SendWorkflowEmailToHDSWithEditsFromDistrictManager(int id, string comments)
        {
            try
            {
                var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
                if (omEntry == null)
                {
                    Console.WriteLine("OM Entry not found.");
                    return;
                }

                var hdsUsers = _hubContext.UserData.Where(e => e.HDS).ToList();
                if (!hdsUsers.Any())
                {
                    Console.WriteLine("HDS Users not found.");
                    return;
                }

                string reviewLink = $"https://dotapps.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}";

                // Construct the email body
                string emailBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; color: #333; background-color: #f8f8f8; padding: 20px;'>
                            <div style='max-width: 600px; margin: auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ddd;'>

                                <!-- Header -->
                                <h2 style='text-align: center; color: #0056b3;'>Revised OM79 Entry Edited by District Manager</h2>
                                <p style='font-size: 14px; text-align: center;'>Entry ID: <strong>{omEntry.SmartID}</strong></p>
                                <hr style='border: 0; border-top: 1px solid #ddd;'>

                                <!-- Greeting -->
                                <p>Hello,</p>

                                <!-- Explanation -->
                                <p>The District Manager has made additional edits to the revised OM79 entry that you have already reviewed. These edits have restarted the workflow.</p>
                                <p>As an HDS user, your review is crucial for finalizing the changes.</p>

                                <!-- Role Responsibilities -->
                                <p><strong>Your Review Options:</strong></p>
                                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF;'>
                                    <p><strong style='color: #0056b3;'>Approve:</strong> Approving the entry will send it to the GIS Manager for review.</p>
                                    <p><strong style='color: #0056b3;'>Edit:</strong> Editing the entry will also send it to the GIS Manager for review.</p>
                                </div>

                                <!-- Comments Section -->
                                <p><strong>Comments from District Manager:</strong></p>
                                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF;'>
                                    <p>{comments}</p>
                                </div>

                                <!-- Call to Action -->
                                <p style='text-align: center; margin-top: 20px;'>
                                    <a href='{reviewLink}' 
                                       style='background: #28a745; color: white; text-decoration: none; padding: 12px 20px; border-radius: 5px; font-size: 16px; display: inline-block;'>
                                       Review & Sign OM79
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
                        </html>";

                // Compose the email
                var message = new MailMessage
                {
                    From = new MailAddress("DOTPJ103Srv@wv.gov"),
                    Subject = "Revised OM79 Entry Edited by District Manager",
                    Body = emailBody,
                    IsBodyHtml = true
                };


                foreach (var hdsUser in hdsUsers)
                {
                    message.To.Add(hdsUser.Email);
                    message.CC.Add("ethan.m.johnson@wv.gov");

                }

                using (var client = new SmtpClient
                {
                    Host = "10.204.145.32",
                    Port = 25,
                    EnableSsl = false,
                    Credentials = new NetworkCredential("ethan.m.johnson@wv.gov", "trippononemails")
                })
                {
                    await client.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
            }
        }
        #endregion

        /*This is the functionality for the Regional Engineer for Approving/Denying the OM79*/
        #region SignOMCentralRegionalEngineer


        public async Task<IActionResult> SignOMCentralRegionalEngineer()
        {
            var hubkey = int.Parse(Request.Form["HubKey"]); // Define hubkey here
            var decision = Request.Form["decisionRegionalEngineer"];
            var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == hubkey);

            if (omEntry == null)
            {
                return NotFound("OM Entry not found");
            }
            var userRole = "Regional Engineer"; // Adjust as needed for other roles

            // The regional engineer has just approved the OM79, it needs to go to the RegionalDirectorOfOperations
            if (decision == "approve")
            {
                var signature = new SignatureData
                {
                    HubKey = hubkey,
                    IsApprove = true,
                    IsDenied = false,
                    IsEdited = false,
                    Comments = Request.Form["commentsmodal"],
                    Signatures = Request.Form["signaturemodal"],
                    SigType = "Regional Engineer",
                    ENumber = HttpContext.User.Identity.Name,
                    DateSubmitted = DateTime.Now,
                    IsCurrentSig = true,
                };
                _hubContext.Add(signature);
                await _hubContext.SaveChangesAsync();

                omEntry.WorkflowStep = "SubmittedToDirectorOfOperations";
                await _hubContext.SaveChangesAsync();
                await SendWorkflowEmailToDirectorOfOperations(hubkey, Request.Form["commentsmodal"]);
                await SendWorkflowUpdateEmailToHDSUser(hubkey, userRole);
            }

            // The regional engineer has just denied the OM79.
            if (decision == "deny")
            {
                var signature = new SignatureData
                {
                    HubKey = hubkey,
                    IsApprove = false,
                    IsDenied = true,
                    IsEdited = false,
                    Comments = Request.Form["commentsmodal"],
                    Signatures = Request.Form["signaturemodal"],
                    SigType = "Regional Engineer",
                    ENumber = HttpContext.User.Identity.Name,
                    DateSubmitted = DateTime.Now,
                    IsCurrentSig = true,
                };
                _hubContext.Add(signature);
                await _hubContext.SaveChangesAsync();

                omEntry.WorkflowStep = "SubmittedBackToDistrictManagerFromOperations";
                await _hubContext.SaveChangesAsync();

                await SendDenialEmailToDistrictManager(hubkey, Request.Form["commentsmodal"], "Regional Engineer");
                await SendDenialUpdateEmailToHDSUser(hubkey, "Regional Engineer");

                omEntry.HasGISReviewed = false;

                var allCentralOfficeSignatures = _hubContext.SignatureData
                                       .Where(e => e.HubKey == hubkey && e.IsCurrentSig == true && (e.SigType == "HDS" || e.SigType == "GIS Manager" || e.SigType == "Regional Engineer"))
                                       .ToList();

                foreach (var sig in allCentralOfficeSignatures)
                {
                    sig.IsCurrentSig = false;
                    _hubContext.Update(sig);  // Ensure that the entity state is updated
                }
                await _hubContext.SaveChangesAsync();  // Save all changes in one call
            }

            return RedirectToAction("Details", "Central79Hub", new { id = hubkey });
        }

        //public async Task<IActionResult> SignOMCentralRegionalEngineer()
        //{
        //    var hubkey = int.Parse(Request.Form["HubKey"]); // Define hubkey here
        //    var approved = Request.Form["apradio"];
        //    var denied = Request.Form["denradio"];
        //    var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == hubkey);

        //    if (omEntry == null)
        //    {
        //        return NotFound("OM Entry not found");
        //    }
        //    var userRole = "Regional Engineer"; // Adjust as needed for other roles

        //    //The regional engineer has just approved the OM79, it needs to go to the RegionalDirectorOfOperations
        //    if (approved.FirstOrDefault() == "approve")
        //    {
        //        var signature = new SignatureData
        //        {
        //            HubKey = hubkey,
        //            IsApprove = true,
        //            IsDenied = false,
        //            IsEdited = false,
        //            Comments = Request.Form["commentsmodal"],
        //            Signatures = Request.Form["signaturemodal"],
        //            SigType = "Regional Engineer",
        //            ENumber = HttpContext.User.Identity.Name,
        //            DateSubmitted = DateTime.Now,
        //            IsCurrentSig = true,
        //        };
        //        _hubContext.Add(signature);
        //        await _hubContext.SaveChangesAsync();

        //        omEntry.WorkflowStep = "SubmittedToDirectorOfOperations";
        //        await _hubContext.SaveChangesAsync();
        //        await SendWorkflowEmailToDirectorOfOperations(hubkey, Request.Form["commentsmodal"]);
        //        await SendWorkflowUpdateEmailToHDSUser(hubkey, userRole);

        //    }

        //    //The regional engineer has just denied the OM79. 
        //    if (denied.FirstOrDefault() == "deny")
        //    {
        //        var signature = new SignatureData
        //        {
        //            HubKey = hubkey,
        //            IsApprove = false,
        //            IsDenied = true,
        //            IsEdited = false,
        //            Comments = Request.Form["commentsmodal"],
        //            Signatures = Request.Form["signaturemodal"],
        //            SigType = "Regional Engineer",
        //            ENumber = HttpContext.User.Identity.Name,
        //            DateSubmitted = DateTime.Now,
        //            IsCurrentSig = true,
        //        };
        //        _hubContext.Add(signature);
        //        await _hubContext.SaveChangesAsync();

        //        omEntry.WorkflowStep = "SubmittedBackToDistrictManagerFromOperations";
        //        await _hubContext.SaveChangesAsync();

        //        await SendDenialEmailToDistrictManager(hubkey, Request.Form["commentsmodal"], "Regional Engineer");
        //        await SendDenialUpdateEmailToHDSUser(hubkey, "Regional Engineer");

        //        omEntry.HasGISReviewed = false;

        //        var allCentralOfficeSignatures = _hubContext.SignatureData
        //                           .Where(e => e.HubKey == hubkey && e.IsCurrentSig == true && (e.SigType == "HDS" || e.SigType == "GIS Manager" || e.SigType == "Regional Engineer"))
        //                           .ToList();

        //        foreach (var sig in allCentralOfficeSignatures)
        //        {
        //            sig.IsCurrentSig = false;
        //            _hubContext.Update(sig);  // Ensure that the entity state is updated
        //        }
        //        await _hubContext.SaveChangesAsync();  // Save all changes in one call

        //    }

        //    return RedirectToAction("Details", "Central79Hub", new { id = hubkey });
        //}
        private async Task SendWorkflowEmailToDirectorOfOperations(int id, string comments)
        {
            try
            {
                var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
                if (omEntry == null)
                {
                    Console.WriteLine("OM Entry not found.");
                    return;
                }

                var directorOfOperations = _hubContext.UserData.FirstOrDefault(e => e.DirectorOfOperations == true);
                if (directorOfOperations == null)
                {
                    Console.WriteLine("Director of Operations not found.");
                    return;
                }

                string reviewLink = $"https://dotapps.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}";

                // Construct the email body
                string emailBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; color: #333; background-color: #f8f8f8; padding: 20px;'>
                            <div style='max-width: 600px; margin: auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ddd;'>

                                <!-- Header -->
                                <h2 style='text-align: center; color: #0056b3;'>OM79 Submission Requires Your Review</h2>
                                <p style='font-size: 14px; text-align: center;'>Entry ID: <strong>{omEntry.SmartID}</strong></p>
                                <hr style='border: 0; border-top: 1px solid #ddd;'>

                                <!-- Greeting -->
                                <p>Hello,</p>

                                <!-- Explanation -->
                                <p>An OM79 entry has been approved by the Regional Engineer and is now awaiting your review. As the Director of Operations, your review is crucial for finalizing this entry.</p>

                                <!-- Role Responsibilities -->
                                <p><strong>Your Review Options:</strong></p>
                                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF;'>
                                    <p><strong style='color: #0056b3;'>Approve:</strong> Approving this entry will forward the OM79 to the Chief Engineer of Operations.</p>
                                    <p><strong style='color: #0056b3;'>Deny:</strong> Denying this entry will send it back to the District Manager with your requested changes. The District Manager can then either make the requested edits and restart the workflow or close the request.</p>
                                </div>

                                <!-- Comments Section -->
                                <p><strong>Comments from Regional Engineer:</strong></p>
                                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF;'>
                                    <p>{comments}</p>
                                </div>

                                <!-- Call to Action -->
                                <p style='text-align: center; margin-top: 20px;'>
                                    <a href='{reviewLink}' 
                                       style='background: #28a745; color: white; text-decoration: none; padding: 12px 20px; border-radius: 5px; font-size: 16px; display: inline-block;'>
                                       Review & Take Action on OM79 Entry
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
                        </html>";

                // Compose the email
                var message = new MailMessage
                {
                    From = new MailAddress("DOTPJ103Srv@wv.gov"),
                    Subject = "OM79 Entry Ready for Director of Operations Review",
                    Body = emailBody,
                    IsBodyHtml = true
                };

                message.To.Add(directorOfOperations.Email);
                message.CC.Add("ethan.m.johnson@wv.gov");


                using (var client = new SmtpClient
                {
                    Host = "10.204.145.32",
                    Port = 25,
                    EnableSsl = false,
                    Credentials = new NetworkCredential("ethan.m.johnson@wv.gov", "trippononemails")
                })
                {
                    await client.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
            }
        }
        private async Task SendWorkflowUpdateEmailToHDSUser(int id, string userRole)
        {
            try
            {
                var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
                if (omEntry == null)
                {
                    Console.WriteLine("OM Entry not found.");
                    return;
                }

                var hdsUsers = _hubContext.UserData.Where(e => e.HDS).ToList();
                if (!hdsUsers.Any())
                {
                    Console.WriteLine("HDS Users not found.");
                    return;
                }

                string nextRole;
                if (userRole == "Regional Engineer")
                {
                    nextRole = "Director of Operations";
                }
                else if (userRole == "Director of Operations")
                {
                    nextRole = "Chief Engineer of Operations";
                }
                else if (userRole == "Chief Engineer of Operations")
                {
                    nextRole = "ready for a paper package to be delivered to the State Highway Engineer for final review.";
                }
                else
                {
                    nextRole = "the next reviewer";
                }

                string reviewLink = $"https://dotapps.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}";

                // Construct the email body
                string emailBody = $@"
                            <html>
                            <body style='font-family: Arial, sans-serif; color: #333; background-color: #f8f8f8; padding: 20px;'>
                                <div style='max-width: 600px; margin: auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ddd;'>

                                    <!-- Header -->
                                    <h2 style='text-align: center; color: #0056b3;'>OM79 Entry Approved by {userRole}</h2>
                                    <p style='font-size: 14px; text-align: center;'>Entry ID: <strong>{omEntry.SmartID}</strong></p>
                                    <hr style='border: 0; border-top: 1px solid #ddd;'>

                                    <!-- Greeting -->
                                    <p>Hello,</p>

                                    <!-- Explanation -->
                                    <p>The <strong>{userRole}</strong> has approved this OM79 entry.</p>";

                                            if (userRole == "Chief Engineer of Operations")
                                            {
                                                emailBody += @"
                                    <p>This marks the end of the digital workflow. It is now time to prepare and deliver a paper package of the OM79 to the <strong>State Highway Engineer</strong> for final review.</p>";
                                            }
                                            else
                                            {
                                                emailBody += $@"
                                    <p>It is now in the hands of <strong>{nextRole}</strong> for further review.</p>
                                    <p>No action is required on your part at this moment.</p>";
                                            }

                                            emailBody += $@"
                                    <!-- Call to Action -->
                                    <p style='text-align: center; margin-top: 20px;'>
                                        <a href='{reviewLink}' 
                                           style='background: #28a745; color: white; text-decoration: none; padding: 12px 20px; border-radius: 5px; font-size: 16px; display: inline-block;'>
                                           View OM79 Entry
                                        </a>
                                    </p>

                                    <hr style='border: 0; border-top: 1px solid #ddd; margin-top: 20px;'>

                                    <!-- Footer -->
                                    <p style='font-size: 12px; text-align: center; color: #667;'>Thank you,<br>OM79 Automated System</p>
                                </div>
                            </body>
                            </html>";

                var message = new MailMessage
                {
                    From = new MailAddress("DOTPJ103Srv@wv.gov"),
                    Subject = $"OM79 Entry Approved by {userRole}",
                    Body = emailBody,
                    IsBodyHtml = true
                };

                foreach (var hdsUser in hdsUsers)
                {
                    message.To.Add(hdsUser.Email);
                    message.CC.Add("ethan.m.johnson@wv.gov");

                }

                using (var client = new SmtpClient
                {
                    Host = "10.204.145.32",
                    Port = 25,
                    EnableSsl = false,
                    Credentials = new NetworkCredential("richard.a.tucker@wv.gov", "trippononemails")
                })
                {
                    await client.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
            }
        }

        private async Task SendDenialEmailToDistrictManager(int id, string comments, string userRole)
        {
            try
            {
                var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
                if (omEntry == null)
                {
                    Console.WriteLine("OM Entry not found.");
                    return;
                }

                var districtManager = _hubContext.UserData.FirstOrDefault(e => e.DistrictManager == true && e.District == omEntry.District);
                if (districtManager == null)
                {
                    Console.WriteLine("District Manager not found.");
                    return;
                }

                string reviewLink = $"https://dotapps.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}";

                // Construct the email body
                string emailBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; color: #333; background-color: #f8f8f8; padding: 20px;'>
                            <div style='max-width: 600px; margin: auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ddd;'>

                                <!-- Header -->
                                <h2 style='text-align: center; color: #d9534f;'>OM79 Entry Denied by {userRole}</h2>
                                <p style='font-size: 14px; text-align: center;'>Entry ID: <strong>{omEntry.SmartID}</strong> | District: <strong>{omEntry.District}</strong></p>
                                <hr style='border: 0; border-top: 1px solid #ddd;'>

                                <!-- Greeting -->
                                <p>Hello,</p>

                                <!-- Explanation -->
                                <p>The <strong>{userRole}</strong> has denied the OM79 entry and provided the following comments:</p>

                                <!-- Comments Section -->
                                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #d9534f;'>
                                    <p>{comments}</p>
                                </div>

                                <!-- Review Options -->
                                <p><strong>Your Next Steps:</strong></p>
                                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF;'>
                                    <p><strong style='color: #0056b3;'>Make the requested changes:</strong> If there are any changes requested, you can make the necessary edits and restart the workflow.</p>
                                    <p><strong style='color: #0056b3;'>Close the request:</strong> If you choose to close the request, it will be removed from the system.</p>
                                </div>

                                <!-- Call to Action -->
                                <p style='text-align: center; margin-top: 20px;'>
                                    <a href='{reviewLink}' 
                                       style='background: #28a745; color: white; text-decoration: none; padding: 12px 20px; border-radius: 5px; font-size: 16px; display: inline-block;'>
                                       Review OM79 Entry
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
                        </html>";

                // Compose the email
                var message = new MailMessage
                {
                    From = new MailAddress("DOTPJ103Srv@wv.gov"),
                    Subject = $"OM79 Entry Denied by {userRole}",
                    Body = emailBody,
                    IsBodyHtml = true
                };

                message.To.Add(districtManager.Email);
                message.CC.Add("ethan.m.johnson@wv.gov");


                using (var client = new SmtpClient
                {
                    Host = "10.204.145.32",
                    Port = 25,
                    EnableSsl = false,
                    Credentials = new NetworkCredential("richard.a.tucker@wv.gov", "trippononemails")
                })
                {
                    await client.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
            }
        }


        #endregion

        /* This is the functionality for the Director of Operations for Approving/Denying the OM79 */
        #region SignOMCentralDirectorOfOperations


        public async Task<IActionResult> SignOMCentralDirectorOfOperations()
        {
            var hubkey = int.Parse(Request.Form["HubKey"]); // Define hubkey here
            var decision = Request.Form["decisionDirectorOfOperations"];
            var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == hubkey);

            if (omEntry == null)
            {
                return NotFound("OM Entry not found");
            }
            var userRole = "Director of Operations"; // Adjust as needed for other roles

            // The Director of Operations has just approved the OM79, it needs to go to the Chief Engineer of Operations
            if (decision == "approve")
            {
                var signature = new SignatureData
                {
                    HubKey = hubkey,
                    IsApprove = true,
                    IsDenied = false,
                    IsEdited = false,
                    Comments = Request.Form["commentsmodal"],
                    Signatures = Request.Form["signaturemodal"],
                    SigType = "Director of Operations",
                    ENumber = HttpContext.User.Identity.Name,
                    DateSubmitted = DateTime.Now,
                    IsCurrentSig = true,
                };
                _hubContext.Add(signature);
                await _hubContext.SaveChangesAsync();

                omEntry.WorkflowStep = "SubmittedToCentralChief";
                await _hubContext.SaveChangesAsync();
                await SendWorkflowEmailToChiefEngineerOfOperations(hubkey, Request.Form["commentsmodal"]);
                await SendWorkflowUpdateEmailToHDSUser(hubkey, userRole);
            }

            // The Director of Operations has just denied the OM79
            if (decision == "deny")
            {
                var signature = new SignatureData
                {
                    HubKey = hubkey,
                    IsApprove = false,
                    IsDenied = true,
                    IsEdited = false,
                    Comments = Request.Form["commentsmodal"],
                    Signatures = Request.Form["signaturemodal"],
                    SigType = "Director of Operations",
                    ENumber = HttpContext.User.Identity.Name,
                    DateSubmitted = DateTime.Now,
                    IsCurrentSig = true,
                };
                _hubContext.Add(signature);
                await _hubContext.SaveChangesAsync();

                omEntry.WorkflowStep = "SubmittedBackToDistrictManagerFromOperations";
                await _hubContext.SaveChangesAsync();

                await SendDenialEmailToDistrictManager(hubkey, Request.Form["commentsmodal"], "Director of Operations");
                await SendDenialUpdateEmailToHDSUser(hubkey, "Director of Operations");

                omEntry.HasGISReviewed = false;

                var allCentralOfficeSignatures = _hubContext.SignatureData
                                           .Where(e => e.HubKey == hubkey && e.IsCurrentSig == true && (e.SigType == "HDS" || e.SigType == "GIS Manager" || e.SigType == "Regional Engineer" || e.SigType == "Director of Operations"))
                                           .ToList();

                foreach (var sig in allCentralOfficeSignatures)
                {
                    sig.IsCurrentSig = false;
                    _hubContext.Update(sig);  // Ensure that the entity state is updated
                }
                await _hubContext.SaveChangesAsync();  // Save all changes in one call
            }

            return RedirectToAction("Details", "Central79Hub", new { id = hubkey });
        }


        //public async Task<IActionResult> SignOMCentralDirectorOfOperations()
        //{
        //    var hubkey = int.Parse(Request.Form["HubKey"]); // Define hubkey here
        //    var approved = Request.Form["apradio"];
        //    var denied = Request.Form["denradio"];
        //    var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == hubkey);

        //    if (omEntry == null)
        //    {
        //        return NotFound("OM Entry not found");
        //    }
        //    var userRole = "Director of Operations"; // Adjust as needed for other roles

        //    // The Director of Operations has just approved the OM79, it needs to go to the Chief Engineer of Operations
        //    if (approved.FirstOrDefault() == "approve")
        //    {
        //        var signature = new SignatureData
        //        {
        //            HubKey = hubkey,
        //            IsApprove = true,
        //            IsDenied = false,
        //            IsEdited = false,
        //            Comments = Request.Form["commentsmodal"],
        //            Signatures = Request.Form["signaturemodal"],
        //            SigType = "Director of Operations",
        //            ENumber = HttpContext.User.Identity.Name,
        //            DateSubmitted = DateTime.Now,
        //            IsCurrentSig = true,
        //        };
        //        _hubContext.Add(signature);
        //        await _hubContext.SaveChangesAsync();

        //        omEntry.WorkflowStep = "SubmittedToCentralChief";
        //        await _hubContext.SaveChangesAsync();
        //        await SendWorkflowEmailToChiefEngineerOfOperations(hubkey, Request.Form["commentsmodal"]);
        //        await SendWorkflowUpdateEmailToHDSUser(hubkey, userRole);
        //    }

        //    // The Director of Operations has just denied the OM79
        //    if (denied.FirstOrDefault() == "deny")
        //    {
        //        var signature = new SignatureData
        //        {
        //            HubKey = hubkey,
        //            IsApprove = false,
        //            IsDenied = true,
        //            IsEdited = false,
        //            Comments = Request.Form["commentsmodal"],
        //            Signatures = Request.Form["signaturemodal"],
        //            SigType = "Director of Operations",
        //            ENumber = HttpContext.User.Identity.Name,
        //            DateSubmitted = DateTime.Now,
        //            IsCurrentSig = true,
        //        };
        //        _hubContext.Add(signature);
        //        await _hubContext.SaveChangesAsync();

        //        omEntry.WorkflowStep = "SubmittedBackToDistrictManagerFromOperations";
        //        await _hubContext.SaveChangesAsync();

        //        await SendDenialEmailToDistrictManager(hubkey, Request.Form["commentsmodal"], "Director of Operations");
        //        await SendDenialUpdateEmailToHDSUser(hubkey, "Director of Operations");

        //        omEntry.HasGISReviewed = false;

        //        var allCentralOfficeSignatures = _hubContext.SignatureData
        //                               .Where(e => e.HubKey == hubkey && e.IsCurrentSig == true && (e.SigType == "HDS" || e.SigType == "GIS Manager" || e.SigType == "Regional Engineer" || e.SigType == "Director of Operations"))
        //                               .ToList();

        //        foreach (var sig in allCentralOfficeSignatures)
        //        {
        //            sig.IsCurrentSig = false;
        //            _hubContext.Update(sig);  // Ensure that the entity state is updated
        //        }
        //        await _hubContext.SaveChangesAsync();  // Save all changes in one call
        //    }

        //    return RedirectToAction("Details", "Central79Hub", new { id = hubkey });
        //}

        private async Task SendWorkflowEmailToChiefEngineerOfOperations(int id, string comments)
        {
            try
            {
                var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
                if (omEntry == null)
                {
                    Console.WriteLine("OM Entry not found.");
                    return;
                }

                var chiefEngineerOfOperations = _hubContext.UserData.FirstOrDefault(e => e.Chief == true);
                if (chiefEngineerOfOperations == null)
                {
                    Console.WriteLine("Chief Engineer of Operations not found.");
                    return;
                }

                string reviewLink = $"https://dotapps.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}";

                // Construct the email body
                string emailBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; color: #333; background-color: #f8f8f8; padding: 20px;'>
                            <div style='max-width: 600px; margin: auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ddd;'>

                                <!-- Header -->
                                <h2 style='text-align: center; color: #0056b3;'>OM79 Submission Requires Your Review</h2>
                                <p style='font-size: 14px; text-align: center;'>Entry ID: <strong>{omEntry.SmartID}</strong></p>
                                <hr style='border: 0; border-top: 1px solid #ddd;'>

                                <!-- Greeting -->
                                <p>Hello,</p>

                                <!-- Explanation -->
                                <p>An OM79 entry has been approved by the Director of Operations and is now awaiting your review. As the Chief Engineer of Operations, your review is crucial for finalizing this entry.</p>

                                <!-- Role Responsibilities -->
                                <p><strong>Your Review Options:</strong></p>
                                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF;'>
                                    <p><strong style='color: #0056b3;'>Approve:</strong> Approving this entry will end the digital workflow and notify the HDS manager(s) to provide a printed copy to the State Highway Engineer for final review.</p>
                                    <p><strong style='color: #0056b3;'>Deny:</strong> Denying this entry will send it back to the District Manager with your requested changes. The District Manager can then either make the requested edits and restart the workflow or close the request.</p>
                                </div>

                                <!-- Comments Section -->
                                <p><strong>Comments from Director of Operations:</strong></p>
                                <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 5px solid #007BFF;'>
                                    <p>{comments}</p>
                                </div>

                                <!-- Call to Action -->
                                <p style='text-align: center; margin-top: 20px;'>
                                    <a href='{reviewLink}' 
                                       style='background: #28a745; color: white; text-decoration: none; padding: 12px 20px; border-radius: 5px; font-size: 16px; display: inline-block;'>
                                       Review & Take Action on OM79 Entry
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
                        </html>";

                var message = new MailMessage
                {
                    From = new MailAddress("DOTPJ103Srv@wv.gov"),
                    Subject = "OM79 Entry Ready for Chief Engineer of Operations Review",
                    Body = emailBody,
                    IsBodyHtml = true
                };

                message.To.Add(chiefEngineerOfOperations.Email);
                message.CC.Add("ethan.m.johnson@wv.gov");

                using (var client = new SmtpClient
                {
                    Host = "10.204.145.32",
                    Port = 25,
                    EnableSsl = false,
                    Credentials = new NetworkCredential("ethan.m.johnson@wv.gov", "trippononemails")
                })
                {
                    await client.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
            }
        }

        private async Task SendDenialUpdateEmailToHDSUser(int id, string userRole)
        {
            try
            {
                var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
                if (omEntry == null)
                {
                    Console.WriteLine("OM Entry not found.");
                    return;
                }

                var hdsUsers = _hubContext.UserData.Where(e => e.HDS).ToList();
                if (!hdsUsers.Any())
                {
                    Console.WriteLine("HDS Users not found.");
                    return;
                }

                string reviewLink = $"https://dotapps.transportation.wv.gov/om79/CENTRAL79HUB/Details/{id}";

                // Construct the email body
                string emailBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; color: #333; background-color: #f8f8f8; padding: 20px;'>
                            <div style='max-width: 600px; margin: auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0px 0px 10px #ddd;'>

                                <!-- Header -->
                                <h2 style='text-align: center; color: #d9534f;'>OM79 Entry Denied by {userRole}</h2>
                                <p style='font-size: 14px; text-align: center;'>Entry ID: <strong>{omEntry.SmartID}</strong></p>
                                <hr style='border: 0; border-top: 1px solid #ddd;'>

                                <!-- Greeting -->
                                <p>Hello,</p>

                                <!-- Explanation -->
                                <p>The <strong>{userRole}</strong> has denied the OM79 entry, and it has been returned to the District Manager for further review.</p>
                                <p>No action is required on your part at this moment.</p>

                                <!-- Call to Action -->
                                <p style='text-align: center; margin-top: 20px;'>
                                    <a href='{reviewLink}' 
                                       style='background: #28a745; color: white; text-decoration: none; padding: 12px 20px; border-radius: 5px; font-size: 16px; display: inline-block;'>
                                       View OM79 Entry
                                    </a>
                                </p>

                                <hr style='border: 0; border-top: 1px solid #ddd; margin-top: 20px;'>

                                <!-- Footer -->
                                <p style='font-size: 12px; text-align: center; color: #667;'>Thank you,<br>OM79 Automated System</p>
                            </div>
                        </body>
                        </html>";

                var message = new MailMessage
                {
                    From = new MailAddress("DOTPJ103Srv@wv.gov"),
                    Subject = $"OM79 Entry Denied by {userRole}",
                    Body = emailBody,
                    IsBodyHtml = true
                };

                foreach (var hdsUser in hdsUsers)
                {
                    message.To.Add(hdsUser.Email);
                    message.CC.Add("ethan.m.johnson@wv.gov");

                }

                using (var client = new SmtpClient
                {
                    Host = "10.204.145.32",
                    Port = 25,
                    EnableSsl = false,
                    Credentials = new NetworkCredential("ethan.m.johnson@wv.gov", "trippononemails")
                })
                {
                    await client.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
            }
        }

        #endregion



        /* This is the functionality for the Chief Engineer of Operations for Approving/Denying the OM79 */
        #region SignOMCentralChiefEngineerOfOperations
        public async Task<IActionResult> SignOMCentralChiefEngineerOfOperations()
        {
            var hubkey = int.Parse(Request.Form["HubKey"]); // Define hubkey here
            var decision = Request.Form["decisionChiefEngineer"];
            var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == hubkey);

            if (omEntry == null)
            {
                return NotFound("OM Entry not found");
            }
            var userRole = "Chief Engineer of Operations"; // Adjust as needed for other roles

            // The Chief Engineer of Operations has just approved the OM79, it needs to go to the Deputy Secretary / Deputy Commissioner of Highways
            if (decision == "approve")
            {
                var signature = new SignatureData
                {
                    HubKey = hubkey,
                    IsApprove = true,
                    IsDenied = false,
                    IsEdited = false,
                    Comments = Request.Form["commentsmodal"],
                    Signatures = Request.Form["signaturemodal"],
                    SigType = "Chief Engineer of Operations",
                    ENumber = HttpContext.User.Identity.Name,
                    DateSubmitted = DateTime.Now,
                    IsCurrentSig = true,
                };
                _hubContext.Add(signature);
                await _hubContext.SaveChangesAsync();

                omEntry.WorkflowStep = "Finalized";
                await _hubContext.SaveChangesAsync();
                //await SendWorkflowEmailToDeputySecretary(hubkey, Request.Form["commentsmodal"]);
                await SendWorkflowUpdateEmailToHDSUser(hubkey, userRole);
            }

            // The Chief Engineer of Operations has just denied the OM79
            if (decision == "deny")
            {
                var signature = new SignatureData
                {
                    HubKey = hubkey,
                    IsApprove = false,
                    IsDenied = true,
                    IsEdited = false,
                    Comments = Request.Form["commentsmodal"],
                    Signatures = Request.Form["signaturemodal"],
                    SigType = "Chief Engineer of Operations",
                    ENumber = HttpContext.User.Identity.Name,
                    DateSubmitted = DateTime.Now,
                    IsCurrentSig = true,
                };
                _hubContext.Add(signature);
                await _hubContext.SaveChangesAsync();

                omEntry.WorkflowStep = "SubmittedBackToDistrictManagerFromOperations";
                await _hubContext.SaveChangesAsync();

                await SendDenialEmailToDistrictManager(hubkey, Request.Form["commentsmodal"], "Chief Engineer of Operations");
                await SendDenialUpdateEmailToHDSUser(hubkey, "Chief Engineer of Operations");

                omEntry.HasGISReviewed = false;

                var allCentralOfficeSignatures = _hubContext.SignatureData
                                           .Where(e => e.HubKey == hubkey && e.IsCurrentSig == true && (e.SigType == "HDS" || e.SigType == "GIS Manager" || e.SigType == "Regional Engineer" || e.SigType == "Director of Operations" || e.SigType == "Chief Engineer of Operations"))
                                           .ToList();

                foreach (var sig in allCentralOfficeSignatures)
                {
                    sig.IsCurrentSig = false;
                    _hubContext.Update(sig);  // Ensure that the entity state is updated
                }
                await _hubContext.SaveChangesAsync();  // Save all changes in one call
            }

            return RedirectToAction("Details", "Central79Hub", new { id = hubkey });
        }

        //public async Task<IActionResult> SignOMCentralChiefEngineerOfOperations()
        //{
        //    var hubkey = int.Parse(Request.Form["HubKey"]); // Define hubkey here
        //    var approved = Request.Form["apradio"];
        //    var denied = Request.Form["denradio"];
        //    var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == hubkey);

        //    if (omEntry == null)
        //    {
        //        return NotFound("OM Entry not found");
        //    }
        //    var userRole = "Chief Engineer of Operations"; // Adjust as needed for other roles

        //    // The Chief Engineer of Operations has just approved the OM79, it needs to go to the Deputy Secretary / Deputy Commissioner of Highways
        //    if (approved.FirstOrDefault() == "approve")
        //    {
        //        var signature = new SignatureData
        //        {
        //            HubKey = hubkey,
        //            IsApprove = true,
        //            IsDenied = false,
        //            IsEdited = false,
        //            Comments = Request.Form["commentsmodal"],
        //            Signatures = Request.Form["signaturemodal"],
        //            SigType = "Chief Engineer of Operations",
        //            ENumber = HttpContext.User.Identity.Name,
        //            DateSubmitted = DateTime.Now,
        //            IsCurrentSig = true,
        //        };
        //        _hubContext.Add(signature);
        //        await _hubContext.SaveChangesAsync();

        //        omEntry.WorkflowStep = "Finalized";
        //        await _hubContext.SaveChangesAsync();
        //        await SendWorkflowEmailToDeputySecretary(hubkey, Request.Form["commentsmodal"]);
        //        await SendWorkflowUpdateEmailToHDSUser(hubkey, userRole);
        //    }

        //    // The Chief Engineer of Operations has just denied the OM79
        //    if (denied.FirstOrDefault() == "deny")
        //    {
        //        var signature = new SignatureData
        //        {
        //            HubKey = hubkey,
        //            IsApprove = false,
        //            IsDenied = true,
        //            IsEdited = false,
        //            Comments = Request.Form["commentsmodal"],
        //            Signatures = Request.Form["signaturemodal"],
        //            SigType = "Chief Engineer of Operations",
        //            ENumber = HttpContext.User.Identity.Name,
        //            DateSubmitted = DateTime.Now,
        //            IsCurrentSig = true,
        //        };
        //        _hubContext.Add(signature);
        //        await _hubContext.SaveChangesAsync();

        //        omEntry.WorkflowStep = "SubmittedBackToDistrictManagerFromOperations";
        //        await _hubContext.SaveChangesAsync();

        //        await SendDenialEmailToDistrictManager(hubkey, Request.Form["commentsmodal"], "Chief Engineer of Operations");
        //        await SendDenialUpdateEmailToHDSUser(hubkey, "Chief Engineer of Operations");

        //        omEntry.HasGISReviewed = false;

        //        var allCentralOfficeSignatures = _hubContext.SignatureData
        //                                   .Where(e => e.HubKey == hubkey && e.IsCurrentSig == true && (e.SigType == "HDS" || e.SigType == "GIS Manager" || e.SigType == "Regional Engineer" || e.SigType == "Director of Operations" || e.SigType == "Chief Engineer of Operations"))
        //                                   .ToList();

        //        foreach (var sig in allCentralOfficeSignatures)
        //        {
        //            sig.IsCurrentSig = false;
        //            _hubContext.Update(sig);  // Ensure that the entity state is updated
        //        }
        //        await _hubContext.SaveChangesAsync();  // Save all changes in one call
        //    }

        //    return RedirectToAction("Details", "Central79Hub", new { id = hubkey });
        //}

        private async Task SendWorkflowEmailToDeputySecretary(int id, string comments)
        {
            try
            {
                var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == id);
                if (omEntry == null)
                {
                    Console.WriteLine("OM Entry not found.");
                    return;
                }

                var deputySecretary = _hubContext.UserData.FirstOrDefault(e => e.DeputySecretary == true);
                if (deputySecretary == null)
                {
                    Console.WriteLine("Deputy Secretary / Deputy Commissioner of Highways not found.");
                    return;
                }

                var message = new MailMessage
                {
                    From = new MailAddress("DOTPJ103Srv@wv.gov"),
                    Subject = "OM79 Entry Ready for Deputy Secretary / Deputy Commissioner of Highways Review",
                    Body = $"Hello,<br><br>" +
                       $"An OM79 entry has been approved by the Chief Engineer of Operations and is now awaiting your review. As the Deputy Secretary / Deputy Commissioner of Highways, your review is crucial for finalizing this entry.<br><br>" +
                       $"<br><br>" +
                       $"<b>Comments from Chief Engineer of Operations:</b> {comments}<br><br>" +
                       $"Attached is a PDF copy of the OM79 for your review and action.<br><br>" +
                       $"Thank you for your prompt attention to this matter.<br><br>" +
                       $"Best regards,<br>" +
                       $"OM79 Automated System",
                    IsBodyHtml = true

                };

                message.To.Add(deputySecretary.Email);
                message.CC.Add("ethan.m.johnson@wv.gov");


                using (var client = new SmtpClient
                {
                    Host = "10.204.145.32",
                    Port = 25,
                    EnableSsl = false,
                    Credentials = new NetworkCredential("ethan.m.johnson@wv.gov", "trippononemails")
                })
                {
                    await client.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + " this is because the email is incorrect form");
            }
        }
        #endregion



        /* This is the functionality for the District Manager for Editing/Closing the OM79*/
        #region SignOMCentralDeniedDistrictManager

        public async Task<IActionResult> SignOMCentralDeniedDistrictManager()
        {
            var hubkey = int.Parse(Request.Form["HubKey"]); // Define hubkey here
            var decision = Request.Form["decisionDeniedDM"];
            var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == hubkey);

            if (omEntry == null)
            {
                return NotFound("OM Entry not found");
            }

            // The District Manager has just edited the OM79 entry
            if (decision == "edit")
            {
                var signature = new SignatureData
                {
                    HubKey = hubkey,
                    IsApprove = false,
                    IsDenied = false,
                    IsEdited = false,
                    Comments = Request.Form["commentsmodal"],
                    Signatures = Request.Form["signaturemodal"],
                    SigType = "District Manager",
                    ENumber = HttpContext.User.Identity.Name,
                    DateSubmitted = DateTime.Now,
                    IsCurrentSig = true,
                };
                _hubContext.Add(signature);
                await _hubContext.SaveChangesAsync();

                // Update the workflow step and notify the appropriate users
                omEntry.HasGISReviewed = false;
                omEntry.WorkflowStep = "SubmittedToCentralHDS";

                var allCentralOfficeSignatures = _hubContext.SignatureData
                                           .Where(e => e.HubKey == hubkey && e.IsCurrentSig == true && (e.SigType == "HDS" || e.SigType == "GIS Manager" || e.SigType == "Regional Engineer" || e.SigType == "Director of Operations" || e.SigType == "Chief Engineer of Operations"))
                                           .ToList();

                foreach (var sig in allCentralOfficeSignatures)
                {
                    sig.IsCurrentSig = false;
                    _hubContext.Update(sig);  // Ensure that the entity state is updated
                }
                await _hubContext.SaveChangesAsync();  // Save all changes in one call

                // Notify the HDS user about the edits
                await SendWorkflowEmailToHDSWithEditsFromDistrictManager(hubkey, Request.Form["commentsmodal"]);
            }

            // The District Manager has just closed the OM79 entry
            if (decision == "close")
            {
                // Handle the case where the OM79 entry is closed
                var signature = new SignatureData
                {
                    HubKey = hubkey,
                    IsApprove = false,
                    IsDenied = false,
                    IsEdited = false,
                    Comments = Request.Form["commentsmodal"],
                    Signatures = Request.Form["signaturemodal"],
                    SigType = "District Manager",
                    ENumber = HttpContext.User.Identity.Name,
                    DateSubmitted = DateTime.Now,
                    IsCurrentSig = true,
                };
                _hubContext.Add(signature);
                await _hubContext.SaveChangesAsync();

                var allSignatures = _hubContext.SignatureData.Where(e => e.HubKey == hubkey).ToList();

                foreach (var sig in allSignatures)
                {
                    sig.IsCurrentSig = false;
                    _hubContext.Update(sig);  // Ensure that the entity state is updated
                }

                omEntry.WorkflowStep = "CancelledRequestArchive";
                omEntry.IsArchive = true;
                await _hubContext.SaveChangesAsync();

                var allItems = await _om79Context.OMTable.Where(e => e.HubId == omEntry.OMId).ToListAsync();

                foreach (var item in allItems)
                {
                    item.IsArchive = true;
                }
                await _om79Context.SaveChangesAsync();


            }

            return RedirectToAction("Details", "Central79Hub", new { id = hubkey });
        }


        //public async Task<IActionResult> SignOMCentralDeniedDistrictManager()
        //{
        //    var hubkey = int.Parse(Request.Form["HubKey"]); // Define hubkey here
        //    var edited = Request.Form["editradio"];
        //    var closed = Request.Form["closeradio"];
        //    var omEntry = _hubContext.CENTRAL79HUB.FirstOrDefault(e => e.OMId == hubkey);

        //    if (omEntry == null)
        //    {
        //        return NotFound("OM Entry not found");
        //    }

        //    // The District Manager has just edited the OM79 entry
        //    if (edited.FirstOrDefault() == "edit")
        //    {
        //        var signature = new SignatureData
        //        {
        //            HubKey = hubkey,
        //            IsApprove = false,
        //            IsDenied = false,
        //            IsEdited = true,
        //            Comments = Request.Form["commentsmodal"],
        //            Signatures = Request.Form["signaturemodal"],
        //            SigType = "District Manager",
        //            ENumber = HttpContext.User.Identity.Name,
        //            DateSubmitted = DateTime.Now,
        //            IsCurrentSig = true,
        //        };
        //        _hubContext.Add(signature);
        //        await _hubContext.SaveChangesAsync();

        //        // Update the workflow step and notify the appropriate users
        //        omEntry.HasGISReviewed = false;
        //        omEntry.WorkflowStep = "SubmittedToCentralHDS";

        //        var allCentralOfficeSignatures = _hubContext.SignatureData
        //                               .Where(e => e.HubKey == hubkey && e.IsCurrentSig == true && (e.SigType == "HDS" || e.SigType == "GIS Manager" || e.SigType == "Regional Engineer" || e.SigType == "Director of Operations" || e.SigType == "Chief Engineer of Operations"))
        //                               .ToList();

        //        foreach (var sig in allCentralOfficeSignatures)
        //        {
        //            sig.IsCurrentSig = false;
        //            _hubContext.Update(sig);  // Ensure that the entity state is updated
        //        }
        //        await _hubContext.SaveChangesAsync();  // Save all changes in one call

        //        // Notify the HDS user about the edits
        //        await SendWorkflowEmailToHDSWithEditsFromDistrictManager(hubkey, Request.Form["commentsmodal"]);
        //    }

        //    // The District Manager has just closed the OM79 entry
        //    if (closed.FirstOrDefault() == "close")
        //    {
        //        // Handle the case where the OM79 entry is closed
        //        // Add the appropriate actions here in the future
        //        var signature = new SignatureData
        //        {
        //            HubKey = hubkey,
        //            IsApprove = false,
        //            IsDenied = false,
        //            IsEdited = false,
        //            Comments = Request.Form["commentsmodal"],
        //            Signatures = Request.Form["signaturemodal"],
        //            SigType = "District Manager",
        //            ENumber = HttpContext.User.Identity.Name,
        //            DateSubmitted = DateTime.Now,
        //            IsCurrentSig = true,
        //        };
        //        _hubContext.Add(signature);
        //        await _hubContext.SaveChangesAsync();

        //        var allSignatures = _hubContext.SignatureData.Where(e => e.HubKey == hubkey).ToList();

        //        foreach(var sig in allSignatures)
        //        {
        //            sig.IsCurrentSig = false;
        //            _hubContext.Update(sig);  // Ensure that the entity state is updated
        //        }

        //        omEntry.WorkflowStep = "CancelledRequestArchive";
        //        await _hubContext.SaveChangesAsync();
        //    }

        //    return RedirectToAction("Details", "Central79Hub", new { id = hubkey });
        //}

        #endregion
    }
}