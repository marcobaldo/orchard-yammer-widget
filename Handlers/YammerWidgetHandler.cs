using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Codewisp.Yammer.Models;

namespace Codewisp.Yammer.Handlers
{
    public class YammerWidgetHandler : ContentHandler
    {
        public YammerWidgetHandler(IRepository<YammerWidgetPartRecord> repository)
        {
            Filters.Add(StorageFilter.For(repository));
        }
    }
}