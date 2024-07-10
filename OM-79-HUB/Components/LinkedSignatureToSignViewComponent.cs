using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Build.Framework;
using OM_79_HUB.Data;
using OM_79_HUB.Models;
using OM_79_HUB.Models.DB.OM79Hub;
using OM79.Models.DB;

namespace OM_79_HUB.Components
{
    [ViewComponent(Name = "LinkedSignaturesToSign")]
    public class LinkedSignatureToSignViewComponent : ViewComponent
    {
        private readonly OM_79_HUBContext _context;

        public LinkedSignatureToSignViewComponent(OM_79_HUBContext ctx) => _context = ctx;

        public IViewComponentResult Invoke(int hubID)
        {
            var userIdentityName = User.Identity.Name;
            var userENumber = userIdentityName.Split('\\').LastOrDefault(); // Assuming the format is EXECUTIVE/E000000

            var omEntry = _context.CENTRAL79HUB.FirstOrDefault(a => a.OMId == hubID);
            var specificDistrictUsers = _context.UserData.Where(a => a.District ==  omEntry.District).ToList();

            bool isDistrictSigner = specificDistrictUsers.Any(u => u.ENumber == userENumber);


            /* Bring back when SubmittedProcess is complete
               if (omEntry.WorkflowStep != "Submitted")
               {
                   //Not ready to be signed, the user has not finished the OM79
                   return View("~/Views/CENTRAL79HUB/_DisplayNone.cshtml");
               }
               */



            if (isDistrictSigner)
            {
                var userRolesEntries = _context.UserData.Where(u => u.ENumber == userENumber).ToList();
                var signeesRoles = new List<string>();

                foreach (var userRoles in userRolesEntries)
                {
                    if (userRoles.BridgeEngineer && !signeesRoles.Contains("Bridge Engineer")) signeesRoles.Add("Bridge Engineer");
                    if (userRoles.TrafficEngineer && !signeesRoles.Contains("Traffic Engineer")) signeesRoles.Add("Traffic Engineer");
                    if (userRoles.MaintenanceEngineer && !signeesRoles.Contains("Maintenance Engineer")) signeesRoles.Add("Maintenance Engineer");
                    if (userRoles.ConstructionEngineer && !signeesRoles.Contains("Construction Engineer")) signeesRoles.Add("Construction Engineer");
                    if (userRoles.RightOfWayManager && !signeesRoles.Contains("Right Of Way Manager")) signeesRoles.Add("Right Of Way Manager");
                    if (userRoles.DistrictManager && !signeesRoles.Contains("District Manager")) signeesRoles.Add("District Manager");
                }

                var currentSignatures = _context.SignatureData.Where(entry => entry.HubKey == hubID).ToList();

                var viewModel = new SignaturesToSignViewModel
                {
                    SigneesRoles = signeesRoles,
                    CurrentSignatures = currentSignatures,
                    OmEntry = omEntry // Include omEntry in the view model
                };

                return View("~/Views/CENTRAL79HUB/_linkedSignaturesToSign.cshtml", viewModel);
            }
            else
            {
                //This view will display the signature data to any user, can not edit any of them
                var entries = _context.SignatureData.Where(entry => entry.HubKey == hubID).ToList();
                return View("~/Views/CENTRAL79HUB/_LinkedSignatures.cshtml", entries);

            }
        }
        public class SignaturesToSignViewModel
        {
            public List<string> SigneesRoles { get; set; }
            public List<SignatureData> CurrentSignatures { get; set; }
            public CENTRAL79HUB OmEntry { get; set; } // Add this property

        }
    }
}
