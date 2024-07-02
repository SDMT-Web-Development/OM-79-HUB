using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OM_79_HUB.Data;
using OM_79_HUB.Models.DB.OM79Hub;
using OM79.Models.DB;

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
            Console.WriteLine($"HDS: {userData.HDS}");
            Console.WriteLine($"GISManager: {userData.GISManager}");
            Console.WriteLine($"Chief: {userData.Chief}");

            if (ModelState.IsValid)
            {
                _hubContext.Add(userData);
                await _hubContext.SaveChangesAsync();
                return RedirectToAction("StatewideAccountSystem", "AccountSystemAndWorkflow");
            }
            return View("StatewideAccountSystem", userData);
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
            }
            return RedirectToAction("AdminAccountSystem", "AccountSystemAndWorkflow");
        }

        [HttpGet]
        public IActionResult GetCentralUsers()
        {
            var users = _hubContext.UserData.ToList();
            return Json(users);
        }

        [HttpGet]
        public IActionResult CheckChiefExists()
        {
            var chiefExists = _hubContext.UserData.Any(u => u.Chief == true);
            return Json(new { exists = chiefExists });
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

    }
}
