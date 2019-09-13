using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack;

namespace KitsuneLayoutManager.Models
{
    [MessagePackObject]
    public class WebCacheFileModel
    {
        [Key(0)]
        public string ContentType = "text/html";

        [Key(1)]
        public string ContentEncoding;

        [Key(2)]
        public Dictionary<string, string> ContentHeaders;

        [Key(3)]
        public bool IsStaticFile = true;

        [Key(4)]
        public byte[] ContentBody;
    }
}
