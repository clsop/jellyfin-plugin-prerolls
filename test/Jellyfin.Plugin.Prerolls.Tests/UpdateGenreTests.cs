using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FluentAssertions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

using Jellyfin.Plugin.Prerolls.Configuration;

namespace Jellyfin.Plugin.Prerolls.Tests
{
    [TestClass]
    public class UpdateGenreTests
    {
        private static Mock<IApplicationPaths> _ApplicationPathsMock;
        private static Mock<IItemRepository> _ItemRepositoryMock;
        private static Mock<IXmlSerializer> _XmlSerializerMock;
        private static Mock<ILibraryManager> _LibraryManagerMock;
        private static Mock<ILogger<PrerollManager>> _LoggerMock;

        static UpdateGenreTests()
        {
            _ApplicationPathsMock = new Mock<IApplicationPaths>();
            _XmlSerializerMock = new Mock<IXmlSerializer>();
            _ItemRepositoryMock = new Mock<IItemRepository>();
            _LibraryManagerMock = new Mock<ILibraryManager>();
            _LoggerMock = new Mock<ILogger<PrerollManager>>();
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
            var videoGenres = new List<(BaseItem, ItemCounts)>()
            {
                (new Genre() { Name = "Action" }, new ItemCounts() { MovieCount = 1 } ),
                (new Genre() { Name = "Adventure" }, new ItemCounts() { MovieCount = 1 } ),
                (new Genre() { Name = "Comedy" }, new ItemCounts() { SeriesCount = 1 } ),
                (new Genre() { Name = "Romance" }, new ItemCounts() { SeriesCount = 1 } )
            };
            var audioGenres = new List<(BaseItem, ItemCounts)>()
            {
                (new Genre() { Name = "Rock" }, new ItemCounts() { ArtistCount = 2, SongCount = 3 } ),
                (new Genre() { Name = "Pop" }, new ItemCounts() { ArtistCount = 3, SongCount = 4 } )
            };
            var expectedGenreCount = videoGenres.Count;
            _ItemRepositoryMock.Setup(x => x.GetGenres(It.Is<InternalItemsQuery>(x => x.MediaTypes.Contains(MediaType.Video))))
                .Returns(new QueryResult<(BaseItem Item, ItemCounts ItemCounts)>(videoGenres));
            _ItemRepositoryMock.Setup(x => x.GetGenres(It.Is<InternalItemsQuery>(x => x.MediaTypes.Contains(MediaType.Audio))))
                .Returns(new QueryResult<(BaseItem Item, ItemCounts ItemCounts)>(audioGenres));

            new Plugin(
                applicationPaths: _ApplicationPathsMock.Object,
                itemRepo: _ItemRepositoryMock.Object,
                xmlSerializer: _XmlSerializerMock.Object,
                libraryManager: _LibraryManagerMock.Object);
            var prerollManager = new PrerollManager(_LoggerMock.Object);

            // Act
            prerollManager.UpdateGenres(_ItemRepositoryMock.Object);

            // Assert
            Plugin.Instance.Configuration.Genres.Should().HaveCount(expectedGenreCount);
        }

        [TestMethod]
        public void Test_Will_MergeNewGenres()
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

            var videoGenres = new List<(BaseItem, ItemCounts)>()
            {
                (new Genre() { Name = "Adventure" }, new ItemCounts() { MovieCount = 2 } )
            };
            var expectedGenreCount = config.Genres.Count + videoGenres.Count;
            _ItemRepositoryMock.Setup(x => x.GetGenres(It.Is<InternalItemsQuery>(x => x.MediaTypes.Contains(MediaType.Video))))
                .Returns(new QueryResult<(BaseItem Item, ItemCounts ItemCounts)>(videoGenres));

            new Plugin(
                applicationPaths: _ApplicationPathsMock.Object,
                itemRepo: _ItemRepositoryMock.Object,
                xmlSerializer: _XmlSerializerMock.Object,
                libraryManager: _LibraryManagerMock.Object);
            var prerollManager = new PrerollManager(_LoggerMock.Object);

            // Act
            prerollManager.UpdateGenres(_ItemRepositoryMock.Object);

            // Assert
            Plugin.Instance.Configuration.Genres.Should().HaveCount(expectedGenreCount);
        }
    }
}