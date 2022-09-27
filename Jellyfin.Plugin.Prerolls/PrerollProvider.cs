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
        private static PrerollManager _Manager;

        public string Name { get; } = "Prerolls";

        public PrerollProvider()
        {
            _Manager = new PrerollManager();
        }

        public Task<IEnumerable<IntroInfo>> GetIntros(BaseItem item, User user)
        {
            // TODO: randomize intro based on genre
            return Task.FromResult(_Manager.Get());
        }

        public IEnumerable<string> GetAllPrerollFiles()
        {
            // not implemented on server
            return Enumerable.Empty<string>();
        }
    }
}
