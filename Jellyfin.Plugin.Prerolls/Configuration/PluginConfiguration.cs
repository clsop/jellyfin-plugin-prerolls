using System;
using System.Collections.Generic;

using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Prerolls.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string Local { get; set; } = string.Empty;

        public string Vimeo { get; set; } = string.Empty;

        public int Preroll { get; set; } = Plugin.DefaultPreroll;

        public int Resolution { get; set; } = Plugin.DefaultResolution;

        public List<GenreConfig> Genres { get; set; } = new List<GenreConfig>();

        public bool Random { get; set; } = false;

        public bool UseGenres { get; set; } = false;
    }
}
