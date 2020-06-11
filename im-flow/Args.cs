using System;
using System.Collections.Generic;
using System.Text;

namespace im_flow
{
    public class Args
    {
        public string Filename { get; set; }
        public bool DisableAutoExpandConsole { get; set; }
        public string OutputFilename { get; set; }
        public bool OpenInEditor { get; set; }
        public bool IgnoreErrors { get; set; }
    }
}
