using Microsoft.AspNetCore.Mvc;
using OM_79_HUB.Data;
using OM_79_HUB.Models;
using OM_79_HUB.Models.DB.OM79Hub;
using System.Diagnostics;

namespace OM_79_HUB.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly OM_79_HUBContext _hubContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HomeController(ILogger<HomeController> logger, OM_79_HUBContext hubContext, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _hubContext = hubContext;   
            _httpContextAccessor = httpContextAccessor;
        }
        public IActionResult Contacts()
        {
            // Retrieve administrators from AdminData
            var admins = _hubContext.AdminData.ToList();
            var systemAdmins = admins.Where(a => a.SystemAdmin == true).ToList();
            var statewideAdmins = admins.Where(a => a.StatewideAdmin == true).ToList();
            var districtAdmins = admins.Where(a => a.DistrictAdmin == true).OrderBy(a => a.DistrictNumber).ToList();

            // Retrieve user roles from UserData
            var users = _hubContext.UserData.ToList();

            // Include "DistrictAdmin" in the roles list
            var rolesList = new[] {
                                        "BridgeEngineer",
                                        "TrafficEngineer",
                                        "MaintenanceEngineer",
                                        "ConstructionEngineer",
                                        "RightOfWayManager",
                                        "DistrictManager",
                                        "DistrictAdmin"
                                    };

            // Initialize districtRoles for districts 1 to 10
            var districtRoles = new Dictionary<int, Dictionary<string, List<string>>>();
            for (int i = 1; i <= 10; i++)
            {
                districtRoles[i] = new Dictionary<string, List<string>>();
                foreach (var role in rolesList)
                {
                    districtRoles[i][role] = new List<string> { "Vacant" };
                }
            }

            // Populate districtRoles with user data
            foreach (var user in users.Where(u => u.District.HasValue))
            {
                int district = user.District.Value;

                foreach (var role in rolesList)
                {
                    var propertyInfo = typeof(UserData).GetProperty(role);
                    bool hasRole = false;

                    if (propertyInfo != null)
                    {
                        var value = propertyInfo.GetValue(user);
                        if (value is bool boolValue)
                        {
                            hasRole = boolValue;
                        }
                    }

                    if (hasRole)
                    {
                        if (districtRoles.ContainsKey(district) && districtRoles[district].ContainsKey(role))
                        {
                            if (districtRoles[district][role].Contains("Vacant"))
                            {
                                districtRoles[district][role].Clear();
                            }
                            districtRoles[district][role].Add($"{user.FirstName} {user.LastName}|{user.Email}");
                        }
                    }
                }
            }

            // Include district system administrators
            foreach (var admin in districtAdmins)
            {
                if (admin.DistrictNumber.HasValue)
                {
                    int district = admin.DistrictNumber.Value;
                    string role = "DistrictAdmin";

                    if (districtRoles.ContainsKey(district) && districtRoles[district].ContainsKey(role))
                    {
                        if (districtRoles[district][role].Contains("Vacant"))
                        {
                            districtRoles[district][role].Clear();
                        }
                        districtRoles[district][role].Add($"{admin.FirstName} {admin.LastName}|{admin.StateEmail}");
                    }
                }
            }

            // Organize central office roles with consistent keys
            var centralOfficeRoles = new Dictionary<string, List<string>>
                        {
                            { "RegionalEngineer", new List<string>() },
                            { "HDS", new List<string>() },
                            { "GISManager", new List<string>() },
                            { "Chief", new List<string>() },
                            { "DirectorOfOperations", new List<string>() },
                            { "StatewideAdmin", new List<string>() }
                        };
            //  Add statewide admins to Central Office
            foreach (var admin in statewideAdmins)
            {
                centralOfficeRoles["StatewideAdmin"].Add($"{admin.FirstName} {admin.LastName}|{admin.StateEmail}");
            }

            foreach (var user in users)
            {
                if (user.RegionalEngineer)
                {
                    string districts = user.DistrictsForRegionalEngineer ?? "N/A";
                    centralOfficeRoles["RegionalEngineer"].Add($"{user.FirstName} {user.LastName}|{user.Email}|{districts}");
                }
                if (user.HDS)
                {
                    centralOfficeRoles["HDS"].Add($"{user.FirstName} {user.LastName}|{user.Email}");
                }
                if (user.GISManager)
                {
                    centralOfficeRoles["GISManager"].Add($"{user.FirstName} {user.LastName}|{user.Email}");
                }
                if (user.Chief)
                {
                    centralOfficeRoles["Chief"].Add($"{user.FirstName} {user.LastName}|{user.Email}");
                }
                if (user.DirectorOfOperations)
                {
                    centralOfficeRoles["DirectorOfOperations"].Add($"{user.FirstName} {user.LastName}|{user.Email}");
                }
            }
            

            // Handle vacant central office roles
            foreach (var role in centralOfficeRoles.Keys.ToList())
            {
                if (!centralOfficeRoles[role].Any())
                {
                    centralOfficeRoles[role].Add("Vacant");
                }
            }

            // Pass data to the view
            ViewData["SystemAdmins"] = systemAdmins;
            ViewData["DistrictRoles"] = districtRoles;
            ViewData["CentralOfficeRoles"] = centralOfficeRoles;

            return View();
        }






        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Resources()
        {
            return View();
        }

        public IActionResult SaveForLater()
        {
            HttpContext.Session.Remove("UniqueID");
            return RedirectToAction("Index", "Home");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}