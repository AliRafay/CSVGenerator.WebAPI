using System;
using System.Collections.Generic;
using System.Text;

namespace CSVGenerator.Services.DataTransferObjects
{
    public class RequestDto
    {
        public List<string> Prefixes { get; set; }

        public List<string> IgnoreAssemblyKeywords { get; set; } = new List<string>();
    }
}
