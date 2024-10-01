using Newtonsoft.Json;

#nullable disable

namespace PixivFanboxDownloader.Json.Plan.ListSupporting
{
    public class Body
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("fee")]
        public int Fee { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("coverImageUrl")]
        public string CoverImageUrl { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }

        [JsonProperty("creatorId")]
        public string CreatorId { get; set; }

        [JsonProperty("hasAdultContent")]
        public bool HasAdultContent { get; set; }

        [JsonProperty("paymentMethod")]
        public string PaymentMethod { get; set; }
    }

    public class ListSupporting
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
