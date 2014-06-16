using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Records;

namespace Codewisp.Yammer.Models
{
    public class YammerWidgetPartRecord : ContentPartRecord
    {
        public virtual int ItemsToDisplay { get; set; }
        public virtual int CacheMinutes { get; set; }
        public virtual string ClientId { get; set; }
        public virtual string ClientSecret { get; set; }
        public virtual string AuthAccessToken { get; set; }
        public virtual string SelectedNetworkAccessToken { get; set; }
    }

    public class YammerWidgetPart : ContentPart<YammerWidgetPartRecord>
    {
        public string RedirectUri { get; set; }

        public int ItemsToDisplay
        {
            get { return Record.ItemsToDisplay; }
            set { Record.ItemsToDisplay = value; }
        }

        public int CacheMinutes
        {
            get { return Record.CacheMinutes; }
            set { Record.CacheMinutes = value; }
        }

        public string ClientId
        {
            get { return Record.ClientId; }
            set { Record.ClientId = value; }
        }

        public string ClientSecret
        {
            get { return Record.ClientSecret; }
            set { Record.ClientSecret = value; }
        }

        public string AuthAccessToken
        {
            get { return Record.AuthAccessToken; }
            set { Record.AuthAccessToken = value; }
        }

        public string SelectedNetworkAccessToken
        {
            get { return Record.SelectedNetworkAccessToken; }
            set { Record.SelectedNetworkAccessToken = value; }
        }

        public IEnumerable<SelectListItem> NetworkList { get; set; }
    }
}