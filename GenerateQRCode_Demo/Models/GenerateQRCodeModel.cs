using System.ComponentModel.DataAnnotations;

namespace GenerateQRCode_Demo.Models
{
    public class GenerateQRCodeModel
    {
        [Display(Name ="Enter QR Code Text")]
        public string QRCodeText { get; set; }
    }

    public class DisplayingCodeInfo
    {
        public int No { get; set; }
        public string QRCodeUri  { get; set; }
        public string LinkDownload  { get; set; }
    }
}
