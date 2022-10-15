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

        public static new IApplicationPaths ApplicationPaths { get; private set; }

        public static ILibraryManager LibraryManager { get; private set; }

        public Plugin(IApplicationPaths applicationPaths, IItemRepository itemRepo, IXmlSerializer xmlSerializer, ILibraryManager libraryManager)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;

            ApplicationPaths = applicationPaths;
            LibraryManager = libraryManager;

            if (Instance.Configuration.Genres == null || Instance.Configuration.Genres.Count == 0)
            {
                var movieGeneres = itemRepo.GetGenres(new InternalItemsQuery()
                {
                    IsMovie = true
                }).Items.Select(item => item.Item.Name);
                var serieGeneres = itemRepo.GetGenres(new InternalItemsQuery()
                {
                    IsSeries = true
                }).Items.Select(item => item.Item.Name);

                var commonGenres = movieGeneres.Intersect(serieGeneres);
                var allGenres = commonGenres.Concat(movieGeneres.Except(commonGenres)).Concat(serieGeneres.Except(commonGenres));

                Instance.Configuration.Genres = allGenres.Select(genre => new GenreConfig()
                {
                    Name = genre,
                    LocalSource = null
                }).ToList();
            }
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield return new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
            };
        }
    }
}
