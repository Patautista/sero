using System;
using System.Collections.Generic;

namespace Business.MobileConfig
{
    public class RssConfig
    {
        public List<RssFeedWidget> Widgets { get; set; } = new();

        public static RssConfig Default => new RssConfig
        {
            Widgets = new List<RssFeedWidget>()
        };
    }

    public class RssFeedWidget
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
        public int MaxItems { get; set; } = 5;
        public int DisplayOrder { get; set; } = 0;
    }
}