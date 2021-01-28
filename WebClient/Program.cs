using FeedReader.WebClient.Models;
using FeedReader.WebClient.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System;
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
            builder.Services.AddSingleton<ApiService>(new ApiService(builder.HostEnvironment.BaseAddress));
            builder.Services.AddSingleton<FeedService>();
            builder.Services.AddSingleton<LocalUser>();
            builder.Services.AddAuthorizationCore();

            var host = builder.Build();
            var apiService = host.Services.GetRequiredService<ApiService>();
            var uriBuilder = new UriBuilder(builder.HostEnvironment.BaseAddress);
            if (uriBuilder.Uri.Host == "localhost")
            {
                apiService.GitHubClientId = "9b946fa144483d9ea46c";
            }
            else if (uriBuilder.Uri.Host == "devtest.feedreader.org")
            {
                apiService.GitHubClientId = "ab74187cb66e942be0cd";
            }
            else if (uriBuilder.Uri.Host == "feedreader.org")
            {
                apiService.GitHubClientId = "e1889b2003a7896f241b";
            }
            apiService.TimezoneOffset = await host.Services.GetRequiredService<IJSRuntime>().InvokeAsync<int>("eval", "-new Date().getTimezoneOffset()");
            var localUser = host.Services.GetRequiredService<LocalUser>();
            await localUser.InitializeAsync();
            await host.RunAsync();
        }
    }
}
