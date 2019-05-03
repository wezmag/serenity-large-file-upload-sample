using System.Linq;
using System.Net.Http;

namespace SereneLargeFileUpload.Common.LargeFileUpload
{
    public static class HttpRequestMessageExtensions
    {
        public static bool IsChunkUpload(this HttpRequestMessage request)
        {
            return request.Content.Headers.ContentRange != null;
        }

        public static FileChunkMetaData GetChunkMetaData(this HttpRequestMessage request)
        {
            return new FileChunkMetaData()
            {
                ChunkToken = request.Headers.Contains("X-File-Token") ? request.Headers.GetValues("X-File-Token").FirstOrDefault() : null,
                //ChuckFileName = request.Headers.Contains("X-File-Name") ? request.Headers.GetValues("X-File-Name").FirstOrDefault() : null,
                ChunkStart = request.Content.Headers.ContentRange.From,
                ChunkEnd = request.Content.Headers.ContentRange.To,
                TotalLength = request.Content.Headers.ContentRange.Length
            };
        }
    }
}
