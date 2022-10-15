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

        public List<GenreConfig> Genres { get; set; } = null;

        public bool Random { get; set; } = false;

        public bool UseGenres { get; set; } = false;

        // used internally to track the current preroll
        public Guid Id { get; set; }
    }
}
