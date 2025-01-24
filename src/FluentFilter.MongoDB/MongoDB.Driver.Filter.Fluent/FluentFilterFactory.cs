// <copyright file="FluentFilterFactory.cs" company="Georgy Losenkov">
// Copyright (c) Georgy Losenkov. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver.Parsing;

namespace MongoDB.Driver.Filter.Fluent;

/// <summary>
/// Provides functionaly to create filters for searching in MongoDb collections.
/// </summary>
public readonly struct FluentFilterFactory
{
    private readonly IReadOnlyList<Entry> m_entries;
    private readonly Int32 m_startIndex;

    internal FluentFilterFactory(IReadOnlyList<Entry> entries, Int32 startIndex)
    {
        m_entries = Ensure.NotNull(entries, nameof(entries));
        m_startIndex = startIndex;
    }

    /// <summary>
    /// Creates filter using provided <paramref name="expressionEvaluator"/> for evaluating all expression that parsed filter text contains.
    /// </summary>
    /// <param name="expressionEvaluator">Evaluator for expressions.</param>
    /// <returns>Filter for searching in MongoDb collections expressed as <see cref="BsonDocument"/>.</returns>
    public BsonDocument Create(Func<String, BsonValue> expressionEvaluator)
    {
        Ensure.NotNull(expressionEvaluator, nameof(expressionEvaluator));

        if (m_entries.Count == 0 && m_startIndex == -1)
        {
            return new BsonDocument();
        }
        else
        {
            return new ResultExecutor(m_entries, m_startIndex, expressionEvaluator).Execute();
        }
    }

    /// <summary>
    /// Creates filter in assumption that parsed text filter does not contain any expressions.
    /// </summary>
    /// <returns>Filter for searching in MongoDb collections expressed as <see cref="BsonDocument"/>.</returns>
    /// <exception cref="NotSupportedException">The parsed text filter contains expression.</exception>
    public BsonDocument Create()
    {
        return Create(EvaluateExpression);
    }

    private static BsonValue EvaluateExpression(String expressionText)
    {
        throw new NotSupportedException("Variables are not supported");
    }
}
