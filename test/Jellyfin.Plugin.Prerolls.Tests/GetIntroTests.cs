using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

using FluentAssertions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Mock;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

using Jellyfin.Plugin.Prerolls.Configuration;

namespace Jellyfin.Plugin.Prerolls.Tests
{
    [TestClass]
    public class GetIntroTests
    {
        private static Mock<IApplicationPaths> _ApplicationPathsMock;
        private static Mock<IItemRepository> _ItemRepositoryMock;
        private static Mock<IXmlSerializer> _XmlSerializerMock;
        private static Mock<ILibraryManager> _LibraryManagerMock;
        private static Mock<ILogger<PrerollManager>> _LoggerMock;

        static GetIntroTests()
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
            _LibraryManagerMock.Setup(x => x.GetItemList(It.IsAny<InternalItemsQuery>())).Returns(new List<BaseItem>()
            {
                new Video()
            });
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
        [Ignore("Cannot test static access to file path changing")]
        public async Task Test_DeletePreroll_When_PathDoesntExist()
        {
            // Arrange
            var genreConfigMock = new Mock<GenreConfig>();
            genreConfigMock.SetupProperty(x => x.LocalSource).SetupSequence(x => x.LocalSource).Returns("testfile").Returns("/fake-path");
            
            new Plugin(_ApplicationPathsMock.Object, _ItemRepositoryMock.Object, _XmlSerializerMock.Object, _LibraryManagerMock.Object);
            var prerollManager = new PrerollManager(_LoggerMock.Object);

            // Act
            await prerollManager.Get(new List<GenreConfig>() { genreConfigMock.Object });

            // Assert
            _LoggerMock.Verify(x => x.LogError(It.IsAny<string>()), Times.Once);
            _LibraryManagerMock.Verify(x => x.DeleteItem(It.IsAny<Video>(), It.IsIn<DeleteOptions>(new DeleteOptions()
            {
                DeleteFileLocation = false
            })), Times.Once);
        }

        [TestMethod]
        public async Task Test_SelectRandomGenre_When_Enabled()
        {
            // Arrange
            var genreConfigs = new List<GenreConfig>()
            {
                new GenreConfig()
                {
                    Name = "Test",
                    LocalSource = "testFiles/testfile"
                },
                new GenreConfig()
                {
                    Name = "Test 2",
                    LocalSource = "testFiles/testfile2"
                },
                new GenreConfig()
                {
                    Name = "Test 3",
                    LocalSource = "testFiles/testfile3"
                }
            };
            
            new Plugin(_ApplicationPathsMock.Object, _ItemRepositoryMock.Object, _XmlSerializerMock.Object, _LibraryManagerMock.Object);
            var prerollManager = new PrerollManager(_LoggerMock.Object);

            // Act
            var result = await prerollManager.Get(genreConfigs);

            // Assert
            genreConfigs.Should().Contain(x => x.LocalSource == result.First().Path);
        }

        [TestMethod]
        public async Task Test_SelectRandomPreroll_WhenGenres_Enabled()
        {
            // Arrange
            var fileNames = new string[] {
                "testFiles/testfile",
                "testFiles/testfile2",
                "testFiles/testfile3"
            };
            var genreConfigs = new List<GenreConfig>()
            {
                new GenreConfig()
                {
                    Name = "Test",
                    LocalSource = "testFiles"
                },
            };

            new Plugin(_ApplicationPathsMock.Object, _ItemRepositoryMock.Object, _XmlSerializerMock.Object, _LibraryManagerMock.Object);
            var prerollManager = new PrerollManager(_LoggerMock.Object);

            // Act
            var result = await prerollManager.Get(genreConfigs);

            // Assert
            fileNames.Should().Contain(fileName => fileName == result.First().Path);
        }

        // TODO: vimeo
        [TestMethod]
        [Ignore("TODO")]
        public void Test_SelectDefaultPreroll_When_NoPath()
        {
        }

        [TestMethod]
        public async Task Test_ErrorOn_NoDefaultPreroll()
        {
            // Arrange
            new Plugin(_ApplicationPathsMock.Object, _ItemRepositoryMock.Object, _XmlSerializerMock.Object, _LibraryManagerMock.Object);
            var prerollManager = new PrerollManager(_LoggerMock.Object);

            // Act
            var result = await prerollManager.Get();

            // Assert
            _LoggerMock.VerifyLog().ErrorWasCalled();
            result.Should().HaveCount(0);
        }
    }
}