using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Prerolls.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Library;
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

        public static IApplicationPaths ApplicationPaths { get; private set; }

        public static ILibraryManager LibraryManager { get; private set; }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILibraryManager libraryManager)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;

            ApplicationPaths = applicationPaths;
            LibraryManager = libraryManager;
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