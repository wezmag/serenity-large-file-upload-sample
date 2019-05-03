using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using Serenity;
using Serenity.Web;

namespace SereneLargeFileUpload.Common.LargeFileUpload
{
    public class UploadFileService
    {
        private readonly MultipartFormDataStreamProvider _streamProvider;
        private readonly UploadSettings _uploadSettings;

        public UploadFileService()
        {
            _uploadSettings = Config.Get<UploadSettings>();
            if (_uploadSettings.Path.StartsWith("~"))
                _uploadSettings.Path = HostingEnvironment.MapPath(_uploadSettings.Path);

            if (!Directory.Exists(_uploadSettings.Path))
                Directory.CreateDirectory(_uploadSettings.Path);

            _streamProvider = new MultipartFormDataStreamProvider(_uploadSettings.Path);
        }

        public async Task<UploadProcessingResult> HandleRequest(HttpRequestMessage request)
        {
            await request.Content.ReadAsMultipartAsync(_streamProvider);
            return await ProcessFile(request);
        }

        private async Task<UploadProcessingResult> ProcessFile(HttpRequestMessage request)
        {
            var uploadPath = Path.Combine(_uploadSettings.Path, TempFolder);
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            if (IsDangerousExtension(Path.GetExtension(OriginalFileName)))
                throw new Exception("Unsupported file extension!");

            if (request.IsChunkUpload())
            {
                return await ProcessChunk(request, uploadPath);
            }

            var xtoken = request.Headers.Contains("X-File-Token") ? request.Headers.GetValues("X-File-Token").FirstOrDefault() : null;
            var savedFileName = GetSafeFileName($"{xtoken}_{OriginalFileName}");

            string savedFilePath = Path.Combine(uploadPath, savedFileName);

            if (File.Exists(savedFilePath))
                File.Delete(savedFilePath);

            File.Move(LocalFileName, savedFilePath);

            return new UploadProcessingResult()
            {
                IsComplete = true,
                FileName = OriginalFileName,
                LocalFilePath = savedFilePath,
                FileMetadata = _streamProvider.FormData
            };
        }

        private async Task<UploadProcessingResult> ProcessChunk(HttpRequestMessage request, string uploadPath)
        {
            FileChunkMetaData chunkMetaData = request.GetChunkMetaData();
            var savedFileName = GetSafeFileName($"{chunkMetaData.ChunkToken}_{OriginalFileName}");

            string savedFilePath = Path.Combine(uploadPath, savedFileName);

            //append chunks to construct original file
            using (FileStream fileStream = new FileStream(savedFilePath, FileMode.OpenOrCreate | FileMode.Append))
            {
                var localFileInfo = new FileInfo(LocalFileName);
                var localFileStream = localFileInfo.OpenRead();

                await localFileStream.CopyToAsync(fileStream);
                await fileStream.FlushAsync();

                fileStream.Close();
                localFileStream.Close();

                localFileInfo.Delete();
            }

            return new UploadProcessingResult()
            {
                IsComplete = chunkMetaData.IsLastChunk,
                FileName = OriginalFileName,
                LocalFilePath = chunkMetaData.IsLastChunk ? savedFilePath : null,
                FileMetadata = _streamProvider.FormData
            };

        }

        private bool IsDangerousExtension(string extension)
        {
            return extension.EndsWith(".exe") ||
                extension.EndsWith(".bat") ||
                extension.EndsWith(".cmd") ||
                extension.EndsWith(".dll") ||
                extension.EndsWith(".jar") ||
                extension.EndsWith(".jsp") ||
                extension.EndsWith(".htaccess") ||
                extension.EndsWith(".htpasswd") ||
                extension.EndsWith(".lnk") ||
                extension.EndsWith(".vbs") ||
                extension.EndsWith(".vbe") ||
                extension.EndsWith(".aspx") ||
                extension.EndsWith(".ascx") ||
                extension.EndsWith(".config") ||
                extension.EndsWith(".com") ||
                extension.EndsWith(".asmx") ||
                extension.EndsWith(".asax") ||
                extension.EndsWith(".compiled") ||
                extension.EndsWith(".php");
        }

        private string GetSafeFileName(string fileName) {
            return new string(fileName.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch).ToArray());
        }

        private string LocalFileName
        {
            get
            {
                MultipartFileData fileData = _streamProvider.FileData.FirstOrDefault();
                return fileData.LocalFileName;
            }
        }

        private string OriginalFileName
        {
            get
            {
                MultipartFileData fileData = _streamProvider.FileData.FirstOrDefault();
                return fileData.Headers.ContentDisposition.FileName.Replace("\"", string.Empty);
            }
        }

        public static string TempFolder
        {
            get {
                return "temporary";
            }
        }


    }
}
