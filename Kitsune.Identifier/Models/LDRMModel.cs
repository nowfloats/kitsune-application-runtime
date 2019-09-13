using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kitsune.Identifier.Models
{
    internal class LDRM_Rule
    {
        internal string origin_domain_host { get; set; }
        internal string destination_domain { get; set; }
        internal int destination_domain_port { get; set; }
        internal Regex[] rules { get; set; }
    }

    internal class LDRMResponse
    {
        internal bool match;
        internal string destination_domain;
        internal int destination_domain_port;
    }
}