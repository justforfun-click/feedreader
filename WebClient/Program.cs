using FeedReader.WebClient.Models;
using FeedReader.WebClient.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Newtonsoft.Json;
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
            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<FeedService>();
            builder.Services.AddSingleton<LocalUser>();
            builder.Services.AddAuthorizationCore();

            var host = builder.Build();
            DateTimeExtension.sTimezoneOffset = await host.Services.GetRequiredService<IJSRuntime>().InvokeAsync<int>("eval", "-new Date().getTimezoneOffset()");
            var localUser = host.Services.GetRequiredService<LocalUser>();
            await localUser.InitializeAsync();
            await host.RunAsync();
        }
    }

    static class DateTimeExtension
    {
        public static int sTimezoneOffset;

        public static DateTime ToClientLocalTime(this DateTime datetime)
        {
            return datetime.AddMinutes(sTimezoneOffset);
        }
    }
}
