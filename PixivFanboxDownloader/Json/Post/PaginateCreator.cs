using Newtonsoft.Json;

#nullable disable

namespace PixivFanboxDownloader.Json.Post
{
    public class PaginateCreator
    {
        [JsonProperty("body")]
        public List<string> Body { get; set; }
    }
}
