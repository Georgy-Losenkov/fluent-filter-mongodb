// <copyright file="ResultExecutor.cs" company="Georgy Losenkov">
// Copyright (c) Georgy Losenkov. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Parsing;

namespace MongoDB.Driver.Filter.Fluent;

internal sealed class ResultExecutor
{
    private readonly IReadOnlyList<Entry> m_entries;
    private readonly Int32 m_startIndex;
    private readonly Func<String, BsonValue> m_expressionEvaluator;

    internal ResultExecutor(IReadOnlyList<Entry> entries, Int32 startIndex, Func<String, BsonValue> expressionEvaluator)
    {
        m_entries = Ensure.NotNull(entries, nameof(entries));
        m_startIndex = startIndex;
        m_expressionEvaluator = Ensure.NotNull(expressionEvaluator, nameof(expressionEvaluator));
    }

    internal BsonDocument Execute()
    {
        return CreateDocument(m_startIndex, negate: false);
    }

    private BsonDocument CreateDocument(Int32 index, Boolean negate)
    {
        var entry = m_entries[index];

        switch (entry.Type)
        {
            case EntryType.And:
            {
                var array = CreateDocumentArray(ref entry);
                return new MongoFilterBuilder(negate).CreateAnd(array);
            }

            case EntryType.AnyIs:
            {
                var subFilter = CreateDocument(entry.Index1, negate: false);
                return new MongoFilterBuilder(negate).CreateAny(entry.Text, subFilter);
            }

            case EntryType.AnyNis:
            {
                negate = !negate;
                goto case EntryType.AnyIs;
            }

            case EntryType.Between:
            {
                var from = CreateValue(entry.Index1);
                var to = CreateValue(entry.Index2);
                return new MongoFilterBuilder(negate).CreateBetween(entry.Text, from, to);
            }

            case EntryType.Exist:
            {
                return new MongoFilterBuilder(negate).CreateExist(entry.Text);
            }

            case EntryType.In:
            {
                var array = CreateValueArray(entry.Index1);
                return new MongoFilterBuilder(negate).CreateIn(entry.Text, array);
            }

            case EntryType.Match:
            {
                var regex = CreateValue(entry.Index1);
                return new MongoFilterBuilder(negate).CreateMatch(entry.Text, regex);
            }

            case EntryType.MatchOp:
            {
                var regex = CreateValue(entry.Index1);
                var options = CreateValue(entry.Index2);
                return new MongoFilterBuilder(negate).CreateMatchOptions(entry.Text, regex, options);
            }

            case EntryType.Nbetween:
            {
                negate = !negate;
                goto case EntryType.Between;
            }

            case EntryType.Nexist:
            {
                negate = !negate;
                goto case EntryType.Exist;
            }

            case EntryType.Nin:
            {
                negate = !negate;
                goto case EntryType.In;
            }

            case EntryType.Nmatch:
            {
                negate = !negate;
                goto case EntryType.Match;
            }

            case EntryType.NmatchOp:
            {
                negate = !negate;
                goto case EntryType.MatchOp;
            }

            case EntryType.Not:
            {
                return CreateDocument(entry.Index1, !negate);
            }

            case EntryType.Or:
            {
                var array = CreateDocumentArray(ref entry);
                return new MongoFilterBuilder(negate).CreateOr(array);
            }

            case EntryType.TypeEq:
            {
                var value = CreateValue(entry.Index1);
                return new MongoFilterBuilder(negate).CreateTypeEq(entry.Text, value);
            }

            case EntryType.TypeIn:
            {
                var array = CreateValueArray(entry.Index1);
                return new MongoFilterBuilder(negate).CreateTypeIn(entry.Text, array);
            }

            case EntryType.TypeNeq:
            {
                negate = !negate;
                goto case EntryType.TypeEq;
            }

            case EntryType.TypeNin:
            {
                negate = !negate;
                goto case EntryType.TypeIn;
            }

            case EntryType.Gt:
            {
                var value = CreateValue(entry.Index1);
                return new MongoFilterBuilder(negate).CreateComparison(entry.Text, MongoOperators.Gt, value);
            }

            case EntryType.Gte:
            {
                var value = CreateValue(entry.Index1);
                return new MongoFilterBuilder(negate).CreateComparison(entry.Text, MongoOperators.Gte, value);
            }

            case EntryType.Eq:
            {
                var value = CreateValue(entry.Index1);
                return new MongoFilterBuilder(negate).CreateComparison(entry.Text, MongoOperators.Eq, value);
            }

            case EntryType.Lt:
            {
                var value = CreateValue(entry.Index1);
                return new MongoFilterBuilder(negate).CreateComparison(entry.Text, MongoOperators.Lt, value);
            }

            case EntryType.Lte:
            {
                var value = CreateValue(entry.Index1);
                return new MongoFilterBuilder(negate).CreateComparison(entry.Text, MongoOperators.Lte, value);
            }

            case EntryType.Neq:
            {
                var value = CreateValue(entry.Index1);
                return new MongoFilterBuilder(negate).CreateComparison(entry.Text, MongoOperators.Neq, value);
            }

            default:
            {
                throw new InvalidOperationException($"Entry type {entry.Type} is not expected");
            }
        }
    }

    private BsonArray CreateDocumentArray(ref Entry entry)
    {
        var count = 0;

        for (var e = entry; ; e = m_entries[e.Index1])
        {
            count++;

            if (e.Type != entry.Type)
            {
                throw new InvalidOperationException($"Entry type {e.Type} is not expected. Expected {entry.Type}");
            }

            if (e.Index1 == -1)
            {
                break;
            }
        }

        var result = new BsonArray(count);
        result.AddRange(Enumerable.Range(0, count).Select(x => BsonNull.Value));

        for (var e = entry; ; e = m_entries[e.Index1])
        {
            result[--count] = CreateDocument(e.Index2, negate: false);

            if (e.Index1 == -1)
            {
                break;
            }
        }

        return result;
    }

    private BsonArray CreateValueArray(Int32 index)
    {
        var entry = m_entries[index];

        switch (entry.Type)
        {
            case EntryType.ArrayExpr:
            {
                if (m_expressionEvaluator(entry.Text) is BsonArray result)
                {
                    return result;
                }

                throw new InvalidOperationException($"Expression ${{{entry.Text}}} must evaluate to BsonArray");
            }

            case EntryType.List:
            {
                var count = 0;

                for (var e = entry; ; e = m_entries[e.Index1])
                {
                    count++;

                    if (e.Type != EntryType.List)
                    {
                        throw new InvalidOperationException($"Entry type {e.Type} is not expected. Expected {EntryType.List}");
                    }

                    if (e.Index1 == -1)
                    {
                        break;
                    }
                }

                var result = new BsonArray(count);
                result.AddRange(Enumerable.Range(0, count).Select(x => BsonNull.Value));

                for (var e = entry; ; e = m_entries[e.Index1])
                {
                    result[--count] = CreateValue(e.Index2);

                    if (e.Index1 == -1)
                    {
                        break;
                    }
                }

                return result;
            }

            default:
                throw new InvalidOperationException($"Entry type {entry.Type} is not expected. Expected {EntryType.ArrayExpr} or {EntryType.List}");
        }
    }

    private BsonValue CreateValue(Int32 index)
    {
        var entry = m_entries[index];

        switch (entry.Type)
        {
            case EntryType.Value:
                return entry.Value;

            case EntryType.ValueExpr:
                return m_expressionEvaluator(entry.Text);

            default:
                throw new InvalidOperationException($"Entry type {entry.Type} is not expected. Expected {EntryType.Value} or {EntryType.ValueExpr}");
        }
    }
}
