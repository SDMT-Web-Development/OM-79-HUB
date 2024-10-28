using System.ComponentModel.DataAnnotations;

namespace OM_79_HUB.Models.DB.OM79Hub
{
    public class AdminData
    {
        [Key]
        public int AdminDataID { get; set; }

        [Required]
        public string? FirstName { get; set; }

        [Required]
        public string? LastName { get; set; }

        [Required]
        public string? ENumber { get; set; }

        [Required]
        [EmailAddress]
        public string? StateEmail { get; set; }

        public DateTime? DateEstablished { get; set; }

        public bool? DistrictAdmin { get; set; }

        public bool? StatewideAdmin { get; set; }

        public bool? SystemAdmin { get; set; }
        public int? DistrictNumber { get; set; }
    }
}
