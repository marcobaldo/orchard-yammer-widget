using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using Orchard.Caching;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Localization;
using Orchard.UI.Notify;
using Orchard.Widgets.Models;
using Codewisp.Yammer.Models;
using Codewisp.Yammer.Services;

namespace Codewisp.Yammer.Drivers
{
    public class YammerWidgetDriver : ContentPartDriver<YammerWidgetPart>
    {
        private readonly IContentManager _contentManager;
        private readonly INotifier _notifier;
        private readonly ISignals _signals;
        private readonly IYammerWidgetService _service;
        public Localizer T { get; set; }

        public YammerWidgetDriver(IContentManager contentManager, INotifier notifier, ISignals signals, IYammerWidgetService service)
        {
            _contentManager = contentManager;
            _notifier = notifier;
            _signals = signals;
            _service = service;
            T = NullLocalizer.Instance;
        }

        protected override DriverResult Display(YammerWidgetPart part, string displayType, dynamic shapeHelper)
        {
            IEnumerable<YammerWidgetFeedItem> feed = null;
            try
            {
                feed = _service.GetYammerFeed(part);
                part.NetworkList = _service.GetNetworkList(part);
            }
            catch (Exception e)
            {
                // TODO : Log error
                feed = new List<YammerWidgetFeedItem>();
            }

            return ContentShape("Parts_YammerWidget", () =>
                shapeHelper.Parts_YammerWidget(
                    Feed: feed
                ));
        }

        // GET
        protected override DriverResult Editor(YammerWidgetPart part, dynamic shapeHelper)
        {
            var httpContext = HttpContext.Current;
            var request = httpContext.Request;
            var code = request.QueryString["code"];
            var error = request.QueryString["error"];
            var clear = request.QueryString["clearCredentials"];

            // Generate RedirectUri for sign-in button
            var redirectUrl = string.Empty;
            redirectUrl += request.Url.Scheme + "://";
            redirectUrl += request.Url.Host;
            redirectUrl += "/Admin/Widgets/EditWidget/{0}?returnUrl=%2FAdmin%2FWidgets%3FlayerId%3D{1}";
            part.RedirectUri = String.Format(redirectUrl, part.ContentItem.Id, part.ContentItem.As<WidgetPart>().LayerId);

            // User requested to clear authentication details
            if (clear != null)
            {
                ClearYammerAuthDetails(part);

                _notifier.Error(@T("You need to authenticate with Yammer to enable this widget."));
                return GetContentShape(part, shapeHelper);
            }

            // No app client id / secret configured yet, return the view
            if (String.IsNullOrEmpty(part.ClientId) || String.IsNullOrEmpty(part.ClientSecret))
            {
                _notifier.Error(@T("You need to authenticate with Yammer to enable this widget."));
                return GetContentShape(part, shapeHelper);
            }

            // OAuth2 errored, do something!
            if (error != null)
            {
                var errorReason = request.QueryString["error_reason"];
                var errorDescription = request.QueryString["error_description"];
                _notifier.Error(@T("OAuth2 workflow errored. " + errorReason + ": " + errorDescription));

                return GetContentShape(part, shapeHelper);
            }

            // OAuth2 workflow resumes from user login! :)
            if (code != null)
            {
                // Exchange code for an access_token
                var oauthEndpoint =
                    String.Format(
                        "https://www.yammer.com/oauth2/access_token.json?client_id={0}&client_secret={1}&code={2}",
                        part.ClientId, part.ClientSecret, code);

                using (var webClient = new WebClient())
                {
                    try
                    {
                        var jsonStringResult = webClient.DownloadString(oauthEndpoint);
                        var jsonResult = JObject.Parse(jsonStringResult);

                        // Check for error
                        var tokenRequestError = jsonResult["error"];
                        if (tokenRequestError != null)
                        {
                            var tokenRequestErrorType = tokenRequestError["type"].ToString();
                            var tokenRequestErrorMessage = tokenRequestError["message"].ToString();
                            _notifier.Error(@T(tokenRequestErrorType + ": " + tokenRequestErrorMessage));

                            return GetContentShape(part, shapeHelper);
                        }

                        // Parse out the token details
                        var accessToken = jsonResult["access_token"];
                        var token = accessToken["token"].ToString();

                        part.AuthAccessToken = token;

                        //var userId = int.Parse(accessToken["user_id"].ToString());
                        //var networkId = int.Parse(accessToken["network_id"].ToString());

                        try
                        {
                            // Get the list of networks
                            part.NetworkList = _service.GetNetworkList(part);

                            // Get the access token for the 'default network'
                            if (String.IsNullOrEmpty(part.SelectedNetworkAccessToken))
                            {
                                var item = part.NetworkList.FirstOrDefault();
                                if (item != null) part.SelectedNetworkAccessToken = item.Value;
                            }
                        }
                        catch (Exception e)
                        {
                            // TODO : Log error
                            part.NetworkList = new SelectList(new List<YammerNetworkItem>());
                        }
                    }
                    catch (Exception e)
                    {
                        // TODO : Log OAuth2 workflow error
                    }

                    _contentManager.Publish(part.ContentItem);
                }

                return GetContentShape(part, shapeHelper);
            }

            if (String.IsNullOrEmpty(part.AuthAccessToken) && request.HttpMethod != "POST")
            {
                _notifier.Error(@T("You need to authenticate with Yammer to enable this widget."));
                return GetContentShape(part, shapeHelper);
            }

            part.NetworkList = _service.GetNetworkList(part);

            return GetContentShape(part, shapeHelper);
        }

        // POST
        protected override DriverResult Editor(YammerWidgetPart part, IUpdateModel updater, dynamic shapeHelper)
        {
            _signals.Trigger("Codewisp.YammerWidget.Settings.Updated");

            var httpContext = HttpContext.Current;
            var request = httpContext.Request;

            var newClientId = request["YammerWidgetPart.ClientId"];
            var newClientSecret = request["YammerWidgetPart.ClientSecret"];

            // Check to see if auth details have changed
            if (newClientId != part.ClientId || newClientSecret != part.ClientSecret)
            {
                ClearYammerAuthDetails(part);
            }

            updater.TryUpdateModel(part, Prefix, null, null);
            return Editor(part, shapeHelper);
        }

        private void ClearYammerAuthDetails(YammerWidgetPart part)
        {
            part.AuthAccessToken = null;
            part.SelectedNetworkAccessToken = null;

            _contentManager.Publish(part.ContentItem);
        }

        private DriverResult GetContentShape(YammerWidgetPart part, dynamic shapeHelper)
        {
            return ContentShape("Parts_YammerWidget_Edit", () =>
                                                           shapeHelper.EditorTemplate(
                                                               TemplateName: "Parts/YammerWidget",
                                                               Model: part,
                                                               Prefix: Prefix
                                                               ));
        }
    }
}