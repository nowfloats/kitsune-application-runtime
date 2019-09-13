using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kitsune.Identifier.Models
{
    public class RequestModel
    {
    }

    public class RoutingObjectModel
    {
        public string File { get; set; }
        public string ResourceId { get; set; }
        public string RedirectPath { get; set; }
        public int StatusCode { get; set; }
    }

    //This model is used for only content from S3
    public class FileContent
    {
        public byte[] contentStream;
        public string contentEncoding;
        public string contentType;
    }

    public class KitsunePreviewModel
    {
        public string FileContent { get; set; }
        public string ProjectId { get; set; }
        public string View { get; set; }
        public string ViewType { get; set; }
        public string WebsiteTag { get; set; }
        public string DeveloperId { get; set; }
        public string[] UrlParams { get; set; }
        public string NoCacheQueryParam { get; set; }
    }
}
