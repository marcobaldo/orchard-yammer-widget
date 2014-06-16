using System;

namespace Codewisp.Yammer.Models
{
    public class YammerWidgetFeedItem
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public YammerUser Author { get; set; }
        public DateTime Date { get; set; }
    }

    public class YammerUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ProfileUrl { get; set; }
        public string MugshotUrl { get; set; }
    }
}