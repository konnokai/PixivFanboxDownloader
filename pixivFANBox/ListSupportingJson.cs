namespace pixivFanBox
{
    public class ListSupportingUser
    {
        public string userId { get; set; }
        public string name { get; set; }
        public string iconUrl { get; set; }
    }

    public class ListSupportingBody
    {
        public string id { get; set; }
        public string title { get; set; }
        public int fee { get; set; }
        public string description { get; set; }
        public string coverImageUrl { get; set; }
        public ListSupportingUser user { get; set; }
        public string creatorId { get; set; }
        public bool hasAdultContent { get; set; }
        public string paymentMethod { get; set; }
    }

    public class ListSupportingJson
    {
        public List<ListSupportingBody> body { get; set; }
    }


}
