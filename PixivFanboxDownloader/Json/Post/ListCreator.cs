using Newtonsoft.Json;

#nullable disable

namespace PixivFanboxDownloader.Json.Post.ListCreator
{
    public class Body
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("feeRequired")]
        public int FeeRequired { get; set; }

        [JsonProperty("publishedDatetime")]
        public DateTime PublishedDatetime { get; set; }

        [JsonProperty("updatedDatetime")]
        public DateTime UpdatedDatetime { get; set; }

        [JsonProperty("tags")]
        public List<object> Tags { get; set; }

        [JsonProperty("isLiked")]
        public bool IsLiked { get; set; }

        [JsonProperty("likeCount")]
        public int LikeCount { get; set; }

        [JsonProperty("commentCount")]
        public int CommentCount { get; set; }

        [JsonProperty("isRestricted")]
        public bool IsRestricted { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }

        [JsonProperty("creatorId")]
        public string CreatorId { get; set; }

        [JsonProperty("hasAdultContent")]
        public bool HasAdultContent { get; set; }

        [JsonProperty("cover")]
        public Cover Cover { get; set; }

        [JsonProperty("excerpt")]
        public string Excerpt { get; set; }

        [JsonProperty("isPinned")]
        public bool IsPinned { get; set; }
    }

    public class Cover
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class ListCreator
    {
        [JsonProperty("body")]
        public List<Body> Body { get; set; }
    }

    public class User
    {
        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("iconUrl")]
        public string IconUrl { get; set; }
    }
}
