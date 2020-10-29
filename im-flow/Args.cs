using System;
using System.Collections.Generic;
using System.Text;

namespace im_flow
{
    public class Args
    {
        public List<string> Filenames { get; set; }
        public bool DisableAutoExpandConsole { get; set; }
        public string OutputFilename { get; set; }
        public bool OpenInEditor { get; set; }
        public bool IgnoreErrors { get; set; }
        public bool SuppressAnnotations { get; set; }
        public bool IncludeHeartbeat { get; set; }
        public bool ParseLogDatesAsLocal { get; set; }
        public List<string> MatchMessages { get; set; }
        public bool ShowHelp { get; set; }
        public bool ShowAllInfoMessages { get; set; }
    }
}
