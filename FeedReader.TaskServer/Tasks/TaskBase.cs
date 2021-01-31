using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FeedReader.TaskServer.Tasks
{
    public abstract class TaskBase : IHostedService, IDisposable
    {
        private readonly string _name;
        protected string Name => _name;

        private readonly TimeSpan _minimalInterval;

        private readonly ILogger _logger;
        protected ILogger Logger => _logger;

        private CancellationTokenSource _cancelTokenSource;

        private Task _task;

        protected TaskBase(string taskName, TimeSpan minimalInterval, ILogger logger)
        {
            _name = taskName;
            _minimalInterval = minimalInterval;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancelTokenSource = new CancellationTokenSource();

            _task = DoTaskAsync(_cancelTokenSource.Token);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancelTokenSource?.Cancel();
            return _task != null ? _task : Task.CompletedTask;
        }

        public void Dispose()
        {
        }

        protected async Task DoTaskAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Task {_name} is started.");

            while (!cancellationToken.IsCancellationRequested)
            {
                var startTime = DateTime.Now;
                _logger.LogInformation($"Task {_name} is running at: {startTime}");

                try
                {
                    await DoTaskOnce(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Task {_name} throws exception: {ex}");
                }

                var endTime = DateTime.Now;
                _logger.LogInformation($"Task {_name} is finished at: {endTime}");

                if (!cancellationToken.IsCancellationRequested)
                {
                    var taskRunTime = endTime - startTime;
                    if (taskRunTime < _minimalInterval)
                    {
                        Task.Delay(_minimalInterval - taskRunTime, cancellationToken).Wait(cancellationToken);
                    }
                }
            }

            _logger.LogInformation($"Task {_name} is terminated.");
        }

        protected abstract Task DoTaskOnce(CancellationToken cancellationToken);
    }
}
