using System;

namespace SereneLargeFileUpload
{
    public partial class LargeFileUploadEditorAttribute
    {
        public String FilenameFormat { get; set; }
        public String SubFolder { get; set; }
        public Boolean CopyToHistory { get; set; }
    }
}
