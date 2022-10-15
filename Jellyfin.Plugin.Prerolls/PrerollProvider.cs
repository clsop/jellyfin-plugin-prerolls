using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Jellyfin.Data.Entities;
using Jellyfin.Plugin.Prerolls.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Prerolls
{
    public class PrerollProvider : IIntroProvider
    {
        private static PrerollManager _Manager;
        private PluginConfiguration _Configuration;

        public string Name { get; } = "Prerolls";

        public PrerollProvider()
        {
            _Manager = new PrerollManager();
            _Configuration = Plugin.Instance.Configuration;
        }

        public async Task<IEnumerable<IntroInfo>> GetIntros(BaseItem item, User user)
        {
            if (_Configuration.UseGenres)
            {
                var itemGenresConfigs = _Configuration.Genres.Where(x => item.Genres.Contains(x.Name)).ToList();
                return await _Manager.Get(itemGenresConfigs);
            }

            return await _Manager.Get();
        }

        public IEnumerable<string> GetAllPrerollFiles()
        {
            // not implemented on server
            return Enumerable.Empty<string>();
        }
    }
}
