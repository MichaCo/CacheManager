﻿using System;
using System.Linq;
using CacheManager.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AspnetCore.WebApp
{
    using Microsoft.Extensions.Configuration;
    using Swashbuckle.AspNetCore.Swagger;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            var builder = new ConfigurationBuilder().AddConfiguration(configuration);
            
            /* Add Default configurations */
            builder.AddConfiguration(configuration);

            /* Add Custom configurations */
            // adding cache.json which contains cachemanager configuration(s)
            builder.AddJsonFile("cache.json"); 
            
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1",
                    new Info
                    {
                        Title = "My API - V1",
                        Version = "v1"
                    }
                 );
            });

            services.AddLogging(c => c.AddConsole().AddDebug().AddConfiguration(Configuration));

            // using the new overload which adds a singleton of the configuration to services and the configure method to add logging
            // TODO: still not 100% happy with the logging part
            services.AddCacheManagerConfiguration(Configuration, cfg => cfg.WithMicrosoftLogging(services));

            // uses a refined configurastion (this will not log, as we added the MS Logger only to the configuration above
            services.AddCacheManager<int>(Configuration, configure: builder => builder.WithJsonSerializer());

            // creates a completely new configuration for this instance (also not logging)
            services.AddCacheManager<DateTime>(inline => inline.WithDictionaryHandle());

            // any other type will be this. Configurastion used will be the one defined by AddCacheManagerConfiguration earlier.
            services.AddCacheManager();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            // add console logging with the configured log levels from appsettings.json
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));

            // give some error details in debug mode
            if (env.IsDevelopment())
            {
                app.Use(async (ctx, next) =>
                {
                    try
                    {
                        await next.Invoke();
                    }
                    catch (Exception ex)
                    {
                        await ctx.Response.WriteAsync($"{{\"error\": \"{ex}\"}}");
                    }
                });
            }

            // lets redirect to the swagger ui, there is nothing else to display otherwise ;)
            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Path.StartsWithSegments("/"))
                {
                    ctx.Response.Redirect("/swagger/ui");
                }
                else
                {
                    await next.Invoke();
                }
            });

            //app.UseStaticFiles();
            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });
        }
    }
}
