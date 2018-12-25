using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Indexer.Models
{
    public class FileItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Hash { get; set; }
    }
}