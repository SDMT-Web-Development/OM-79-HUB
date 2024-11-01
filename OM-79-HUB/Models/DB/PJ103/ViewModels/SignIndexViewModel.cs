using static OM_79_HUB.Controllers.CENTRAL79HUBController;
using X.PagedList;

namespace OM_79_HUB.Models.DB.PJ103.ViewModels
{
    public class SignIndexViewModel
    {
        public List<UserRole> UserRoles { get; set; }
        public Dictionary<string, IPagedList<CENTRAL79HUB>> RoleEntries { get; set; }

        public SignIndexViewModel()
        {
            UserRoles = new List<UserRole>();
            RoleEntries = new Dictionary<string, IPagedList<CENTRAL79HUB>>();
        }
    }
}
