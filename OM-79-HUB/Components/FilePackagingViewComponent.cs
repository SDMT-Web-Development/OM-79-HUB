using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using OM_79_HUB.Data;
using OM79.Models.DB;
using OM_79_HUB.Models.FileInformation;

namespace OM_79_HUB.Components
{
    public class FilePackagingViewComponent : ViewComponent
    {

        private readonly OM_79_HUBContext _hubContext;
        private readonly OM79Context _om79Context;
        private readonly Pj103Context _pj103Context;
        private readonly IWebHostEnvironment _webHostEnvironment;


        public FilePackagingViewComponent(OM_79_HUBContext HUB, OM79Context OM79,  Pj103Context PJ103, IWebHostEnvironment webHostEnviorment)
        {
            _hubContext = HUB;
            _om79Context = OM79;    
            _pj103Context = PJ103;
            _webHostEnvironment = webHostEnviorment;
        }


        public IViewComponentResult Invoke(int hubID)
        {
            var baseDir = Path.Combine(_webHostEnvironment.WebRootPath, "OM79HubAttachments");
            var hubDir = Path.Combine(baseDir, "Hub-" + hubID + "-Attachments");
            var om79Directories = new List<DirectoryModel>();
            var om79Dirs = Directory.GetDirectories(hubDir);
            foreach (var om79Dir in om79Dirs)
            {
                var om79Model = new DirectoryModel
                {
                    DirectoryName = Path.GetFileName(om79Dir),
                    HubId = hubID, // Set the HubId
                    OM79Id = GetOM79IdFromDirectoryName(Path.GetFileName(om79Dir)) // Set the OM79Id
                };

                var om79Files = Directory.GetFiles(om79Dir);
                om79Model.Files.AddRange(om79Files.Select(Path.GetFileName));

                var pj103Dirs = Directory.GetDirectories(om79Dir);
                foreach (var pj103Dir in pj103Dirs)
                {
                    var pj103Model = new DirectoryModel
                    {
                        DirectoryName = Path.GetFileName(pj103Dir),
                        HubId = hubID, // Set the HubId for subdirectories
                        OM79Id = om79Model.OM79Id, // Set the OM79Id for subdirectories
                        PJ103Id = GetPJ103IdFromDirectoryName(Path.GetFileName(pj103Dir)) // Set the PJ103Id
                    };

                    var pj103Files = Directory.GetFiles(pj103Dir);
                    pj103Model.Files.AddRange(pj103Files.Select(Path.GetFileName));

                    om79Model.SubDirectories.Add(pj103Model);
                }

                om79Directories.Add(om79Model);
            }

            return View("~/Views/Central79Hub/_FilePackaging.cshtml", om79Directories);
        }




        private int GetOM79IdFromDirectoryName(string directoryName)
        {
            // Split the directory name by '-' characters
            string[] parts = directoryName.Split('-');

            // Check if there are at least two parts (e.g., "OM79" and "<id>Attachments")
            if (parts.Length >= 2)
            {
                // Attempt to parse the second part as an integer to get the OM79Id
                if (int.TryParse(parts[1], out int om79Id))
                {
                    return om79Id;
                }
            }

            // If the format is not as expected or parsing fails, return a default value or handle the error accordingly
            return -1; // Replace with appropriate handling or default value
        }

        private int GetPJ103IdFromDirectoryName(string directoryName)
        {
            // Split the directory name by '-' characters
            string[] parts = directoryName.Split('-');

            // Check if there are at least two parts (e.g., "PJ103" and "<id>Attachments")
            if (parts.Length >= 2)
            {
                // Attempt to parse the second part as an integer to get the PJ103Id
                if (int.TryParse(parts[1], out int pj103Id))
                {
                    return pj103Id;
                }
            }

            // If the format is not as expected or parsing fails, return a default value or handle the error accordingly
            return -1; // Replace with appropriate handling or default value
        }
    }
}