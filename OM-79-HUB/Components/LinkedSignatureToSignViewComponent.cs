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

            var centralOfficeUsers = _context.UserData.Where(e => e.GISManager || e.HDS || e.Chief);
            bool isCentralSigner = centralOfficeUsers.Any(u => u.ENumber == userENumber);

            //District level signing view
            if (isDistrictSigner && (omEntry.WorkflowStep == "SubmittedToDistrict" || omEntry.WorkflowStep == "SubmittedToDistrictManager"))
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
            // Special case for District Manager when sent back
            else if (isDistrictSigner && (omEntry.WorkflowStep == "SubmittedBackToDistrictManager" || omEntry.WorkflowStep == "SubmittedBackToDistrictManagerFromOperations"))
            {
                var userRolesEntries = _context.UserData.Where(u => u.ENumber.ToLower() == userENumber.ToLower()).ToList();
                var signeesRoles = new List<string>();

                foreach (var userRoles in userRolesEntries)
                {
                    if (userRoles.DistrictManager && !signeesRoles.Contains("District Manager")) signeesRoles.Add("District Manager");
                }

                var currentSignatures = _context.SignatureData.Where(entry => entry.HubKey == hubID).ToList();

                var viewModel = new SignaturesToSignViewModel
                {
                    SigneesRoles = signeesRoles,
                    CurrentSignatures = currentSignatures,
                    OmEntry = omEntry // Include omEntry in the view model
                };

                return View("~/Views/CentralSignatureWorkflow/_linkedSignaturesToSignCentral.cshtml", viewModel);
            }

            // Central office level signing view
            else if (isCentralSigner && (omEntry.WorkflowStep == "SubmittedToCentralHDS" || omEntry.WorkflowStep == "SubmittedToCentralGIS" || omEntry.WorkflowStep == "SubmittedToRegionalEngineer" || omEntry.WorkflowStep == "SubmittedToDirectorOfOperations" || omEntry.WorkflowStep == "SubmittedToCentralChief"))
            {
                var userRolesEntries = _context.UserData.Where(u => u.ENumber.ToLower() == userENumber.ToLower()).ToList();
                var signeesRoles = new List<string>();

                foreach (var userRoles in userRolesEntries)
                {
                    if (userRoles.HDS && !signeesRoles.Contains("HDS")) signeesRoles.Add("HDS");
                    if (userRoles.GISManager && !signeesRoles.Contains("GIS Manager")) signeesRoles.Add("GIS Manager");
                    if (userRoles.Chief && !signeesRoles.Contains("Chief")) signeesRoles.Add("Chief");
                    if (userRoles.RegionalEngineer && !signeesRoles.Contains("Regional Engineer")) signeesRoles.Add("Regional Engineer");
                    if (userRoles.DirectorOfOperations && !signeesRoles.Contains("Director of Operations")) signeesRoles.Add("Director of Operations");
                    if (userRoles.DeputySecretary && !signeesRoles.Contains("DeputySecretary")) signeesRoles.Add("Deputy Secretary");
                }

                var currentSignatures = _context.SignatureData.Where(entry => entry.HubKey == hubID).ToList();

                var viewModel = new SignaturesToSignViewModel
                {
                    SigneesRoles = signeesRoles,
                    CurrentSignatures = currentSignatures,
                    OmEntry = omEntry // Include omEntry in the view model
                };

                return View("~/Views/CentralSignatureWorkflow/_linkedSignaturesToSignCentral.cshtml", viewModel);
            }

            // Not submitted to be reviewed 
            else if (omEntry.IsSubmitted == false)
            {
                // The user has not submitted the OM79 yet so no signatures needs to be displayed to them
                return View("~/Views/CENTRAL79HUB/_DisplayNone.cshtml");
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
            public List<SignatureData>? CurrentSignatures { get; set; }
            public CENTRAL79HUB OmEntry { get; set; } // Add this property

        }
    }
}
