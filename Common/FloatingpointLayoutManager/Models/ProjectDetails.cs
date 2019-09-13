using Kitsune.Models.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Models
{
    public class ProjectDetails
    {
        public string SchemaId { get; set; }
        public BucketNames BucketNames { get; set; }
        public int Version { get; set; }
        public int CompilerVersion { get; set; }
        public List<ProjectComponent> Components { get; set; }
        public bool RuntimeOptimization { get; set; }
    }
}
