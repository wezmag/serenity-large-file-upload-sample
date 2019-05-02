using Serenity;
using Serenity.ComponentModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace SereneLargeFileUpload
{
    public partial class LargeFileUploadEditorAttribute : CustomEditorAttribute
    {
        public const string Key = "SereneLargeFileUpload.LargeFileUploadEditor";

        public LargeFileUploadEditorAttribute()
            : base(Key)
        {
        }

        public String UrlPrefix
        {
            get { return GetOption<String>("urlPrefix"); }
            set { SetOption("urlPrefix", value); }
        }
    }
}

