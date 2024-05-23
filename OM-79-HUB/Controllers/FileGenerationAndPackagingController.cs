using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.IO;
using OM_79_HUB.Data; // Assuming your DbContexts are in this namespace
using OM_79_HUB.Models;
using OM79.Models.DB;
using PJ103V3.Models.DB;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using System;
using System.ComponentModel;
using System.Composition;
using System.Diagnostics;
using IContainer = QuestPDF.Infrastructure.IContainer;

namespace OM_79_HUB.Controllers
{
    public class FileGenerationAndPackagingController : Controller
    {
        private readonly OM_79_HUBContext _hubContext;
        private readonly OM79Context _om79Context;
        private readonly Pj103Context _pj103Context;
        private readonly IWebHostEnvironment _webHostEnvironment;


        public FileGenerationAndPackagingController(OM_79_HUBContext hubContext, OM79Context om79Context, Pj103Context pj103Context, IWebHostEnvironment hostingEnvironment)
        {
            _hubContext = hubContext;
            _om79Context = om79Context;
            _pj103Context = pj103Context;
            _webHostEnvironment = hostingEnvironment;

        }





        //This section is for renaming, deleting, and deleting files attached to any hub om or pj
        //
        //
        //
        //
        //
        [HttpPost]
        public async Task<IActionResult> UploadFiles(IFormFile[] attachments, int om79Id)
        {
            if (attachments == null || attachments.Length == 0)
            {
                return BadRequest("No files uploaded.");
            }

            var baseDir = Path.Combine(_webHostEnvironment.WebRootPath, "OMAttachments");
            var omDir = Path.Combine(baseDir, "OM79-" + om79Id + "-Attachments");

            if (!Directory.Exists(omDir))
            {
                Directory.CreateDirectory(omDir);
            }

            foreach (var file in attachments)
            {
                if (file.Length > 0)
                {
                    var filePath = Path.Combine(omDir, file.FileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
            }
            var omItem = await _om79Context.OMTable.FindAsync(om79Id);
            if (omItem == null)
            {
                return NotFound();
            }

            return RedirectToAction("Details", "Central79Hub", new { id = omItem.HubId });
        }


        public async Task<IActionResult> DeleteFile(string fileName, int om79Id)
        {
            try
            {
                var baseDir = Path.Combine(_webHostEnvironment.WebRootPath, "OMAttachments");
                var omDir = Path.Combine(baseDir, "OM79-" + om79Id + "-Attachments");
                string filePath = Path.Combine(omDir, fileName);

                Console.WriteLine($"Attempting to delete file: {fileName} at path: {filePath}");

                Console.WriteLine("----------------------------------------------------------------------");
                Console.WriteLine("----------------------------------------------------------------------");
                Console.WriteLine("----------------------------------------------------------------------");
                Console.WriteLine("----------------------------------------------------------------------");
                Console.WriteLine("----------------------------------------------------------------------");
                Console.WriteLine("----------------------------------------------------------------------");
                Console.WriteLine(filePath);
                Console.WriteLine("----------------------------------------------------------------------");
                Console.WriteLine("----------------------------------------------------------------------");
                Console.WriteLine("----------------------------------------------------------------------");
                Console.WriteLine("----------------------------------------------------------------------");

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    Console.WriteLine("File deleted successfully.");
                }
                else
                {
                    Console.WriteLine("File not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during file deletion: {ex.Message}");
                // Optionally, add more error handling logic here
            }

            var omItem = await _om79Context.OMTable.FindAsync(om79Id);
            if (omItem == null)
            {
                return NotFound();
            }

            return RedirectToAction("Details", "Central79Hub", new { id = omItem.HubId });
        }

        public async Task<IActionResult> DownloadFile(string fileName, int om79Id)
        {
            try
            {
                var baseDir = Path.Combine(_webHostEnvironment.WebRootPath, "OMAttachments");
                var omDir = Path.Combine(baseDir, "OM79-" + om79Id + "-Attachments");
                string filePath = Path.Combine(omDir, fileName);

                Console.WriteLine($"Attempting to download file: {fileName} at path: {filePath}");

                if (System.IO.File.Exists(filePath))
                {
                    var memory = new MemoryStream();
                    using (var stream = new FileStream(filePath, FileMode.Open))
                    {
                        await stream.CopyToAsync(memory);
                    }
                    memory.Position = 0;
                    var contentType = "application/octet-stream";
                    return File(memory, contentType, fileName);
                }
                else
                {
                    Console.WriteLine("File not found.");
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during file download: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }











        // Packaging & Exporting Section
        //
        //|<a asp-controller="FileGenerationAndPackaging" asp-action="PrintOM79File" asp-route-id="@entry.Id" class="btn btn-primary">Export OM79</a>
        //|<a asp-controller="FileGenerationAndPackaging" asp-action="PrintPJ103File" asp-route-id="@entry.Id" class="btn btn-secondary">Export OM79 with PJ103</a>
        //|<a asp-controller="FileGenerationAndPackaging" asp-action="PrintPackagedOMPJFile" asp-route-id="@entry.Id" class="btn btn-secondary">Export Packaged OM79 w/ PJ103</a>
        //|<a asp-controller="FileGenerationAndPackaging" asp-action="PrintPackagedHubFile" asp-route-id="@entry.Id" class="btn btn-secondary">Export OM79 with PJ103</a>

        // Export Packaged Single Hub w/ zero or more OM79s w/ zero or more PJ103s
        public async Task<IActionResult> PrintOM79HUBwOM79(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var omHUB = await _hubContext.CENTRAL79HUB.FirstOrDefaultAsync(e => e.OMId == id.Value);
            if(omHUB == null)
            {
                return NotFound();
            }


            var OM79AttachedToHUB = await _om79Context.OMTable
                                   .Where(e => e.HubId == id.Value)
                                   .ToListAsync();

            if (!OM79AttachedToHUB.Any())
            {
                //There are no OM79s attached to this OMHUB
                //Nothing to export
                return RedirectToAction("Details", "CENTRAL79HUB", new { id = id.Value });
            }


            var reports = new List<IDocument>();
            var fileName = $"Hub_{omHUB.OMId}_Report.pdf";


            foreach (var om79 in OM79AttachedToHUB)
            {
                var om79Report = new OM79DocumentGeneration(om79);
                reports.Add((IDocument)om79Report);

                var pj103sAttachedToOM79 = await _pj103Context.Submissions
                                                .Where(submission => submission.OM79Id == om79.Id)
                                                .ToListAsync();

                foreach (var pj103 in pj103sAttachedToOM79)
                {
                    var pj103Report = new PJ103DocumentGeneration(pj103);
                    reports.Add((IDocument)pj103Report);
                }
            }

            if (reports.Any())
            {
                // Merge and generate PDF
                Document
                    .Merge(reports.ToArray())
                    .GeneratePdf(fileName);


                //-----------------------------------------------------------
                // Local PDF Generation
                var startInfo = new ProcessStartInfo("explorer.exe", fileName);
                Process.Start(startInfo);
                //-----------------------------------------------------------

                //-----------------------------------------------------------
                // Live PDF Generation
                // var streamManager = HttpContext.RequestServices.GetRequiredService<RecyclableMemoryStreamManager>();
                // using var memoryStream = streamManager.GetStream();
                // HttpContext.Response.ContentType = "application/pdf";
                // HttpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
                // await memoryStream.CopyToAsync(HttpContext.Response.Body);
                //-----------------------------------------------------------
            }
            return RedirectToAction("Details", "Central79Hub", new { id = id.Value });
        }


        // Export Packaged Single OM79 w/ zero or more PJ103s
        public async Task<IActionResult> PrintOM79wPJ103(int? id)
        {
            if (id == null)
            {
                return NotFound();  
            }

            var omTableEntry = await _om79Context.OMTable.FirstOrDefaultAsync(e => e.Id == id.Value);
            if (omTableEntry == null)
            {
                return NotFound();
            }

            var pj103AttachedToOM = await _pj103Context.Submissions
                                    .Where(submission => submission.OM79Id == id.Value)
                                    .ToListAsync();

            if (!pj103AttachedToOM.Any())
            {
                //There are no PJs attached to this OM79
                //Call OM79 File Generation Instead
                await PrintOM79File(id);
                return RedirectToAction("Details", "OM79", new { id = id.Value });
            }

            var fileName = "OM79[" + omTableEntry.Id + "] with PJ103s" + ".pdf";
            var reports = new List<IDocument>();

            var om79 = new OM79DocumentGeneration(omTableEntry);
            reports.Add((IDocument)om79);

            foreach (var pj103 in pj103AttachedToOM)
            {
                var pj103Report = new PJ103DocumentGeneration(pj103);
                reports.Add((IDocument)pj103Report);
            }

            //-----------------------------------------------------------
            //Local PDF Generation
            Document
                .Merge(reports.ToArray())
                .GeneratePdf(fileName);

            var startInfo = new ProcessStartInfo("explorer.exe", fileName);
            Process.Start(startInfo);
            //---------------------------------------------------------------



            //---------------------------------------------------------------
            //Live PDF Generation
            //var streamManager = HttpContext.RequestServices.GetRequiredService<RecyclableMemoryStreamManager>();
            //using var memoryStream = streamManager.GetStream();
            //HttpContext.Response.ContentType = "application/pdf";
            //HttpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
            //await memoryStream.CopyToAsync(HttpContext.Response.Body);
            //---------------------------------------------------------------

            return RedirectToAction("Details", "OM79", new { id = id.Value });
        }




        // Export Single PJ103 to PDF
        public async Task<IActionResult> PrintPJ103File(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pjTableEntry = await _pj103Context.Submissions.FirstOrDefaultAsync(e => e.SubmissionID == id.Value);
            if (pjTableEntry == null)
            {
                return NotFound();
            }
            var fileName = "PJ103[" + pjTableEntry.SubmissionID + "].pdf";
            var report = new PJ103DocumentGeneration(pjTableEntry);



            //-----------------------------------------------------------
            //Local PDF Generation
            report.GeneratePdf(fileName);
            var startInfo = new ProcessStartInfo("explorer.exe", fileName);
            Process.Start(startInfo);
            //---------------------------------------------------------------



            //---------------------------------------------------------------
            //Live PDF Generation
            //var streamManager = HttpContext.RequestServices.GetRequiredService<RecyclableMemoryStreamManager>();
            //using var memoryStream = streamManager.GetStream();
            //HttpContext.Response.ContentType = "application/pdf";
            //HttpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
            //await memoryStream.CopyToAsync(HttpContext.Response.Body);
            //---------------------------------------------------------------



            return RedirectToAction("Details", "PJ103", new { id = id.Value });
        }



        // Export Single OM79 to PDF
        public async Task<IActionResult> PrintOM79File(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var omTableEntry = await _om79Context.OMTable.FirstOrDefaultAsync(e => e.Id == id.Value);
            if (omTableEntry == null)
            {
                return NotFound();
            }
            var fileName = "OM79[" + omTableEntry.Id + "].pdf";
            var report = new OM79DocumentGeneration(omTableEntry);



            //-----------------------------------------------------------
            //Local PDF Generation
            report.GeneratePdf(fileName);
            var startInfo = new ProcessStartInfo("explorer.exe", fileName);
            Process.Start(startInfo);
            //---------------------------------------------------------------



            //---------------------------------------------------------------
            //Live PDF Generation
            //var streamManager = HttpContext.RequestServices.GetRequiredService<RecyclableMemoryStreamManager>();
            //using var memoryStream = streamManager.GetStream();
            //HttpContext.Response.ContentType = "application/pdf";
            //HttpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
            //await memoryStream.CopyToAsync(HttpContext.Response.Body);
            //---------------------------------------------------------------



            return RedirectToAction("Details", "OM79", new { id = id.Value });
        }




        // PJ103 QUEST PDF Section
        //
        //
        //
        //
        //
        public class PJ103DocumentGeneration : IDocument
        {
            public Submission Submission { get; }


            public PJ103DocumentGeneration(Submission pj103Object)
            {
                Submission = pj103Object;
            }

            public DocumentMetadata GetMetadata() => DocumentMetadata.Default;


            public void Compose(IDocumentContainer container)
            {
                container
                    .Page(page =>
                    {
                        page.Margin(50);

                        page.Header().Element(ComposeHeader);
                        //page.Content().Element(ComposeContent);
                        page.Footer().Height(10).Background(Colors.Grey.Lighten1);
                    });
            }

            void ComposeHeader(IContainer container)
            {
                container.Row(row =>
                {
                    var titleStyle = TextStyle.Default.FontSize(10).SemiBold().Italic().FontColor(Colors.Black);
                    row.ConstantItem(50).Height(50).Image("wwwroot/Assets/dot.png");
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().AlignCenter().Text(text =>
                        {
                            text.Span("Pj103").Style(titleStyle);
                        });
                        column.Item().AlignRight().Text(text =>
                        {
                            text.Span("Submission Date: " + Submission.DateComplete).Style(titleStyle);
                        });
                    });
                });
            }
        }


        // OM79 QUEST PDF Section
        //
        //
        //
        //
        //
        // OM79 File Generation
        public class OM79DocumentGeneration : IDocument
        {
            public OMTable OMTable { get; }


            public OM79DocumentGeneration(OMTable OM_Table)
            {
                OMTable = OM_Table;
            }

            public DocumentMetadata GetMetadata() => DocumentMetadata.Default;


            public void Compose(IDocumentContainer container)
            {
                container
                    .Page(page =>
                    {
                        page.Margin(50);

                        page.Header().Element(ComposeHeader);
                        //page.Content().Element(ComposeContent);
                        page.Footer().Height(10).Background(Colors.Grey.Lighten1);
                    });
            }

            void ComposeHeader(IContainer container)
            {
                container.Row(row =>
                {
                    var titleStyle = TextStyle.Default.FontSize(10).SemiBold().Italic().FontColor(Colors.Black);
                    row.ConstantItem(50).Height(50).Image("wwwroot/Assets/dot.png");


                    row.RelativeItem().Column(column =>
                    {
                        column.Item().AlignCenter().Text(text =>
                        {
                            text.Span("OM79").Style(titleStyle);
                        });

                        column.Item().AlignRight().Text(text =>
                        {
                            string formattedDate = OMTable.SubmissionDate.HasValue
                                                   ? OMTable.SubmissionDate.Value.ToString("MM/dd/yyyy") // or "MM/dd/yyyy" or any other format you prefer
                                                   : "N/A"; // In case the date is null
                            text.Span(formattedDate).Style(titleStyle);
                        });

                    });
                });
            }
        }
    }
}