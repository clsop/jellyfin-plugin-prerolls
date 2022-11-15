using System;
using System.Collections.Generic;
using System.Linq;

using Jellyfin.Plugin.Prerolls.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Prerolls
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public override string Name => "Prerolls";
        public override Guid Id => Guid.Parse("40ebce45-fe78-4e84-b819-3a9f537da73c");

        public const int DefaultPreroll = 443404335;
        public const int DefaultResolution = 1080;

        public static Plugin Instance { get; private set; }
        public new IApplicationPaths ApplicationPaths { get; private set; }
        public ILibraryManager LibraryManager { get; private set; }
        public IItemRepository ItemRepository { get; private set; }

        public Plugin(IApplicationPaths applicationPaths, IItemRepository itemRepo, IXmlSerializer xmlSerializer, ILibraryManager libraryManager)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;

            ApplicationPaths = applicationPaths;
            LibraryManager = libraryManager;
            ItemRepository = itemRepo;
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            // revalidate cached configuration
            Plugin.Instance.SaveConfiguration(Plugin.Instance.Configuration);

            var movieGeneres = this.ItemRepository.GetGenres(new InternalItemsQuery()
            {
                IsMovie = true
            }).Items.Select(item => item.Item.Name);
            var serieGeneres = ItemRepository.GetGenres(new InternalItemsQuery()
            {
                IsSeries = true
            }).Items.Select(item => item.Item.Name);

            var genres = Instance.Configuration.Genres.Select(x => x.Name);
            var commonGenres = movieGeneres.Intersect(serieGeneres);
            var movieAndSeriesGenres = commonGenres.Concat(movieGeneres.Except(commonGenres)).Concat(serieGeneres.Except(commonGenres));
            var newGenres = movieAndSeriesGenres.Except(genres);
            Instance.Configuration.Genres = Instance.Configuration.Genres.Concat(newGenres.Select(genreName => new GenreConfig()
            {
                Name = genreName,
                LocalSource = null
            })).ToList();

            yield return new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
            };
        }
    }
}
