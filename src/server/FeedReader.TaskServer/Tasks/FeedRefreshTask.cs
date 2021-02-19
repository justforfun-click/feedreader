using FeedReader.ServerCore.Datas;
using FeedReader.WebApi.AdminFunctions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FeedReader.TaskServer.Tasks
{
    class FeedRefreshTask : TaskBase
    {
        private IDbContextFactory<FeedReaderDbContext> _dbFactory;

        public FeedRefreshTask(IDbContextFactory<FeedReaderDbContext> dbFactory, ILogger<FeedRefreshTask> logger)
            : base("FeedRefreshTask", TimeSpan.FromMinutes(10), logger)
        {
            _dbFactory = dbFactory;
        }

        protected override async Task DoTaskOnce(CancellationToken cancellationToken)
        {
            await UpdateFeedFunc.UpdateFeeds(_dbFactory, cancellationToken, Logger);
        }
    }
}
