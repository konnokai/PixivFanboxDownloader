using Newtonsoft.Json;

#nullable disable

namespace PixivFanboxDownloader.Json.Post.Info
{
    public class Info
    {
        [JsonProperty("body")]
        public Body Body { get; set; }
    }

    public class Block
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("fileId")]
        public string FileId { get; set; }

        [JsonProperty("imageId")]
        public string ImageId { get; set; }
    }

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

        [JsonProperty("isRestricted")]
        public bool IsRestricted { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }

        [JsonProperty("creatorId")]
        public string CreatorId { get; set; }

        [JsonProperty("hasAdultContent")]
        public bool HasAdultContent { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("coverImageUrl")]
        public string CoverImageUrl { get; set; }

        [JsonProperty("body")]
        public BlockBody BlockBody { get; set; }

        [JsonProperty("imageForShare")]
        public string ImageForShare { get; set; }

        [JsonProperty("isPinned")]
        public bool IsPinned { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("images")]
        public List<Image> Images { get; set; }

        [JsonProperty("files")]
        public List<File> Files { get; set; }
    }

    public class BlockBody
    {
        [JsonProperty("blocks")]
        public List<Block> Blocks { get; set; }
    }

    public class Image
    {
        [JsonProperty("originalUrl")]
        public string OriginalUrl { get; set; }

        [JsonProperty("thumbnailUrl")]
        public string ThumbnailUrl { get; set; }
    }

    public class File
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("extension")]
        public string Extension { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
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
