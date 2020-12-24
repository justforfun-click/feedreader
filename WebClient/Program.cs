using FeedReader.WebClient.Models;
using FeedReader.WebClient.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FeedReader.WebClient
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");
            builder.Services.AddSingleton<LogService>();
            builder.Services.AddSingleton<LocalStorageService>();
            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<FeedService>();
            builder.Services.AddSingleton<LocalUser>();
            builder.Services.AddAuthorizationCore();

            var host = builder.Build();
            var apiService = host.Services.GetRequiredService<ApiService>();
            var uriBuilder = new UriBuilder(builder.HostEnvironment.BaseAddress);
            if (uriBuilder.Uri.Host == "localhost")
            {
                uriBuilder.Scheme = "http";
                uriBuilder.Port = 7071;
            }
            else if (uriBuilder.Uri.Host == "www.feedreader.org")
            {
                uriBuilder.Host = "api.feedreader.org";
            }
            else if (uriBuilder.Uri.Host == "test.feedreader.org")
            {
                uriBuilder.Host = "test.api.feedreader.org";
            }
            uriBuilder.Path += "api/";
            apiService.HttpClient = new HttpClient() { BaseAddress = uriBuilder.Uri };
            apiService.TimezoneOffset = await host.Services.GetRequiredService<IJSRuntime>().InvokeAsync<int>("eval", "-new Date().getTimezoneOffset()");
            var localUser = host.Services.GetRequiredService<LocalUser>();
            await localUser.InitializeAsync();
            await host.RunAsync();
        }
    }
}
