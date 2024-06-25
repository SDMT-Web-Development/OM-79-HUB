using System.ComponentModel;
using System.ComponentModel.DataAnnotations;


namespace OM_79_HUB.Models.DB.OM79
{
    public class PJ103Workflow
    {
        [Key]
        public int PJ103WorkflowID { get; set; }
        public int? OMID { get; set; }
        public int? NumberOfSegments { get; set; }
    }
}
