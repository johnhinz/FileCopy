using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriggeredFileCopy
{
    [Table(name:"History")]
    internal class History
    {
        [Key]
        public long id { get; set; }
        [Column("file_name")]
        public string filename { get; set; }
        [Column("file_path")]
        public string filepath { get; set; }
        [Column("file_size")]
        public long filesize { get; set; }
        public bool verify { get; set; }
    }
}
