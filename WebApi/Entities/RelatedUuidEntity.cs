using Microsoft.WindowsAzure.Storage.Table;

namespace FeedReader.WebApi.Entities
{
    class RelatedUuidEntity : TableEntity
    {
        public string FeedReaderUuid { get; set; }

        public string ThirdPartyUUid { get; set; }
    }
}
