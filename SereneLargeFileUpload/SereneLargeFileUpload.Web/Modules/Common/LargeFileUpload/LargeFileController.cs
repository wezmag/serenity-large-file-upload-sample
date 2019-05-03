using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using Serenity.Services;

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

            var uploadFileService = new UploadFileService();

            UploadProcessingResult uploadResult = await uploadFileService.HandleRequest(Request);

            if (uploadResult.IsComplete)
            {
                var uniqueId = Guid.NewGuid().ToString("N");
                var saveFileName = $"{uniqueId}{Path.GetExtension(uploadResult.FileName)}";
                var saveFilePath = Path.Combine(Path.GetDirectoryName(uploadResult.LocalFilePath), saveFileName);
                if (File.Exists(saveFilePath))
                    File.Delete(saveFilePath);

                File.Move(uploadResult.LocalFilePath, saveFilePath);

                using (var sw = new StreamWriter(Path.ChangeExtension(saveFilePath, ".orig")))
                    sw.WriteLine(uploadResult.FileName);


                return Ok(new UploadResponse()
                {
                    TemporaryFile = UrlCombine(UploadFileService.TempFolder, saveFileName)
                });
            }

            return Ok(HttpStatusCode.Continue);

        }

        private class UploadResponse : ServiceResponse
        {
            public string TemporaryFile { get; set; }
            public Int64 Size { get; set; }
        }

        public string UrlCombine(string url1, string url2)
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
    }
}