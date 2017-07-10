using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// Helper functions for manipulating keys.
    /// </summary>
    static internal class CacheKeysHelper
    {
        /// <summary>
        /// Filter a list of keys based on pattern. For handles that already have a list of keys and don't have service side filtering.
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="pattern">Glob to filter the key strings with</param>
        /// <returns></returns>
        public static IEnumerable<string> FilterBy(this IEnumerable<string> keys, string pattern)
        {
            switch (MatchType(pattern))
            {
                case KeyMatchPattern.All:
                    return keys;
                case KeyMatchPattern.ContainsString:
                    return keys.Where(k => k.Contains(pattern));
                case KeyMatchPattern.EndsWithChar:
                    return keys.Where(k => k.Length == pattern.Length + 1 && k.StartsWith(pattern));
                case KeyMatchPattern.EndsWithWildCard:
                    return keys.Where(k => k.StartsWith(pattern));
                case KeyMatchPattern.StartsWithChar:
                    return keys.Where(k => k.Length == pattern.Length + 1 && k.EndsWith(pattern));
                case KeyMatchPattern.StartsWithWildcard:
                    return keys.Where(k => k.EndsWith(pattern));
                case KeyMatchPattern.ContainsSingleChar:
                    {
                        var patterns = pattern.Split('?');
                        return keys.Where(k => k.Length == pattern.Length + 1 && k.StartsWith(patterns[0]) && k.EndsWith(patterns[1]));
                    }
                case KeyMatchPattern.ContainsSingleWildcard:
                    {
                        var patterns = pattern.Split('*');
                        return keys.Where(k => k.StartsWith(patterns[0]) && k.EndsWith(patterns[1]));
                    }
                default:
                    {
                        var regexPattern = "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
                        var regex = new Regex(regexPattern);
                        return keys.Where(k => regex.IsMatch(k));
                    }
            }
        }

        static KeyMatchPattern MatchType(string pattern)
        {
            if (pattern == "*")
            {
                return KeyMatchPattern.All;
            }

            var firstCharMatch = pattern.IndexOf('?');

            if (firstCharMatch == -1)
            {
                var firstWildcard = pattern.IndexOf('*');
                if (firstWildcard == -1)
                {
                    return KeyMatchPattern.ContainsString;
                }
                var lastWildcard = pattern.LastIndexOf('*');
                if (firstWildcard == lastWildcard)
                {
                    if (firstWildcard == 0)
                    {
                        return KeyMatchPattern.StartsWithWildcard;
                    }
                    if (firstWildcard == pattern.Length - 1)
                    {
                        return KeyMatchPattern.EndsWithWildCard;
                    }
                    return KeyMatchPattern.ContainsSingleWildcard;
                }
            }
            else
            {
                var lastCharMatch = pattern.LastIndexOf('?');
                if (firstCharMatch == lastCharMatch)
                {
                    if (firstCharMatch == 0)
                    {
                        return KeyMatchPattern.StartsWithChar;
                    }
                    if (firstCharMatch == pattern.Length - 1)
                    {
                        return KeyMatchPattern.EndsWithChar;
                    }
                    return KeyMatchPattern.ContainsSingleChar;
                }

            }
            return KeyMatchPattern.RegEx;
        }

        private enum KeyMatchPattern
        {
            /// <summary>Pattern "*"</summary>
            All,
            /// <summary>Pattern "*somestring"</summary>
            StartsWithWildcard,
            /// <summary>Pattern "somestring*"</summary>
            EndsWithWildCard,
            /// <summary>Pattern "some*string"</summary>
            ContainsSingleWildcard,
            /// <summary>Pattern "?somestring"</summary>
            StartsWithChar,
            /// <summary>Pattern "somestring?"</summary>
            EndsWithChar,
            /// <summary>Pattern "*some?string"</summary>
            ContainsSingleChar,
            /// <summary>Pattern "somestring"</summary>
            ContainsString,
            /// <summary>More complex cases</summary>
            RegEx,
        }
    }
}
