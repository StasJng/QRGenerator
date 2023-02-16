﻿using GenerateQRCode_Demo.Models;
using IronBarCode;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;

namespace GenerateQRCode_Demo.Controllers
{
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public HomeController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult CreateQRCode()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateQRCode(GenerateQRCodeModel generateQRCode, string file)
        {
            try
            {
                GeneratedBarcode barcode = QRCodeWriter.CreateQrCode(generateQRCode.QRCodeText, 200);
                barcode.AddBarcodeValueTextBelowBarcode();
                // Styling a QR code and adding annotation text
                barcode.SetMargins(5);
                barcode.ChangeBarCodeColor(Color.Black);

                string path = Path.Combine(_environment.WebRootPath, "GeneratedQRCode");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string filePath = Path.Combine(_environment.WebRootPath, "GeneratedQRCode/qrcode.png");
                barcode.SaveAsPng(filePath);
                string fileName = Path.GetFileName(filePath);
                string imageUrl = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}"+ "/GeneratedQRCode/" + fileName;

                ViewBag.QrCodeUri = imageUrl;

                //WebClient client = new WebClient();
                //Stream stream = client.OpenRead(imageUrl);
                //Bitmap bitmap; 
                //bitmap = new Bitmap(stream);

                //if (bitmap != null)
                //{
                //    bitmap.Save("qr_img_for_" + generateQRCode.QRCodeText, ImageFormat.Png);
                //}

                //stream.Flush();
                //stream.Close();
                //client.Dispose();

                WebClient client = new WebClient();
                Stream stream = client.OpenRead(imageUrl);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (Bitmap bitMap = new Bitmap(stream))
                    {
                        if (bitMap != null)
                        {
                            bitMap.Save(ms, ImageFormat.Png);
                        }
                        var qrResult = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                        ViewBag.qrString = qrResult;
                        ViewBag.linkDownload = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return View();
        }
        //read data from uploaded file
        private DataTable ReadImportExcelFile(string sheetName, string path)
        {
            sheetName = sheetName.Trim();
            path = path.Trim();
            using (OleDbConnection conn = new OleDbConnection())
            {
                DataTable dt = new DataTable();
                string Import_FileName = path;
                string fileExtension = Path.GetExtension(Import_FileName);
                if (fileExtension == ".xls")
                    conn.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + Import_FileName + ";" + "Extended Properties='Excel 8.0;HDR=YES;'";
                if (fileExtension == ".xlsx")
                    conn.ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + Import_FileName + ";" + "Extended Properties='Excel 12.0 Xml;HDR=YES;'";
                using (OleDbCommand comm = new OleDbCommand())
                {
                    comm.CommandText = "Select * from [" + sheetName + "$]";
                    comm.Connection = conn;
                    using (OleDbDataAdapter da = new OleDbDataAdapter())
                    {
                        da.SelectCommand = comm;
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadFileThenGenQR(IFormFile file)
        {
            #region Upload file
            if (file == null || file.Length == 0) 
            {
                ViewBag.FileNotSelected = "File not selected!";
                return View("~/Views/Shared/Error.cshtml");
            }

            var path = Path.Combine(
                        Directory.GetCurrentDirectory(), "wwwroot",
                        file.GetFilename());

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            #endregion

            #region Generate QR code list
            try
            {
                #region Read Data From imported file
                DataTable dt = new DataTable();
                dt = ReadImportExcelFile("Sheet1", Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.GetFilename())); //Get Excel file with static path

                List<DataRow> list = dt.AsEnumerable().ToList();
                List<GenerateQRCodeModel> lstCode = (from DataRow row in dt.Rows
                                                     select new GenerateQRCodeModel
                                                     {
                                                         QRCodeText = row["Code"].ToString()
                                                     }
                                                    ).ToList();
                #endregion
                #region Generate QR list
                var rowNum = 0;

                List<DisplayingCodeInfo> lstDisplay = new List<DisplayingCodeInfo>();
                foreach (var code in lstCode)
                {
                    rowNum++;
                    GeneratedBarcode barcode = QRCodeWriter.CreateQrCode(code.QRCodeText, 200);
                    barcode.AddBarcodeValueTextBelowBarcode();

                    // Styling a QR code and adding annotation text
                    barcode.SetMargins(5);
                    barcode.ChangeBarCodeColor(Color.Black);

                    string imageQRPath = Path.Combine(_environment.WebRootPath, "GeneratedQRCode");
                    if (!Directory.Exists(imageQRPath))
                    {
                        Directory.CreateDirectory(imageQRPath);
                    }

                    string filePath = Path.Combine(_environment.WebRootPath, "GeneratedQRCode/qrcode_" + code.QRCodeText + ".png");
                    barcode.SaveAsPng(filePath);
                    string fileName = Path.GetFileName(filePath);
                    string imageUrl = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}" + "/GeneratedQRCode/" + fileName;

                    WebClient client = new WebClient();
                    Stream stream = client.OpenRead(imageUrl);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (Bitmap bitMap = new Bitmap(stream))
                        {
                            if (bitMap != null)
                            {
                                bitMap.Save(ms, ImageFormat.Png);
                            }
                            var qrResult = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                            ViewBag.linkDownload = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                        }
                    }

                    lstDisplay.Add(new DisplayingCodeInfo()
                    {
                        No = rowNum,
                        QRCodeUri = imageUrl,
                        LinkDownload = ViewBag.linkDownload
                    });

                    ViewBag.listDisplay = lstDisplay;
                }
                #endregion
            }
            catch (Exception ex)
            {
                throw;
            }
            #endregion

            //Remove file uploaded
            System.IO.File.Delete(path);

            return View("~/Views/Home/CreateQRCode.cshtml");
        }

        //get project file path + filename 
        //[HttpPost]
        //public string PickAFile(string fileName)
        //{
        //    if (fileName == null || fileName.Length == 0)
        //        return "file not selected";

        //    var path = Path.Combine(
        //                Directory.GetCurrentDirectory(), "wwwroot",
        //                fileName);

        //    return path;
        //}
    }
}