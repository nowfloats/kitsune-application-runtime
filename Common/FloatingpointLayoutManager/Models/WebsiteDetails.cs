using Kitsune.Models.WebsiteModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Models
{
	public class WebsiteDetails
	{
		public string WebsiteId { get; set; }
		public string DeveloperId { get; set; }
		public string ProjectId { get; set; }
		public string WebsiteUrl { get; set; }
		public string RootPath { get; set; }
		public string WebsiteTag { get; set; }
		public DateTime CreatedOn { get; set; }
		public DateTime PublishedOn { get; set; }
		public DateTime UpdatedOn { get; set; }
		public bool IsActive { get; set; }
		public WebsiteUserDetails WebsiteOwner { get; set; }
	}

	public class WebsiteUserDetails
	{
		public string UserId { get; set; }
		public string UserName { get; set; }
		public string AccessType { get; set; }
		public ContactDetails Contact { get; set; }
		public bool IsActive { get; set; }
		public DateTime LastLoginTimeStamp { get; set; }
		public DateTime UpdatedOn { get; set; }
		public DateTime CreatedOn { get; set; }
	}
}
