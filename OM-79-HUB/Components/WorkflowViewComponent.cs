using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OM_79_HUB.Data;
using OM_79_HUB.Models;
using OM_79_HUB.Models.DB.OM79;
using OM_79_HUB.Models.DB.OM79Hub;
using OM79.Models.DB;
using PJ103V3.Models.DB;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OM_79_HUB.Components
{
    public class WorkflowViewComponent : ViewComponent
    {
        private readonly OM_79_HUBContext _OMcontext;
        private readonly OM79Context _ItemContext;
        private readonly Pj103Context _PJContext;

        public WorkflowViewComponent(OM_79_HUBContext OMcontext, OM79Context ItemContext, Pj103Context PJContext)
        {
            _OMcontext = OMcontext;
            _ItemContext = ItemContext;
            _PJContext = PJContext;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int? uniqueID = HttpContext.Session.GetInt32("UniqueID");
            WorkflowViewModel viewModel = new WorkflowViewModel();

            if (uniqueID != null)
            {
                var central79Hub = await _OMcontext.CENTRAL79HUB.FirstOrDefaultAsync(c => c.OMId == uniqueID.Value);

                if (central79Hub == null)
                {
                    // Should never happen unless someone is messing with url
                    return View("/Views/Shared/_Workflow.cshtml", viewModel);
                }

                var OM79workflow = await _OMcontext.OM79Workflow.FirstOrDefaultAsync(c => c.HubID == uniqueID.Value);
                var OM79Attached = await _ItemContext.OMTable.Where(o => o.HubId == central79Hub.OMId).ToListAsync();

                var pj103SegmentsByOMTable = new Dictionary<int, List<Submission>>();
                var pj103WorkflowsByOMTable = new Dictionary<int, PJ103Workflow?>();

                foreach (var omTable in OM79Attached)
                {
                    var pj103Segments = await _PJContext.Submissions.Where(p => p.OM79Id == omTable.Id).ToListAsync();
                    pj103SegmentsByOMTable[omTable.Id] = pj103Segments.Any() ? pj103Segments : new List<Submission>();

                    var pj103Workflow = await _ItemContext.PJ103Workflow.FirstOrDefaultAsync(p => p.OMID == omTable.Id);
                    pj103WorkflowsByOMTable[omTable.Id] = pj103Workflow;
                }

                // Get the current user's identity name
                var userIdentityName = User.Identity.Name;

                // Determine if the current user matches the OM entry UserId
                bool isCurrentUserOMEntryUser = central79Hub.UserId == userIdentityName;

                // Get the current user's ENumber and check their roles
                var userENumber = userIdentityName.Split('\\').LastOrDefault();
                var currentUserRoles = await _OMcontext.UserData.Where(u => u.ENumber == userENumber).ToListAsync();

                bool isHDSUser = false;
                bool isGISUser = false;
                bool isDistrictManager = false;

                foreach (var role in currentUserRoles)
                {
                    if (role.HDS)
                    {
                        isHDSUser = true;
                    }
                    if (role.GISManager)
                    {
                        isGISUser = true;
                    }
                    if (role.DistrictManager)
                    {
                        isDistrictManager = true;
                    }
                }

                viewModel = new WorkflowViewModel
                {
                    Central79Hub = central79Hub,
                    OM79Workflow = OM79workflow,
                    OMTableList = OM79Attached,
                    OMTableCount = OM79Attached.Count,
                    OMRequiredCount = OM79workflow?.NumberOfItems,
                    PJ103SegmentsByOMTable = pj103SegmentsByOMTable,
                    PJ103WorkflowsByOMTable = pj103WorkflowsByOMTable,
                    IsHDSUser = isHDSUser,
                    IsGISUser = isGISUser,
                    IsDistrictManager = isDistrictManager,
                    IsCurrentUserOMEntryUser = isCurrentUserOMEntryUser 
                };
            }

            return View("/Views/Shared/_Workflow.cshtml", viewModel);
        }
    }

    public class WorkflowViewModel
    {
        public CENTRAL79HUB? Central79Hub { get; set; }
        public OM79Workflow? OM79Workflow { get; set; }
        public List<OMTable>? OMTableList { get; set; }
        public int? OMTableCount { get; set; }
        public int? OMRequiredCount { get; set; }
        public Dictionary<int, List<Submission>>? PJ103SegmentsByOMTable { get; set; }
        public Dictionary<int, PJ103Workflow?>? PJ103WorkflowsByOMTable { get; set; }
        public bool IsHDSUser { get; set; }
        public bool IsGISUser { get; set; }
        public bool IsDistrictManager { get; set; }
        public bool IsCurrentUserOMEntryUser { get; set; } // Added to hold the user's OM entry status
    }
}
