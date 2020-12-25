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
                apiService.GitHubClientId = "9b946fa144483d9ea46c";
            }
            else if (uriBuilder.Uri.Host == "www.feedreader.org")
            {
                uriBuilder.Host = "api.feedreader.org";
                apiService.GitHubClientId = "e1889b2003a7896f241b";
            }
            else if (uriBuilder.Uri.Host == "test.feedreader.org")
            {
                uriBuilder.Host = "test.api.feedreader.org";
                apiService.GitHubClientId = "ab74187cb66e942be0cd";
            }
            else if (uriBuilder.Uri.Host == "beta.feedreader.org")
            {
                uriBuilder.Host = "beta.api.feedreader.org";
                apiService.GitHubClientId = "16914b94ba5da6047cf4";
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
