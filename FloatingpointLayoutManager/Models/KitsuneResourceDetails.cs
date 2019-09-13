using Kitsune.Models.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Models
{
    public class ResourceDetails
    {
        public string OptimizedPath { get; set; }
        public string SourcePath { get; set; }
        public string UrlPattern { get; set; }
        public string UrlPatternRegex { get; set; }
        public bool isStatic { get; set; }
        public KitsunePageType PageType { get; set; }
        public bool IsRedirect { get; set; }
        public string RedirectPath { get; set; }

        public int StatusCode { get; set; }
    }

    public class KitsuneConvertMailRequest
    {
        public string ClientId { get; set; }
        public string EmailBody { get; set; }
        public string Subject { get; set; }
        public List<string> To { get; set; }
        public int Type { get { return 4; } }
        public List<string> Attachments { get; set; }
    }

    /// <summary>
    /// Kitsune Customer details/DomainDetails
    /// </summary>
    //public class KitsuneDomainDetails
    //{
    //    public string CustomerId { get; set; }
    //    public string ProjectId { get; set; }
    //    public string ClientId { get; set; }
    //    public string Domain { get; set; }
    //    public string WebsiteTag { get; set; }

    //    public int Version { get; set; }

    //    public bool IsRedirect { get; set; }
    //    public string RedirectUrl { get; set; }
    //    public bool isSSLEnabled { get; set; }
    //}

    public enum KitsuneRequestUrlType
    {
        DEMO,
        PREVIEW,
        PRODUCTION
    }
}