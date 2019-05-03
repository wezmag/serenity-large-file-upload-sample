using System;
using System.Collections.Generic;
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
            if (request.IsChunkUpload())
            {
                return await ProcessChunk(request);
            }

            //將檔案搬到對應的資料夾
            var xtoken = request.Headers.Contains("X-File-Token") ? request.Headers.GetValues("X-File-Token").FirstOrDefault() : null;
            var savedFileName = GetSafeFileName($"{xtoken}_{OriginalFileName}");

            var uploadPath = Path.Combine(_uploadSettings.Path, "temporary");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);
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

        private async Task<UploadProcessingResult> ProcessChunk(HttpRequestMessage request)
        {
            //use the unique identifier sent from client to identify the file
            FileChunkMetaData chunkMetaData = request.GetChunkMetaData();
            var savedFileName = GetSafeFileName($"{chunkMetaData.ChunkToken}_{OriginalFileName}");

            string uploadPath = Path.Combine(_uploadSettings.Path, TempFolder);
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

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

                //delete chunk
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

        public static string TempFileExtension
        {
            get
            {
                return ".orig";
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
