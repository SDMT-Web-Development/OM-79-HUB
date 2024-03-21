using System.ComponentModel.DataAnnotations;
namespace OM_79_HUB.Models.DB.OM79Hub
{
    public class UserData
    {
        [Key]
        public int UserKey { get; set; }
        public string? ENumber { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool? CRU { get; set; }
        public bool? CRA { get; set; }
        public bool? HDS { get; set; }
        public bool? LRS { get; set; }
        public bool? GISManager { get; set; }
        public bool? Chief { get; set; }
        public bool? DistrictReview { get; set; }
        public int? District { get; set; }
        public string? Email { get; set; }


    }
}
