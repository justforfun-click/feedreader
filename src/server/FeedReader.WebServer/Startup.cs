using AspNetCore.Proxy;
using FeedReader.Server.Services;
using FeedReader.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace FeedReader.Server
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc(options =>
            {
                options.Interceptors.Add<GrpcExceptionInterceptor>();
            });
            services.AddProxies();
            services.AddSingleton<AuthService>();

            var dbConns = Environment.GetEnvironmentVariable(Consts.ENV_KEY_FEEDREADER_DB_CONNECTION_STRING);
            services.AddDbContextFactory<ServerCore.Datas.FeedReaderDbContext>(options => options.UseNpgsql(dbConns));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseHsts();
            }


            app.Use((context, next) =>
            {
                if (context.Request.Host.Value == "www.feedreader.org")
                {
                    context.Response.Redirect("https://feedreader.org", permanent: true);
                    return Task.CompletedTask;
                }

                return next();
            });

            
            app.UseProxies(proxies =>
            {
                proxies.Map("_img_proxy", proxy => proxy.UseHttp((context, args) =>
                {
                    return context.Request.Query["url"];
                }));
            });

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseBlazorFrameworkFiles();

            app.UseRouting();

            app.UseGrpcWeb();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<ApiService>().EnableGrpcWeb();

                endpoints.MapFallbackToFile("index.html");
            });

            WebApi.MigrationTools.MoveUserTableFromAzureToDb();
        }
    }
}
