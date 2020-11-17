using Microsoft.Extensions.Logging;
using System;

namespace FeedReader.WebApi.Processors
{
    public class Processor
    {
        private readonly ILogger _logger;

        public Processor(ILogger logger = null)
        {
            _logger = logger;
        }

        protected void LogError(string msg, params object[] args)
        {
            if (_logger != null)
            {
                _logger.LogError(msg, args);
            }
            else
            {
                Console.WriteLine(msg, args);
            }
        }
    }
}
