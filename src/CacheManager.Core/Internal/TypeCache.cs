using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// Used by serializers to find value types
    /// </summary>
    public static class TypeCache
    {
        private static readonly Dictionary<string, Type> _types = new Dictionary<string, Type>();
        private static readonly object _typesLock = new object();
        private static List<Func<string, Type>> _resolvers = new List<Func<string, Type>>();

        /// <summary>
        /// Returns <c>typeof(object)</c>.
        /// </summary>
        public static Type ObjectType { get; } = typeof(object);

        /// <summary>
        /// Registers a custom type resolver in case you really need to manipulate the way serialization works with types.
        /// The <paramref name="resolve"/> func is allowed to return null in case you cannot resolve the requested type.
        /// Any exception the <paramref name="resolve"/> func might throw will not bubble up.
        /// </summary>
        /// <param name="resolve">The resolver</param>
        public static void RegisterResolveType(Func<string, Type> resolve)
        {
            lock (_typesLock)
            {
                _resolvers.Add(resolve);
            }
        }

        /// <summary>
        /// Gets <see cref="Type"/> by full name (with falling back to the first part only).
        /// </summary>
        /// <param name="type">The type name.</param>
        /// <returns>The <see cref="Type"/> if valid.</returns>
        /// <exception cref="TypeLoadException">In case the <paramref name="type"/> is not a valid type. (Might also throw other type load related exceptions).</exception>
        public static Type GetType(string type)
        {
            if (!_types.ContainsKey(type))
            {
                lock (_typesLock)
                {
                    if (!_types.ContainsKey(type))
                    {
                        Type typeResult = null;
                        if (_resolvers.Count > 0)
                        {
                            foreach (var resolver in _resolvers)
                            {
                                try
                                {
                                    var result = resolver(type);
                                    if (result != null)
                                    {
                                        typeResult = result;
                                    }
                                }
                                catch { }
                            }
                        }

                        if (typeResult == null)
                        {
                            try
                            {
                                typeResult = Type.GetType(type, false);
                            }
                            catch { /* catching file load exceptions which seem to be thrown although we don't want any exceptions... */ }

                            if (typeResult == null)
                            {
                                // try remove version from the type string and resolve it (should work even for signed assemblies).
                                var withoutVersion = Regex.Replace(type, @", Version=\d+.\d+.\d+.\d+", string.Empty);
                                try
                                {
                                    typeResult = Type.GetType(withoutVersion, false);
                                }
                                catch { }
                            }

                            if (typeResult == null)
                            {
                                // fixing an issue for corlib types if mixing net core clr and full clr calls
                                // (e.g. typeof(string) is different for those two, either System.String, System.Private.CoreLib or System.String, mscorlib)
                                var typeName = type.Split(',').FirstOrDefault();

                                try
                                {
                                    typeResult = Type.GetType(typeName, false);
                                }
                                catch { }
                            }
                        }

                        if (typeResult == null)
                        {
                            throw new InvalidOperationException($"Could not load type '{type}'. Try add TypeCache.RegisterResolveType to resolve your type if the resolving continues to fail.");
                        }

                        _types.Add(type, typeResult);
                    }
                }
            }

            return _types[type];
        }
    }
}