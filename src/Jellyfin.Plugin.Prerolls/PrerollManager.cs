#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

using Jellyfin.Plugin.Prerolls.Configuration;
using MediaBrowser.Controller.Persistence;

namespace Jellyfin.Plugin.Prerolls
{
    public class PrerollManager
    {
        private const string PROVIDER_ID = "prerolls.video";
        private readonly ILogger<PrerollManager> _Logger;
        private readonly CookieContainer _CookieContainer;
        private readonly Random _Random;
        // private readonly HttpClient _HttpClient;

        private readonly int[] _prerolls = {
            459725398,
            440978154,
            440793415,
            440978850,
            462141918,
            442118203,
            456990827,
            442744420,
            459723868,
            464974136,
            464989433,
            443076423,
            443381251,
            484072189,
            483691239,
            483129067,
            483129129,
            442179153,
            443404430,
            443082153,
            443076524,
            443076432,
            443081703,
            443076518,
            441615803,
            443076406,
            443076410,
            443081835,
            443081756,
            443404417,
            443081683,
            443076427,
            443404395,
            443081932,
            440785270,
            440791181,
            443125346,
            443076495,
            445012069
        };

        private readonly string _cachePath = $"{Plugin.Instance.ApplicationPaths.CachePath}/prerolls";

        public PrerollManager(ILogger<PrerollManager> logger)
        {
            _Logger = logger;
            _Random = new Random();
            _CookieContainer = new CookieContainer();
            // _HttpClient = new HttpClient();
        }

        private string GetPrerollPath(int preroll, int resolution) => $"{_cachePath}/{preroll}-{resolution}.mp4";

        private T GetRandom<T>(IList<T> collection) => collection.ElementAt(_Random.Next(collection.Count));

        private Guid createVideoEntity(string path, Dictionary<string, string> provider)
        {
            // generate a video entity
            var videoGuid = Guid.NewGuid();
            var video = new Video
            {
                Id = videoGuid,
                Path = path,
                ProviderIds = provider,
                Tags = new string[] { PROVIDER_ID },
                Name = $"preroll-{videoGuid}",
                IsVirtualItem = true // don't show as a newly added video
            };

            // insert the video into the database
            Plugin.Instance.LibraryManager.CreateItem(video, null);

            return videoGuid;
        }

        private string Local(string path)
        {
            var options = new List<string>();
            var location = File.GetAttributes(path);

            if (location.HasFlag(FileAttributes.Directory))
            {
                options.AddRange(Directory.EnumerateFiles(path));
            }
            else
            {
                options.Add(path);
            }

            var selection = options[_Random.Next(options.Count)];
            return selection;
        }

        private void Cache(int intro)
        {
            var request = CreateRequest("https://vimeo.com/" + intro);

            using var response = GetResponse(request);
            if (response.StatusCode != HttpStatusCode.OK) return;

            var responseStream = response.GetResponseStream();
            if (responseStream == null) return;

            var page = new StreamReader(responseStream).ReadToEnd();
            var match = Regex.Match(page, @"""config_url"":""(.+?)""", RegexOptions.Singleline);

            // this should be a json file containing stream information
            if (match.Groups.Count != 2) return;

            var configRequest = CreateRequest(match.Groups[1].Value.Replace(@"\", string.Empty));

            using var configResponse = GetResponse(configRequest);
            responseStream = configResponse.GetResponseStream();

            if (responseStream == null) return;

            var configData = new StreamReader(responseStream).ReadToEnd();
            var config = JsonSerializer.Deserialize<VimeoConfig>(configData);

            // directory not present on first installation
            if (!Directory.Exists(_cachePath))
            {
                Directory.CreateDirectory(_cachePath);
            }

            var minimum = 100000;
            var selection = config.request.files.progressive[0];

            foreach (var stream in config.request.files.progressive)
            {
                if (stream.height == Plugin.Instance.Configuration.Resolution)
                {
                    // break the loop if the exact resolution exists
                    selection = stream;
                    break;
                }

                var difference = Math.Abs(stream.height - Plugin.Instance.Configuration.Resolution);
                if (difference < minimum)
                {
                    // find the resolution closest to the requested quality
                    minimum = difference;
                    selection = stream;
                }
            }

            // remove old files for now
            foreach (var file in Directory.EnumerateFiles(_cachePath))
            {
                File.Delete(file);
            }

            // TODO: use HttpClient
            using var client = new WebClient();
            client.DownloadFile(selection.url, GetPrerollPath(intro, selection.height));

            // should probably do this from the get method
            //UpdateLibrary(config.video.title, GetPrerollPath(intro, selection.height));
        }

        private HttpWebRequest CreateRequest(string url)
        {
            // TODO: use HttpClient
            var request = (HttpWebRequest)WebRequest.Create(url);

            request.CookieContainer = _CookieContainer;
            request.AllowAutoRedirect = false;
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.Headers[HttpRequestHeader.AcceptLanguage] = "en-US,en;q=0.5";
            request.Headers[HttpRequestHeader.AcceptEncoding] = "gzip,deflate";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.KeepAlive = true;
            request.Timeout = 20000;

            return request;
        }

        private HttpWebResponse GetResponse(HttpWebRequest request)
        {
            var response = (HttpWebResponse)request.GetResponse();

            // store the cookies for subsequent requests
            _CookieContainer.Add(response.Cookies);

            return response;
        }

        private Guid UpdateLibrary(string path)
        {
            var fileName = Path.GetFileName(path);
            var provider = new Dictionary<string, string>
            {
                { PROVIDER_ID, fileName }
            };
            var prerollItems = Plugin.Instance.LibraryManager.GetItemList(new InternalItemsQuery
            {
                HasAnyProviderId = provider
            });

            // if video is there (file path) just use it, instead of adding a new entry
            if (prerollItems.Count > 0)
            {
                // ever only expect one video from provider
                var video = prerollItems[0];

                if (File.Exists(path))
                {
                    return video.Id;
                }

                _Logger.LogError($"File '{path}' for database preroll does not exist.");
                Plugin.Instance.LibraryManager.DeleteItem(video, new DeleteOptions()
                {
                    DeleteFileLocation = false
                });
            }

            return createVideoEntity(path, provider);
        }

        public void UpdateGenres(IItemRepository itemRepository)
        {
            var movieGeneres = itemRepository.GetGenres(new InternalItemsQuery()
            {
                IsMovie = true
            }).Items.Select(item => item.Item.Name);
            var serieGeneres = itemRepository.GetGenres(new InternalItemsQuery()
            {
                IsSeries = true
            }).Items.Select(item => item.Item.Name);

            // seperate old from new genres if any has been added to a library
            var genres = Plugin.Instance.Configuration.Genres.Select(x => x.Name);
            var commonGenres = movieGeneres.Intersect(serieGeneres).ToList();
            var movieAndSeriesGenres = commonGenres.Concat(movieGeneres.Except(commonGenres)).Concat(serieGeneres.Except(commonGenres));
            var newGenres = movieAndSeriesGenres.Except(genres).ToList();

            // update the configuration if any new genres
            if (newGenres.Count > 0)
            {
                Plugin.Instance.Configuration.Genres = Plugin.Instance.Configuration.Genres.Concat(newGenres.Select(genreName => new GenreConfig()
                {
                    Name = genreName,
                    LocalSource = null
                })).ToList();
                Plugin.Instance.SaveConfiguration(Plugin.Instance.Configuration);
                Plugin.Instance.UpdateConfiguration(Plugin.Instance.Configuration);
            }
        }

        public async Task<IEnumerable<IntroInfo>> Get(List<GenreConfig>? genreConfigs = null)
        {
            // only relevant on first installation
            // if (Plugin.Instance.Configuration.Id == Guid.Empty)
            // {
            //     Cache(Plugin.DefaultPreroll);
            // }

            Guid itemId = Guid.Empty;
            var path = GetPrerollPath(Plugin.Instance.Configuration.Preroll, Plugin.Instance.Configuration.Resolution);
            var selection = Plugin.Instance.Configuration.Preroll;

            if (genreConfigs != null)
            {
                var genreConfig = GetRandom(genreConfigs);
                path = Local(genreConfig.LocalSource);
            }
            else if (Plugin.Instance.Configuration.Local != string.Empty)
            {
                path = Local(Plugin.Instance.Configuration.Local);
            }
            // else if (Plugin.Instance.Configuration.Vimeo != string.Empty)
            // {
            //     var options = Plugin.Instance.Configuration.Vimeo.Split(',');
            //     int.TryParse(GetRandom(options), out selection);

            //     path = GetPrerollPath(selection, Plugin.Instance.Configuration.Resolution);
            // }
            // else if (Plugin.Instance.Configuration.Random)
            // {
            //     selection = GetRandom(_prerolls);
            //     path = GetPrerollPath(selection, Plugin.Instance.Configuration.Resolution);
            // }

            if (!File.Exists(path))
            {
                _Logger.LogError($"No preroll found: {path}!");
                return new List<IntroInfo>();
                //_Logger.LogWarning($"Could not find the path for preroll: {path}! using default preroll");
                //Cache(selection != 0 ? selection : 375468729);
            }

            itemId = UpdateLibrary(path);

            // grab the ID again since it might have changed
            return new List<IntroInfo>()
            {
                new IntroInfo()
                {
                    ItemId = itemId,
                    Path = path
                }
            };
        }
    }
}
