using System;

namespace AsmResolver.Collections;

/// <summary>
/// Provides compatibility shims for the <see cref="Array"/> class for builds that target older .NET framework versions.
/// </summary>
public static class ArrayShim
{
    /// <summary>
    /// Obtains a singleton instance of an empty array of the provided type.
    /// </summary>
    /// <typeparam name="T">The type to get the empty array for.</typeparam>
    /// <returns>The empty array.</returns>
#if NETSTANDARD2_0_OR_GREATER
    public static T[] Empty<T>() => Array.Empty<T>();
#else
    public static T[] Empty<T>() => ArrayHelper<T>.Empty;

    private static class ArrayHelper<T>
    {
        public static readonly T[] Empty = new T[0];
    }
#endif
}
