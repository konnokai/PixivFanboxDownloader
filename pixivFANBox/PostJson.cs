using System;
using System.Collections.Generic;

namespace pixivFanBox
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Block
    {
        public string type { get; set; }
        public string text { get; set; }
        public string fileId { get; set; }
        public string imageId { get; set; }
    }

    public class Image
    {
        public string id { get; set; }
        public string extension { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string originalUrl { get; set; }
        public string thumbnailUrl { get; set; }
    }

    public class Video
    {
        public string serviceProvider { get; set; }
        public string videoId { get; set; }
    }

    public class PostFile
    {
        public string id { get; set; }
        public string name { get; set; }
        public string extension { get; set; }
        public int size { get; set; }
        public string url { get; set; }
    }

    public class Body
    {
        public string text { get; set; }
        public List<Image> images { get; set; }
        public List<PostFile> files { get; set; }
        public List<Block> blocks { get; set; }
        public Video video { get; set; }
    }

    public class User
    {
        public string userId { get; set; }
        public string name { get; set; }
        public string iconUrl { get; set; }
    }

    public class Item
    {
        public string id { get; set; }
        public string title { get; set; }
        public string coverImageUrl { get; set; }
        public int feeRequired { get; set; }
        public DateTime publishedDatetime { get; set; }
        public DateTime updatedDatetime { get; set; }
        public string type { get; set; }
        public Body body { get; set; }
        public List<string> tags { get; set; }
        public string excerpt { get; set; }
        public bool isLiked { get; set; }
        public int likeCount { get; set; }
        public int commentCount { get; set; }
        public int? restrictedFor { get; set; }
        public User user { get; set; }
        public string creatorId { get; set; }
        public bool hasAdultContent { get; set; }
    }

    public class PostJson
    {
        public List<Item> items { get; set; }
        public string nextUrl { get; set; }
    }
}
