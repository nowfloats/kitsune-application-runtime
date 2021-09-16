using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.AspNetCoreServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Kitsune.Identifier
{
	public class LambdaFunction : APIGatewayHttpApiV2ProxyFunction
	{
		protected override void Init(IWebHostBuilder builder)
		{
			RegisterResponseContentEncoding();
			builder.UseContentRoot(Directory.GetCurrentDirectory())
				.UseSetting("System.GC.Concurrent", "true")
				.UseSetting("System.GC.Server", "true")
				//.UseLibuv(options => options.ThreadCount = 2)
				.ConfigureAppConfiguration((builderContext, config) =>
				{
					config
						.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
						.AddEnvironmentVariables();
				})
				.UseStartup<Startup>()
				.UseLambdaServer();
		}

		private void RegisterResponseContentEncoding()
		{
			var dict = new Dictionary<string, ResponseContentEncoding>
			{
				{"image/jpg", ResponseContentEncoding.Base64 }
			};

			foreach (var item in dict)
				RegisterResponseContentEncodingForContentType(item.Key, item.Value);
		}
	}
}
