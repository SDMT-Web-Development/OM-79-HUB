using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PJ103V3.Models.DB
{
    public class Attachments
    {
        [Key]
        public int AttachmentID { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        [ForeignKey("SubmissionID")]
        public int? SubmissionID { get; set; }
        public Submission? Submission { get; set; }

    }
}
