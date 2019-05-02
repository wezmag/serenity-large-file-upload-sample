using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Serenity;
using Serenity.Web;

namespace SereneLargeFileUpload.Common.LargeFileUpload
{
    public class UploadFileService
    {
        private readonly string _uploadPath;
        private readonly MultipartFormDataStreamProvider _streamProvider;

        public UploadFileService()
        {
            _uploadPath = UserLocalPath;
            if (!Directory.Exists(_uploadPath))
                Directory.CreateDirectory(_uploadPath);

            _streamProvider = new MultipartFormDataStreamProvider(_uploadPath);
        }

        #region Interface

        public async Task<UploadProcessingResult> HandleRequest(HttpRequestMessage request)
        {
            await request.Content.ReadAsMultipartAsync(_streamProvider);
            return await ProcessFile(request);
        }

        #endregion    

        #region Private implementation

        private async Task<UploadProcessingResult> ProcessFile(HttpRequestMessage request)
        {
            if (request.IsChunkUpload())
            {
                return await ProcessChunk(request);
            }

            //將檔案搬到對應的資料夾
            var token = request.Headers.Contains("X-File-Token") ? request.Headers.GetValues("X-File-Token").FirstOrDefault() : null;
            var fileName = request.Headers.Contains("X-File-Name") ? request.Headers.GetValues("X-File-Name").FirstOrDefault() : null;
            var uploadPath = Path.Combine(_uploadPath, token);
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);
            string filePath = Path.Combine(uploadPath, $"{fileName}{TempFileExtension}");

            if (File.Exists(filePath))
                File.Delete(filePath);

            File.Move(LocalFileName, filePath);

            return new UploadProcessingResult()
            {
                IsComplete = true,
                FileName = OriginalFileName,
                LocalFilePath = filePath,
                FileMetadata = _streamProvider.FormData
            };
        }

        private async Task<UploadProcessingResult> ProcessChunk(HttpRequestMessage request)
        {
            //use the unique identifier sent from client to identify the file
            FileChunkMetaData chunkMetaData = request.GetChunkMetaData();
            string uploadPath = Path.Combine(_uploadPath, chunkMetaData.ChunkToken);
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            string filePath = Path.Combine(uploadPath, $"{chunkMetaData.ChuckFileName}{TempFileExtension}");

            //append chunks to construct original file
            using (FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate | FileMode.Append))
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
                LocalFilePath = chunkMetaData.IsLastChunk ? filePath : null,
                FileMetadata = _streamProvider.FormData
            };

        }

        #endregion    

        #region Properties

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

        public static string UserLocalPath
        {
            get
            {
                var settings = Config.Get<UploadSettings>();
                return settings.Path;
            }
        }

        public static string TempFileExtension
        {
            get
            {
                return ".orig";
            }
        }

        #endregion    
    }
}
