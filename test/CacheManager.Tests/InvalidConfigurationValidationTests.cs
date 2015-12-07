using System;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using CacheManager.Core;
using CacheManager.Core.Configuration;
using CacheManager.Core.Internal;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests
{
    /// <summary>
    /// To run the test, the app.config of the test project must at least contain a cacheManager section.
    /// </summary>
    [ExcludeFromCodeCoverage]
#if NET40
    [Trait("Framework", "NET40")]
#else
    [Trait("Framework", "NET45")]
#endif
    public class InvalidConfigurationValidationTests : BaseCacheManagerTest
    {
        [Fact]
        [ReplaceCulture]
        public void Cfg_BuildConfiguration_MissingSettings()
        {
            // arrange act
            Action act = () => ConfigurationBuilder.BuildConfiguration(null);

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("*Parameter name: settings");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_LoadConfiguration_EmptyString()
        {
            // arrange
            string cfgName = string.Empty;

            // act
            Action act = () => ConfigurationBuilder.LoadConfiguration(cfgName);

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("*Parameter name: configName");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_LoadConfiguration_NullString()
        {
            // arrange
            string cfgName = null;

            // act
            Action act = () => ConfigurationBuilder.LoadConfiguration(cfgName);

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("*Parameter name: configName");
        }

#if DNX451
        [Fact(Skip = "DNX doesn't read from app.config")]
#else
        [Fact]
#endif
        [ReplaceCulture]
        public void Cfg_LoadConfiguration_NotExistingCacheCfgName()
        {
            // arrange
            string cfgName = Guid.NewGuid().ToString();

            // act
            Action act = () => ConfigurationBuilder.LoadConfiguration(cfgName);

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("No cache manager configuration found for name*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_LoadConfiguration_InvalidSectionName()
        {
            // arrange act
            Action act = () => ConfigurationBuilder.LoadConfiguration(null, "config");

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: sectionName*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_LoadConfiguration_InvalidConfigName()
        {
            // arrange act
            Action act = () => ConfigurationBuilder.LoadConfiguration("cacheManager", string.Empty);

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("*Parameter name: configName*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_LoadConfiguration_SectionDoesNotExist()
        {
            // arrange
            var sectionName = Guid.NewGuid().ToString();

            // act
            Action act = () => ConfigurationBuilder.LoadConfiguration(sectionName, "configName");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("*No section defined with name " + sectionName + ".");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_LoadConfigurationFile_EmptyCfgFileName()
        {
            // arrange act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(string.Empty, "configName");

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("*Parameter name: configFileName*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_LoadConfigurationFile_EmptySectionName()
        {
            // arrange act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile("file", null, "configName");

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("*Parameter name: sectionName*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_LoadConfigurationFile_EmptyConfigName()
        {
            // arrange act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile("file", "section", null);

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("*Parameter name: configName*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_LoadConfigurationFile_NotExistingCfgFileName()
        {
            // arrange
            string fileName = "notexistingconfiguration.config";

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(fileName, "configName");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("Configuration file not found*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_MissingCacheManagerCfgName()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.missingName.config");

            // act
            var exception = Record.Exception(() => ConfigurationBuilder.LoadConfigurationFile(fileName, "configName"));

            // assert
            exception.Should().NotBeNull();
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_NoSection()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.noSection.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(fileName, "configName");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("No section with name * found in file *");
        }

        /* handle definition */

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_MissingDefId()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.missingDefId.config");

            // act
            var exception = Record.Exception(() => ConfigurationBuilder.LoadConfigurationFile(fileName, "configName"));

            // assert
            exception.Should().NotBeNull();
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_InvalidType()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.invalidType.config");

            // act
            var exception = Record.Exception(() => CacheFactory.FromConfiguration<object>("configName", ConfigurationBuilder.LoadConfigurationFile(fileName, "configName")));

            // assert
            exception.Should().NotBeNull();
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_InvalidType_NumberOfGenericArgs()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.invalidType.config");

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile(fileName, "cacheManager2", "configName");
            Action act = () => CacheFactory.FromConfiguration<string>("mycache", cfg);

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("Cache handle type* should not have any generic arguments*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_InvalidType_HandleType()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.invalidType.config");

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile(fileName, "cacheManager4", "configName");

            Action act = () => CacheFactory.FromConfiguration<object>("mycache", cfg);

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("Configured cache handle does not implement*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_InvalidType_WrongNumberOfGenericTypeArgs()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.invalidType.config");

            // act
            var exception = Record.Exception(() => CacheFactory.FromConfiguration<object>("configName", ConfigurationBuilder.LoadConfigurationFile(fileName, "configName")));

            // assert
            exception.Should().NotBeNull();
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_NoHandleDef()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.emptyHandleDefinition.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(fileName, "configName");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("There are no cache handles defined.");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_CacheManagerWithoutLinkedHandles()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.managerWithoutHandles.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(fileName, "c1");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("There are no valid cache handles linked to the cache manager configuration [c1]");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_CacheManagerWithOneInvalidRef()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.InvalidRef.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(fileName, "c1");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("Referenced cache handle [thisRefIsInvalid] cannot be found in cache handles definition.");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_HandleDefInvalidExpirationMode()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.invalidDefExpMode.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(fileName, "c1");

            // assert
            act.ShouldThrow<ConfigurationErrorsException>()
                .WithMessage("*defaultExpirationMode*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_HandleDefInvalidTimeout()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.invalidDefTimeout.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(fileName, "c1");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("The value of the property 'defaultTimeout' cannot be parsed [20Invalid].");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_InvalidExpirationMode()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.InvalidExpMode.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(fileName, "c1");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("*ThisIsInvalid*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_InvalidEnableStats()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.InvalidEnableStats.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(fileName, "c1");

            // assert
            act.ShouldThrow<ConfigurationErrorsException>()
                .WithMessage("*enableStatistics*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_InvalidEnablePerfCounters()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.InvalidEnablePerfCounters.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(fileName, "c1");

            // assert
            act.ShouldThrow<ConfigurationErrorsException>()
                .WithMessage("*enablePerformanceCounters*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_ManagerInvalidTimeout()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.InvalidTimeout.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(fileName, "c1");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("The value of the property 'timeout' cannot be parsed [thisisinvalid].");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_ManagerInvalidUpdateMode()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.InvalidUpdateMode.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(fileName, "c1");

            // assert
            act.ShouldThrow<ConfigurationErrorsException>()
                .WithMessage("*updateMode*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_ExpirationModeWithoutTimeout()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.ExpirationWithoutTimeout.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(fileName, "c1");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("Expiration mode set without a valid timeout specified for handle [h1]");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_MaxRetriesLessThanOne()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.MaxRetries.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(fileName, "c1");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("Maximum number of retries must be greater than zero.");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_RetryTimeoutLessThanZero()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.RetryTimeout.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(fileName, "c1");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("Retry timeout must be greater than or equal to zero.");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_BackPlateNameButNoType()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.backPlateNameNoType.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(fileName, "c1");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("BackPlateType cannot be null if BackPlateName is specified.");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_BackPlateTypeButNoName()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.backPlateTypeNoName.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(fileName, "c1");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("BackPlateName cannot be null if BackPlateType is specified.");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_SerializerType_A()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.serializerType.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(fileName, "c1");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("*must implement " + nameof(ICacheSerializer) + "*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_SerializerType_B()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.invalid.serializerType.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile(fileName, "c2");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("*type cannot be loaded*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_CreateConfig_CacheManagerHandleCollection()
        {
            // arrange act
            var col = new CacheManagerHandleCollection()
            {
                Name = "name",
                UpdateMode = CacheUpdateMode.Up,
                EnablePerformanceCounters = true,
                EnableStatistics = true,
                MaximumRetries = 10012,
                RetryTimeout = 234,
                BackPlateName = "backPlate",
                BackPlateType = typeof(string).AssemblyQualifiedName
            };

            // assert
            col.Name.Should().Be("name");
            col.BackPlateName.Should().Be("backPlate");
            col.BackPlateType.Should().Be(typeof(string).AssemblyQualifiedName);
            col.UpdateMode.Should().Be(CacheUpdateMode.Up);
            col.EnablePerformanceCounters.Should().BeTrue();
            col.EnableStatistics.Should().BeTrue();
            col.MaximumRetries.Should().Be(10012);
            col.RetryTimeout.Should().Be(234);
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_CreateConfig_CacheManagerHandle()
        {
            // arrange act
            var col = new CacheManagerHandle()
            {
                IsBackPlateSource = true,
                Name = "name",
                ExpirationMode = ExpirationMode.Absolute.ToString(),
                Timeout = "22m",
                RefHandleId = "ref"
            };

            // assert
            col.Name.Should().Be("name");
            col.ExpirationMode.Should().Be(ExpirationMode.Absolute.ToString());
            col.Timeout.Should().Be("22m");
            col.RefHandleId.Should().Be("ref");
            col.IsBackPlateSource.Should().BeTrue();
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_CreateConfig_CacheHandleDefinition()
        {
            // arrange act
            var col = new CacheHandleDefinition()
            {
                Id = "id",
                HandleType = typeof(string),
                DefaultTimeout = "22m",
                DefaultExpirationMode = ExpirationMode.None
            };

            // assert
            col.Id.Should().Be("id");
            col.HandleType.Should().Be(typeof(string));
            col.DefaultTimeout.Should().Be("22m");
            col.DefaultExpirationMode.Should().Be(ExpirationMode.None);
        }
    }
}