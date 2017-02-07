using System;
using System.Diagnostics.CodeAnalysis;
using CacheManager.Core;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
#if NET40
    [Trait("Framework", "NET40")]
#else
    [Trait("Framework", "NET45")]
#endif
    public class CacheItemValidation
    {
        [Fact]
        public void CacheItem_WithAbsoluteExpiration()
        {
            // arrange
            var baseItem = new CacheItem<object>("key", "region", "value", ExpirationMode.Sliding, TimeSpan.FromDays(10));

            // act
            var result = baseItem.WithAbsoluteExpiration(DateTimeOffset.Now.AddMinutes(10));

            // assert
            result.ExpirationMode.Should().Be(ExpirationMode.Absolute);
            result.ExpirationTimeout.Should().BeCloseTo(TimeSpan.FromMinutes(10), precision: 200);
            result.Value.Should().Be(baseItem.Value);
            result.Region.Should().Be(baseItem.Region);
            result.Key.Should().Be(baseItem.Key);
            result.CreatedUtc.Should().Be(baseItem.CreatedUtc);
            result.LastAccessedUtc.Should().Be(baseItem.LastAccessedUtc);
        }

        [Fact]
        public void CacheItem_WithAbsoluteExpiration_Invalid()
        {
            // arrange
            var baseItem = new CacheItem<object>("key", "region", "value", ExpirationMode.Sliding, TimeSpan.FromDays(10));

            // act
            Action act = () => baseItem.WithAbsoluteExpiration(DateTimeOffset.Now.AddMinutes(-10));

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("*value must be greater*");
        }

        [Fact]
        public void CacheItem_WithAbsoluteExpiration_InvalidB()
        {
            // arrange
            var baseItem = new CacheItem<object>("key", "region", "value", ExpirationMode.Sliding, TimeSpan.FromDays(10));

            // act
            Action act = () => baseItem.WithAbsoluteExpiration(TimeSpan.FromMilliseconds(-10));

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("*value must be greater*");
        }


        [Fact]
        public void CacheItem_WithSlidingExpiration_Invalid()
        {
            // arrange
            var baseItem = new CacheItem<object>("key", "region", "value", ExpirationMode.Sliding, TimeSpan.FromDays(10));

            // act
            Action act = () => baseItem.WithSlidingExpiration(TimeSpan.FromDays(-1));

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("*value must be greater*");
        }

        [Fact]
        public void CacheItem_WithExpiration_Invalid()
        {
            // arrange
            // act
            Action act = () => new CacheItem<object>("key", "region", "value", ExpirationMode.Sliding, TimeSpan.FromTicks(long.MaxValue));

            // assert
            act.ShouldThrow<ArgumentOutOfRangeException>().WithMessage("*365*");
        }

        [Fact]
        public void CacheItem_WithCreated()
        {
            // arrange
            var baseItem = new CacheItem<object>("key", "region", "value", ExpirationMode.Sliding, TimeSpan.FromDays(10));
            var created = DateTime.UtcNow.AddMinutes(-10);

            // act
            var result = baseItem.WithCreated(created);

            // assert
            result.CreatedUtc.Should().Be(created);
            result.Value.Should().Be(baseItem.Value);
            result.Region.Should().Be(baseItem.Region);
            result.Key.Should().Be(baseItem.Key);
            result.LastAccessedUtc.Should().Be(baseItem.LastAccessedUtc);
            result.ExpirationMode.Should().Be(baseItem.ExpirationMode);
            result.ExpirationTimeout.Should().Be(baseItem.ExpirationTimeout);
        }

        [Fact]
        public void CacheItem_WithExpiration_None()
        {
            // arrange
            var baseItem = new CacheItem<object>("key", "region", "value", ExpirationMode.Sliding, TimeSpan.FromDays(10));

            // act
            var result = baseItem.WithNoExpiration();

            // assert
            result.ExpirationMode.Should().Be(ExpirationMode.None);
            result.ExpirationTimeout.Should().Be(TimeSpan.Zero);    // should be zero although we set to to 10 minutes
            result.Value.Should().Be(baseItem.Value);
            result.Region.Should().Be(baseItem.Region);
            result.Key.Should().Be(baseItem.Key);
            result.CreatedUtc.Should().Be(baseItem.CreatedUtc);
            result.LastAccessedUtc.Should().Be(baseItem.LastAccessedUtc);
        }

        [Fact]
        public void CacheItem_WithExpiration_Sliding()
        {
            // arrange
            var baseItem = new CacheItem<object>("key", "region", "value", ExpirationMode.Absolute, TimeSpan.FromDays(10));

            // act
            var result = baseItem.WithSlidingExpiration(TimeSpan.FromMinutes(10));

            // assert
            result.ExpirationMode.Should().Be(ExpirationMode.Sliding);
            result.ExpirationTimeout.Should().Be(TimeSpan.FromMinutes(10));
            result.Value.Should().Be(baseItem.Value);
            result.Region.Should().Be(baseItem.Region);
            result.Key.Should().Be(baseItem.Key);
            result.CreatedUtc.Should().Be(baseItem.CreatedUtc);
            result.LastAccessedUtc.Should().Be(baseItem.LastAccessedUtc);
        }

        [Fact]
        public void CacheItem_WithExpiration_Absolute()
        {
            // arrange
            var baseItem = new CacheItem<object>("key", "region", "value", ExpirationMode.Sliding, TimeSpan.FromDays(10));

            // act
            var result = baseItem.WithAbsoluteExpiration(DateTime.UtcNow.AddMinutes(10));

            // assert
            result.ExpirationMode.Should().Be(ExpirationMode.Absolute);
            result.ExpirationTimeout.Should().BeCloseTo(TimeSpan.FromMinutes(10), 100);
            result.Value.Should().Be(baseItem.Value);
            result.Region.Should().Be(baseItem.Region);
            result.Key.Should().Be(baseItem.Key);
            result.CreatedUtc.Should().Be(baseItem.CreatedUtc);
            result.LastAccessedUtc.Should().Be(baseItem.LastAccessedUtc);
        }

        [Fact]
        public void CacheItem_WithNoExpiration()
        {
            // arrange
            var baseItem = new CacheItem<object>("key", "region", "value", ExpirationMode.Sliding, TimeSpan.FromDays(10));

            // act
            var result = baseItem.WithNoExpiration();

            // assert
            result.ExpirationMode.Should().Be(ExpirationMode.None);
            result.ExpirationTimeout.Should().Be(TimeSpan.Zero);
            result.Value.Should().Be(baseItem.Value);
            result.Region.Should().Be(baseItem.Region);
            result.Key.Should().Be(baseItem.Key);
            result.CreatedUtc.Should().Be(baseItem.CreatedUtc);
            result.LastAccessedUtc.Should().Be(baseItem.LastAccessedUtc);
        }

        [Fact]
        public void CacheItem_WithSlidingExpiration()
        {
            // arrange
            var baseItem = new CacheItem<object>("key", "region", "value", ExpirationMode.Absolute, TimeSpan.FromDays(10));

            // act
            var result = baseItem.WithSlidingExpiration(TimeSpan.FromHours(2));

            // assert
            result.ExpirationMode.Should().Be(ExpirationMode.Sliding);
            result.ExpirationTimeout.Should().Be(TimeSpan.FromHours(2));
            result.Value.Should().Be(baseItem.Value);
            result.Region.Should().Be(baseItem.Region);
            result.Key.Should().Be(baseItem.Key);
            result.CreatedUtc.Should().Be(baseItem.CreatedUtc);
            result.LastAccessedUtc.Should().Be(baseItem.LastAccessedUtc);
        }

        [Fact]
        public void CacheItem_WithValue()
        {
            // arrange
            var baseItem = new CacheItem<object>("key", "region", "value", ExpirationMode.Absolute, TimeSpan.FromDays(10));

            // act
            var result = baseItem.WithValue("new value");

            // assert
            result.Value.Should().Be("new value");
            result.Region.Should().Be(baseItem.Region);
            result.Key.Should().Be(baseItem.Key);
            result.CreatedUtc.Should().Be(baseItem.CreatedUtc);
            result.LastAccessedUtc.Should().Be(baseItem.LastAccessedUtc);
            result.ExpirationMode.Should().Be(baseItem.ExpirationMode);
            result.ExpirationTimeout.Should().Be(baseItem.ExpirationTimeout);
        }

        #region ctor1

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor1_EmptyKey()
        {
            // arrange
            var key = string.Empty;
            object value = null;

            // act
            Action act = () => new CacheItem<object>(key, value);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("*Parameter name: key");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor1_NullKey()
        {
            // arrange
            string key = null;
            object value = null;

            // act
            Action act = () => new CacheItem<object>(key, value);

            // assert
            act.ShouldThrow<ArgumentNullException>().WithMessage("*Parameter name: key");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor1_WhitespaceKey()
        {
            // arrange
            string key = "    ";
            object value = null;

            // act
            Action act = () => new CacheItem<object>(key, value);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("*Parameter name: key");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor1_NullValue()
        {
            // arrange
            string key = "key";
            object value = null;

            // act
            Action act = () => new CacheItem<object>(key, value);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("*cannot be null.\r\nParameter name: value");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor1_ValidateCreatedResult()
        {
            // arrange
            string key = "key";
            object value = "value";

            // act
            var act = new CacheItem<object>(key, value);

            // assert
            act.Should()
                .Match<CacheItem<object>>(p => p.ExpirationMode == ExpirationMode.Default)
                .And.Match<CacheItem<object>>(p => p.ExpirationTimeout == TimeSpan.Zero)
                .And.Match<CacheItem<object>>(p => p.Key == key)
                .And.Match<CacheItem<object>>(p => p.Value == value)
                .And.Match<CacheItem<object>>(p => p.Region == null);
        }

        #endregion ctor1

        #region ctor2

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor2_EmptyKey()
        {
            // arrange
            var key = string.Empty;
            object value = null;
            string region = null;

            // act
            Action act = () => new CacheItem<object>(key, region, value);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("*Parameter name: key");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor2_NullKey()
        {
            // arrange
            string key = null;
            object value = null;
            string region = null;

            // act
            Action act = () => new CacheItem<object>(key, region, value);

            // assert
            act.ShouldThrow<ArgumentNullException>().WithMessage("*Parameter name: key");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor2_WhitespaceKey()
        {
            // arrange
            string key = "    ";
            object value = null;
            string region = null;

            // act
            Action act = () => new CacheItem<object>(key, region, value);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("*Parameter name: key");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor2_NullValue()
        {
            // arrange
            string key = "key";
            object value = null;
            string region = null;

            // act
            Action act = () => new CacheItem<object>(key, region, value);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("*cannot be null.\r\nParameter name: value");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor2_EmptyRegion()
        {
            // arrange
            string key = "key";
            string value = "value";
            string region = string.Empty;

            // act
            Action act = () => new CacheItem<object>(key, region, value);

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("*Parameter name: region");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor2_NullRegion()
        {
            // arrange
            string key = "key";
            string value = "value";
            string region = null;

            // act
            Action act = () => new CacheItem<object>(key, region, value);

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: region");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor2_WhitespaceRegion()
        {
            // arrange
            string key = "key";
            string value = "value";
            string region = "  ";

            // act
            Action act = () => new CacheItem<object>(key, region, value);

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("*Parameter name: region");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor2_ValidateCreatedResult()
        {
            // arrange
            string key = "key";
            object value = "value";
            string region = "region";

            // act
            var act = new CacheItem<object>(key, region, value);

            // assert
            act.Should()
                .Match<CacheItem<object>>(p => p.ExpirationMode == ExpirationMode.Default)
                .And.Match<CacheItem<object>>(p => p.ExpirationTimeout == TimeSpan.Zero)
                .And.Match<CacheItem<object>>(p => p.Key == key)
                .And.Match<CacheItem<object>>(p => p.Value == value)
                .And.Match<CacheItem<object>>(p => p.Region == region);
        }

        #endregion ctor2

        #region ctor3

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor3_EmptyKey()
        {
            // arrange
            var key = string.Empty;
            object value = null;
            ExpirationMode mode = 0;
            TimeSpan timeout = default(TimeSpan);

            // act
            Action act = () => new CacheItem<object>(key, value, mode, timeout);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("*Parameter name: key");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor3_NullKey()
        {
            // arrange
            string key = null;
            object value = null;
            ExpirationMode mode = 0;
            TimeSpan timeout = default(TimeSpan);

            // act
            Action act = () => new CacheItem<object>(key, value, mode, timeout);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("*cannot be null.\r\nParameter name: key");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor3_WhitespaceKey()
        {
            // arrange
            string key = "    ";
            object value = null;
            ExpirationMode mode = 0;
            TimeSpan timeout = default(TimeSpan);

            // act
            Action act = () => new CacheItem<object>(key, value, mode, timeout);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("*Parameter name: key");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor3_NullValue()
        {
            // arrange
            string key = "key";
            object value = null;
            ExpirationMode mode = 0;
            TimeSpan timeout = default(TimeSpan);

            // act
            Action act = () => new CacheItem<object>(key, value, mode, timeout);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("*cannot be null.\r\nParameter name: value");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor3_ValidateCreatedResult()
        {
            // arrange
            string key = "key";
            object value = "value";
            ExpirationMode mode = ExpirationMode.Sliding;
            TimeSpan timeout = new TimeSpan(0, 23, 45);

            // act
            var act = new CacheItem<object>(key, value, mode, timeout);

            // assert
            act.Should()
                .Match<CacheItem<object>>(p => p.ExpirationMode == mode)
                .And.Match<CacheItem<object>>(p => p.ExpirationTimeout == timeout)
                .And.Match<CacheItem<object>>(p => p.Key == key)
                .And.Match<CacheItem<object>>(p => p.Value == value)
                .And.Match<CacheItem<object>>(p => p.Region == null);
        }

        #endregion ctor3

        #region ctor4

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor4_EmptyKey()
        {
            // arrange
            var key = string.Empty;
            object value = null;
            string region = null;
            ExpirationMode mode = ExpirationMode.None;
            TimeSpan timeout = default(TimeSpan);

            // act
            Action act = () => new CacheItem<object>(key, region, value, mode, timeout);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("*Parameter name: key");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor4_NullKey()
        {
            // arrange
            string key = null;
            object value = null;
            string region = null;
            ExpirationMode mode = ExpirationMode.None;
            TimeSpan timeout = default(TimeSpan);

            // act
            Action act = () => new CacheItem<object>(key, region, value, mode, timeout);

            // assert
            act.ShouldThrow<ArgumentNullException>().WithMessage("*Parameter name: key");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor4_WhitespaceKey()
        {
            // arrange
            string key = "    ";
            object value = null;
            string region = null;
            ExpirationMode mode = ExpirationMode.None;
            TimeSpan timeout = default(TimeSpan);

            // act
            Action act = () => new CacheItem<object>(key, region, value, mode, timeout);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("*Parameter name: key");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor4_NullValue()
        {
            // arrange
            string key = "key";
            object value = null;
            string region = null;
            ExpirationMode mode = ExpirationMode.None;
            TimeSpan timeout = default(TimeSpan);

            // act
            Action act = () => new CacheItem<object>(key, region, value, mode, timeout);

            // assert
            act.ShouldThrow<ArgumentNullException>().WithMessage("*Parameter name: value");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor4_EmptyRegion()
        {
            // arrange
            string key = "key";
            string value = "value";
            string region = string.Empty;
            ExpirationMode mode = ExpirationMode.None;
            TimeSpan timeout = default(TimeSpan);

            // act
            Action act = () => new CacheItem<object>(key, region, value, mode, timeout);

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("*Parameter name: region");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor4_NullRegion()
        {
            // arrange
            string key = "key";
            string value = "value";
            string region = null;
            ExpirationMode mode = ExpirationMode.None;
            TimeSpan timeout = default(TimeSpan);

            // act
            Action act = () => new CacheItem<object>(key, region, value, mode, timeout);

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: region");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor4_WhitespaceRegion()
        {
            // arrange
            string key = "key";
            string value = "value";
            string region = "  ";
            ExpirationMode mode = ExpirationMode.None;
            TimeSpan timeout = default(TimeSpan);

            // act
            Action act = () => new CacheItem<object>(key, region, value, mode, timeout);

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("*Parameter name: region");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor4_ValidateCreatedResult()
        {
            // arrange
            string key = "key";
            object value = "value";
            string region = "region";
            ExpirationMode mode = ExpirationMode.Sliding;
            TimeSpan timeout = new TimeSpan(0, 23, 45);

            // act
            var act = new CacheItem<object>(key, region, value, mode, timeout);

            // assert
            act.Should()
                .Match<CacheItem<object>>(p => p.ExpirationMode == mode)
                .And.Match<CacheItem<object>>(p => p.ExpirationTimeout == timeout)
                .And.Match<CacheItem<object>>(p => p.Key == key)
                .And.Match<CacheItem<object>>(p => p.Value == value)
                .And.Match<CacheItem<object>>(p => p.Region == region);
        }

        #endregion ctor4

        [Fact]
        [ReplaceCulture]
        public void CacheItem_Ctor_ExpirationTimeoutDefaults()
        {
            // arrange
            string key = "key";
            object value = "value";

            // act
            var act = new CacheItem<object>(key, value, ExpirationMode.None, TimeSpan.FromDays(1));

            // assert - should reset to TimeSpan.Zero because mode is "None"
            act.Should()
                .Match<CacheItem<object>>(p => p.ExpirationMode == ExpirationMode.None)
                .And.Match<CacheItem<object>>(p => p.ExpirationTimeout == TimeSpan.Zero)
                .And.Match<CacheItem<object>>(p => p.Key == key)
                .And.Match<CacheItem<object>>(p => p.Value == value);
        }
    }
}