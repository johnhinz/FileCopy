using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriggeredFileCopy
{
    internal class History
    {
        [Key]
        public long id { get; set; }
        public string filename { get; set; }
        public string filepath { get; set; }
        public long filesize { get; set; }
        public bool verify { get; set; }
    }
}
