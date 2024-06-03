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
using System.Drawing;
using System.Security.Cryptography;
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
            if (omHUB == null)
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

                // Fetch the PJ103 submissions attached to the OM79 entry
                var pj103AttachedToOMSubmission = await _pj103Context.Submissions
                                            .Where(submission => submission.OM79Id == om79.Id)
                                            .ToListAsync();
                var submissionIds = pj103AttachedToOMSubmission.Select(s => s.SubmissionID).ToList();
                // Fetch the related RouteInfo entries
                var pj103AttachedToOM = await _pj103Context.RouteInfo
                                            .Where(routeInfo => submissionIds.Contains(routeInfo.SubmissionID.Value))
                                            .ToListAsync();
                foreach (var pj103 in pj103AttachedToOM)
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


            var OMID = omTableEntry.Id;
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("++++++++++++++++++++++" + OMID + "++++++++++++++++++++++++++++");
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");


            // Fetch the PJ103 submissions attached to the OM79 entry
            var pj103AttachedToOMSubmission = await _pj103Context.Submissions
                                        .Where(submission => submission.OM79Id == OMID)
                                        .ToListAsync();

            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("+++++++++++++++++" + pj103AttachedToOMSubmission.Count + "++++");
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            // Ensure there are related submissions
            if (!pj103AttachedToOMSubmission.Any())
            {
                // If no PJ103 submissions are found, generate only the OM79 file
                await PrintOM79File(id);
                return RedirectToAction("Details", "OM79", new { id = id.Value });
            }

            // Extract SubmissionIDs
            var submissionIds = pj103AttachedToOMSubmission.Select(s => s.SubmissionID).ToList();

            // Fetch the related RouteInfo entries
            var pj103AttachedToOM = await _pj103Context.RouteInfo
                                        .Where(routeInfo => submissionIds.Contains(routeInfo.SubmissionID.Value))
                                        .ToListAsync();

            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("++++++++++++++++++++++" + submissionIds + "+++++++++++++++++++");
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");


            // If no RouteInfo entries are found, generate only the OM79 file
            if (!pj103AttachedToOM.Any())
            {
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

            var pjTableEntry = await _pj103Context.RouteInfo.FirstOrDefaultAsync(e => e.SubmissionID == id.Value);
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
            public RouteInfo RouteInfo { get; }

            public PJ103DocumentGeneration(RouteInfo routeInfo)
            {
                RouteInfo = routeInfo;
            }

            public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

            public void Compose(IDocumentContainer container)
            {
                container
                    .Page(page =>
                    {
                        page.Margin(50);
                        page.Header().Element(ComposeHeader);
                        page.Content().Element(ComposeContent);
                        page.Footer().Height(10).Background(Colors.Grey.Lighten1);
                    });
            }

            void ComposeHeader(IContainer container)
            {
                container.Row(row =>
                {
                    var fallbackStyle = TextStyle.Default.FontFamily("Microsoft PhagsPa");
                    var titleStyle = TextStyle.Default.FontSize(10).SemiBold().Italic().FontColor(Colors.Black).Fallback(fallbackStyle);
                    row.ConstantItem(50).Height(50).Image("wwwroot/Assets/dot.png");

                    row.RelativeItem().Column(column =>
                    {
                        column.Item().AlignCenter().Text(text =>
                        {
                            text.Span("PJ103").Style(titleStyle);
                        });

                        column.Item().AlignRight().Text(text =>
                        {
                            string formattedDate = RouteInfo.SubmissionID.HasValue
                                                   ? RouteInfo.SubmissionID.Value.ToString()
                                                   : "N/A";
                            text.Span("Submission ID: " + formattedDate).Style(titleStyle);
                        });
                    });
                });
            }

            void ComposeContent(IContainer container)
            {
                var fallbackStyle = TextStyle.Default.FontFamily("Microsoft PhagsPa");
                var tableStyle = TextStyle.Default.FontSize(10).FontColor(Colors.Black).Fallback(fallbackStyle);
                var boldTableStyle = TextStyle.Default.FontSize(13).Bold().FontColor(Colors.Black).Fallback(fallbackStyle);

                container
                    .Background(Colors.White)
                    .AlignLeft()
                    .AlignCenter()
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Row(1).ColumnSpan(3).Column(1).PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                        table.Cell().Row(2).ColumnSpan(3).Column(1).Text("Nature of Change Section").Style(boldTableStyle);
                        table.Cell().Row(3).ColumnSpan(3).Column(1).PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                        // First Row
                        table.Cell().Row(4).Column(1).Element(CellStyle)
                            .Text("Access Control:\n" + (RouteInfo.AccessControl ?? "N/A")).Style(tableStyle);
                        table.Cell().Row(4).Column(2).Element(CellStyle)
                            .Text("Through Lanes:\n" + (RouteInfo.ThroughLanes?.ToString() ?? "N/A")).Style(tableStyle);
                        table.Cell().Row(4).Column(3).Element(CellStyle)
                            .Text("Counter Peak Lanes:\n" + (RouteInfo.CounterPeakLanes?.ToString() ?? "N/A")).Style(tableStyle);

                        // Second Row
                        table.Cell().Row(5).Column(1).Element(CellStyle)
                            .Text("Peak Lanes:\n" + (RouteInfo.PeakLanes?.ToString() ?? "N/A")).Style(tableStyle);
                        table.Cell().Row(5).Column(2).Element(CellStyle)
                            .Text("Reversible Lanes:\n" + (RouteInfo.ReverseLanes ?? "N/A")).Style(tableStyle);
                        table.Cell().Row(5).Column(3).Element(CellStyle)
                            .Text("Median Width (ft.):\n" + (RouteInfo.MedianWidth?.ToString() ?? "N/A")).Style(tableStyle);

                        // Third Row
                        table.Cell().Row(6).Column(1).Element(CellStyle)
                            .Text("Lane Width (ft.) (round to whole number):\n" + (RouteInfo.LaneWidth?.ToString() ?? "N/A")).Style(tableStyle);
                        table.Cell().Row(6).Column(2).Element(CellStyle)
                            .Text("Grade Width (ft.) (round to whole number):\n" + (RouteInfo.GradeWidth?.ToString() ?? "N/A")).Style(tableStyle);
                        table.Cell().Row(6).Column(3).Element(CellStyle)
                            .Text("Roadway Width (ft.) (round to whole number):\n" + (RouteInfo.PavementWidth?.ToString() ?? "N/A")).Style(tableStyle);

                        // Fourth Row
                        table.Cell().Row(7).Column(1).Element(CellStyle)
                            .Text("Surface Type:\n" + (RouteInfo.SurfaceType1 ?? "N/A")).Style(tableStyle);
                        table.Cell().Row(7).Column(2).Element(CellStyle)
                            .Text("Facility Type:\n" + (RouteInfo.FacilityType ?? "N/A")).Style(tableStyle);
                        table.Cell().Row(7).Column(3).Element(CellStyle)
                            .Text("Median Type:\n" + (RouteInfo.MedianType ?? "N/A")).Style(tableStyle);

                        // Fifth Row
                        table.Cell().Row(8).Column(1).Element(CellStyle)
                            .Text("Gov ID Ownership:\n" + (RouteInfo.GovIDOwnership ?? "N/A")).Style(tableStyle);
                        table.Cell().Row(8).Column(2).Element(CellStyle)
                            .Text("WV Functional Class:\n" + (RouteInfo.WVlegalClass ?? "N/A")).Style(tableStyle);
                        table.Cell().Row(8).Column(3).Element(CellStyle)
                            .Text("Federal Functional Class:\n" + (RouteInfo.FunctionalClass ?? "N/A")).Style(tableStyle);

                        // Sixth Row
                        table.Cell().Row(9).Column(1).Element(CellStyle)
                            .Text("Federal Aid:\n" + (RouteInfo.FederalAid ?? "N/A")).Style(tableStyle);
                        table.Cell().Row(9).Column(2).Element(CellStyle)
                            .Text("Special System:\n" + (RouteInfo.SpecialSys ?? "N/A")).Style(tableStyle);
                        table.Cell().Row(9).Column(3).Element(CellStyle)
                            .Text("Federal Forest Highway:\n" + (RouteInfo.FedForestHighway ?? "N/A")).Style(tableStyle);

                        // Seventh Row
                        table.Cell().Row(10).Column(1).Element(CellStyle)
                            .Text("Truck Route:\n" + (RouteInfo.TruckRoute ?? "N/A")).Style(tableStyle);
                        table.Cell().Row(10).Column(2).Element(CellStyle)
                            .Text("NHS:\n" + (RouteInfo.NHS ?? "N/A")).Style(tableStyle);
                        table.Cell().Row(10).Column(3).Element(CellStyle)
                            .Text("Starting MP:\n" + (RouteInfo.MPSegmentStart?.ToString() ?? "N/A")).Style(tableStyle);

                        // Eighth Row
                        table.Cell().Row(11).Column(1).Element(CellStyle)
                            .Text("Ending MP:\n" + (RouteInfo.MPSegmentEnd?.ToString() ?? "N/A")).Style(tableStyle);
                        table.Cell().Row(11).Column(2).Element(CellStyle)
                            .Text("Surface Type N:\n" + (RouteInfo.SurfaceTypeN ?? "N/A")).Style(tableStyle);
                        table.Cell().Row(11).Column(3).Element(CellStyle)
                            .Text("Depth:\n" + (RouteInfo.Depth?.ToString() ?? "N/A")).Style(tableStyle);

                        // Additional Comments Section
                        table.Cell().Row(12).ColumnSpan(3).Column(1).PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                        table.Cell().Row(13).ColumnSpan(3).Column(1).Text("Additional Comments").Style(boldTableStyle).Underline();
                        table.Cell().Row(14).ColumnSpan(3).Column(1).Element(CellStyle)
                            .Text(RouteInfo.SubmissionIDUAB?.ToString() ?? "N/A").Style(tableStyle);
                    });
            }

            IContainer CellStyle(IContainer container) => container
                .Border(1)
                .BorderColor(Colors.Black)
                .Padding(5);
        }




        /*
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
                    var fallbackStyle = TextStyle.Default.FontFamily("Microsoft PhagsPa");
                    var titleStyle = TextStyle.Default.FontSize(10).SemiBold().Italic().FontColor(Colors.Black).Fallback(fallbackStyle); // Specify fallback font
                    
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
        */


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
                        page.Content().Element(ComposeContent);
                        page.Footer().Height(10).Background(Colors.Grey.Lighten1);
                    });
            }

            void ComposeHeader(IContainer container)
            {
                container.Row(row =>
                {
                    var fallbackStyle = TextStyle.Default.FontFamily("Microsoft PhagsPa");
                    var titleStyle = TextStyle.Default.FontSize(10).SemiBold().Italic().FontColor(Colors.Black).Fallback(fallbackStyle); // Apply fallback style
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
                                                   ? OMTable.SubmissionDate.Value.ToString("MM/dd/yyyy") 
                                                   : "N/A"; 
                            text.Span(formattedDate).Style(titleStyle);
                        });

                    });
                });
            }



            void ComposeContent(IContainer container)
            {
                var fallbackStyle = TextStyle.Default.FontFamily("Microsoft PhagsPa");
                var tableStyle = TextStyle.Default.FontSize(10).FontColor(Colors.Black).Fallback(fallbackStyle); // Apply fallback style;
                var boldTableStyle = TextStyle.Default.FontSize(13).Bold().FontColor(Colors.Black).Fallback(fallbackStyle);


                container
                        .Background(Colors.White)
                        .AlignLeft()
                        .AlignCenter()
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });
                            // More Title Info 
                            table.Cell().Row(1).ColumnSpan(3).Column(1).PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                            table.Cell().Row(2).ColumnSpan(3).Column(1).Text("Changes to the State Road System").Style(boldTableStyle);
                            table.Cell().Row(3).ColumnSpan(3).Column(1).PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                            table.Cell().Row(4).ColumnSpan(3).Column(1).Text("The District requests a Commissioner's Order be entered to implement the following road change(s):").Style(tableStyle);
                            table.Cell().Row(5).ColumnSpan(3).Column(1).PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);



                            // Road Change Type Section with Textbox Outline
                            table.Cell().Row(6).ColumnSpan(3).Column(1).Text("Road Change Type").Style(boldTableStyle).Underline();

                            table.Cell().Row(7).Column(1).Element(CellStyle)
                                .Text("Road Change Type:\n" + (OMTable?.RoadChangeType ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(7).Column(2).Element(CellStyle)
                                .Text("Requested By:\n" + (OMTable?.RequestedBy ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(7).Column(3).Element(CellStyle)
                                .Text("Requester Name:\n" + (OMTable?.RequestedByName ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(8).ColumnSpan(3).Column(1).Element(CellStyle)
                                .PaddingVertical(10).Text("Description:\n" + (OMTable?.Attachments ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(9).Column(1).Element(CellStyle)
                                .Text("Route Assignment:\n" + (OMTable?.RouteAssignment ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(9).Column(2).Element(CellStyle)
                                .Text("Route:\n" + (OMTable?.Route?.ToString() ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(9).Column(3).Element(CellStyle)
                                .Text("SubRoute:\n" + (OMTable?.SubRoute?.ToString() ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(10).Column(1).Element(CellStyle)
                                .Text("Supplemental Code:\n" + (OMTable?.Supplemental ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(10).Column(2).Element(CellStyle)
                                .Text("Right Of Way Width:\n" + (OMTable?.RightOfWayWidth ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(11).ColumnSpan(3).Column(1).Element(CellStyle)
                                .PaddingVertical(10).Text("Explanation:\n" + (OMTable?.Comments ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(12).ColumnSpan(3).Column(1).PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);


                            // Adjacent Property Section
                            table.Cell().Row(13).ColumnSpan(3).Column(1).Text("Adjacent Property Information").Style(boldTableStyle).Underline();

                            table.Cell().Row(14).Column(1).Element(CellStyle)
                                .Text("Number of Houses:\n" + (OMTable?.APHouses?.ToString() ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(14).Column(2).Element(CellStyle)
                                .Text("Number of Businesses:\n" + (OMTable?.APBusinesses?.ToString() ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(14).Column(3).Element(CellStyle)
                                .Text("Number of Schools:\n" + (OMTable?.APSchools?.ToString() ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(15).Column(1).Element(CellStyle)
                                .Text("Number of Other:\n" + (OMTable?.APOther?.ToString() ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(15).Column(2).Element(CellStyle)
                                .Text("Total Adjacent Property:\n" + (OMTable?.AdjacentProperty?.ToString() ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(15).Column(3).Element(CellStyle)
                                .Text("Other Properties Explanation:\n" + (OMTable?.APOtherIdentify ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(16).ColumnSpan(3).Column(1).PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                            // Bridge/Railroad Information Section
                            table.Cell().Row(17).ColumnSpan(3).Column(1)
                                .Text("Bridge/Railroad Information").Style(boldTableStyle).Underline();

                            table.Cell().Row(18).Column(1).Element(CellStyle)
                                .Text("Is there a bridge?:\n" + (OMTable?.BridgeInv == null ? "N/A" : OMTable.BridgeInv = true ? "Yes" : "No")).Style(tableStyle);

                            table.Cell().Row(18).Column(2).Element(CellStyle)
                                .Text("Number of bridges:\n" + (OMTable?.BridgeAmount?.ToString() ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(18).Column(3).Element(CellStyle)
                                .Text("BARS (Separate by comma):\n" + (OMTable?.BridgeNumbers ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(19).Column(1).Element(CellStyle)
                                .Text("Is there a railroad?:\n" + (OMTable?.RailroadInv == null ? "N/A" : OMTable.RailroadInv = true ? "Yes" : "No")).Style(tableStyle);

                            table.Cell().Row(19).Column(2).Element(CellStyle)
                                .Text("Number of railroad crossings:\n" + (OMTable?.RailroadAmount?.ToString() ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(19).Column(3).Element(CellStyle)
                                .Text("Railroad crossing numbers:\n" + (OMTable?.DOTAARNumber ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(20).ColumnSpan(3).Column(1).PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);


                            // General Information Section
                            table.Cell().Row(21).ColumnSpan(3).Column(1).Text("General Information").Style(boldTableStyle).Underline();

                            table.Cell().Row(22).Column(1).Element(CellStyle)
                                .Text("Sign System:\n" + (OMTable?.SignSystem ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(22).Column(2).Element(CellStyle)
                                .Text("Project Number:\n" + (OMTable?.ProjectNumber ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(22).Column(3).Element(CellStyle)
                                .Text("Date Complete:\n" + (OMTable?.DateComplete?.ToString("MM/dd/yyyy") ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(23).Column(1).Element(CellStyle)
                                .Text("Route Number:\n" + (OMTable?.RouteNumber?.ToString() ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(23).Column(2).Element(CellStyle)
                                .Text("Subroute Number:\n" + (OMTable?.SubRouteNumber?.ToString() ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(23).Column(3).Element(CellStyle)
                                .Text("Org Number:\n" + (OMTable?.MaintOrg ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(24).Column(1).Element(CellStyle)
                                .Text("Starting MP:\n" + (OMTable?.StartingMilePoint?.ToString() ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(24).Column(2).Element(CellStyle)
                                .Text("Ending MP:\n" + (OMTable?.EndingMilePoint?.ToString() ?? "N/A")).Style(tableStyle);

                            table.Cell().Row(24).Column(3).Element(CellStyle)
                                .Text("Year Of Survey:\n" + (OMTable?.YearOfSurvey?.ToString() ?? "N/A")).Style(tableStyle);
                        });
                IContainer CellStyle(IContainer container) => container
                    .Border(1)
                    .BorderColor(Colors.Black)
                    .Padding(5);
            }
        }
    }
}
