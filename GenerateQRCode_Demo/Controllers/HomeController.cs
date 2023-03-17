using GenerateQRCode_Demo.Models;
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
                #region Generate QR Image
                GeneratedBarcode barcode = QRCodeWriter.CreateQrCode(generateQRCode.QRCodeText, 95);
                //barcode.AddBarcodeValueTextBelowBarcode();
                // Styling a QR code and adding annotation text
                barcode.SetMargins(5);
                barcode.ChangeBarCodeColor(Color.Gray);
                #endregion

                #region QR code image to Server
                string path = Path.Combine(_environment.WebRootPath, "GeneratedQRCode");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string filePath = Path.Combine(_environment.WebRootPath, "GeneratedQRCode/qrcode" + generateQRCode.QRCodeText + ".png");

                if (System.IO.File.Exists(filePath))
                {
                    //System.IO.File.Delete(filePath);

                    string imgExistedemoUrl = Path.Combine(_environment.ContentRootPath, "assets/img/tickets/ticket_" + generateQRCode.QRCodeText + ".png");
                    using MemoryStream ms = new MemoryStream();

                    Image image = Image.FromFile(imgExistedemoUrl);
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ViewBag.QrCodeUri = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                    ViewBag.linkDownload = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());

                    return View();
                };

                barcode.SaveAsPng(filePath);

                #endregion

                #region Create QR Image with Background Image (Watermarking !?)
                //Image backgorundImage = Image.FromFile(@"C:\\Users\\User\\Desktop\\voucherForm.jpg");
                Image backgorundImage = Image.FromFile("assets/img/voucherForm.png");
                Image imageQR = Image.FromFile(Path.Combine(_environment.WebRootPath, "GeneratedQRCode/qrcode" + generateQRCode.QRCodeText + ".png"));
                Graphics outputDemo = Graphics.FromImage(backgorundImage);
                //outputDemo.DrawImage(imageQR, backgorundImage.Width / 2 + 305, backgorundImage.Height / 2 + 105);
                outputDemo.DrawImage(imageQR, 50, 50);


                if (System.IO.File.Exists("assets/img/tickets/ticket_" + generateQRCode.QRCodeText + ".png"))
                {
                    string imgExistedemoUrl = Path.Combine(_environment.ContentRootPath, "assets/img/tickets/ticket_" + generateQRCode.QRCodeText + ".png");
                    using MemoryStream ms = new MemoryStream();

                    Image image = Image.FromFile(imgExistedemoUrl);
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ViewBag.QrCodeUri = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                    ViewBag.linkDownload = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());

                    return View();
                };

                backgorundImage.Save("assets/img/tickets/ticket_" + generateQRCode.QRCodeText + ".png");

                string imgDemoUrl = Path.Combine(_environment.ContentRootPath, "assets/img/tickets/ticket_" + generateQRCode.QRCodeText + ".png");
                using MemoryStream msDemo = new MemoryStream();

                Image img = Image.FromFile(imgDemoUrl);
                img.Save(msDemo, System.Drawing.Imaging.ImageFormat.Png);
                ViewBag.QrCodeUri = "data:image/png;base64," + Convert.ToBase64String(msDemo.ToArray());
                ViewBag.linkDownload = "data:image/png;base64," + Convert.ToBase64String(msDemo.ToArray());
                msDemo.Close();
                msDemo.Flush();
                msDemo.Dispose();
                imageQR.Dispose();
                backgorundImage.Dispose();
                #endregion

                //Set & Show QR Image only(older version)
                //string fileName = Path.GetFileName(filePath);
                //string imageUrl = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}" + "/GeneratedQRCode/" + fileName;
                ////ViewBag.QrCodeUri = imageUrl;

                //WebClient client = new WebClient();
                //Stream stream = client.OpenRead(imageUrl);

                //using (MemoryStream ms = new MemoryStream())
                //{
                //    using (Bitmap bitMap = new Bitmap(stream))
                //    {
                //        if (bitMap != null)
                //        {
                //            bitMap.Save(ms, ImageFormat.Png);
                //        }
                //        var qrResult = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                //        ViewBag.qrString = qrResult;
                //        ViewBag.linkDownload = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                //    }
                //}

            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("~/Views/Shared/Error.cshtml");
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
                ViewBag.Error = "File not selected!";
                return View("~/Views/Shared/Error.cshtml");
            }

            string fileExtension = Path.GetExtension(file.FileName);

            if (fileExtension != ".xlsx" && fileExtension != ".xls")
            {
                ViewBag.Error = "Support Excel File only!";
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
                    //barcode.StampToExistingPdfPage(@"C:\\Users\\User\\Desktop\\demoImport.pdf", 0, 0, 1, null); 

                    // Styling a QR code and adding annotation text
                    barcode.SetMargins(5, 5, 0, 5);
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

                    //stream.Flush();
                    //stream.Close();
                    //client.Dispose();
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