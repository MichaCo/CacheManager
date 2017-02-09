using System;
using System.Collections.Generic;
using System.Linq;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// Used by serializers to find value types
    /// </summary>
    public static class TypeCache
    {
        private static readonly Dictionary<string, Type> _types = new Dictionary<string, Type>();
        private static readonly object _typesLock = new object();

        /// <summary>
        /// Returns <c>typeof(object)</c>.
        /// </summary>
        public static Type ObjectType { get; } = typeof(object);

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
                        var typeResult = Type.GetType(type, false);
                        if (typeResult == null)
                        {
                            // fixing an issue for corlib types if mixing net core clr and full clr calls
                            // (e.g. typeof(string) is different for those two, either System.String, System.Private.CoreLib or System.String, mscorlib)
                            var typeName = type.Split(',').FirstOrDefault();
                            typeResult = Type.GetType(typeName, true);
                        }

                        _types.Add(type, typeResult);
                    }
                }
            }

            return _types[type];
        }
    }
}