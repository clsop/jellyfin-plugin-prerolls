using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Prerolls
{
    public class PrerollProvider : IIntroProvider
    {
        public string Name { get; } = "Prerolls";

        public Task<IEnumerable<IntroInfo>> GetIntros(BaseItem item, User user)
        {
            return Task.FromResult(new PrerollManager().Get());
        }

        public IEnumerable<string> GetAllPrerollFiles()
        {
            // not implemented on server
            return Enumerable.Empty<string>();
        }
    }
}
