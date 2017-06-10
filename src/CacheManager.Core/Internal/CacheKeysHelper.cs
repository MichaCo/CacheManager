using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// Helper functions for manipulating keys.
    /// </summary>
    static public class CacheKeysHelper
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
                case KeyMatchPattern.ALL:
                    return keys;
                case KeyMatchPattern.CONTAINS_STRING:
                    return keys.Where(k => k.Contains(pattern));
                case KeyMatchPattern.ENDS_WITH_CHAR:
                    return keys.Where(k => k.Length == pattern.Length + 1 && k.StartsWith(pattern));
                case KeyMatchPattern.ENDS_WITH_WILDCARD:
                    return keys.Where(k => k.StartsWith(pattern));
                case KeyMatchPattern.STARTS_WITH_CHAR:
                    return keys.Where(k => k.Length == pattern.Length + 1 && k.EndsWith(pattern));
                case KeyMatchPattern.STARTS_WITH_WILDCARD:
                    return keys.Where(k => k.EndsWith(pattern));
                case KeyMatchPattern.CONTAINS_SINGLE_CHAR:
                    {
                        var patterns = pattern.Split('?');
                        return keys.Where(k => k.Length == pattern.Length + 1 && k.StartsWith(patterns[0]) && k.EndsWith(patterns[1]));
                    }
                case KeyMatchPattern.CONTAINS_SINGLE_WILDCARD:
                    {
                        var patterns = pattern.Split('?');
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
                return KeyMatchPattern.ALL;

            var firstCharMatch = pattern.IndexOf('?');

            if (firstCharMatch == -1)
            {
                var firstWildcard = pattern.IndexOf('*');
                if (firstWildcard == -1)
                    return KeyMatchPattern.CONTAINS_STRING;

                var lastWildcard = pattern.LastIndexOf('*');
                if (firstWildcard == lastWildcard)
                {
                    if (firstWildcard == 0)
                        return KeyMatchPattern.STARTS_WITH_WILDCARD;
                    if (firstWildcard == pattern.Length - 1)
                        return KeyMatchPattern.ENDS_WITH_WILDCARD;
                    return KeyMatchPattern.CONTAINS_SINGLE_WILDCARD;
                }
            }
            else
            {
                var lastCharMatch = pattern.LastIndexOf('?');
                if (firstCharMatch == lastCharMatch)
                {
                    if (firstCharMatch == 0)
                        return KeyMatchPattern.STARTS_WITH_CHAR;
                    if (firstCharMatch == pattern.Length - 1)
                        return KeyMatchPattern.ENDS_WITH_CHAR;
                    return KeyMatchPattern.CONTAINS_SINGLE_CHAR;
                }

            }
            return KeyMatchPattern.REGEX;
        }

        enum KeyMatchPattern
        {
            /// <summary>Pattern "*"</summary>
            ALL,
            /// <summary>Pattern "*somestring"</summary>
            STARTS_WITH_WILDCARD,
            /// <summary>Pattern "somestring*"</summary>
            ENDS_WITH_WILDCARD,
            /// <summary>Pattern "some*string"</summary>
            CONTAINS_SINGLE_WILDCARD,
            /// <summary>Pattern "?somestring"</summary>
            STARTS_WITH_CHAR,
            /// <summary>Pattern "somestring?"</summary>
            ENDS_WITH_CHAR,
            /// <summary>Pattern "*some?string"</summary>
            CONTAINS_SINGLE_CHAR,
            /// <summary>Pattern "somestring"</summary>
            CONTAINS_STRING,
            /// <summary>More complex cases</summary>
            REGEX,
        }

    }
}
