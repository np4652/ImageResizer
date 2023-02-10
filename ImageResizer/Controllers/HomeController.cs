using ImageResizer.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Net.Http.Headers;
using System.Text;
//using static System.Net.Mime.MediaTypeNames;

namespace ImageResizer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult ResizeImage(IFormFile file)
        {
            var response = new Response();
            var resizedImg = resizeImage(file);
            using (var stream = new MemoryStream())
            {
                resizedImg.Save(stream, ImageFormat.Jpeg);
                var formFile = new FormFile(stream, 0, stream.Length, "req[0].file", file.FileName)
                {
                    Headers = new HeaderDictionary(),
                    ContentDisposition = "form-data;FileName=" + file.FileName,
                    ContentType = "image/jpeg"
                };

                string Paths = FileDirectories.ProductVariant;
                response = UploadFile(new FileUploadModel
                {
                    file = formFile,
                    FileName = DateTime.Now.ToString("ddMMyyyyhhmmsstt")+file.FileName,
                    FilePath = Paths,
                    IsThumbnailRequired = false,
                });
            }
            return View(response);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private Bitmap resizeImage(IFormFile file, int width = 0, int height = 0)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    var bytes = ms.ToArray();
                    Bitmap image = (Bitmap)Image.FromStream(ms, true, false);
                    if (width==0)
                    {
                        width = image.Width;
                    }
                    if (height==0)
                    {
                        height = image.Height;
                    }
                    Bitmap result = new Bitmap(width, height, PixelFormat.Format16bppRgb555);// Format24bppRgb
                    result.SetResolution(image.HorizontalResolution, image.VerticalResolution);
                    using (Graphics g = Graphics.FromImage(result))
                    {
                        Rectangle oRectangle = new Rectangle(0, 0, width, height);
                        g.DrawImage(image, oRectangle);
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {

            }
            return null;
        }

        public Response UploadFile(FileUploadModel request)
        {
            var response = new Response
            {
                StatusCode = ResponseStatus.Success,
                ResponseText = ResponseStatus.Success.ToString()
            };
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(request.FilePath);
                if (!Directory.Exists(sb.ToString()))
                {
                    Directory.CreateDirectory(sb.ToString());
                }
                var filename = ContentDispositionHeaderValue.Parse(request.file.ContentDisposition).FileName.Trim('"');
                string originalExt = Path.GetExtension(filename).ToLower();
                string[] Extensions = { ".png", ".jpeg", ".jpg" };
                if (string.IsNullOrEmpty(request.FileName))
                {
                    request.FileName = filename;
                }
                sb.Append(request.FileName);
                using (FileStream fs = System.IO.File.Create(sb.ToString()))
                {
                    request.file.CopyTo(fs);
                    fs.Flush();
                }
                response.StatusCode = ResponseStatus.Success;
                response.ResponseText = "File uploaded successfully";
            }
            catch (Exception ex)
            {
                response.ResponseText = "Error in file uploading. Try after sometime...";
            }
            return response;
        }
    }

    public class Response
    {
        public ResponseStatus StatusCode { get; set; } = ResponseStatus.Failed;
        public string ResponseText { get; set; } = ResponseStatus.Failed.ToString();
    }

    public class FileUploadModel
    {
        public IFormFile file { get; set; }
        public List<IFormFile> Files { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int Id { get; set; }
        public bool IsThumbnailRequired { get; set; }
    }

    public enum ResponseStatus
    {
        Failed = -1,
        Success = 1,
        Pending = 2,
        info = 3,
        warning = 4,
        Expired = -2,
    }

    public class FileDirectories
    {
        public const string ProductSuffix = "Image/Product/";
        public static string ProductVariant = Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot/{ProductSuffix}/");
    }
}