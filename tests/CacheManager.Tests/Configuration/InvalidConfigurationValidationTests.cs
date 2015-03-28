using System;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CacheManager.Core;
using CacheManager.Core.Configuration;
using CacheManager.StackExchange.Redis;
using CacheManager.Tests.TestCommon;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests.Configuration
{
    /// <summary>
    /// To run the test, the app.config of the test project must at least contain a cacheManager section.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class InvalidConfigurationValidationTests
    {
        [Fact]
        [ReplaceCulture]
        public void Cfg_BuildConfiguration_MissingName()
        {
            // arrange
            // act
            Action act = () => ConfigurationBuilder.BuildConfiguration<object>(null, null);

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("*Parameter name: cacheName");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_BuildConfiguration_MissingSettings()
        {
            // arrange
            // act
            Action act = () => ConfigurationBuilder.BuildConfiguration<object>("cacheName", null);

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
            Action act = () => ConfigurationBuilder.LoadConfiguration<object>(cfgName);

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
            Action act = () => ConfigurationBuilder.LoadConfiguration<object>(cfgName);

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("*Parameter name: configName");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_LoadConfiguration_NotExistingCacheCfgName()
        {
            // arrange
            string cfgName = Guid.NewGuid().ToString();

            // act
            Action act = () => ConfigurationBuilder.LoadConfiguration<object>(cfgName);

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("No cache manager configuration found for name*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_LoadConfiguration_InvalidSectionName()
        {
            // arrange
            // act
            Action act = () => ConfigurationBuilder.LoadConfiguration<object>(null, "config");

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: sectionName*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_LoadConfiguration_InvalidConfigName()
        {
            // arrange
            // act
            Action act = () => ConfigurationBuilder.LoadConfiguration<object>("cacheManager", "");

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: configName*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_LoadConfiguration_SectionDoesNotExist()
        {
            // arrange
            var sectionName = Guid.NewGuid().ToString();
            // act
            Action act = () => ConfigurationBuilder.LoadConfiguration<object>(sectionName, "");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("*No section defined with name " + sectionName);
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_LoadConfigurationFile_EmptyCfgFileName()
        {
            // arrange
            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>("", "configName");

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("*Parameter name: configFileName*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_LoadConfigurationFile_EmptySectionName()
        {
            // arrange
            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>("file", null, "configName");

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("*Parameter name: sectionName*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_LoadConfigurationFile_EmptyConfigName()
        {
            // arrange
            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>("file", "section", null);

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
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "configName");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("Configuration file not found*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_MissingCacheManagerCfgName()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.missingName.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "configName");

            // assert
            act.ShouldThrow<ConfigurationErrorsException>()
                .WithMessage("Required attribute 'name' not found*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_NoSection()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.NoSection.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "configName");

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
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.missingDefId.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "configName");

            // assert
            act.ShouldThrow<ConfigurationErrorsException>()
                .WithMessage("Required attribute 'id' not found*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_MissingType()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.missingType.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "configName");

            // assert
            act.ShouldThrow<ConfigurationErrorsException>()
                .WithMessage("Required attribute 'type' not found*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_InvalidType()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.invalidType.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "configName");

            // assert
            act.ShouldThrow<ConfigurationErrorsException>()
                .WithMessage("The value of the property 'type' cannot be parsed. The error is: The type '*' cannot be resolved*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_InvalidType_NumberOfGenericArgs()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.invalidType.config");

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "cacheManager2", "configName");
            Action act = () => CacheFactory.FromConfiguration<object>(cfg);

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("Invalid number of generic type arguments*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_InvalidType_ValueType()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.invalidType.config");

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "cacheManager3", "configName");
            Action act = () => CacheFactory.FromConfiguration<object>(cfg);

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("Item value type configured * does not match*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_InvalidType_HandleType()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.invalidType.config");

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "cacheManager4", "configName");
            Action act = () => CacheFactory.FromConfiguration<object>(cfg);

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("Configured cache handle does not implement BaseCacheHandle<>*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_InvalidType_WrongNumberOfGenericTypeArgs()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.invalidType.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "configName");

            // assert
            act.ShouldThrow<ConfigurationErrorsException>()
                .WithMessage("The value of the property 'type' cannot be parsed. The error is: The type '*' cannot be resolved*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_NoHandleDef()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.emptyHandleDefinition.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "configName");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("There are no cache handles defined.");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_CacheManagerWithoutLinkedHandles()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.managerWithoutHandles.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "c1");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("There are no valid cache handles linked to the cache manager configuration [c1]");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_CacheManagerWithOneInvalidRef()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.InvalidRef.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "c1");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("Referenced cache handle [thisRefIsInvalid] cannot be found in cache handles definition.");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_HandleDefInvalidExpirationMode()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.invalidDefExpMode.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "c1");

            // assert
            act.ShouldThrow<ConfigurationErrorsException>()
                .WithMessage("The value of the property 'defaultExpirationMode' cannot be parsed. The error is: The enumeration value must be one of the following:*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_HandleDefInvalidTimeout()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.invalidDefTimeout.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "c1");

            // assert
            act.ShouldThrow<ConfigurationErrorsException>()
                .WithMessage("The value of the property 'defaultTimeout' cannot be parsed [20Invalid].");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_InvalidExpirationMode()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.InvalidExpMode.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "c1");

            // assert
            act.ShouldThrow<ConfigurationErrorsException>()
                .WithMessage("The value of the property 'expirationMode' cannot be parsed.*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_InvalidEnableStats()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.InvalidEnableStats.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "c1");

            // assert
            act.ShouldThrow<ConfigurationErrorsException>()
                .WithMessage("The value of the property 'enableStatistics' cannot be parsed.*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_InvalidEnablePerfCounters()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.InvalidEnablePerfCounters.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "c1");

            // assert
            act.ShouldThrow<ConfigurationErrorsException>()
                .WithMessage("The value of the property 'enablePerformanceCounters' cannot be parsed.*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_ManagerInvalidTimeout()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.InvalidTimeout.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "c1");

            // assert
            act.ShouldThrow<ConfigurationErrorsException>()
                .WithMessage("The value of the property 'timeout' cannot be parsed [thisisinvalid].");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_ManagerInvalidUpdateMode()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.InvalidUpdateMode.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "c1");

            // assert
            act.ShouldThrow<ConfigurationErrorsException>()
                .WithMessage("The value of the property 'updateMode' cannot be parsed*");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_ExpirationModeWithoutTimeout()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.ExpirationWithoutTimeout.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "c1");

            // assert
            act.ShouldThrow<ConfigurationErrorsException>()
                .WithMessage("Expiration mode set without a valid timeout specified for handle [h1]");
        }
        
        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_MaxRetriesLessThanOne()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.MaxRetries.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "c1");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("Maximum number of retries must be greater than zero.");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_RetryTimeoutLessThanZero()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.RetryTimeout.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "c1");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("Retry timeout must be greater than or equal to zero.");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_BackPlateNameButNoType()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.backPlateNameNoType.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "c1");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("BackPlateType cannot be null if BackPlateName is specified.");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_InvalidCfgFile_BackPlateTypeButNoName()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.invalid.backPlateTypeNoName.config");

            // act
            Action act = () => ConfigurationBuilder.LoadConfigurationFile<object>(fileName, "c1");

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("BackPlateName cannot be null if BackPlateType is specified.");
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_CreateConfig_CacheManagerHandleCollection()
        {
            // arrange
            // act
            var col = new CacheManagerHandleCollection()
            {
                Name = "name",
                UpdateMode = CacheUpdateMode.Up,
                EnablePerformanceCounters = true,
                EnableStatistics = true, 
                MaximumRetries = 10012,
                RetryTimeout = 234, 
                BackPlateName = "backPlate",
                BackPlateType = typeof(string).FullName
            };

            // assert
            col.Name.Should().Be("name");
            col.BackPlateName.Should().Be("backPlate");
            col.BackPlateType.Should().Be(typeof(string).FullName);
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
            // arrange
            // act
            var col = new CacheManagerHandle()
            {
                IsBackPlateSource = true,
                Name = "name",
                ExpirationMode = ExpirationMode.Absolute,
                Timeout = "22m",
                RefHandleId = "ref"
            };

            // assert
            col.Name.Should().Be("name");
            col.ExpirationMode.Should().Be(ExpirationMode.Absolute);
            col.Timeout.Should().Be("22m");
            col.RefHandleId.Should().Be("ref");
            col.IsBackPlateSource.Should().BeTrue();
        }

        [Fact]
        [ReplaceCulture]
        public void Cfg_CreateConfig_CacheHandleDefinition()
        {
            // arrange
            // act
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

        private static string GetCfgFileName(string fileName)
        {
            return AppDomain.CurrentDomain.BaseDirectory + (fileName.StartsWith("\\") ? fileName : "\\" + fileName);
        }
    }
}