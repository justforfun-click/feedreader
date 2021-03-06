using FeedReader.Protos;
using FeedReader.ServerCore.Datas;
using FeedReader.ServerCore.Models;
using FeedReader.WebApi.Processors;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FeedReader.ServerCore.Services
{
    public interface IUserService
    {
        Task<User> Login(User user);
        Task MarkItemsAsReaded(User user, string feedUri, DateTime datatimeInUtc);
        Task<Share.DataContracts.Feed> GetFeedItemsAsync(User user, string feedUri, int page);
        Task<List<FeedItem>> GetStaredFeedItemsAsync(User user, int page);
        Task StarFeedItemAsync(User user, Protos.FeedItemMessageWithFeedInfo feedItem);
        Task UnstarFeedItemAsync(User user, string feedItemUri, DateTime publishDateInUtc);
        Task<Share.DataContracts.Feed> SubscribeFeedAsync(User user, string feedUri, string group);
        Task UnsubscribeFeedAsync(User user, string feedUri);
        Task UpdateFeedAsync(User user, string feedUri, string newGroup);
    }

    class UserService : IUserService
    {
        IDbContextFactory<FeedReaderDbContext> _dbFactory;

        public UserService(IDbContextFactory<FeedReaderDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public Task<Share.DataContracts.Feed> GetFeedItemsAsync(User user, string feedUri, int page)
        {
            return new FeedProcessor(_dbFactory).GetFeedItemsAsync(feedUri, page, user);
        }

        public Task<List<FeedItem>> GetStaredFeedItemsAsync(User user, int page)
        {
            return new UserProcessor(_dbFactory).GetStaredFeedItemsAsync(page, user);
        }

        public Task<User> Login(User user)
        {
            var processor = new UserProcessor(_dbFactory);
            return processor.LoginAsync(user);
        }

        public Task MarkItemsAsReaded(User user, string feedUri, DateTime datatimeInUtc)
        {
            return new UserProcessor(_dbFactory).MarkItemsAsReaded(user, feedUri, datatimeInUtc);
        }

        public Task StarFeedItemAsync(User user, FeedItemMessageWithFeedInfo feedItem)
        {
            return new UserProcessor(_dbFactory).StarFeedItemAsync(GetDataContractFeedItem(feedItem), user);
        }

        public Task<Share.DataContracts.Feed> SubscribeFeedAsync(User user, string feedUri, string group)
        {
            return new FeedProcessor(_dbFactory).SubscribeFeedAsync(feedUri, group, user);
        }

        public Task UnstarFeedItemAsync(User user, string feedItemUri, DateTime publishDateInUtc)
        {
            return new UserProcessor(_dbFactory).UnstarFeedItemAsync(feedItemUri, publishDateInUtc, user);
        }

        public Task UnsubscribeFeedAsync(User user, string feedUri)
        {
            return new FeedProcessor(_dbFactory).UnsubscribeFeedAsync(feedUri, user);
        }

        public Task UpdateFeedAsync(User user, string feedUri, string newGroup)
        {
            return new FeedProcessor(_dbFactory).UpdateFeedAsync(feedUri, newGroup, user);
        }

        private Share.DataContracts.FeedItem GetDataContractFeedItem(FeedItemMessageWithFeedInfo f)
        {
            var feedItem = GetDataContractFeedItem(f.FeedItem);
            feedItem.FeedIconUri = f.FeedIconUri;
            feedItem.FeedName = f.FeedName;
            feedItem.FeedUri = f.FeedUri;
            return feedItem;
        }

        private Share.DataContracts.FeedItem GetDataContractFeedItem(FeedItemMessage f)
        {
            return new Share.DataContracts.FeedItem
            {
                Content = f.Content,
                IsReaded = f.IsReaded,
                IsStared = f.IsReaded,
                PermentLink = f.PermentLink,
                PubDate = f.PubDate.ToDateTime(),
                Summary = f.Summary,
                Title = f.Title,
                TopicPictureUri = f.TopicPictureUri
            };
        }
    }
}
