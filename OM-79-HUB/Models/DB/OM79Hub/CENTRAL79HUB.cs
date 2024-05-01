using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OM_79_HUB.Models
{
    public class CENTRAL79HUB
    {
        [Key]
        public int OMId { get; set; }
        public string? UserId { get; set; }
        [DisplayName("Comment Box")]
        public string? Otherbox { get; set; }

        public int? OM79Key { get; set; }
        public int? PJ103Key { get; set; }
        public string? County {  get; set; }
        public int? District {  get; set; }
        public string? IDNumber {  get; set; }
        public string? RouteID { get; set; }
        public bool? IsArchive { get; set; }


        


    }
}
