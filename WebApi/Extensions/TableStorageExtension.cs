using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.WindowsAzure.Storage;
using System;

namespace FeedReader.WebApi.Extensions
{
    [Binding]
    class TableStorageAttribute : Attribute
    {
    }

    class TableStorageExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            context
                .AddBindingRule<TableStorageAttribute>()
                .BindToInput(
                    attr => CloudStorageAccount
                        .Parse(Environment.GetEnvironmentVariable(Consts.ENV_KEY_AZURE_TABLE_STORAGE))
                        .CreateCloudTableClient()
                );
        }
    }
}
