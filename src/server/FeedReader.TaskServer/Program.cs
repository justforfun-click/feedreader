using FeedReader.TaskServer.Tasks;
using FeedReader.WebApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace FeedReader.TaskServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var dbConns = Environment.GetEnvironmentVariable(Consts.ENV_KEY_FEEDREADER_DB_CONNECTION_STRING);
                    services.AddDbContextFactory<ServerCore.Datas.FeedReaderDbContext>(options => options.UseNpgsql(dbConns));
                    services.AddHostedService<FeedRefreshTask>();
                });
    }
}
