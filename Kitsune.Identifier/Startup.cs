using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Kitsune.Identifier.Constants;
using Kitsune.Identifier.Helpers;
using Kitsune.Identifier.Models;
using KitsuneLayoutManager.Constant;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Kitsune.Identifier
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
			IdentifierEnvironmentConstants.IdentifierConfigurations = configuration.Get<IdentifierConfigurations>();
			KLMEnvironmentalConstants.KLMConfigurations = IdentifierEnvironmentConstants.IdentifierConfigurations.KLMConfigurations;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddLogging();
			services.AddOptions();
			services.AddCors();

			//services.Configure<GzipCompressionProviderOptions>(options =>
			//{
			//    options.Level = CompressionLevel.Optimal;
			//});

			//services.AddResponseCompression(options =>
			//{
			//    options.Providers.Add<GzipCompressionProvider>();

			//    //options.EnableForHttps = true;
			//    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] {
			//        // Default
			//        "text/plain",
			//        "text/css",
			//        "application/javascript",
			//        "text/html",
			//        "application/xml",
			//        "text/xml",
			//        "application/json",
			//        "text/json",
			//        // Custom
			//        "application/xhtml+xml",
			//        "application/atom+xml",
			//        "image/svg+xml",
			//    });
			//});

			//services.AddResponseCaching();

			services.AddSingleton<MiddlewareHelper>();

			if (IdentifierEnvironmentConstants.IdentifierConfigurations?.EnableMVC == true)
				services.AddMvc();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseBrowserLink();
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
			}

			app.UseCors(builder =>
			{
				builder.AllowAnyOrigin()
					   .AllowAnyMethod()
					   .AllowAnyHeader();
			});

			app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto, ForwardLimit = 10 });

			//app.UseResponseCompression();

			app.UseStaticFiles();
			//app.UseResponseCaching();

			app.Use(async (context, next) =>
			{
				context.Response.Headers.TryAdd("server", "kitsune 2.0");
				context.Response.Headers.TryAdd("x-powered-by", "kitsune");

				if (context.Request.Path.Value.EndsWith("Home/Ping", StringComparison.InvariantCultureIgnoreCase))
					return;

				if (context.Request.Path.Value.EndsWith("kitsune-settings.json",
					StringComparison.InvariantCultureIgnoreCase))
				{
					context.Response.StatusCode = 404;
					return;
				}

				context.Response.Headers.Remove("Content-Encoding");

				await next.Invoke();
			});

			app.UseMiddleware<MiddlewareHelper>();

			if (IdentifierEnvironmentConstants.IdentifierConfigurations?.EnableMVC == true)
			{
				app.UseMvc(routes =>
				{
					routes.MapRoute(
						name: "default",
						template: "{controller=Home}/{action=Index}/{id?}");
				});
			}
		}
	}
}
