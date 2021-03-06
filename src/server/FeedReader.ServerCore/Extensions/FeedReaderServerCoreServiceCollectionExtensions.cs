﻿using FeedReader.ServerCore.Datas;
using FeedReader.ServerCore.Services;
using FeedReader.WebApi;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics.CodeAnalysis;
using SolrNet;
using Solrs = FeedReader.ServerCore.Models.Solrs;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FeedReaderServerCoreServiceCollectionExtensions
    {
        public static IServiceCollection AddFeedReaderServerCoreServices([NotNullAttribute] this IServiceCollection services)
        {
            // Add log service.
            var sc = new ServiceCollection();
            sc.AddLogging();

            // Add db factory.
            var dbConns = Environment.GetEnvironmentVariable(Consts.ENV_KEY_FEEDREADER_DB_CONNECTION_STRING);
            sc.AddDbContextFactory<FeedReaderDbContext>(options => options.UseNpgsql(dbConns));

            // Add local cache.
            sc.AddMemoryCache();

            // Add remote cache.
            var redisConnectionString = Environment.GetEnvironmentVariable(Consts.ENV_KEY_FEEDREADER_REDIS_CONNECTION_STRING);
            sc.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = StackExchange.Redis.ConfigurationOptions.Parse(redisConnectionString);
            });

            // Add solr service.
            var solrFeedItemConnectionString = Environment.GetEnvironmentVariable(Consts.ENV_KEY_FEEDREADER_SOLR_FEEDITEM_CONNECTION_STRING);
            sc.AddSolrNet<Solrs.FeedItem>(solrFeedItemConnectionString);

            // Add feed service.
            sc.AddSingleton<IAuthService, AuthService>();
            sc.AddSingleton<IUserService, UserService>();
            sc.AddSingleton<IFeedService, FeedService>();

            // Expose feed reader server core services.
            var sp = sc.BuildServiceProvider();
            services.AddSingleton<IAuthService>(_ => sp.GetRequiredService<IAuthService>());
            services.AddSingleton<IUserService>(_ => sp.GetRequiredService<IUserService>());
            services.AddSingleton<IFeedService>(_ => sp.GetRequiredService<IFeedService>());
            return services;
        }
    }
}
