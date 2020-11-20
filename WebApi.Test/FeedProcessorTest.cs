using FeedReader.WebApi.Entities;
using FeedReader.WebApi.Processors;
using Microsoft.Azure.Cosmos.Table;
using NSubstitute;
using RichardSzalay.MockHttp;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace WebApi.Test
{
    public class FeedProcessorTest
    {
        [Fact]
        public async void SubscribeNewFeed()
        {
            var feedTable = Substitute.For<CloudTableMock>();
            FeedInfoEntity subscribedFeedEntity = null;
            feedTable.ExecuteAsync(Arg.Any<TableOperation>()).Returns(x =>
            {
                var operation = x.ArgAt<TableOperation>(0);
                if (operation.OperationType == TableOperationType.Retrieve)
                {
                    return Task.FromResult(new TableResult());
                }
                else if (operation.OperationType == TableOperationType.Insert)
                {
                    subscribedFeedEntity = (FeedInfoEntity)operation.Entity;
                    return Task.FromResult(new TableResult());
                }
                else
                {
                    throw new Exception("Unexpected feed table operation.");
                }
            });

            var usersFeedsTable = Substitute.For<CloudTableMock>();
            UserFeedEntity insertedFeed = null;
            usersFeedsTable.ExecuteAsync(Arg.Any<TableOperation>()).Returns(x =>
            {
                var operation = x.ArgAt<TableOperation>(0);
                if (operation.OperationType == TableOperationType.InsertOrReplace)
                {
                    insertedFeed = (UserFeedEntity)operation.Entity;
                    return Task.FromResult(new TableResult());
                }
                else
                {
                    throw new Exception("Unexpected feed table operation.");
                }
            });

            var httpHandler = new MockHttpMessageHandler();
            httpHandler.When("*").Respond("text/xml", LoadTestData("NetflixTechblog.2020.07.28.xml"));
            await new FeedProcessor(new HttpClient(httpHandler)).SubscribeFeedAsync("http://12345Abcde", "name-test", "group-test", "feedreader:uuid:1234", usersFeedsTable, feedTable);
            Assert.NotNull(subscribedFeedEntity);
            Assert.Equal("http://12345abcde", subscribedFeedEntity.Uri);
            Assert.Equal("http://12345Abcde", subscribedFeedEntity.OriginalUri);
            Assert.Equal("Netflix TechBlog - Medium", subscribedFeedEntity.Name);

            Assert.NotNull(insertedFeed);
            Assert.Equal("http://12345abcde", insertedFeed.Uri);
            Assert.Equal("http://12345Abcde", insertedFeed.OriginalUri);
            Assert.Equal("name-test", insertedFeed.Name);
            Assert.Equal("group-test", insertedFeed.Group);
        }

        [Fact]
        public async void SubscribeOtherSubscribedFeed()
        {
            var feedTable = Substitute.For<CloudTableMock>();
            feedTable.ExecuteAsync(Arg.Any<TableOperation>()).Returns(x =>
            {
                var operation = x.ArgAt<TableOperation>(0);
                if (operation.OperationType == TableOperationType.Retrieve)
                {
                    return Task.FromResult(new TableResult()
                    {
                        Result = new FeedInfoEntity()
                        {
                            Uri = "http://12345",
                            Name = "save-item",
                        }
                    });
                }
                else
                {
                    throw new Exception("Unexpected feed table operation.");
                }
            });

            var usersFeedsTable = Substitute.For<CloudTableMock>();
            UserFeedEntity insertedFeed = null;
            usersFeedsTable.ExecuteAsync(Arg.Any<TableOperation>()).Returns(x =>
            {
                var operation = x.ArgAt<TableOperation>(0);
                if (operation.OperationType == TableOperationType.InsertOrReplace)
                {
                    insertedFeed = (UserFeedEntity)operation.Entity;
                    return Task.FromResult(new TableResult());
                }
                else
                {
                    throw new Exception("Unexpected feed table operation.");
                }
            });

            var httpHandler = new MockHttpMessageHandler();
            httpHandler.When("*").Respond(HttpStatusCode.BadRequest);
            await new FeedProcessor(new HttpClient(httpHandler)).SubscribeFeedAsync("http://12345", null, "group-test", "feedreader:uuid:1234", usersFeedsTable, feedTable);
            Assert.NotNull(insertedFeed);
            Assert.Equal("http://12345", insertedFeed.Uri);
            Assert.Equal("save-item", insertedFeed.Name);
            Assert.Equal("group-test", insertedFeed.Group);
        }

        private string LoadTestData(string testFilename)
        {
            return File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", testFilename), Encoding.UTF8);
        }
    }
}
