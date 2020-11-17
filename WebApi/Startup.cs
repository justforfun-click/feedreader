using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using FeedReader.WebApi.Extensions;

[assembly: WebJobsStartup(typeof(FeedReader.WebApi.Startup))]

namespace FeedReader.WebApi
{
    class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddExtension<AuthenticationExtension>();
            builder.AddExtension<HttpRequestContentExtension>();

#if DEBUG
            AzureStorage.GetUserContainer().CreateIfNotExists();
            AzureStorage.GetRelatedUuidIndexTable().CreateIfNotExists();
            AzureStorage.GetUsersFeedsTable().CreateIfNotExists();
#endif
        }
    }
}
