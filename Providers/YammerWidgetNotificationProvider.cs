using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Localization;
using Orchard.UI.Admin.Notification;
using Orchard.UI.Notify;
using Codewisp.Yammer.Services;

namespace Codewisp.Yammer.Providers
{
    public class YammerWidgetNotificationProvider : INotificationProvider
    {
        private readonly IYammerWidgetService _service;

        public YammerWidgetNotificationProvider(IYammerWidgetService service)
        {
            _service = service;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public IEnumerable<NotifyEntry> GetNotifications()
        {
            var widgets = _service.GetUnauthorizedWidgets();
            return widgets.Select(widget => new NotifyEntry
                {
                    Message =
                        @T(
                            "One of your Yammer widgets is not yet authenticated. Click <a href='/Admin/Widgets/EditWidget/" + widget.Id
                            + "'>here</a> to configure."),
                    Type = NotifyType.Error
                });
        }
    }
}