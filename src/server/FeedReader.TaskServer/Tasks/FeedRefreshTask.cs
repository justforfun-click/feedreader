using FeedReader.ServerCore.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FeedReader.TaskServer.Tasks
{
    class FeedRefreshTask : TaskBase
    {
        private readonly IFeedService _feedService;

        public FeedRefreshTask(IFeedService feedService, ILogger<FeedRefreshTask> logger)
            : base("FeedRefreshTask", TimeSpan.FromMinutes(10), logger)
        {
            _feedService = feedService;
        }

        protected override Task DoTaskOnce(CancellationToken cancellationToken)
        {
            return _feedService.UpdateFeeds(cancellationToken);
        }
    }
}
