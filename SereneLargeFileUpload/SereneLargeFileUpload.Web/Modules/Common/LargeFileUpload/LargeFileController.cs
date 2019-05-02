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
                // do other stuff here after file upload complete
                if (Path.GetExtension(uploadResult.LocalFilePath).Equals(UploadFileService.TempFileExtension, StringComparison.InvariantCultureIgnoreCase))
                {
                    string destFileName = uploadResult.LocalFilePath.Substring(0, uploadResult.LocalFilePath.Length - UploadFileService.TempFileExtension.Length);

                    if (File.Exists(destFileName))
                        File.Delete(destFileName);

                    File.Move(uploadResult.LocalFilePath, destFileName);
                }
                return Ok(new UploadResponse()
                {
                    TemporaryFile = uploadResult.LocalFilePath,

                });
            }

            return Ok(HttpStatusCode.Continue);

        }

        private class UploadResponse : ServiceResponse
        {
            public string TemporaryFile { get; set; }
        }
    }
}