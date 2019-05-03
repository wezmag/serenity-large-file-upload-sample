namespace SereneLargeFileUpload.Common.LargeFileUpload
{
    public class FileChunkMetaData
    {
        public string ChunkToken { get; set; }

        public long? ChunkStart { get; set; }

        public long? ChunkEnd { get; set; }

        public long? TotalLength { get; set; }

        public bool IsLastChunk
        {
            get { return ChunkEnd + 1 >= TotalLength; }
        }
    }
}
