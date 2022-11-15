using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using FluentAssertions;
using Jellyfin.Plugin.Prerolls.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Jellyfin.Plugin.Prerolls.Tests
{
    [TestClass]
    public class PluginTests
    {
        private static Mock<IApplicationPaths> _ApplicationPathsMock;
        private static Mock<IItemRepository> _ItemRepositoryMock;
        private static Mock<IXmlSerializer> _XmlSerializerMock;
        private static Mock<ILibraryManager> _LibraryManagerMock;

        static PluginTests()
        {
            _ApplicationPathsMock = new Mock<IApplicationPaths>();
            _XmlSerializerMock = new Mock<IXmlSerializer>();
            _ItemRepositoryMock = new Mock<IItemRepository>();
            _LibraryManagerMock = new Mock<ILibraryManager>();
        }

        [ClassInitialize]
        public static void FixtureInit(TestContext context)
        {
            _ApplicationPathsMock.SetupGet(x => x.CachePath).Returns(string.Empty);
            _ApplicationPathsMock.SetupGet(x => x.ConfigurationDirectoryPath).Returns(string.Empty);
            _ApplicationPathsMock.SetupGet(x => x.DataPath).Returns(string.Empty);
            _ApplicationPathsMock.SetupGet(x => x.ImageCachePath).Returns(string.Empty);
            _ApplicationPathsMock.SetupGet(x => x.LogDirectoryPath).Returns(string.Empty);
            _ApplicationPathsMock.SetupGet(x => x.PluginConfigurationsPath).Returns(".");
            _ApplicationPathsMock.SetupGet(x => x.PluginsPath).Returns(string.Empty);
            _ApplicationPathsMock.SetupGet(x => x.ProgramDataPath).Returns(string.Empty);
            _ApplicationPathsMock.SetupGet(x => x.ProgramSystemPath).Returns(string.Empty);
            _ApplicationPathsMock.SetupGet(x => x.SystemConfigurationFilePath).Returns(string.Empty);
            _ApplicationPathsMock.SetupGet(x => x.TempDirectory).Returns(string.Empty);
            _ApplicationPathsMock.SetupGet(x => x.VirtualDataPath).Returns(string.Empty);
            _ApplicationPathsMock.SetupGet(x => x.WebPath).Returns(string.Empty);

            _XmlSerializerMock.Setup(x => x.DeserializeFromBytes(It.IsAny<Type>(), It.IsAny<byte[]>())).Returns(new { });
            _XmlSerializerMock.Setup(x => x.DeserializeFromFile(It.IsAny<Type>(), It.IsAny<string>())).Returns(new { });
            _XmlSerializerMock.Setup(x => x.DeserializeFromStream(It.IsAny<Type>(), It.IsAny<Stream>())).Returns(new { });
        }

        [ClassCleanup]
        public static void FixtureTeardown()
        {
            _ApplicationPathsMock.Reset();
            _XmlSerializerMock.Reset();
        }

        [TestInitialize]
        public void Setup()
        {
            _ItemRepositoryMock.Setup(x => x.GetGenres(It.IsAny<InternalItemsQuery>()))
                .Returns(new QueryResult<(BaseItem Item, ItemCounts ItemCounts)>(new List<(BaseItem Item, ItemCounts ItemCounts)>()));
            _XmlSerializerMock.Setup(x => x.DeserializeFromBytes(It.IsAny<Type>(), It.IsAny<byte[]>())).Returns(new { });
            _XmlSerializerMock.Setup(x => x.DeserializeFromFile(It.IsAny<Type>(), It.IsAny<string>())).Returns(new { });
            _XmlSerializerMock.Setup(x => x.DeserializeFromStream(It.IsAny<Type>(), It.IsAny<Stream>())).Returns(new { });
        }

        [TestCleanup]
        public void Teardown()
        {
            _ItemRepositoryMock.Reset();
            _LibraryManagerMock.Reset();
        }

        [TestMethod]
        public void Test_WillOnly_GetVideoGenres()
        {
            // Arrange
            var movieGenres = new List<(BaseItem, ItemCounts)>()
            {
                (new Genre() { Name = "Action" }, new ItemCounts() { MovieCount = 1 } ),
                (new Genre() { Name = "Adventure" }, new ItemCounts() { MovieCount = 1 } )
            };
            var seriesGenres = new List<(BaseItem, ItemCounts)>()
            {
                (new Genre() { Name = "Comedy" }, new ItemCounts() { SeriesCount = 1 } ),
                (new Genre() { Name = "Romance" }, new ItemCounts() { SeriesCount = 1 } )
            };
            var musicGenres = new List<(BaseItem, ItemCounts)>()
            {
                (new Genre() { Name = "Rock" }, new ItemCounts() { ArtistCount = 2, SongCount = 3 } ),
                (new Genre() { Name = "Pop" }, new ItemCounts() { ArtistCount = 3, SongCount = 4 } )
            };
            var expectedGenreCount = movieGenres.Count + seriesGenres.Count;
            _ItemRepositoryMock.Setup(x => x.GetGenres(It.Is<InternalItemsQuery>(x => x.IsMovie.HasValue && x.IsMovie.Value)))
                .Returns(new QueryResult<(BaseItem Item, ItemCounts ItemCounts)>(movieGenres));
            _ItemRepositoryMock.Setup(x => x.GetGenres(It.Is<InternalItemsQuery>(x => x.IsSeries.HasValue && x.IsSeries.Value)))
                .Returns(new QueryResult<(BaseItem Item, ItemCounts ItemCounts)>(seriesGenres));

            var plugin = new Plugin(
                applicationPaths: _ApplicationPathsMock.Object,
                itemRepo: _ItemRepositoryMock.Object,
                xmlSerializer: _XmlSerializerMock.Object,
                libraryManager: _LibraryManagerMock.Object);

            // Act
            plugin.GetPages().ToList();

            // Assert
            Plugin.Instance.Configuration.Genres.Should().HaveCount(expectedGenreCount);
        }

        [TestMethod]
        public void Test_Will_FindNewGenres()
        {
            // Arrange
            var config = new PluginConfiguration();
            _XmlSerializerMock.Setup(x => x.DeserializeFromFile(It.IsAny<Type>(), It.IsAny<string>())).Returns(config);

            var movieGenres = new List<(BaseItem, ItemCounts)>()
            {
                (new Genre() { Name = "Mystery" }, new ItemCounts() { MovieCount = 1 } )
            };
            var expectedGenreCount = movieGenres.Count;
            _ItemRepositoryMock.Setup(x => x.GetGenres(It.Is<InternalItemsQuery>(x => x.IsMovie.HasValue && x.IsMovie.Value)))
                .Returns(new QueryResult<(BaseItem Item, ItemCounts ItemCounts)>(movieGenres));

            var plugin = new Plugin(
                applicationPaths: _ApplicationPathsMock.Object,
                itemRepo: _ItemRepositoryMock.Object,
                xmlSerializer: _XmlSerializerMock.Object,
                libraryManager: _LibraryManagerMock.Object);

            // Act
            plugin.GetPages().ToList();

            // Assert
            Plugin.Instance.Configuration.Genres.Should().HaveCount(expectedGenreCount);
        }

        [TestMethod]
        public void Test_Will_MergeWithOldGenres()
        {
            // Arrange
            var config = new PluginConfiguration()
            {
                Genres = new List<GenreConfig>()
                {
                    new GenreConfig()
                    {
                        Name = "Action",
                        LocalSource = null
                    }
                }
            };
            _XmlSerializerMock.Setup(x => x.DeserializeFromFile(It.IsAny<Type>(), It.IsAny<string>())).Returns(config);

            var movieGenres = new List<(BaseItem, ItemCounts)>()
            {
                (new Genre() { Name = "Adventure" }, new ItemCounts() { MovieCount = 2 } )
            };
            var expectedGenreCount = config.Genres.Count + movieGenres.Count;
            _ItemRepositoryMock.Setup(x => x.GetGenres(It.Is<InternalItemsQuery>(x => x.IsMovie.HasValue && x.IsMovie.Value)))
                .Returns(new QueryResult<(BaseItem Item, ItemCounts ItemCounts)>(movieGenres));

            var plugin = new Plugin(
                applicationPaths: _ApplicationPathsMock.Object,
                itemRepo: _ItemRepositoryMock.Object,
                xmlSerializer: _XmlSerializerMock.Object,
                libraryManager: _LibraryManagerMock.Object);

            // Act
            plugin.GetPages().ToList();

            // Assert
            Plugin.Instance.Configuration.Genres.Should().HaveCount(expectedGenreCount);
        }
    }
}