#if NETFRAMEWORK

namespace System.Runtime.CompilerServices
{
  /// <summary>
  /// Polyfill for the marker type the C# compiler requires to emit <c>init</c>-only setters
  /// (used by records). .NET Framework does not ship this type, so define it here for net48.
  /// </summary>
  internal static class IsExternalInit
  {
  }
}

#endif
