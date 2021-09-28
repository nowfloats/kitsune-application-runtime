using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KLM.Web
{
    public class Program
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
                    IWebHostEnvironment env = builderContext.HostingEnvironment;

                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    //.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                })
               .UseUrls("http://*:80")
               .UseStartup<Startup>()
               .Build();

        //public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        //    WebHost.CreateDefaultBuilder(args)
        //        .UseKestrel()
        //        .UseSetting("System.GC.Concurrent", "true")
        //        .UseSetting("System.GC.Server", "true")
        //        //.UseLibuv(options => options.ThreadCount = 30)
        //        .UseUrls("http://*:80")
        //        .ConfigureAppConfiguration((builderContext, config) =>
        //        {
        //            IHostingEnvironment env = builderContext.HostingEnvironment;

        //            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        //            //.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
        //        })
        //        .UseStartup<Startup>()
        //        .Build();
    }
}
