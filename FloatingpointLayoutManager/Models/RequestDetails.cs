using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kitsune.Models.Project;

namespace KitsuneLayoutManager.Models
{
    public class RequestDetails
    {
        public string IPAddress { get; set; }
        public string UserAgent { get; set; }
        public string ReferenceQuery { get; set; }
        public string Perflog { get; set; }
        public string Referer { get; set; }
        public bool IsCrawler { get; set; }
    }

    public class KitsuneDomainDetails
    {
        public string DeveloperId { get; set; }
        public string CustomerId { get; set; }
        public string ProjectId { get; set; }
        public string Domain { get; set; }
        public int Version { get; set; }
        public string ClientId { get; set; }
        public string WebsiteTag { get; set; }

        public bool IsRedirect { get; set; }
        public string RedirectUrl { get; set; }
        public bool isSSLEnabled { get; set; }
    }

    public class ProjectDetails
    {
        public string SchemaId { get; set; }
        public BucketNames BucketNames { get; set; }
        public int Version { get; set; }
        public int CompilerVersion { get; set; }
        public List<ProjectComponent> Components { get; set; }
        public bool RuntimeOptimization { get; set; }
        public string DeveloperEmail { get; set; }

        public string ProjectId { get; set; }
    }
}
