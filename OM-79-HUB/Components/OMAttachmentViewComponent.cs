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
            Console.WriteLine($"Starting OMAttachmentViewComponent for OMItemID: {OMItemID}");
            Console.WriteLine("-----------------------------------------------------------------------------------");

            var baseDir = Path.Combine(_webHostEnvironment.WebRootPath, "OMAttachments");
            Console.WriteLine($"Base Directory: {baseDir}");

            var OMItemfolderDir = Path.Combine(baseDir, "OM79-" + OMItemID + "-Attachments");
            Console.WriteLine($"OMItem Folder Directory: {OMItemfolderDir}");

            List<string> files = new List<string>();
            if (Directory.Exists(OMItemfolderDir))
            {
                Console.WriteLine("Directory exists. Fetching files...");
                files = Directory.GetFiles(OMItemfolderDir).Select(Path.GetFileName).ToList();

                if (files.Any())
                {
                    Console.WriteLine("Files found:");
                    foreach (var file in files)
                    {
                        Console.WriteLine($" - {file}");
                    }
                }
                else
                {
                    Console.WriteLine("No files found in the directory.");
                }
            }
            else
            {
                Console.WriteLine("Directory does not exist.");
            }

            var viewModel = new OMAttachmentViewModel
            {
                DirectoryName = OMItemfolderDir,
                Files = files,
                OMITEMID = OMItemID // Assuming OMItemID is the HubId, update accordingly
            };

            Console.WriteLine("-----------------------------------------------------------------------------------");
            Console.WriteLine("Completed OMAttachmentViewComponent invocation.");
            Console.WriteLine("-----------------------------------------------------------------------------------");

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