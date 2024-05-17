// <copyright file="FluentFilter.cs" company="Georgy Losenkov">
// Copyright (c) Georgy Losenkov. All rights reserved.
// </copyright>

using System;
using MongoDB.Driver.Parsing;

namespace MongoDB.Driver.Filter.Fluent;

/// <summary>
/// Provides method for creating filter factory.
/// </summary>
public static class FluentFilter
{
    /// <summary>
    /// Parses text filter and returns factory that generates filters for searching in MongoDb collections.
    /// </summary>
    /// <param name="filterText">Text filter.</param>
    /// <returns>Filter factory <see cref="FluentFilterFactory"/>.</returns>
    /// <exception cref="ArgumentNullException">The <paramref name="filterText"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The <paramref name="filterText"/> is not proper text filter.</exception>
    public static FluentFilterFactory Parse(String filterText)
    {
        Ensure.NotNull(filterText, nameof(filterText));

        var scanner = new ExpressionScanner();
        scanner.SetSource(filterText, 0);

        var parser = new ExpressionParser(scanner);
        if (!parser.Parse())
        {
            throw new ArgumentException(scanner.ErrorMessage);
        }

        return parser.Result;
    }
}
