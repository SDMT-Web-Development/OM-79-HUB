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
    public class AccountSystemAndWorkflowController : Controller
    {
        private readonly Pj103Context _pj103Context;
        private readonly OM79Context _om79Context;
        private readonly OM_79_HUBContext _hubContext;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountSystemAndWorkflowController(Pj103Context context, IWebHostEnvironment webHostEnvironment, OM79Context om79Context, OM_79_HUBContext hubContext)
        {
            _webHostEnvironment = webHostEnvironment;
            _hubContext = hubContext;
            _om79Context = om79Context;
            _pj103Context = context;
        }

        /*
         * This controller will be used for: 
         * 
         * district specific admin page
         * statewide admin page
         * admin admin page
         * OM79 workflow
         * email system
         * 
         * 
         */




        [HttpGet]
        public IActionResult AdminAccountSystem()
        {
            // Extract the ENumber from User.Identity.Name
            var userIdentityName = User.Identity.Name;
            var userENumber = userIdentityName.Split('\\').LastOrDefault(); // Assuming the format is EXECUTIVE/E000000

            if (userENumber != null)
            {
                // Check if the ENumber exists in the AdminData table and is a SystemAdmin
                var isSystemAdmin = _hubContext.AdminData
                    .Any(ad => ad.ENumber == userENumber && ad.SystemAdmin == true);

                if (isSystemAdmin)
                {
                    return View();
                }
            }

            // If not an admin, redirect to an unauthorized page or handle accordingly
            return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
        }

        [HttpGet]
        public IActionResult Unauthorized()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCentralOfficeUser(UserData userData)
        {
            // Logging the incoming data
            Console.WriteLine("AddCentralOfficeUser called");
            Console.WriteLine($"FirstName: {userData.FirstName}");
            Console.WriteLine($"LastName: {userData.LastName}");
            Console.WriteLine($"ENumber: {userData.ENumber}");
            Console.WriteLine($"Email: {userData.Email}");
            Console.WriteLine($"CRU: {userData.CRU}");
            Console.WriteLine($"CRA: {userData.CRA}");
            Console.WriteLine($"HDS: {userData.HDS}");
            Console.WriteLine($"LRS: {userData.LRS}");
            Console.WriteLine($"GISManager: {userData.GISManager}");
            Console.WriteLine($"Chief: {userData.Chief}");
            Console.WriteLine($"DistrictReview: {userData.DistrictReview}");
            Console.WriteLine($"District: {userData.District}");
            Console.WriteLine($"BridgeEngineer: {userData.BridgeEngineer}");
            Console.WriteLine($"TrafficEngineer: {userData.TrafficEngineer}");
            Console.WriteLine($"MaintenanceEngineer: {userData.MaintenanceEngineer}");
            Console.WriteLine($"ConstructionEngineer: {userData.ConstructionEngineer}");
            Console.WriteLine($"RightOfWayManager: {userData.RightOfWayManager}");
            Console.WriteLine($"DistrictManager: {userData.DistrictManager}");
            Console.WriteLine($"RegionalEngineer: {userData.RegionalEngineer}");
            Console.WriteLine($"DirectorOfOperations: {userData.DirectorOfOperations}");
            Console.WriteLine($"DeputySecretary: {userData.DeputySecretary}");
            Console.WriteLine($"DistrictsForRegionalEngineer: {userData.DistrictsForRegionalEngineer}");

            if (ModelState.IsValid)
            {
                _hubContext.Add(userData);
                await _hubContext.SaveChangesAsync();
                // Determine roles assigned to the user
                var userRoles = GetUserRoles(userData);

                // Send the welcome email
                await SendWelcomeEmailAsync(userData.FirstName, userData.Email, userRoles, isAdmin: false);

                return RedirectToAction("StatewideAccountSystem", "AccountSystemAndWorkflow");
            }
            return View("StatewideAccountSystem", userData);
        }

        private List<string> GetUserRoles(UserData userData)
        {
            var roles = new List<string>();

            if (userData.BridgeEngineer) roles.Add("Bridge Engineer");
            if (userData.TrafficEngineer) roles.Add("Traffic Engineer");
            if (userData.MaintenanceEngineer) roles.Add("Maintenance Engineer");
            if (userData.ConstructionEngineer) roles.Add("Construction Engineer");
            if (userData.RightOfWayManager) roles.Add("Right of Way Manager");
            if (userData.DistrictManager) roles.Add("District Manager");
            if (userData.HDS == true) roles.Add("Help Desk Support Manager");
            if (userData.GISManager == true) roles.Add("GIS Manager");
            if (userData.Chief == true) roles.Add("Chief Engineer of Operations");
            if (userData.DirectorOfOperations == true) roles.Add("Director of Operations");
            if (userData.RegionalEngineer == true) roles.Add("Regional Engineer");
            if (userData.DeputySecretary == true) roles.Add("Deputy Secretary");
            if (userData.DistrictReview == true) roles.Add("District Review");

            return roles;
        }


        // POST: AccountSystemAndWorkflow/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminCreate(AdminData adminData)
        {
            // Logging the incoming data
            Console.WriteLine("AdminCreate called");
            Console.WriteLine($"FirstName: {adminData.FirstName}");
            Console.WriteLine($"LastName: {adminData.LastName}");
            Console.WriteLine($"ENumber: {adminData.ENumber}");
            Console.WriteLine($"StateEmail: {adminData.StateEmail}");
            Console.WriteLine($"DateEstablished: {adminData.DateEstablished}");
            Console.WriteLine($"DistrictAdmin: {adminData.DistrictAdmin}");
            Console.WriteLine($"StatewideAdmin: {adminData.StatewideAdmin}");
            Console.WriteLine($"SystemAdmin: {adminData.SystemAdmin}");

            if (ModelState.IsValid)
            {
                adminData.DateEstablished = DateTime.Now;
                _hubContext.Add(adminData);
                await _hubContext.SaveChangesAsync();

                // Determine admin type
                string adminType = DetermineAdminType(adminData);

                // Send the welcome email to admin
                await SendWelcomeEmailAsync(adminData.FirstName, adminData.StateEmail, new List<string>(), isAdmin: true, adminType: adminType);
            }
            return RedirectToAction("AdminAccountSystem", "AccountSystemAndWorkflow");
        }

      
        private async Task SendWelcomeEmailAsync(string firstName, string email, List<string> roles, bool isAdmin = false, string adminType = null)
        {
            try
            {
                string subject = "Welcome to the OM79 System";
                string body = string.Empty;

                if (!isAdmin)
                {
                    // Compose email for non-admin users
                    body = $"<p>Hello <strong>{firstName}</strong>,</p>" +
                           $"<p>You have been added to the OM79 system as a <strong>{string.Join(", ", roles)}</strong>.</p>";

                    // Check if user has any reviewer roles
                   
                    body += $"<p>Your role includes reviewing OM79 submissions already in the system. Please visit the <a href='https://dotappstest.transportation.wv.gov/om79/Central79Hub/SignIndex'>My OM79(s) Pending Signatures</a> page to review and sign any submissions that are ready for your attention.</p>";
                    

                    body += $"<p>Thank you for your work and welcome to the OM79 system!</p>" +
                            $"<p>Best regards,<br/>OM79 Automated System</p>";
                }
                else
                {
                    // Compose email for admin users
                    string managementLink = GetManagementLink(adminType);

                    body = $"<p>Hello <strong>{firstName}</strong>,</p>" +
                           $"<p>You have been added to the OM79 system as a <strong>{adminType}</strong>.</p>" +
                           $"<p>You can manage users in the system by visiting the following link:</p>" +
                           $"<p><a href='{managementLink}'>{managementLink}</a></p>" +
                           $"<p>Thank you for your administrative role and contributions to the OM79 system!</p>" +
                           $"<p>Best regards,<br/>OM79 Automated System</p>";
                }

                var message = new MailMessage
                {
                    From = new MailAddress("DOTPJ103Srv@wv.gov"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(new MailAddress(email));

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

                Console.WriteLine($"Welcome email sent to {firstName} at {email}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email to {email}. Error: {ex.Message}");
            }
        }

       
        private string DetermineAdminType(AdminData adminData)
        {
            if (adminData.DistrictAdmin == true)
            {
                return "District Administrator";
            }
            else if (adminData.StatewideAdmin == true)
            {
                return "Central Office Administrator";
            }
            else if (adminData.SystemAdmin == true)
            {
                return "System Administrator";
            }
            else
            {
                return "Administrator";
            }
        }

        
        private string GetManagementLink(string adminType)
        {
            switch (adminType)
            {
                case "District Administrator":
                    return "https://dotappstest.transportation.wv.gov/om79/AccountSystemAndWorkflow/DistrictAccountSystem";
                case "Central Office Administrator":
                    return "https://dotappstest.transportation.wv.gov/om79/AccountSystemAndWorkflow/StatewideAccountSystem";
                case "System Administrator":
                    return "https://dotappstest.transportation.wv.gov/om79/AccountSystemAndWorkflow/AdminAccountSystem";
                default:
                    return "https://dotappstest.transportation.wv.gov/om79/";
            }
        }

      
        private bool IsReviewerRole(string role)
        {
            var reviewerRoles = new List<string>
    {
        "Bridge Engineer",
        "Traffic Engineer",
        "Maintenance Engineer",
        "Construction Engineer",
        "Right of Way Manager",
        "District Manager",
        "District Administrator",
        "Regional Engineer"
        // Add other reviewer roles as needed
    };

            return reviewerRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }


        [HttpGet]
        public IActionResult GetCentralUsers()
        {
            try
            {
                var users = _hubContext.UserData.ToList();
                return Json(users);
            }
            catch (Exception ex)
            {
                // Log the exception (use your preferred logging mechanism)
                Console.WriteLine($"Error fetching users: {ex.Message} - {ex.StackTrace}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet]
        public IActionResult CheckChiefExists()
        {
            var chiefExists = _hubContext.UserData.Any(u => u.Chief == true);
            return Json(new { exists = chiefExists });
        }


        [HttpGet]
        public IActionResult CheckRegionalEngineerExists()
        {
            // Define all possible districts (assuming districts are numbered 1 to 10)
            var allDistricts = Enumerable.Range(1, 10).ToList();

            // Get all districts currently listed in the 'DistrictsForRegionalEngineer' column, excluding null values
            var usedDistricts = _hubContext.UserData
                .Where(u => !string.IsNullOrEmpty(u.DistrictsForRegionalEngineer))
                .Select(u => u.DistrictsForRegionalEngineer)
                .ToList()
                .SelectMany(d => d.Split(',').Select(int.Parse))
                .Distinct()
                .ToList();

            // Determine available districts
            var availableDistricts = allDistricts.Except(usedDistricts).ToList();

            // Check if there are no available districts
            if (!availableDistricts.Any())
            {
                return Json(new { message = "NoAvailableDistricts" });
            }

            return Json(new { availableDistricts });
        }



        [HttpGet]
        public IActionResult CheckDirectorOfOperationsExists()
        {
            var directorOfOperationsExists = _hubContext.UserData.Any(u => u.DirectorOfOperations == true);
            return Json(new { exists = directorOfOperationsExists });
        }
        [HttpGet]
        public IActionResult CheckDeputySecretaryExists()
        {
            var deputySecretaryExists = _hubContext.UserData.Any(u => u.DeputySecretary == true);
            return Json(new { exists = deputySecretaryExists });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveCentralUser(int id)
        {
            var user = await _hubContext.UserData.FindAsync(id);
            if (user != null)
            {
                _hubContext.UserData.Remove(user);
                await _hubContext.SaveChangesAsync();
                return RedirectToAction("StatewideAccountSystem");
            }
            return NotFound();
        }
        public IActionResult GetAvailableDistricts()
        {
            var allDistricts = Enumerable.Range(1, 10);
            var assignedDistricts = _hubContext.UserData
                .Where(u => u.RegionalEngineer && !string.IsNullOrEmpty(u.DistrictsForRegionalEngineer))
                .AsEnumerable() // Load the data into memory
                .SelectMany(u => u.DistrictsForRegionalEngineer.Split(',').Select(int.Parse))
                .ToList();

            var availableDistricts = allDistricts.Except(assignedDistricts).ToList();
            return Json(availableDistricts);
        }



        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _hubContext.AdminData.ToListAsync();
            return Json(users);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveUser(int id)
        {
            var user = await _hubContext.AdminData.FindAsync(id);
            if (user != null)
            {
                _hubContext.AdminData.Remove(user);
                await _hubContext.SaveChangesAsync();
                return Ok();
            }
            return NotFound();
        }



        [HttpGet]
        public IActionResult StatewideAccountSystem()
        {
            // Extract the ENumber from User.Identity.Name
            var userIdentityName = User.Identity.Name;
            var userENumber = userIdentityName.Split('\\').LastOrDefault(); // Assuming the format is EXECUTIVE/E000000

            if (userENumber != null)
            {
                // Check if the ENumber exists in the AdminData table and is a StatewideAdmin
                var isStatewideAdmin = _hubContext.AdminData
                    .Any(ad => ad.ENumber == userENumber && ad.StatewideAdmin == true);

                if (isStatewideAdmin)
                {
                    return View();
                }
            }

            // If not an admin, redirect to an unauthorized page or handle accordingly
            return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
        }


        [HttpGet]
        public IActionResult DistrictAccountSystem()
        {
            var userIdentityName = User.Identity.Name;
            var userENumber = userIdentityName.Split('\\').LastOrDefault(); // Assuming the format is EXECUTIVE/E000000

            if (userENumber != null)
            {
                var adminData = _hubContext.AdminData.FirstOrDefault(ad => ad.ENumber == userENumber && ad.DistrictAdmin == true);

                if (adminData != null)
                {
                    var districtNumber = adminData.DistrictNumber ?? 0;
                    ViewData["DistrictNumber"] = districtNumber;
                    return View();
                }
            }

            return RedirectToAction("Unauthorized", "AccountSystemAndWorkflow");
        }

        [HttpPost]
        public async Task<IActionResult> AddDistrictUser([FromForm] UserData userData, [FromForm] string role)
        {
            switch (role)
            {
                case "BridgeEngineer":
                    userData.BridgeEngineer = true;
                    break;
                case "TrafficEngineer":
                    userData.TrafficEngineer = true;
                    break;
                case "MaintenanceEngineer":
                    userData.MaintenanceEngineer = true;
                    break;
                case "ConstructionEngineer":
                    userData.ConstructionEngineer = true;
                    break;
                case "RightOfWayManager":
                    userData.RightOfWayManager = true;
                    break;
                case "DistrictManager":
                    userData.DistrictManager = true;
                    break;
                default:
                    return BadRequest("Invalid role selected");
            }

            _hubContext.Add(userData);
            await _hubContext.SaveChangesAsync();

            // Determine roles assigned to the user
            var userRoles = GetUserRoles(userData);

            // Send the welcome email
            await SendWelcomeEmailAsync(userData.FirstName, userData.Email, userRoles, isAdmin: false);

            return RedirectToAction("DistrictAccountSystem", "AccountSystemAndWorkflow");
        }


        [HttpGet]
        public IActionResult GetDistrictUsers()
        {
            // Extract the ENumber from User.Identity.Name
            var userIdentityName = User.Identity.Name;
            var userENumber = userIdentityName.Split('\\').LastOrDefault(); // Assuming the format is EXECUTIVE/E000000

            if (userENumber != null)
            {
                // Find the district of the current admin user
                var adminData = _hubContext.AdminData.FirstOrDefault(ad => ad.ENumber == userENumber && ad.DistrictAdmin == true);

                if (adminData != null)
                {
                    // Get the district number of the admin
                    int? districtNumber = adminData.DistrictNumber;

                    // Fetch users for the district
                    var users = _hubContext.UserData.Where(u => u.District == districtNumber).ToList();
                    return Json(users);
                }
            }

            // Return an empty list if the user is not a district admin or any other issue
            return Json(new List<UserData>());
        }

        [HttpPost]
        public async Task<IActionResult> RemoveDistrictUser(int id)
        {
            var user = await _hubContext.UserData.FindAsync(id);
            if (user != null)
            {
                _hubContext.UserData.Remove(user);
                await _hubContext.SaveChangesAsync();
                return Ok();
            }
            return NotFound();
        }

        [HttpGet]
        public IActionResult GetCurrentRoles(int district)
        {
            var users = _hubContext.UserData.Where(u => u.District == district).ToList();
            var rolesInUse = new List<string>();

            foreach (var user in users)
            {
                if (user.BridgeEngineer) rolesInUse.Add("BridgeEngineer");
                if (user.TrafficEngineer) rolesInUse.Add("TrafficEngineer");
                if (user.MaintenanceEngineer) rolesInUse.Add("MaintenanceEngineer");
                if (user.ConstructionEngineer) rolesInUse.Add("ConstructionEngineer");
                if (user.RightOfWayManager) rolesInUse.Add("RightOfWayManager");
                if (user.DistrictManager) rolesInUse.Add("DistrictManager");
            }

            return Json(rolesInUse);
        }

    }
}
