using FeedReader.Share.DataContracts;
using FeedReader.WebApi.Entities;
using FeedReader.WebApi.Extensions;
using FeedReader.WebApi.Processors;
using Newtonsoft.Json;
using NSubstitute;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.JsonPatch.Operations;
using FeedReader.Backend.Share;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Blob;
using Azure.Storage.Blobs;
using System.IO;
using System.Text;
using Azure.Core;
using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Blobs.Models;
using Azure;

namespace FeedReader.WebApi.Test
{
    public class CloudTableMock : CloudTable
    {
        public CloudTableMock()
            : base(new Uri("http://127.0.0.1:12345/devstoreaccount/test"))
        {
        }
    }

    public class AzureCloudTableMock : Microsoft.Azure.Cosmos.Table.CloudTable
    {
        public AzureCloudTableMock()
            : base(new Uri("http://127.0.0.1:12345/devstoreaccount/test"))
        {
        }
    }

    public class CloudBlobckBlobMock : CloudBlockBlob
    {
        private string _name;

        public CloudBlobckBlobMock(string name)
            : base(new Uri("http://localhost/blob"))
        {
            _name = name;
        }

        public override string Name => _name;
    }

    public class AzureResponseMock : Azure.Response
    {
        public override int Status => throw new NotImplementedException();

        public override string ReasonPhrase => throw new NotImplementedException();

        public override Stream ContentStream { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override string ClientRequestId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        protected override bool ContainsHeader(string name)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<HttpHeader> EnumerateHeaders()
        {
            throw new NotImplementedException();
        }

        protected override bool TryGetHeader(string name, [NotNullWhen(true)] out string value)
        {
            throw new NotImplementedException();
        }

        protected override bool TryGetHeaderValues(string name, [NotNullWhen(true)] out IEnumerable<string> values)
        {
            throw new NotImplementedException();
        }
    }

    public class AzureResponseMock<T> : Azure.Response<T>
    {
        public override T Value => default(T);

        public override Response GetRawResponse()
        {
            throw new NotImplementedException();
        }
    }

    public class UserProcessorTest
    {
        [Fact]
        public async void LoginWithNewRegisteredUser()
        {
            Environment.SetEnvironmentVariable("JwtSecret", "test");

            var userBlob = Substitute.For<BlobClient>();
            userBlob.DownloadToAsync(Arg.Any<Stream>()).Returns(x =>
            {
                var content = JsonConvert.SerializeObject(new UserEntity()
                {
                    Uuid = "feedreader:uuid:123",
                });
                var stream = x.ArgAt<Stream>(0);
                stream.Write(Encoding.UTF8.GetBytes(content));
                return new AzureResponseMock();
            });

            var usersFeedsTable = Substitute.For<CloudTableMock>();
            string filterString = null;
            usersFeedsTable.ExecuteQuerySegmentedAsync(Arg.Any<TableQuery<UserFeedEntity>>(), Arg.Any<TableContinuationToken>()).Returns(x =>
            {
                var query = x.ArgAt<TableQuery<UserFeedEntity>>(0);
                filterString = query.FilterString;
                return Task.FromResult((TableQuerySegment<UserFeedEntity>)typeof(TableQuerySegment<UserFeedEntity>)
                    .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                    .FirstOrDefault(c => c.GetParameters().Count() == 1)
                    .Invoke(new object[] { new List<UserFeedEntity>() {
                        new UserFeedEntity() { Uri = "http://abcdefg" },
                        new UserFeedEntity() { Uri = "http://12345" }
                    }}));
            });

            var userContainer = Substitute.For<BlobContainerClient>();
            userContainer.GetBlobClient("feedreader:uuid:123").Returns(x => userBlob);

            var userProcessor = new UserProcessor();
            var user = await userProcessor.LoginAsync(new UserEntity() { Uuid = "feedreader:uuid:123" }, userContainer, null, usersFeedsTable);
            Assert.NotNull(user);
            Assert.NotEmpty(user.Token);
            Assert.Equal("PartitionKey eq 'feedreader:uuid:123'", filterString);
            Assert.Equal("feedreader:uuid:123", user.Uuid);
            Assert.Equal(2, user.Feeds.Count);
            Assert.Equal("http://abcdefg", user.Feeds[0].Uri);
            Assert.Equal("http://12345", user.Feeds[1].Uri);
        }

        [Fact]
        public async void GetStaredFeedItems_UserDoesNotHaveStars()
        {
            var userFeedItemStartsTable = Substitute.For<AzureCloudTableMock>();
            userFeedItemStartsTable.ExecuteQuerySegmentedAsync(
                Arg.Any<Microsoft.Azure.Cosmos.Table.TableQuery<FeedReader.Backend.Share.Entities.FeedItemExEntity>>(),
                Arg.Any<Microsoft.Azure.Cosmos.Table.TableContinuationToken>()
            ).Returns(x =>
            {
                var ctor = typeof(Microsoft.Azure.Cosmos.Table.TableQuerySegment<FeedReader.Backend.Share.Entities.FeedItemExEntity>)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(c => c.GetParameters().Count() == 1);

                var mockQuerySegment = (Microsoft.Azure.Cosmos.Table.TableQuerySegment<FeedReader.Backend.Share.Entities.FeedItemExEntity>)ctor.Invoke(new object[] { new List<FeedReader.Backend.Share.Entities.FeedItemExEntity>() });
                return Task.FromResult(mockQuerySegment);
            });

            var userProcessor = new UserProcessor();
            var staredItems = await userProcessor.GetStaredFeedItemsAsync(null, "test-uuid", userFeedItemStartsTable);
            Assert.NotNull(staredItems);
            Assert.Empty(staredItems);
        }

        [Fact]
        public async void GetStaredFeedItems()
        {
            var feedItems = new List<FeedItem> {
                new FeedItem()
                {
                    PermentLink = "12345",
                    Title = "abcde",
                },
                new FeedItem()
                {
                    PermentLink = "67890",
                    Title = "hijkl",
                },
            };

            var userBlob = Substitute.For<BlobClient>();
            UserEntity userUpdatedEntity = null;
            userBlob.DownloadToAsync(Arg.Any<Stream>()).Returns(x =>
            {
                var content = JsonConvert.SerializeObject(new UserEntity()
                {
                    Uuid = "feedreader: uuid:123"
                });
                var stream = x.ArgAt<Stream>(0);
                stream.Write(Encoding.UTF8.GetBytes(content));
                return new AzureResponseMock();
            });
            userBlob.UploadAsync(Arg.Any<Stream>(), overwrite: true).Returns(x =>
            {
                var stream = x.ArgAt<Stream>(0);
                var bytes = new byte[stream.Length];
                stream.Read(bytes);
                var content = Encoding.UTF8.GetString(bytes);
                userUpdatedEntity = JsonConvert.DeserializeObject<UserEntity>(content);
                return new AzureResponseMock<BlobContentInfo>();
            });

            var staredItems = new List<FeedReader.Backend.Share.Entities.FeedItemExEntity>();
            var userFeedItemStartsTable = Substitute.For<AzureCloudTableMock>();
            userFeedItemStartsTable.ExecuteAsync(Arg.Any<Microsoft.Azure.Cosmos.Table.TableOperation>()).Returns(x =>
            {
                var operation = x.ArgAt<Microsoft.Azure.Cosmos.Table.TableOperation>(0);
                Assert.Equal(Microsoft.Azure.Cosmos.Table.TableOperationType.InsertOrReplace, operation.OperationType);
                staredItems.Add((FeedReader.Backend.Share.Entities.FeedItemExEntity)operation.Entity);
                return Task.FromResult(new Microsoft.Azure.Cosmos.Table.TableResult());
            });

            // Star feed items.
            var userProcessor = new UserProcessor();
            await userProcessor.StarFeedItemAsync(feedItems[0], userBlob, userFeedItemStartsTable);
            Assert.NotNull(userUpdatedEntity);
            Assert.Equal(((UserEntity)userUpdatedEntity).StaredHashs, $"[\"{"12345".Md5()}\"]");
            userUpdatedEntity = null;

            await userProcessor.StarFeedItemAsync(feedItems[1], userBlob, userFeedItemStartsTable);
            Assert.NotNull(userUpdatedEntity);
            Assert.Equal(((UserEntity)userUpdatedEntity).StaredHashs, $"[\"{"67890".Md5()}\"]");

            // Get the feed items out.
            userFeedItemStartsTable.ExecuteQuerySegmentedAsync(
                Arg.Any<Microsoft.Azure.Cosmos.Table.TableQuery<FeedReader.Backend.Share.Entities.FeedItemExEntity>>(),
                Arg.Any<Microsoft.Azure.Cosmos.Table.TableContinuationToken>()
            ).Returns(x =>
            {
                var ctor = typeof(Microsoft.Azure.Cosmos.Table.TableQuerySegment<FeedReader.Backend.Share.Entities.FeedItemExEntity>)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(c => c.GetParameters().Count() == 1);

                var mockQuerySegment = (Microsoft.Azure.Cosmos.Table.TableQuerySegment<FeedReader.Backend.Share.Entities.FeedItemExEntity>)ctor.Invoke(new object[] { staredItems });
                return Task.FromResult(mockQuerySegment);
            });

            var starItems = await userProcessor.GetStaredFeedItemsAsync(null, "feedreader: uuid:123", userFeedItemStartsTable);
            Assert.NotNull(starItems);
            Assert.Equal(2, starItems.Count);
            Assert.Equal("12345", starItems[0].PermentLink);
            Assert.Equal("abcde", starItems[0].Title);
            Assert.Equal("67890", starItems[1].PermentLink);
            Assert.Equal("hijkl", starItems[1].Title);
        }

        [Fact]
        public async void UnstarFeedItemAsync()
        {
            var userBlob = Substitute.For<BlobClient>();
            UserEntity userUpdatedEntity = null;
            userBlob.DownloadToAsync(Arg.Any<Stream>()).Returns(x =>
            {
                var content = JsonConvert.SerializeObject(new UserEntity()
                {
                    Uuid = "feedreader:uuid:123",
                    StaredHashs = $"[\"{"12345".Md5()}\"]",
                });
                var stream = x.ArgAt<Stream>(0);
                stream.Write(Encoding.UTF8.GetBytes(content));
                return new AzureResponseMock();
            });;
            userBlob.UploadAsync(Arg.Any<Stream>(), overwrite: true).Returns(x =>
            {
                var stream = x.ArgAt<Stream>(0);
                var bytes = new byte[stream.Length];
                stream.Read(bytes);
                var content = Encoding.UTF8.GetString(bytes);
                userUpdatedEntity = JsonConvert.DeserializeObject<UserEntity>(content);
                return new AzureResponseMock<BlobContentInfo>();
            });

            var userFeedItemStartsTable = Substitute.For<AzureCloudTableMock>();
            FeedReader.Backend.Share.Entities.FeedItemExEntity deletedItem = null;
            userFeedItemStartsTable.ExecuteAsync(Arg.Any<Microsoft.Azure.Cosmos.Table.TableOperation>()).Returns(x =>
            {
                var operation = x.ArgAt<Microsoft.Azure.Cosmos.Table.TableOperation>(0);
                if (operation.OperationType == Microsoft.Azure.Cosmos.Table.TableOperationType.Retrieve)
                {
                    var properties = typeof(Microsoft.Azure.Cosmos.Table.TableOperation).GetProperties(BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic);
                    Assert.Equal("feedreader:uuid:123", (string)properties.First(p => p.Name == "PartitionKey").GetValue(operation));
                    Assert.Contains($"-{"12345".Sha256()}", (string)properties.First(p => p.Name == "RowKey").GetValue(operation));
                    return Task.FromResult(new Microsoft.Azure.Cosmos.Table.TableResult()
                    {
                        Result = new FeedReader.Backend.Share.Entities.FeedItemExEntity() {
                            ETag = DateTime.Now.ToString(),
                            PermentLink = "12345"
                        }
                    });
                }
                else
                {
                    Assert.Equal(Microsoft.Azure.Cosmos.Table.TableOperationType.Delete, operation.OperationType);
                    deletedItem = (FeedReader.Backend.Share.Entities.FeedItemExEntity)operation.Entity;
                    Assert.Equal("12345", deletedItem.PermentLink);
                    return Task.FromResult(new Microsoft.Azure.Cosmos.Table.TableResult());
                }
            });

            var userProcessor = new UserProcessor();
            await userProcessor.UnstarFeedItemAsync("12345", DateTime.Now, userBlob, userFeedItemStartsTable);
            Assert.NotNull(deletedItem);
            Assert.NotNull(userUpdatedEntity);
            Assert.Equal("[]", userUpdatedEntity.StaredHashs);
        }
    }
}
