#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// C# 9 `init`-only property setter shim for netstandard2.1 / Unity.
    /// Must be present for the compiler to recognize `init` accessors.
    /// </summary>
    internal static class IsExternalInit { }
}
#endif
