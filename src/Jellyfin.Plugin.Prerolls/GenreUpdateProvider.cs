using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Jellyfin.Plugin.Prerolls.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Plugins;

namespace Jellyfin.Plugin.Prerolls
{
    public class GenreUpdateProvider : IServerEntryPoint, ILibraryPostScanTask
    {
        private readonly IItemRepository _ItemRepository;
        private readonly ILogger<PrerollManager> _Logger;
        private readonly PluginConfiguration _Configuration;
        private readonly PrerollManager _PrerollManager;

        public GenreUpdateProvider(ILogger<PrerollManager> logger, IItemRepository itemRepository)
        {
            _ItemRepository = itemRepository;
            _Configuration = Plugin.Instance.Configuration;
            _PrerollManager = new PrerollManager(logger);
        }

        public void Dispose()
        {
        }

        // update genres after library scan (possibly new genres)
        public Task Run(IProgress<double> progress, CancellationToken cancellationToken) => Task.Run(() =>
        {
            _PrerollManager.UpdateGenres(this._ItemRepository);
            progress.Report(100);
        });

        // update genres at startup
        public Task RunAsync() => Task.Run(() =>
        {
            _PrerollManager.UpdateGenres(_ItemRepository);
        });
    }
}
