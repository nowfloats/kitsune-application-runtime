using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kitsune.Identifier
{
	public class LocalEntryPoint
	{
		public static void Main(string[] args)
		{
			BuildWebHost(args).Run();
		}

		public static IWebHost BuildWebHost(string[] args) =>
			WebHost.CreateDefaultBuilder(args)
				.UseKestrel()
				.UseSetting("System.GC.Concurrent", "true")
				.UseSetting("System.GC.Server", "true")
				.ConfigureAppConfiguration((builderContext, config) =>
				{
					config
						.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
						.AddEnvironmentVariables();
				})
				.UseStartup<Startup>()
				.Build();
	}
}
