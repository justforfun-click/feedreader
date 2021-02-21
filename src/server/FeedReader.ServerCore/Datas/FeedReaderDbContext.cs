using FeedReader.ServerCore.Models;
using FeedReader.WebApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace FeedReader.ServerCore.Datas
{
    public class FeedReaderDbContext : DbContext
    {
        public FeedReaderDbContext(DbContextOptions<FeedReaderDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserFavorite>()
                .HasKey(item => new { item.UserId, item.FavoriteItemIdHash });

            modelBuilder.Entity<UserFeed>()
                .HasKey(item => new { item.UserId, item.FeedId });

            modelBuilder.Entity<FeedItem>()
                .Property(f => f.PublishTimeInUtc)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserFavorite> UserFavorites { get; set; }
        public DbSet<Feed> Feeds { get; set; }
        public DbSet<UserFeed> UserFeeds { get; set; }
        public DbSet<FeedItem> FeedItems { get; set; }
    }

    public class DesignTimeFeedReaderDbContextFactory : IDesignTimeDbContextFactory<FeedReaderDbContext>
    {
        public FeedReaderDbContext CreateDbContext(string[] args)
        {
            string connectionString = Environment.GetEnvironmentVariable(Consts.ENV_KEY_FEEDREADER_DB_CONNECTION_STRING);
            if (args != null)
            {
                for (int i = 0; i < args.Length; ++i)
                {
                    if (args[i] == "--conns")
                    {
                        connectionString = args[++i];
                    }
                }
            }

            var optionsBuilder = new DbContextOptionsBuilder<FeedReaderDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            return new FeedReaderDbContext(optionsBuilder.Options);
        }
    }
}
