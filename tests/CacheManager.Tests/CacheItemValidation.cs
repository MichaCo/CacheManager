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
            act.ShouldThrow<ArgumentException>().WithMessage("Value cannot be null.\r\nParameter name: key");
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
            act.ShouldThrow<ArgumentException>().WithMessage("Value cannot be null.\r\nParameter name: key");
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
            act.ShouldThrow<ArgumentException>().WithMessage("Value cannot be null.\r\nParameter name: key");
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
            act.ShouldThrow<ArgumentException>().WithMessage("Value cannot be null.\r\nParameter name: value");
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
                .Match<CacheItem<object>>(p => p.ExpirationMode == ExpirationMode.None)
                .And.Match<CacheItem<object>>(p => p.ExpirationTimeout == new TimeSpan())
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
            Action act = () => new CacheItem<object>(key, value, region);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("Value cannot be null.\r\nParameter name: key");
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
            Action act = () => new CacheItem<object>(key, value, region);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("Value cannot be null.\r\nParameter name: key");
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
            Action act = () => new CacheItem<object>(key, value, region);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("Value cannot be null.\r\nParameter name: key");
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
            Action act = () => new CacheItem<object>(key, value, region);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("Value cannot be null.\r\nParameter name: value");
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
            Action act = () => new CacheItem<object>(key, value, region);

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("Value cannot be null.\r\nParameter name: region");
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
            Action act = () => new CacheItem<object>(key, value, region);

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("Value cannot be null.\r\nParameter name: region");
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
            Action act = () => new CacheItem<object>(key, value, region);

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("Value cannot be null.\r\nParameter name: region");
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
            var act = new CacheItem<object>(key, value, region);

            // assert
            act.Should()
                .Match<CacheItem<object>>(p => p.ExpirationMode == ExpirationMode.None)
                .And.Match<CacheItem<object>>(p => p.ExpirationTimeout == new TimeSpan())
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
            act.ShouldThrow<ArgumentException>().WithMessage("Value cannot be null.\r\nParameter name: key");
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
            act.ShouldThrow<ArgumentException>().WithMessage("Value cannot be null.\r\nParameter name: key");
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
            act.ShouldThrow<ArgumentException>().WithMessage("Value cannot be null.\r\nParameter name: key");
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
            act.ShouldThrow<ArgumentException>().WithMessage("Value cannot be null.\r\nParameter name: value");
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
            Action act = () => new CacheItem<object>(key, value, region, mode, timeout);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("Value cannot be null.\r\nParameter name: key");
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
            Action act = () => new CacheItem<object>(key, value, region, mode, timeout);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("Value cannot be null.\r\nParameter name: key");
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
            Action act = () => new CacheItem<object>(key, value, region, mode, timeout);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("Value cannot be null.\r\nParameter name: key");
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
            Action act = () => new CacheItem<object>(key, value, region, mode, timeout);

            // assert
            act.ShouldThrow<ArgumentException>().WithMessage("Value cannot be null.\r\nParameter name: value");
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
            Action act = () => new CacheItem<object>(key, value, region, mode, timeout);

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("Value cannot be null.\r\nParameter name: region");
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
            Action act = () => new CacheItem<object>(key, value, region, mode, timeout);

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("Value cannot be null.\r\nParameter name: region");
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
            Action act = () => new CacheItem<object>(key, value, region, mode, timeout);

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("Value cannot be null.\r\nParameter name: region");
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
            var act = new CacheItem<object>(key, value, region, mode, timeout);

            // assert
            act.Should()
                .Match<CacheItem<object>>(p => p.ExpirationMode == mode)
                .And.Match<CacheItem<object>>(p => p.ExpirationTimeout == timeout)
                .And.Match<CacheItem<object>>(p => p.Key == key)
                .And.Match<CacheItem<object>>(p => p.Value == value)
                .And.Match<CacheItem<object>>(p => p.Region == region);
        }

        #endregion ctor4
    }
}