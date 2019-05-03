using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using Serenity.Services;
using Serenity.Web;

namespace SereneLargeFileUpload.Common.LargeFileUpload
{
    public class FileUploadController : ApiController
    {
        [HttpPost]
        [Route("api/fileupload")]
        [ValidateMimeMultipartContentFilter]
        public async Task<IHttpActionResult> TemporaryLargeFileUpload()
        {
            var token = Request.Headers.Contains("X-File-Token") ? Request.Headers.GetValues("X-File-Token").FirstOrDefault() : null;
            if (string.IsNullOrWhiteSpace(token))
                return InternalServerError(new Exception("File Token not provided."));


            try
            {
                var uploadFileService = new UploadFileService();
                UploadProcessingResult uploadResult = await uploadFileService.HandleRequest(Request);
                if (uploadResult.IsComplete)
                {
                    var baseFileName = Guid.NewGuid().ToString("N");
                    var saveFileName = $"{baseFileName}{Path.GetExtension(uploadResult.FileName)}";
                    var saveFilePath = Path.Combine(Path.GetDirectoryName(uploadResult.LocalFilePath), saveFileName);
                    if (File.Exists(saveFilePath))
                        File.Delete(saveFilePath);

                    File.Move(uploadResult.LocalFilePath, saveFilePath);

                    using (var sw = new StreamWriter(Path.ChangeExtension(saveFilePath, ".orig")))
                        sw.WriteLine(uploadResult.FileName);

                    //create thumb for image
                    var isImage = false;
                    var height = 0;
                    var width = 0;
                    long size = 0;
                    if (IsImageExtension(saveFileName))
                    {
                        isImage = true;

                        using (var fileContent = new FileStream(saveFilePath, FileMode.Open))
                        {
                            var imageChecker = new ImageChecker();
                            var checkResult = imageChecker.CheckStream(fileContent, true, out var image);
                            height = imageChecker.Height;
                            width = imageChecker.Width;
                            size = imageChecker.DataSize;
                            if (checkResult != ImageCheckResult.JPEGImage &&
                                checkResult != ImageCheckResult.GIFImage &&
                                checkResult != ImageCheckResult.PNGImage)
                            {
                                throw new Exception(imageChecker.FormatErrorMessage(checkResult));
                            }
                            else
                            {
                                using (var thumbImage = ThumbnailGenerator.Generate(image, 128, 96, ImageScaleMode.CropSourceImage, Color.Empty))
                                {
                                    var thumbFile = Path.Combine(Path.GetDirectoryName(uploadResult.LocalFilePath), baseFileName + "_t.jpg");
                                    thumbImage.Save(thumbFile, System.Drawing.Imaging.ImageFormat.Jpeg);

                                    //height = thumbImage.Width;
                                    //width = thumbImage.Height;
                                }
                            }
                        }
                    }

                    return Ok(new UploadResponse()
                    {
                        TemporaryFile = UrlCombine(UploadFileService.TempFolder, saveFileName),
                        IsImage = isImage,
                        Width = width,
                        Height = height,
                        Size = size
                    });
                }

                return Ok(HttpStatusCode.Continue);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private class UploadResponse : ServiceResponse
        {
            public string TemporaryFile { get; set; }
            public long Size { get; set; }
            public bool IsImage { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }

        private string UrlCombine(string url1, string url2)
        {
            if (url1.Length == 0)
            {
                return url2;
            }

            if (url2.Length == 0)
            {
                return url1;
            }

            url1 = url1.TrimEnd('/', '\\');
            url2 = url2.TrimStart('/', '\\');

            return string.Format("{0}/{1}", url1, url2);
        }
        private bool IsImageExtension(string extension)
        {
            return extension.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) ||
                extension.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase) ||
                extension.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) ||
                extension.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}