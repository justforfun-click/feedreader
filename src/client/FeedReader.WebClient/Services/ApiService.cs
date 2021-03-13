using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FeedReader.ClientCore;
using FeedReader.ClientCore.Models;
using FeedReader.Protos;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using static FeedReader.Protos.FeedReaderServerApi;

namespace FeedReader.WebClient.Services
{
    public class ApiService
    {
        private readonly string _serverAddr;

        private FeedReaderServerApiClient _apiClient;
        private FeedReaderClient _client;

        public int TimezoneOffset { get; set; }

        public string GitHubClientId { get; set; }

        public ApiService(string serverAddr, FeedReaderClient client)
        {
            _serverAddr = serverAddr;

            var httpHandler = new HttpClientHandler();
            var grpcHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, httpHandler);
            _apiClient = new FeedReaderServerApiClient(GrpcChannel.ForAddress(_serverAddr, new GrpcChannelOptions { HttpHandler = grpcHandler }));
            _client = client;
        }

        public async Task<User> LoginAsync(string token)
        {
            var httpHandler = new CustomizedHttpClientHandler(token);
            var grpcHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, httpHandler);
            _apiClient = new FeedReaderServerApiClient(GrpcChannel.ForAddress(_serverAddr, new GrpcChannelOptions { HttpHandler = grpcHandler }));
            var user = await _apiClient.LoginAsync(new Protos.LoginRequest());            

            // Switch the token.
            httpHandler = new CustomizedHttpClientHandler(user.Token);
            grpcHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, httpHandler);
            _apiClient = new FeedReaderServerApiClient(GrpcChannel.ForAddress(_serverAddr, new GrpcChannelOptions { HttpHandler = grpcHandler }));
            _client.SetApiClient(_apiClient);
            return new User
            {
                Uuid = user.Uuid,
                Token = user.Token,
                Feeds = user.Feeds.Select(f => GetFeed(f)).ToList()
            };
        }

        public async Task<Feed> SubscribeFeed(Feed feed)
        {
            var feedInfo = await _apiClient.SubscribeFeedAsync(new Protos.SubscribeFeedRequest
            {
                OriginalUri = feed.OriginalUri,
                Group = feed.Group ?? string.Empty,
            });
            return GetFeed(feedInfo.Feed); 
        }

        public async Task UnsubscribeFeed(string feedUri)
        {
            await _apiClient.UnsubscribeFeedAsync(new Protos.UnsubscribeFeedRequest
            {
                FeedUri = feedUri
            });
        }

        public async Task UpdateFeed(Feed feed)
        {
            await _apiClient.UpdateFeedAsync(new Protos.UpdateFeedRequest
            {
                FeedUri = feed.Uri,
                FeedGroup = feed.Group ?? string.Empty
            });
        }

        public async Task<Feed> RefreshFeed(string feedUri, int page)
        {
            var response = await _apiClient.RefreshFeedAsync(new Protos.RefreshFeedRequest
            {
                FeedUri = feedUri,
                Page = page
            });

            var items = response.FeedItems.Select(f => new FeedItem
            {
                Content = f.Content,
                IsReaded = f.IsReaded,
                IsStared = f.IsStared,
                PermentLink = f.PermentLink,
                PubDate = f.PubDate.ToDateTime().AddMinutes(TimezoneOffset),
                Summary = f.Summary,
                Title = f.Title,
                TopicPictureUri = f.TopicPictureUri,
                FeedUri = response.FeedInfo.Uri,
                FeedIconUri = response.FeedInfo.IconUri,
                FeedName = response.FeedInfo.Name,
            }).ToList();

            var feed = new Feed
            {
                Description = response.FeedInfo.Description,
                Group = response.FeedInfo.Group,
                IconUri = response.FeedInfo.IconUri,
                Name = response.FeedInfo.Name,
                OriginalUri = response.FeedInfo.OriginalUri,
                Uri = response.FeedInfo.Uri,
                WebsiteLink = response.FeedInfo.WebsiteLink,
                Items = new FeedItems
                {
                    Items = items,
                    NextPage = items.Count == 50 ? page + 1 : 0
                }
            };
            return feed;
        }

        public async Task MarkAsReaded(string feedUri, DateTime lastReadedTime)
        {
            await _apiClient.MarkFeedAsReadedFromTimestampAsync(new Protos.MarkFeedAsReadedFromTimestampRequest()
            {
                FeedUri = feedUri,
                Timestamp = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(lastReadedTime.AddMinutes(-TimezoneOffset))
            });
        }

        public async Task StarFeedItemAsync(FeedItem feedItem)
        {
            await _apiClient.StarFeedItemAsync(new Protos.StarFeedItemRequest
            {
                FeedItem = GetProtosFeedItemMessageWithFeedInfo(feedItem)
            });
        }

        public async Task UnstarFeedItemAsync(string feedItemUri, DateTime pubDate)
        {
            await _apiClient.UnstarFeedItemAsync(new Protos.UnstarFeedItemRequest
            {
                FeedItemUri = feedItemUri,
                FeedItemPubDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(pubDate.AddMinutes(-TimezoneOffset))
            });
        }

        public async Task<FeedItems> GetStaredFeedItems(int page)
        {
            var res = await _apiClient.GetStaredFeedItemsAsync(new Protos.GetStaredFeedItemsRequest
            {
                Page = page
            });

            var feedItems = res.FeedItems.Select(f => GetFeedItem(f)).ToList();
            return new FeedItems
            {
                Items = feedItems,
                NextPage = feedItems.Count == 50 ? page + 1 : 0
            };
        }

        public async Task<List<FeedItem>> GetFeedItemsByCategory(FeedCategory feedCategory, int page)
        {
            var res = await _apiClient.GetFeedsByCategoryAsync(new Protos.GetFeedsByCategoryRequest
            {
                Category = feedCategory,
                Page = page
            });
            return res.FeedItems.Select(f => GetFeedItem(f)).ToList();
        }

        private Feed GetFeed(Protos.FeedInfo f)
        {
            return new Feed
            {
                Description = f.Description,
                Group = string.IsNullOrEmpty(f.Group) ? "Default" : f.Group,
                IconUri = f.IconUri,
                Name = f.Name,
                OriginalUri = f.OriginalUri,
                Uri = f.Uri,
                WebsiteLink = f.WebsiteLink
            };
        }

        private FeedItem GetFeedItem(Protos.FeedItemMessage f)
        {
            return new FeedItem
            {
                Content = f.Content,
                IsReaded = f.IsReaded,
                IsStared = f.IsStared,
                PermentLink = f.PermentLink,
                PubDate = f.PubDate.ToDateTime().AddMinutes(TimezoneOffset),
                Summary = f.Summary,
                Title = f.Title,
                TopicPictureUri = f.TopicPictureUri
            };
        }

        private FeedItem GetFeedItem(Protos.FeedItemMessageWithFeedInfo f)
        {
            var feedItem = GetFeedItem(f.FeedItem);
            feedItem.FeedUri = f.FeedUri;
            feedItem.FeedIconUri = f.FeedIconUri;
            feedItem.FeedName = f.FeedName;
            return feedItem;
        }

        private Protos.FeedItemMessageWithFeedInfo GetProtosFeedItemMessageWithFeedInfo(FeedItem f)
        {
            return new Protos.FeedItemMessageWithFeedInfo
            {
                FeedItem = GetProtosFeedItemMessage(f),
                FeedIconUri = f.FeedIconUri,
                FeedName = f.FeedName,
                FeedUri = f.FeedUri
            };
        }

        private Protos.FeedItemMessage GetProtosFeedItemMessage(FeedItem f)
        {
            return new Protos.FeedItemMessage
            {
                Content = f.Content,
                IsReaded = f.IsReaded,
                Summary = f.Summary,
                IsStared = f.IsStared,
                PermentLink = f.PermentLink,
                PubDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(f.PubDate.AddMinutes(-TimezoneOffset)),
                Title = f.Title,
                TopicPictureUri = f.TopicPictureUri
            };
        }

        #region Customized http client handler.
        private class CustomizedHttpClientHandler : HttpClientHandler
        {
            private readonly string _token;

            public CustomizedHttpClientHandler(string token)
            {
                _token = token;
            }

            protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Add("authentication", _token);
                return base.Send(request, cancellationToken);
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Add("authentication", _token);
                return base.SendAsync(request, cancellationToken);
            }
        }
        #endregion
    }
}
