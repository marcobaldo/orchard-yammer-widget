using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Orchard.Caching;
using Orchard.ContentManagement;
using Orchard.Logging;
using Orchard.Services;
using Orchard.Widgets.Services;
using Codewisp.Yammer.Helpers;
using Codewisp.Yammer.Models;

namespace Codewisp.Yammer.Services.Concrete
{
    public class YammerWidgetService : IYammerWidgetService
    {
        private const string CacheKeyPrefix = "59D5E72F-60C9-429A-A99E-CCFF5A0F8626_{0}";

        private readonly IContentManager _contentManager;
        private readonly ICacheManager _cacheManager;
        private readonly IClock _clock;
        private readonly ISignals _signals;
        private readonly ILogger _logger;
        private readonly IWidgetsService _widgetService;

        public YammerWidgetService(IContentManager contentManager, ICacheManager cacheManager, IClock clock, ISignals signals, IWidgetsService widgetService)
        {
            _contentManager = contentManager;
            _cacheManager = cacheManager;
            _clock = clock;
            _signals = signals;
            _logger = NullLogger.Instance;
            _widgetService = widgetService;
        }

        public SelectList GetNetworkList(YammerWidgetPart part)
        {
            var networkList = new List<YammerNetworkItem>();

            using (var client = new WebClient())
            {
                //Replaced with new yammer format for this endpoint
                var endpoint = String.Format("https://www.yammer.com/api/v1/oauth/tokens.json"); //"?access_token={0}",part.AuthAccessToken); 
                client.Headers[HttpRequestHeader.Authorization] = "Bearer " + part.AuthAccessToken;

                var jsonStringResult = client.DownloadString(endpoint);
                var jsonResult = JArray.Parse(jsonStringResult);

                foreach (var item in jsonResult)
                {
                    var network = new YammerNetworkItem
                    {
                        Permalink = item["network_permalink"].ToString(),
                        Name = item["network_name"].ToString(),
                        Token = item["token"].ToString()
                    };

                    networkList.Add(network);
                }
            }

            var networkSelectList = new SelectList(networkList, "Token", "Name");
            return networkSelectList;
        }

        public IEnumerable<YammerWidgetFeedItem> GetYammerFeed(YammerWidgetPart part)
        {
            var cacheKey = String.Format(CacheKeyPrefix, part.ContentItem.Id);

            // 15 minute caching
            return _cacheManager.Get(cacheKey, context =>
            {
                context.Monitor(_clock.When(TimeSpan.FromMinutes(part.CacheMinutes)));
                context.Monitor(_signals.When("Codewisp.YammerWidget.Settings.Updated"));
                return RetrieveYammerFeedFor(part);
            });
        }

        private static List<YammerWidgetFeedItem> RetrieveYammerFeedFor(YammerWidgetPart part)
        {
            if (String.IsNullOrEmpty(part.SelectedNetworkAccessToken) || String.IsNullOrEmpty(part.AuthAccessToken))
            {
                return new List<YammerWidgetFeedItem>();
            }

            var endpoint = String.Format("https://www.yammer.com/api/v1/messages.json?threaded=true&limit={0}&access_token={1}", part.ItemsToDisplay, part.SelectedNetworkAccessToken);
            var feed = new List<YammerWidgetFeedItem>();

            // TODO : Add null reference checks
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                client.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + part.SelectedNetworkAccessToken);
                var jsonResultString = client.DownloadString(endpoint);
                var jsonResult = JObject.Parse(jsonResultString);
                var references = jsonResult["references"];
                var messages = jsonResult["messages"];

                // Keep the users in the references object so we can refer to them later
                // This is for widget display purposes
                IDictionary<int, YammerUser> users = new Dictionary<int, YammerUser>();
                foreach (var item in references)
                {
                    var type = item["type"].ToString();
                    if (type == "user")
                    {
                        var user = new YammerUser
                            {
                                Id = int.Parse(item["id"].ToString()),
                                Name = item["full_name"].ToString(),
                                ProfileUrl = item["web_url"].ToString(),
                                MugshotUrl = item["mugshot_url"].ToString()
                            };


                        users.Add(user.Id, user);
                    }
                }

                foreach (var item in messages)
                {
                    var isSystemMessage = (bool) item["system_message"];

                    // Filter messages from the system, i.e.
                    //  (Position) has #joined the [organization] network. Take a moment to welcome [name]. 
                    // ...and others. We want real posts from real humans.
                    if (!isSystemMessage)
                    {
                        var content = item["body"]["rich"].ToString();
                        content = HttpUtility.HtmlDecode(content);
                        content = content.Replace("<br>", " ");
                        content = content.TruncateHtml(140, "&#160;&#8230;");

                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(content);

                        var links = doc.DocumentNode.SelectNodes("//a[@href]");
                        if (links != null)
                        {
                            foreach (HtmlNode link in links)
                            {
                                link.SetAttributeValue("target", "_blank");
                            }
                            using (StringWriter writer = new StringWriter())
                            {
                                doc.Save(writer);
                                content = writer.ToString();
                            }
                        }
                       
                        var post = new YammerWidgetFeedItem
                        {
                            Url = item["web_url"].ToString(),
                            Title = content,
                            Date = DateTime.Parse(item["created_at"].ToString()),
                            Author = users[int.Parse(item["sender_id"].ToString())]
                        };

                        feed.Add(post);
                    }
                }
            }

            return feed.OrderByDescending(y => y.Date).ToList();
        }

        public IEnumerable<YammerWidgetPart> GetUnauthorizedWidgets() {
            var widgets = _contentManager.Query<YammerWidgetPart, YammerWidgetPartRecord>("YammerWidget");
            return widgets.List().Where(w => String.IsNullOrEmpty(w.AuthAccessToken));
        }
    }
}