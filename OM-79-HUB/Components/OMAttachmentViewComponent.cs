using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using OM79.Models.DB; // Update with the correct namespace
using System.Linq;



namespace OM_79_HUB.Components
{
    public class OMAttachmentViewComponent : ViewComponent
    {
        private readonly OM79Context _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public OMAttachmentViewComponent(OM79Context context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public IViewComponentResult Invoke(int OMItemID)
        {
            Console.WriteLine("-----------------------------------------------------------------------------------");
            Console.WriteLine("---------------------------------" + OMItemID + "----------------------------------");
            Console.WriteLine("-----------------------------------------------------------------------------------");

            var baseDir = Path.Combine(_webHostEnvironment.WebRootPath, "OMAttachments");
            var OMItemfolderDir = Path.Combine(baseDir, "OM79-" + OMItemID + "-Attachments");

            List<string> files = new List<string>();
            if (Directory.Exists(OMItemfolderDir))
            {
                files = Directory.GetFiles(OMItemfolderDir).Select(Path.GetFileName).ToList();
            }

            var viewModel = new OMAttachmentViewModel
            {
                DirectoryName = OMItemfolderDir,
                Files = files,
                OMITEMID = OMItemID // Assuming OMItemID is the HubId, update accordingly
            };

            return View("~/Views/OM79/_OMAttachment.cshtml", viewModel);
        }
    }

    public class OMAttachmentViewModel
    {
        public string DirectoryName { get; set; }
        public List<string> Files { get; set; }
        public int OMITEMID { get; set; }
    }
}