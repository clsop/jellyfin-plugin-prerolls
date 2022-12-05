using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Jellyfin.Data.Entities;
using Jellyfin.Plugin.Prerolls.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Prerolls
{
    public class IntroProvider : IIntroProvider
    {
        private PrerollManager _Manager;
        private PluginConfiguration _Configuration;

        public string Name { get; } = "Prerolls";

        public IntroProvider(ILogger<PrerollManager> logger)
        {
            _Manager = new PrerollManager(logger);
            _Configuration = Plugin.Instance.Configuration;
        }

        public async Task<IEnumerable<IntroInfo>> GetIntros(BaseItem item, User user)
        {
            if (_Configuration.UseGenres && _Configuration.Genres.Count > 0 && item.Genres.Length > 0)
            {
                var itemGenresConfigs = _Configuration.Genres.Where(x => item.Genres.Contains(x.Name)).ToList();
                return await _Manager.Get(itemGenresConfigs);
            }

            return await _Manager.Get();
        }
    }
}
