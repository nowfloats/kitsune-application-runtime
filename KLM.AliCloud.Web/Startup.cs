using Kitsune.Identifier.Constants;
using Kitsune.Identifier.Models;
using KitsuneLayoutManager.Constant;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KLM.Web
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
            services.AddOptions();
            services.AddCors();
            //services.AddResponseCompression();
            //services.AddResponseCaching();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseCors(builder => {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });

            app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto, ForwardLimit = 10 });

            app.UseStaticFiles();
            //app.UseResponseCompression();
            //app.UseResponseCaching();

            app.UseMvc();
        }
    }
}
