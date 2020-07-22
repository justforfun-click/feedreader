using Newtonsoft.Json;

namespace FeedReader.WebApi.Datas
{
    public class User : Share.DataContracts.User
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public string AvatarUrl { get; set; }
    }
}
