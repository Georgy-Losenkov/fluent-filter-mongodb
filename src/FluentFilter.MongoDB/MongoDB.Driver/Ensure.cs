// <copyright file="Ensure.cs" company="Georgy Losenkov">
// Copyright (c) Georgy Losenkov. All rights reserved.
// </copyright>

using System;

#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace MongoDB.Driver;

internal static class Ensure
{
    /// <summary>
    /// Ensures that provided value is not <see langword="null"/> and returns it. Otherwise throws <see cref="ArgumentNullException"/>.
    /// </summary>
    /// <typeparam name="T">The type of the <paramref name="value"/> parameter.</typeparam>
    /// <param name="value">The value to be checked.</param>
    /// <param name="paramName">The name of the parameter passed to <see cref="ArgumentNullException(String)"/> constructor of the exception to be thrown.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <returns>The value of the <paramref name="value"/> if it is not <see langword="null"/>.</returns>
#if NET6_0_OR_GREATER
    internal static T NotNull<T>([NotNull] T value, String paramName)
#else
    internal static T NotNull<T>(T value, String paramName)
#endif
        where T : class
    {
        if (value == null)
        {
            throw Error.ArgumentNull(paramName);
        }

        return value;
    }
}