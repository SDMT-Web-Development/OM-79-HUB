using System.ComponentModel.DataAnnotations;
namespace OM_79_HUB.Models.DB.OM79Hub
{
    public class SignatureData
    {
        [Key]
        public int SignatureKey { get; set; }
        public int? HubKey { get; set; }
        public bool IsApprove { get; set; }
        public bool IsDenied { get; set; }
        public string? Comments { get; set; }
        [Required]
        public string? Signatures { get; set; }
        public string? ENumber { get; set; }
        public string? SigList { get; set; }
        public string? SigType { get; set; }
    }
}
