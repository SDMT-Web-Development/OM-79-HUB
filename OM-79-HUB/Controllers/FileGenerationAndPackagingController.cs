using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.IO;
using OM_79_HUB.Data; // Assuming your DbContexts are in this namespace
using OM_79_HUB.Models;
using OM_79_HUB.Models.DB.OM79Hub;
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
        //|<a asp-controller="FileGenerationAndPackaging" asp-action="PrintOM79CoverPage" asp-route-id="@entry.Id" class="btn btn-secondary">Export Cover Page</a>

       



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





        // Export cover page for OM79
        public async Task<IActionResult> PrintOM79CoverPage(int? id)
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

            var omSignatures = await _hubContext.SignatureData
                                    .Where(e => e.HubKey == id.Value)
                                    .ToListAsync();


            var fileName = "OMCover[" + omHUB.OMId + "].pdf";
            var report = new CoverPageDocumentGeneration(omHUB, omSignatures);



            //-----------------------------------------------------------
            //Local PDF Generation
            report.GeneratePdf(fileName);
            var startInfo = new ProcessStartInfo("explorer.exe", fileName);
            Process.Start(startInfo);
            //---------------------------------------------------------------

            //What information needs to be retreived:
            //
            // From Hub
            // ID Submission Number from omHUB
            // District Number from omHUB
            // County from omHub
            // 
            // From Om79Item(s)
            // Need to retrieve all OM79-items and retrieve: (If at least one item is ie Addition then Yes)
            //      1) Addition Y/N
            //      2) Redesignation Y/N
            //      3) Correction to the Map Y/N
            //      4) Abandonment Y/N
            //      5) Removal From Inventory
            //      6) Amend  , This one will also have to retrieve the 
            //      7) Rescind
            //      8) Other
            //
            //
            // From Hub
            // Description (comment Box)

            //
            //
            //
            //
            //

            return RedirectToAction("Details", "Central79Hub", new { id = id.Value });
        }
        // Cover Page QUEST PDF Section
        //
        //
        //
        //
        //
        public class CoverPageDocumentGeneration : IDocument
        {
            public CENTRAL79HUB CENTRAL79HUB { get; }
            public List<SignatureData> SignatureDataList { get; } // Change to list

            public CoverPageDocumentGeneration(CENTRAL79HUB cENTRAL79HUB, List<SignatureData> signatureDataList) // Change the parameter
            {
                CENTRAL79HUB = cENTRAL79HUB;
                SignatureDataList = signatureDataList; // Assign to list property
            }
            public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
            private bool isFirstPage = true;  // Flag to determine if the current page is the first


            public void Compose(IDocumentContainer container)
            {
                container
                    .Page(page =>
                    {
                        page.Margin(50);

                        // Conditionally render the header only on the first page
                        if (isFirstPage)
                        {
                            page.Header().Element(ComposeHeader);
                            isFirstPage = false; // Set flag to false after rendering the header on the first page
                        }

                        page.Content().Element(ComposeContent);
                        // Optionally, add footer here if needed for all pages
                        // page.Footer().Height(10).Background(Colors.Grey.Lighten1);
                    });
            }

            void ComposeHeader(IContainer container)
            {
                container.Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeColumn().Column(col =>
                        {
                            col.Item().Text("OM-79").FontSize(10);
                            //col.Item().Text("Page 1 of 2").FontSize(10);
                            col.Item().Text("Rev. 10/18/23").FontSize(10);
                        });

                        row.RelativeColumn().AlignRight().Column(col =>
                        {
                            col.Item().Text("ID:").FontSize(10);
                        });
                    });

                    column.Item().PaddingVertical(10).BorderBottom(1).BorderColor(Colors.Black);

                    column.Item().Row(row =>
                    {
                        row.RelativeColumn().AlignCenter().Text("WEST VIRGINIA DIVISION OF HIGHWAYS").FontSize(14).Bold();
                    });

                    column.Item().Row(row =>
                    {
                        row.RelativeColumn().AlignCenter().Text("CHANGES TO THE STATE ROAD SYSTEM").FontSize(14).Bold();
                    });

                    column.Item().PaddingVertical(10).BorderBottom(1).BorderColor(Colors.Black);
                });
            }


            void ComposeContent(IContainer container)
            {
                var fallbackStyle = TextStyle.Default.FontFamily("Microsoft PhagsPa");
                var tableStyle = TextStyle.Default.FontSize(10).FontColor(Colors.Black).Fallback(fallbackStyle);
                var boldTableStyle = TextStyle.Default.FontSize(13).Bold().FontColor(Colors.Black).Fallback(fallbackStyle);
                var underlineStyle = TextStyle.Default.Underline().FontSize(10).FontColor(Colors.Black).Fallback(fallbackStyle);

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
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        //Row 1
                        table.Cell().Row(1).ColumnSpan(2).Column(1).AlignLeft().Text("From District: ").Style(tableStyle);
                        //table.Cell().Row(1).ColumnSpan(4).Column(3).AlignLeft().Text("District PlaceHolder").Style(underlineStyle); //Change

                        table.Cell().Row(1).ColumnSpan(4).Column(3).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                        {
                            container.AlignCenter().Text(CENTRAL79HUB.District).Style(tableStyle);
                        });



                        table.Cell().Row(1).ColumnSpan(1).Column(7).AlignRight().Text("Date:").Style(tableStyle);
                        //table.Cell().Row(1).ColumnSpan(2).Column(8).AlignCenter().Text("11/11/1111").Style(underlineStyle); //Change

                        table.Cell().Row(1).ColumnSpan(2).Column(8).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                        {
                            container.AlignCenter().Text("11/11/1111").Style(tableStyle);
                        });


                        //Row 2: Add space here
                        table.Cell().Row(2).ColumnSpan(9).Height(10); // Adjust height as needed


                        //Row 3
                        table.Cell().Row(3).ColumnSpan(2).Column(1).AlignLeft().Text("To: ").Style(tableStyle);
                        //table.Cell().Row(3).ColumnSpan(7).Column(3).AlignLeft().Text(" HO (through CT, from TI, OM)").Style(underlineStyle); //Change?
                        table.Cell().Row(3).ColumnSpan(4).Column(3).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                        {
                            container.AlignCenter().Text("HO (through CT, from TI, OM) (?)").Style(tableStyle);
                        });

                        //Row 4: Add space here
                        table.Cell().Row(4).ColumnSpan(9).Height(10); // Adjust height as needed


                        //Row 5
                        table.Cell().Row(5).ColumnSpan(9).Column(1).AlignLeft().Text("It is hereby requested that a Commissioner's Order be entered for the following described section of roadway").Style(tableStyle);

                        //Row 6: Add space here
                        table.Cell().Row(6).ColumnSpan(9).Height(10); // Adjust height as needed

                        // row 7
                        table.Cell().Row(7).ColumnSpan(3).Column(1).AlignLeft().Text("to the State Road System In: ").Style(tableStyle);
                        table.Cell().Row(7).ColumnSpan(4).Column(4).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                        {
                            container.AlignCenter().Text(CENTRAL79HUB.County).Style(tableStyle);
                        });
                        table.Cell().Row(7).ColumnSpan(2).Column(8).AlignLeft().Text("County, as an: ").Style(tableStyle); //Change

                        //Row 8: Add space here
                        table.Cell().Row(8).ColumnSpan(9).Height(20); // Adjust height as needed


                        //row 9
                        table.Cell().Row(9).ColumnSpan(2).Column(1).AlignLeft().Text("Addition: ").Style(tableStyle);
                        bool Addition = true; // Replace with your actual condition
                        if (Addition)
                        {
                            table.Cell().Row(9).ColumnSpan(1).Column(3).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                            {
                                container.AlignCenter().Text("X").Style(tableStyle);
                            });
                        }
                        else
                        {
                            table.Cell().Row(9).ColumnSpan(1).Column(3).BorderBottom(0.5f).BorderColor(Colors.Black).Text("").Style(tableStyle);
                        }
                        table.Cell().Row(9).ColumnSpan(3).Column(6).AlignLeft().Text("Removal From Inventory: ").Style(tableStyle);
                        if (Addition)
                        {
                            table.Cell().Row(9).ColumnSpan(1).Column(9).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                            {
                                container.AlignCenter().Text("X").Style(tableStyle);
                            });
                        }
                        else
                        {
                            table.Cell().Row(9).ColumnSpan(1).Column(9).BorderBottom(0.5f).BorderColor(Colors.Black).Text("").Style(tableStyle);
                        }


                        //Row 10
                        table.Cell().Row(10).ColumnSpan(9).Height(10); // Adjust height as needed


                        //Row 11
                        table.Cell().Row(11).ColumnSpan(2).Column(1).AlignLeft().Text("Redesignation: ").Style(tableStyle);
                        if (Addition)
                        {
                            table.Cell().Row(11).ColumnSpan(1).Column(3).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                            {
                                container.AlignCenter().Text("X").Style(tableStyle);
                            });
                        }
                        else
                        {
                            table.Cell().Row(11).ColumnSpan(1).Column(3).BorderBottom(0.5f).BorderColor(Colors.Black).Text("").Style(tableStyle);
                        }
                        table.Cell().Row(11).ColumnSpan(3).Column(6).AlignLeft().Text("Correction to the Map: ").Style(tableStyle);
                        if (Addition)
                        {
                            table.Cell().Row(11).ColumnSpan(1).Column(9).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                            {
                                container.AlignCenter().Text("X").Style(tableStyle);
                            });
                        }
                        else
                        {
                            table.Cell().Row(11).ColumnSpan(1).Column(9).BorderBottom(0.5f).BorderColor(Colors.Black).Text("").Style(tableStyle);
                        }

                        //Row 12
                        table.Cell().Row(12).ColumnSpan(9).Height(10); // Adjust height as needed



                        //Row 13
                        table.Cell().Row(13).ColumnSpan(2).Column(1).AlignLeft().Text("Amend: ").Style(tableStyle);
                        if (Addition)
                        {
                            table.Cell().Row(13).ColumnSpan(1).Column(3).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                            {
                                container.AlignCenter().Text("X").Style(tableStyle);
                            });
                        }
                        else
                        {
                            table.Cell().Row(13).ColumnSpan(1).Column(3).BorderBottom(0.5f).BorderColor(Colors.Black).Text("").Style(tableStyle);
                        }
                        table.Cell().Row(13).ColumnSpan(3).Column(6).AlignLeft().Text("Rescind: ").Style(tableStyle);
                        if (Addition)
                        {
                            table.Cell().Row(13).ColumnSpan(1).Column(9).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                            {
                                container.AlignCenter().Text("X").Style(tableStyle);
                            });
                        }
                        else
                        {
                            table.Cell().Row(13).ColumnSpan(1).Column(9).BorderBottom(0.5f).BorderColor(Colors.Black).Text("").Style(tableStyle);
                        }


                        //Row 14
                        table.Cell().Row(14).ColumnSpan(9).Height(10); // Adjust height as needed



                        //Row 15
                        table.Cell().Row(15).ColumnSpan(2).Column(1).AlignLeft().Text("Abandonment: ").Style(tableStyle);
                        if (Addition)
                        {
                            table.Cell().Row(15).ColumnSpan(1).Column(3).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                            {
                                container.AlignCenter().Text("X").Style(tableStyle);
                            });
                        }
                        else
                        {
                            table.Cell().Row(15).ColumnSpan(1).Column(3).BorderBottom(0.5f).BorderColor(Colors.Black).Text("").Style(tableStyle);
                        }
                        table.Cell().Row(15).ColumnSpan(3).Column(6).AlignLeft().Text("Other: ").Style(tableStyle);
                        if (Addition)
                        {
                            table.Cell().Row(15).ColumnSpan(1).Column(9).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                            {
                                container.AlignCenter().Text("X").Style(tableStyle);
                            });
                        }
                        else
                        {
                            table.Cell().Row(15).ColumnSpan(1).Column(9).BorderBottom(0.5f).BorderColor(Colors.Black).Text("").Style(tableStyle);
                        }



                        //Row 16
                        table.Cell().Row(16).ColumnSpan(9).Height(20); // Adjust height as needed

                        //Row 17
                        table.Cell().Row(17).ColumnSpan(9).Column(1).AlignLeft().Text("Description: ").Style(tableStyle);

                        //Row 18
                        table.Cell().Row(18).ColumnSpan(9).Height(0); // Adjust height as needed

                        //Row 19
                        table.Cell().Row(19).ColumnSpan(9).Column(1).AlignLeft()
                            .Text("Description Placeholder Description PlaceholderDescription PlaceholderDescription PlaceholderDescription PlaceholderDescription " +
                            "PlaceholderDescription PlaceholderDescription PlaceholderDescription PlaceholderDescription PlaceholderDescription PlaceholderDescription PlaceholderDescription " +
                            "PlaceholderDescription PlaceholderDescription PlaceholderDescription PlaceholderDescription PlaceholderDescription PlaceholderDescription PlaceholderDescription " +
                            "PlaceholderDescription PlaceholderDescription PlaceholderDescription PlaceholderDescription " +
                            "PlaceholderDescription PlaceholderPlaceholderDescription PlaceholderPlaceholderDescription PlaceholderPlaceholderDescription PlaceholderPlaceholderDescription PlaceholderPlaceholderDescription PlaceholderPlaceholderDescription" +
                            " PlaceholderPlaceholderDescription PlaceholderPlaceholderDescription PlaceholderPlaceholderDescription PlaceholderPlaceholderDescription PlaceholderPlaceholderDescription Placeholder").Style(tableStyle);

                        //Row 20
                        table.Cell().Row(20).ColumnSpan(9).Height(10); // Adjust height as needed

                        //Row 21
                        table.Cell().Row(21).ColumnSpan(2).Column(1).AlignLeft().Text("To be assigned Route No.").Style(tableStyle);
                        table.Cell().Row(21).ColumnSpan(7).Column(3).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                        {
                            container.AlignCenter().Text("Route Number Placeholder").Style(tableStyle);
                        });

                        //Row 22
                        table.Cell().Row(22).ColumnSpan(9).Height(10); // Adjust height as needed

                        //Row 23
                        table.Cell().Row(23).ColumnSpan(2).Column(1).AlignLeft().Text("Right of Way Width: ").Style(tableStyle);
                        table.Cell().Row(23).ColumnSpan(7).Column(3).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                        {
                            container.AlignCenter().Text("Right of Way Placeholder").Style(tableStyle);
                        });

                        //Row 24
                        table.Cell().Row(24).ColumnSpan(9).Height(10); // Adjust height as needed

                        //Row 25
                        table.Cell().Row(25).ColumnSpan(3).Column(1).AlignLeft().Text("Is at Grade R.R. Crossing Involved?").Style(tableStyle);
                        table.Cell().Row(25).ColumnSpan(3).Column(4).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                        {
                            container.AlignCenter().Text("R/R Placeholder").Style(tableStyle);
                        });

                        table.Cell().Row(25).ColumnSpan(2).Column(7).AlignCenter().Text("If Yes DOT/AAR No. Is: ").Style(tableStyle);
                        table.Cell().Row(25).ColumnSpan(1).Column(9).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                        {
                            container.AlignCenter().Text("DOT/AAR").Style(tableStyle);
                        });

                        //Row 26
                        table.Cell().Row(26).ColumnSpan(9).Height(10); // Adjust height as needed

                        //Row 27
                        table.Cell().Row(27).ColumnSpan(2).Column(1).AlignLeft().Text("Requested By: ").Style(tableStyle);
                        table.Cell().Row(27).ColumnSpan(7).Column(3).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                        {
                            container.AlignCenter().Text("Requested By Placeholder").Style(tableStyle);
                        });

                        //Row 28
                        table.Cell().Row(28).ColumnSpan(9).Height(10); // Adjust height as needed

                        //Row 29
                        table.Cell().Row(29).ColumnSpan(2).Column(1).AlignLeft().Text("Explanation (Required):").Style(tableStyle);
                        table.Cell().Row(29).ColumnSpan(7).Column(3).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                        {
                            container.AlignCenter().Text("Explanation Placeholder").Style(tableStyle);
                        });


                        //Row 30
                        table.Cell().Row(30).ColumnSpan(9).Height(20); // Adjust height as needed


                        // Row 31
                        table.Cell().Row(31).ColumnSpan(9).Column(1).AlignLeft().Text("Traffic Estimation Information:").Style(tableStyle);


                        //Row 32
                        table.Cell().Row(32).ColumnSpan(9).Height(10); // Adjust height as needed


                        // Row 33
                        table.Cell().Row(33).ColumnSpan(1).Column(1).AlignLeft().Text("Number of:").Style(tableStyle);
                        table.Cell().Row(33).ColumnSpan(1).Column(3).AlignRight().Text("Houses: ").Style(tableStyle);
                        table.Cell().Row(33).ColumnSpan(1).Column(4).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                        {
                            container.AlignCenter().Text("5").Style(tableStyle);
                        });



                        table.Cell().Row(33).ColumnSpan(2).Column(6).AlignLeft().Text("Attachments:").Style(tableStyle);
                        table.Cell().Row(33).ColumnSpan(1).Column(8).AlignRight().Text("PJ-103: ").Style(tableStyle);
                        table.Cell().Row(33).ColumnSpan(1).Column(9).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                        {
                            container.AlignCenter().Text("...").Style(tableStyle);
                        });


                        //Row 34
                        table.Cell().Row(34).ColumnSpan(9).Height(10); // Adjust height as needed

                        //Row 35
                        table.Cell().Row(35).ColumnSpan(1).Column(3).AlignRight().Text("Buisness: ").Style(tableStyle);
                        table.Cell().Row(35).ColumnSpan(1).Column(4).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                        {
                            container.AlignCenter().Text("5").Style(tableStyle);
                        });

                        table.Cell().Row(35).ColumnSpan(1).Column(8).AlignRight().Text("Checklist: ").Style(tableStyle);
                        table.Cell().Row(35).ColumnSpan(1).Column(9).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                        {
                            container.AlignCenter().Text("...").Style(tableStyle);
                        });

                        //Row 36
                        table.Cell().Row(36).ColumnSpan(9).Height(10); // Adjust height as needed


                        //Row 37
                        table.Cell().Row(37).ColumnSpan(1).Column(3).AlignRight().Text("Schools: ").Style(tableStyle);
                        table.Cell().Row(37).ColumnSpan(1).Column(4).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                        {
                            container.AlignCenter().Text("5").Style(tableStyle);
                        });
                        table.Cell().Row(37).ColumnSpan(1).Column(8).AlignRight().Text("Loc. Maps:").Style(tableStyle);
                        table.Cell().Row(37).ColumnSpan(1).Column(9).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                        {
                            container.AlignCenter().Text("...").Style(tableStyle);
                        });

                        //Row 38
                        table.Cell().Row(38).ColumnSpan(9).Height(10); // Adjust height as needed

                        //row 39
                        table.Cell().Row(39).ColumnSpan(1).Column(3).AlignRight().Text("Other: ").Style(tableStyle);
                        table.Cell().Row(39).ColumnSpan(1).Column(4).BorderBottom(0.5f).BorderColor(Colors.Black).AlignCenter().Element(container =>
                        {
                            container.AlignCenter().Text("5").Style(tableStyle);
                        });

                        //row 40
                        table.Cell().Row(40).ColumnSpan(9).Height(25); // Adjust height as needed




                        //Row 41

                        /* 
                           Signature INFO

                        */
                        table.Cell().Row(41).ColumnSpan(9).Border(1).BorderColor(Colors.Black).Padding(10).AlignCenter().Text("DISTRICT RECOMMENDATION").Style(tableStyle);
                        table.Cell().Row(42).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(10).AlignCenter().Text("").Style(tableStyle);
                        table.Cell().Row(42).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(10).AlignCenter().Text("Signature").Style(tableStyle);
                        table.Cell().Row(42).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(10).AlignCenter().Text("Yes").Style(tableStyle);
                        table.Cell().Row(42).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(10).AlignCenter().Text("No").Style(tableStyle);



                        var ROWMaFound = false;
                        var DisAdmFound = false;
                        var DisEngFound = false;
                        var MainEngFound = false;
                        var ConsEngFound = false;
                        var TrafEngFound = false;
                        var BridEngFound = false;


                        foreach (var signatureData in SignatureDataList)
                        {
                            if (signatureData.SigType == "Right of Way Manager")
                            {
                                ROWMaFound = true;
                                table.Cell().Row(43).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("District Right of Way Manager").Style(tableStyle);
                                table.Cell().Row(43).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text(signatureData.Signatures).Style(tableStyle);
                                if (signatureData.IsApprove)
                                {
                                    table.Cell().Row(43).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("X").Style(tableStyle);
                                    table.Cell().Row(43).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                                }
                                else if (signatureData.IsDenied)
                                {
                                    table.Cell().Row(43).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                                    table.Cell().Row(43).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("X").Style(tableStyle);
                                }
                            }

                            if (signatureData.SigType == "District Administrator")
                            {
                                DisAdmFound = true;
                                table.Cell().Row(44).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("District Administrator").Style(tableStyle);
                                table.Cell().Row(44).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text(signatureData.Signatures).Style(tableStyle);
                                if (signatureData.IsApprove)
                                {
                                    table.Cell().Row(44).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("X").Style(tableStyle);
                                    table.Cell().Row(44).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                                }
                                else if (signatureData.IsDenied)
                                {
                                    table.Cell().Row(44).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                                    table.Cell().Row(44).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("X").Style(tableStyle);
                                }
                            }

                            if (signatureData.SigType == "District Engineer")
                            {
                                DisEngFound = true;
                                table.Cell().Row(45).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("District Engineer").Style(tableStyle);
                                table.Cell().Row(45).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text(signatureData.Signatures).Style(tableStyle);
                                if (signatureData.IsApprove)
                                {
                                    table.Cell().Row(45).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("X").Style(tableStyle);
                                    table.Cell().Row(45).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                                }
                                else if (signatureData.IsDenied)
                                {
                                    table.Cell().Row(45).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                                    table.Cell().Row(45).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("X").Style(tableStyle);
                                }
                            }

                            if (signatureData.SigType == "Maintenance Engineer")
                            {
                                MainEngFound = true;
                                table.Cell().Row(46).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("Maintenance Engineer").Style(tableStyle);
                                table.Cell().Row(46).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text(signatureData.Signatures).Style(tableStyle);
                                if (signatureData.IsApprove)
                                {
                                    table.Cell().Row(46).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("X").Style(tableStyle);
                                    table.Cell().Row(46).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                                }
                                else if (signatureData.IsDenied)
                                {
                                    table.Cell().Row(46).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                                    table.Cell().Row(46).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("X").Style(tableStyle);
                                }
                            }

                            if (signatureData.SigType == "Construction Engineer")
                            {
                                ConsEngFound = true;
                                table.Cell().Row(47).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("Construction Engineer").Style(tableStyle);
                                table.Cell().Row(47).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text(signatureData.Signatures).Style(tableStyle);
                                if (signatureData.IsApprove)
                                {
                                    table.Cell().Row(47).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("X").Style(tableStyle);
                                    table.Cell().Row(47).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                                }
                                else if (signatureData.IsDenied)
                                {
                                    table.Cell().Row(47).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                                    table.Cell().Row(47).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("X").Style(tableStyle);
                                }
                            }

                            if (signatureData.SigType == "Traffic Engineer")
                            {
                                TrafEngFound = true;
                                table.Cell().Row(48).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("Traffic Engineer").Style(tableStyle);
                                table.Cell().Row(48).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text(signatureData.Signatures).Style(tableStyle);
                                if (signatureData.IsApprove)
                                {
                                    table.Cell().Row(48).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("X").Style(tableStyle);
                                    table.Cell().Row(48).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                                }
                                else if (signatureData.IsDenied)
                                {
                                    table.Cell().Row(48).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                                    table.Cell().Row(48).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("X").Style(tableStyle);
                                }
                            }

                            if (signatureData.SigType == "Bridge Engineer")
                            {
                                BridEngFound = true;
                                table.Cell().Row(49).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("Bridge Engineer").Style(tableStyle);
                                table.Cell().Row(49).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text(signatureData.Signatures).Style(tableStyle);
                                if (signatureData.IsApprove)
                                {
                                    table.Cell().Row(49).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("X").Style(tableStyle);
                                    table.Cell().Row(49).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                                }
                                else if (signatureData.IsDenied)
                                {
                                    table.Cell().Row(49).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                                    table.Cell().Row(49).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("X").Style(tableStyle);
                                }
                            }
                        }

                        if (!ROWMaFound)
                        {
                            table.Cell().Row(43).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("District Right of Way Manager").Style(tableStyle);
                            table.Cell().Row(43).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("Not Signed").Style(tableStyle);
                            table.Cell().Row(43).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                            table.Cell().Row(43).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        }

                        if (!DisAdmFound)
                        {
                            table.Cell().Row(44).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("District Administrator").Style(tableStyle);
                            table.Cell().Row(44).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("Not Signed").Style(tableStyle);
                            table.Cell().Row(44).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                            table.Cell().Row(44).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        }

                        if (!DisEngFound)
                        {
                            table.Cell().Row(45).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("District Engineer").Style(tableStyle);
                            table.Cell().Row(45).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("Not Signed").Style(tableStyle);
                            table.Cell().Row(45).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                            table.Cell().Row(45).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        }

                        if (!MainEngFound)
                        {
                            table.Cell().Row(46).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("Maintenance Engineer").Style(tableStyle);
                            table.Cell().Row(46).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("Not Signed").Style(tableStyle);
                            table.Cell().Row(46).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                            table.Cell().Row(46).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        }

                        if (!ConsEngFound)
                        {
                            table.Cell().Row(47).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("Construction Engineer").Style(tableStyle);
                            table.Cell().Row(47).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("Not Signed").Style(tableStyle);
                            table.Cell().Row(47).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                            table.Cell().Row(47).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        }

                        if (!TrafEngFound)
                        {
                            table.Cell().Row(48).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("Traffic Engineer").Style(tableStyle);
                            table.Cell().Row(48).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("Not Signed").Style(tableStyle);
                            table.Cell().Row(48).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                            table.Cell().Row(48).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        }

                        if (!BridEngFound)
                        {
                            table.Cell().Row(49).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("Bridge Engineer").Style(tableStyle);
                            table.Cell().Row(49).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("Not Signed").Style(tableStyle);
                            table.Cell().Row(49).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                            table.Cell().Row(49).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        }

                        //table.Cell().Row(43).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("District Right of Way Manager").Style(tableStyle);
                        //table.Cell().Row(43).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        //table.Cell().Row(43).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        //table.Cell().Row(43).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);

                        //table.Cell().Row(44).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("District Administrator").Style(tableStyle);
                        //table.Cell().Row(44).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        //table.Cell().Row(44).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        //table.Cell().Row(44).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);

                        //table.Cell().Row(45).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("District Engineer").Style(tableStyle);
                        //table.Cell().Row(45).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        //table.Cell().Row(45).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        //table.Cell().Row(45).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);

                        //table.Cell().Row(46).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("Maintenance Engineer").Style(tableStyle);
                        //table.Cell().Row(46).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        //table.Cell().Row(46).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        //table.Cell().Row(46).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);

                        //table.Cell().Row(47).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("Construction Engineer").Style(tableStyle);
                        //table.Cell().Row(47).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        //table.Cell().Row(47).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        //table.Cell().Row(47).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);

                        //table.Cell().Row(48).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("Traffic Engineer").Style(tableStyle);
                        //table.Cell().Row(48).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        //table.Cell().Row(48).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        //table.Cell().Row(48).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);

                        //table.Cell().Row(49).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignLeft().Text("Bridge Engineer").Style(tableStyle);
                        //table.Cell().Row(49).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        //table.Cell().Row(49).ColumnSpan(1).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);
                        //table.Cell().Row(49).ColumnSpan(1).Column(9).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("").Style(tableStyle);


                        table.Cell().Row(50).ColumnSpan(9).Height(25); // Adjust height as needed



                        table.Cell().Row(51).ColumnSpan(9).Column(1).AlignLeft().Text("Note: District recommendation comment boxes on next page.").Style(tableStyle);

                        table.Cell().Row(52).ColumnSpan(9).Height(25); // Adjust height as needed


                        table.Cell().Row(53).ColumnSpan(9).Border(1).BorderColor(Colors.Black).Padding(10).AlignCenter().Text("CHIEF ENGINEER OF OPERATIONS").Style(tableStyle);

                        table.Cell().Row(54).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(10).AlignCenter().Text("").Style(tableStyle);
                        table.Cell().Row(54).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(10).AlignCenter().Text("Signature").Style(tableStyle);
                        table.Cell().Row(54).ColumnSpan(2).Column(8).Border(1).BorderColor(Colors.Black).Padding(10).AlignCenter().Text("Date").Style(tableStyle);

                        table.Cell().Row(55).ColumnSpan(2).Column(1).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("Approved:").Style(tableStyle);
                        table.Cell().Row(55).ColumnSpan(5).Column(3).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("Signature Placeholder").Style(tableStyle);
                        table.Cell().Row(55).ColumnSpan(2).Column(8).Border(1).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("11/11/1111").Style(tableStyle);



                    });
            }
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
