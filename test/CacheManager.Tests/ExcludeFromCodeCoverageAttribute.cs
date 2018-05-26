#if NETCOREAPP1
namespace System.Diagnostics.CodeAnalysis
{
    [Conditional("DEBUG")]
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    internal sealed class ExcludeFromCodeCoverageAttribute : Attribute
    {
    }
}
#endif
