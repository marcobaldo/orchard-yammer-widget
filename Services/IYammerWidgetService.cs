using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Orchard;
using Codewisp.Yammer.Models;

namespace Codewisp.Yammer.Services
{
    public interface IYammerWidgetService : IDependency
    {
        SelectList GetNetworkList(YammerWidgetPart part);
        IEnumerable<YammerWidgetFeedItem> GetYammerFeed(YammerWidgetPart part);
        IEnumerable<YammerWidgetPart> GetUnauthorizedWidgets();
    }
}
