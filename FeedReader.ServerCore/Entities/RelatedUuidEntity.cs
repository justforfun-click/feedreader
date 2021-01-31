using Microsoft.Azure.Cosmos.Table;

namespace FeedReader.WebApi.Entities
{
    class RelatedUuidEntity : TableEntity
    {
        public string FeedReaderUuid { get; set; }

        public string ThirdPartyUUid { get; set; }
    }
}
