using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OM_79_HUB.Models
{
    public class Attachments
    {
        [Key]
        public int AttachmentID { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        [ForeignKey("SubmissionID")]
        public int? SubmissionID { get; set; }
        public OMTable? Submission { get; set; }
    }
}