using System.ComponentModel.DataAnnotations;

namespace OM_79_HUB.Models.DB.OM79Hub
{
    public class OM79Workflow
    {
        [Key]
        public int OM79WorkflowID { get; set; }
        public int? HubID { get; set; }
        public int? NumberOfItems { get; set; }
        public string? NextStep { get; set; }
    }
}
