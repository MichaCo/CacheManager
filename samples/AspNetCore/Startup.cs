﻿namespace AspnetCore.WebApp
{
    using System;
    using CacheManager.Core;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;

    public class Startup
    {
        public Startup(IWebHostEnvironment env, IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            HostingEnvironment = env ?? throw new ArgumentNullException(nameof(env));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public IWebHostEnvironment HostingEnvironment { get; }

        public IConfiguration Configuration { get; }

        public ILoggerFactory LoggerFactory { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1",
                    new Microsoft.OpenApi.Models.OpenApiInfo
                    {
                        Title = "My API - V1",
                        Version = "v1"
                    }
                 );
            });

            // uses a refined configuration (this will not log, as we added the MS Logger only to the configuration above
            services.AddCacheManagerConfiguration(Configuration, configure: builder => builder
                .WithJsonSerializer()
                .WithRedisConfiguration("redis", c => c.WithEndpoint("localhost", 6379))
                .WithRedisCacheHandle("redis"));

            // creates a completely new configuration for this instance (also not logging)
            services.AddCacheManager<DateTime>(inline => inline.WithDictionaryHandle());

            // any other type will be this. Configuration used will be the one defined by AddCacheManagerConfiguration earlier.
            services.AddCacheManager();
        }

        public void Configure(IApplicationBuilder app)
        {
            // give some error details in debug mode
            if (HostingEnvironment.IsDevelopment())
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

            // lets redirect to the swagger UI, there is nothing else to display otherwise ;)
            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Path.StartsWithSegments("/"))
                {
                    ctx.Response.Redirect("/swagger/");
                }
                else
                {
                    await next.Invoke();
                }
            });

            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(b => b.MapControllers());

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });
        }
    }
}
