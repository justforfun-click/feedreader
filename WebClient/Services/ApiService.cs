using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using FeedReader.WebClient.Datas;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Newtonsoft.Json;
using static FeedReader.Protos.FeedReaderServerApi;
using FeedCategory = FeedReader.Share.DataContracts.FeedCategory;

namespace FeedReader.WebClient.Services
{
    public class ApiService
    {
        private HttpClient _http;
        public HttpClient HttpClient
        {
            get => _http;
            set => _http = value;
        }

        private readonly string _serverAddr;

        private FeedReaderServerApiClient _apiClient;

        public int TimezoneOffset { get; set; }

        public string GitHubClientId { get; set; }

        public ApiService(string serverAddr)
        {
            _serverAddr = serverAddr;

            var httpHandler = new HttpClientHandler();
            var grpcHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, httpHandler);
            _apiClient = new FeedReaderServerApiClient(GrpcChannel.ForAddress(_serverAddr, new GrpcChannelOptions { HttpHandler = grpcHandler }));
        }

        public async Task<User> LoginAsync(string token)
        {
            _http.DefaultRequestHeaders.Remove("authentication");
            _http.DefaultRequestHeaders.Add("authentication", token);
            var user = await GetAsync<User>("login");
            _http.DefaultRequestHeaders.Remove("authentication");
            _http.DefaultRequestHeaders.Add("authentication", user.Token);

            var httpHandler = new CustomizedHttpClientHandler(user.Token);
            var grpcHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, httpHandler);
            _apiClient = new FeedReaderServerApiClient(GrpcChannel.ForAddress(_serverAddr, new GrpcChannelOptions { HttpHandler = grpcHandler }));
            return user;
        }

        public Task<Feed> SubscribeFeed(Feed feed)
        {
            return PostAsync<Feed>("feed/subscribe", new Share.DataContracts.Feed()
            {
                Name = feed.Name,
                Group = feed.Group,
                Uri = feed.Uri,
                OriginalUri = feed.OriginalUri
            });
        }

        public Task UnsubscribeFeed(string feedUri)
        {
            return GetAsync("feed/unsubscribe", new Dictionary<string, string>{
                { "feed-uri", feedUri }
            });
        }

        public Task UpdateFeed(Feed feed)
        {
            return PostAsync("feed/update", new Share.DataContracts.Feed()
            {
                Uri = feed.Uri,
                Name = feed.Name,
                Group = feed.Group,
            });
        }

        public async Task<Feed> RefreshFeed(string feedUri, string nextRowKey)
        {
            var response = await _apiClient.RefreshFeedAsync(new Protos.RefreshFeedRequest
            {
                FeedUri = feedUri,
                NextRowKey = nextRowKey ?? string.Empty
            });

            var feed = new Feed
            {
                Description = response.FeedInfo.Description,
                Group = response.FeedInfo.Group,
                IconUri = response.FeedInfo.IconUri,
                Name = response.FeedInfo.Name,
                OriginalUri = response.FeedInfo.OriginalUri,
                Uri = response.FeedInfo.Uri,
                WebsiteLink = response.FeedInfo.WebsiteLink,
                Items = response.FeedItems.Select(f => new FeedItem
                {
                    Content = f.Content,
                    IsReaded = f.IsReaded,
                    IsStared = f.IsStarted,
                    PermentLink = f.PermentLink,
                    PubDate = f.PubDate.ToDateTime().AddMinutes(TimezoneOffset),
                    Summary = f.Summary,
                    Title = f.Title,
                    TopicPictureUri = f.TopicPictureUri,
                    FeedUri = response.FeedInfo.Uri,
                    FeedIconUri = response.FeedInfo.IconUri,
                    FeedName = response.FeedInfo.Name,
                }).ToList(),
                NextRowKey = response.NextRowKey
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

        public async Task<List<FeedItem>> GetStaredFeedItems()
        {
            var res = await _apiClient.GetStaredFeedItemsAsync(new Protos.GetStaredFeedItemsRequest
            {
                // TODO
                NextRowKey = string.Empty
            });

            var feedItems = res.FeedItems.Select(f => GetFeedItem(f)).ToList();
            if (feedItems.Count > 0)
            {
                feedItems.Last().NextRowKey = res.NextRowKey;
            }
            return feedItems;
        }

        public async Task<List<FeedItem>> GetFeedItemsByCategory(FeedCategory feedCategory, string nextRowKey)
        {
            var res = await _apiClient.GetFeedsByCategoryAsync(new Protos.GetFeedsByCategoryRequest
            {
                Category = GetProtosFeedCategory(feedCategory),
                NextRowKey = nextRowKey ?? string.Empty
            });

            var feedItems = res.FeedItems.Select(f => GetFeedItem(f)).ToList();
            if (feedItems.Count > 0)
            {
                feedItems.Last().NextRowKey = res.NextRowKey;
            }
            return feedItems;
        }

        private async Task<TResult> PostAsync<TResult>(string uri, object obj)
        {
            var res = await _http.PostAsync(uri, new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8));
            return JsonConvert.DeserializeObject<TResult>(await res.Content.ReadAsStringAsync());
        }

        private async Task PostAsync(string uri, object obj)
        {
             await _http.PostAsync(uri, new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8));
        }

        private async Task<TResult> GetAsync<TResult>(string uri, Dictionary<string, string> args = null)
        {
            return JsonConvert.DeserializeObject<TResult>(await GetAsync(uri, args));
        }

        private async Task<string> GetAsync(string uri, Dictionary<string, string> args = null)
        {
            if (args == null)
            {
                return await _http.GetStringAsync(uri);
            }
            else
            {
                string.Join("&", args.Select(arg => $"{arg.Key}={HttpUtility.UrlEncode(arg.Value)}"));
                return await _http.GetStringAsync($"{uri}?{string.Join("&", args.Select(arg => $"{arg.Key}={HttpUtility.UrlEncode(arg.Value)}"))}");
            }
        }

        private Protos.FeedCategory GetProtosFeedCategory(FeedCategory category)
        {
            switch (category)
            {
                default:
                case FeedCategory.Recommended:
                    return Protos.FeedCategory.Default;

                case FeedCategory.News:
                    return Protos.FeedCategory.News;

                case FeedCategory.Technology:
                    return Protos.FeedCategory.Technology;

                case FeedCategory.Business:
                    return Protos.FeedCategory.Business;

                case FeedCategory.Sports:
                    return Protos.FeedCategory.Sport;

                case FeedCategory.Art:
                    return Protos.FeedCategory.Art;

                case FeedCategory.Kids:
                    return Protos.FeedCategory.Kids;
            }
        }

        private FeedItem GetFeedItem(Protos.FeedItemMessage f)
        {
            return new FeedItem
            {
                Content = f.Content,
                IsReaded = f.IsReaded,
                IsStared = f.IsStarted,
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
                IsStarted = f.IsStared,
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
