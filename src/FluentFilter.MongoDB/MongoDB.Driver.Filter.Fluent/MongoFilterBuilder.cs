// <copyright file="MongoFilterBuilder.cs" company="Georgy Losenkov">
// Copyright (c) Georgy Losenkov. All rights reserved.
// </copyright>

using System;
using MongoDB.Bson;

namespace MongoDB.Driver.Filter.Fluent;

internal struct MongoFilterBuilder
{
    public static readonly MongoFilterBuilder Positive = new MongoFilterBuilder(false);
    public static readonly MongoFilterBuilder Negative = new MongoFilterBuilder(true);

    private readonly Boolean m_negate;

    internal MongoFilterBuilder(Boolean negate)
    {
        m_negate = negate;
    }

    internal BsonDocument CreateAnd(BsonArray values)
    {
        var comparison = new BsonDocument {
            { MongoOperators.And, values },
        };

        if (m_negate)
        {
            comparison = new BsonDocument {
                { MongoOperators.Nor, new BsonArray(1) { comparison } },
            };
        }

        return comparison;
    }

    internal BsonDocument CreateAny(String path, BsonValue expression)
    {
        var comparison = new BsonDocument {
            { MongoOperators.ElemMatch, expression },
        };

        if (m_negate)
        {
            comparison = new BsonDocument {
                { MongoOperators.Not, comparison },
            };
        }

        if (path != null)
        {
            comparison = new BsonDocument {
                { path, comparison },
            };
        }

        return comparison;
    }

    internal BsonDocument CreateBetween(String path, BsonValue from, BsonValue to)
    {
        var result = new BsonDocument {
            { MongoOperators.Gte, from },
            { MongoOperators.Lte, to },
        };

        if (m_negate)
        {
            result = new BsonDocument {
                { MongoOperators.Not, result },
            };
        }

        if (path != null)
        {
            result = new BsonDocument {
                { path, result },
            };
        }

        return result;
    }

    internal BsonDocument CreateComparison(String path, String @operator, BsonValue value)
    {
        var comparison = new BsonDocument {
            { @operator, value },
        };

        if (m_negate)
        {
            comparison = new BsonDocument {
                { MongoOperators.Not, comparison },
            };
        }

        if (path != null)
        {
            comparison = new BsonDocument {
                { path, comparison },
            };
        }

        return comparison;
    }

    internal BsonDocument CreateExist(String path)
    {
        var comparison = new BsonDocument {
            { MongoOperators.Exists, m_negate ? BsonBoolean.False : BsonBoolean.True },
        };

        if (path != null)
        {
            return new BsonDocument {
                { path, comparison },
            };
        }

        return comparison;
    }

    internal BsonDocument CreateIn(String path, BsonArray values)
    {
        var comparison = new BsonDocument {
            { m_negate ? MongoOperators.Nin : MongoOperators.In, values },
        };

        if (path != null)
        {
            comparison = new BsonDocument {
                { path, comparison },
            };
        }

        return comparison;
    }

    internal BsonDocument CreateMatch(String path, BsonValue regex)
    {
        var comparison = new BsonDocument {
            { MongoOperators.Regex, regex },
        };

        if (m_negate)
        {
            comparison = new BsonDocument {
                { MongoOperators.Not, comparison },
            };
        }

        if (path != null)
        {
            comparison = new BsonDocument {
                { path, comparison },
            };
        }

        return comparison;
    }

    internal BsonDocument CreateMatchOptions(String path, BsonValue regex, BsonValue options)
    {
        var comparison = new BsonDocument {
            { MongoOperators.Regex, regex },
            { MongoOperators.Options, options },
        };

        if (m_negate)
        {
            comparison = new BsonDocument {
                { MongoOperators.Not, comparison },
            };
        }

        if (path != null)
        {
            comparison = new BsonDocument {
                { path, comparison },
            };
        }

        return comparison;
    }

    internal BsonDocument CreateOr(BsonArray values)
    {
        return new BsonDocument {
            { m_negate ? MongoOperators.Nor : MongoOperators.Or, values },
        };
    }

    internal BsonDocument CreateTypeEq(String path, BsonValue value)
    {
        var comparison = new BsonDocument {
            { MongoOperators.Type, value },
        };

        if (m_negate)
        {
            comparison = new BsonDocument {
                { MongoOperators.Not, comparison },
            };
        }

        if (path != null)
        {
            comparison = new BsonDocument {
                { path, comparison },
            };
        }

        return comparison;
    }

    internal BsonDocument CreateTypeIn(String path, BsonArray values)
    {
        var comparison = new BsonDocument {
            { MongoOperators.Type, values },
        };

        if (m_negate)
        {
            comparison = new BsonDocument {
                { MongoOperators.Not, comparison },
            };
        }

        if (path != null)
        {
            comparison = new BsonDocument {
                { path, comparison },
            };
        }

        return comparison;
    }
}
