using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serenity.ComponentModel;

namespace SereneLargeFileUpload
{
    public partial class LargeFileUploadEditorAttribute
    {
        public String FilenameFormat { get; set; }
        public String SubFolder { get; set; }

        public Boolean CopyToHistory { get; set; }
    }
}
