using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Driver.Models;
using Xunit;

namespace MongoDB.Driver.Filter.Fluent;

[Collection(nameof(SamplesCollection))]
public class MongoFilterBuilderTests
{
    private readonly SamplesFixture m_fixture;
    private readonly CancellationTokenSource m_cancellationTokenSource;
    private static readonly ProjectionDefinition<TestModel, String> s_projectId =
         new FindExpressionProjectionDefinition<TestModel, String>(x => x.Id);

    public MongoFilterBuilderTests(SamplesFixture fixture)
    {
        m_fixture = fixture;
        m_cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    }

    public static IEnumerable<Object[]> CreateAny_WorksAsExpectedAsync_Data()
    {
        var values = new IEnumerable<BsonValue>[] {
            Samples.Instance.Data.Take(1).Select(x => new BsonObjectId(new ObjectId(x.Id))),
            Samples.Instance.Binaries.Take(1).Select(x => new BsonBinaryData(x)),
            Samples.Instance.Booleans.Select(x => (BsonValue)x),
            Samples.Instance.Dates.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Decimals.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Integers.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Guids.Take(1).Select(x => new BsonBinaryData(x, GuidRepresentation.Standard)),
            Samples.Instance.Integers.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Strings.Take(1).Select(x => (BsonValue)x),
            new BsonValue[] { BsonNull.Value, new BsonArray(Array.Empty<BsonValue>()) },
        }.SelectMany(x => x).ToArray();

        foreach (var field in GetFieldNames())
        {
            foreach (var value in values)
            {
                yield return new Object[] {
                    field,
                    new MongoFilterBuilder(negate: false).CreateComparison(path: null, MongoOperators.Lte, value)
                };
            }
        }

        var subFields = new[] {
            nameof(SimpleModel.Binary),
            nameof(SimpleModel.Boolean),
            nameof(SimpleModel.Date),
            nameof(SimpleModel.Decimal),
            nameof(SimpleModel.Guid),
            nameof(SimpleModel.Integer),
            nameof(SimpleModel.Missing),
            nameof(SimpleModel.Object),
            nameof(SimpleModel.String),
        };


        foreach (var subField in subFields)
        {
            foreach (var value in values)
            {
                yield return new Object[] {
                    nameof(TestModel.InnerArray),
                    new MongoFilterBuilder(negate: false).CreateComparison(subField, MongoOperators.Lte, value)
                };
            }
        }
    }

    [Theory]
    [MemberData(nameof(CreateAny_WorksAsExpectedAsync_Data))]
    public async Task CreateAny_WorksAsExpectedAsync(String path, BsonValue filter)
    {
        // Arrange
        var positiveFilter = new MongoFilterBuilder(negate: false).CreateAny(path, filter);
        var negativeFilter = new MongoFilterBuilder(negate: true).CreateAny(path, filter);

        // Act
        var positiveIds = await m_fixture.Collection.Find(positiveFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var negativeIds = await m_fixture.Collection.Find(negativeFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var allIds = await m_fixture.Collection.Find(new BsonDocument()).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);

        // Assert
        allIds.Should().HaveCount(positiveIds.Count + negativeIds.Count);

        positiveIds.Should().NotIntersectWith(negativeIds);
        positiveIds.Union(negativeIds).Should().BeEquivalentTo(allIds);
    }

    public static IEnumerable<Object[]> Filters()
    {
        var values = new IEnumerable<BsonValue>[] {
            Samples.Instance.Data.Take(1).Select(x => new BsonObjectId(new ObjectId(x.Id))),
            Samples.Instance.Binaries.Take(1).Select(x => new BsonBinaryData(x)),
            Samples.Instance.Booleans.Select(x => (BsonValue)x),
            Samples.Instance.Dates.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Decimals.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Integers.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Guids.Take(1).Select(x => new BsonBinaryData(x, GuidRepresentation.Standard)),
            Samples.Instance.Integers.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Strings.Take(1).Select(x => (BsonValue)x),
            new BsonValue[] { BsonNull.Value, new BsonArray(Array.Empty<BsonValue>()) },
        }.SelectMany(x => x).ToArray();

        var filters = (
            from field in GetFieldNames()
            from value in values
            select new MongoFilterBuilder(negate: false).CreateComparison(field, MongoOperators.Lte, value)
        ).ToArray();

        foreach (var filter in filters)
        {
            yield return new Object[] { new BsonArray { filter } };
        }

        foreach (var (filter1, filter2) in filters.Zip(filters.Skip(20)))
        {
            yield return new Object[] { new BsonArray { filter1, filter2 } };
        }

        foreach (var (filter1, filter2, filter3) in filters.Zip(filters.Skip(20), filters.Skip(40)))
        {
            yield return new Object[] { new BsonArray { filter1, filter2, filter3 } };
        }
    }

    [Theory]
    [MemberData(nameof(Filters))]
    public async Task CreateAnd_WorksAsExpectedAsync(BsonArray filters)
    {
        // Arrange
        var positiveFilter = new MongoFilterBuilder(negate: false).CreateAnd(filters);
        var negativeFilter = new MongoFilterBuilder(negate: true).CreateAnd(filters);

        // Act
        var positiveIds = await m_fixture.Collection.Find(positiveFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var negativeIds = await m_fixture.Collection.Find(negativeFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var allIds = await m_fixture.Collection.Find(new BsonDocument()).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);

        // Assert
        allIds.Should().HaveCount(positiveIds.Count + negativeIds.Count);

        positiveIds.Should().NotIntersectWith(negativeIds);
        positiveIds.Union(negativeIds).Should().BeEquivalentTo(allIds);
    }

    [Theory]
    [MemberData(nameof(Filters))]
    public async Task CreateOr_WorksAsExpectedAsync(BsonArray filters)
    {
        // Arrange
        var positiveFilter = new MongoFilterBuilder(negate: false).CreateOr(filters);
        var negativeFilter = new MongoFilterBuilder(negate: true).CreateOr(filters);

        // Act
        var positiveIds = await m_fixture.Collection.Find(positiveFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var negativeIds = await m_fixture.Collection.Find(negativeFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var allIds = await m_fixture.Collection.Find(new BsonDocument()).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);

        // Assert
        allIds.Should().HaveCount(positiveIds.Count + negativeIds.Count);

        positiveIds.Should().NotIntersectWith(negativeIds);
        positiveIds.Union(negativeIds).Should().BeEquivalentTo(allIds);
    }

    public static IEnumerable<Object[]> CreateBetween_WorksAsExpectedAsync_Data()
    {
        var values = new IEnumerable<BsonValue>[] {
            Samples.Instance.Data.Take(1).Select(x => new BsonObjectId(new ObjectId(x.Id))),
            Samples.Instance.Binaries.Take(1).Select(x => new BsonBinaryData(x)),
            Samples.Instance.Booleans.Select(x => (BsonValue)x),
            Samples.Instance.Dates.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Decimals.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Integers.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Guids.Take(1).Select(x => new BsonBinaryData(x, GuidRepresentation.Standard)),
            Samples.Instance.Integers.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Strings.Take(1).Select(x => (BsonValue)x),
            new BsonValue[] { BsonNull.Value, new BsonArray(Array.Empty<BsonValue>()) },
        }.SelectMany(x => x).ToArray();

        foreach (var field in GetFieldNames())
        {
            foreach (var (value1, value2) in values.Zip(values.Skip(1)))
            {
                yield return new Object[] { field, value1, value2 };
            }

            foreach (var (value1, value2) in values.Zip(values.Reverse()))
            {
                yield return new Object[] { field, value1, value2 };
            }
        }
    }

    [Theory]
    [MemberData(nameof(CreateBetween_WorksAsExpectedAsync_Data))]
    public async Task CreateBetween_WorksAsExpectedAsync(String path, BsonValue from, BsonValue to)
    {
        // Arrange
        var positiveFilter = new MongoFilterBuilder(negate: false).CreateBetween(path, from, to);
        var negativeFilter = new MongoFilterBuilder(negate: true).CreateBetween(path, from, to);

        // Act
        var positiveIds = await m_fixture.Collection.Find(positiveFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var negativeIds = await m_fixture.Collection.Find(negativeFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var allIds = await m_fixture.Collection.Find(new BsonDocument()).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);

        // Assert
        allIds.Should().HaveCount(positiveIds.Count + negativeIds.Count);

        positiveIds.Should().NotIntersectWith(negativeIds);
        positiveIds.Union(negativeIds).Should().BeEquivalentTo(allIds);
    }

    public static IEnumerable<Object[]> CreateComparison_WorksAsExpectedAsync_Data()
    {
        var values = new IEnumerable<BsonValue>[] {
            Samples.Instance.Data.Take(1).Select(x => new BsonObjectId(new ObjectId(x.Id))),
            Samples.Instance.Binaries.Take(1).Select(x => new BsonBinaryData(x)),
            Samples.Instance.Booleans.Select(x => (BsonValue)x),
            Samples.Instance.Dates.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Decimals.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Integers.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Guids.Take(1).Select(x => new BsonBinaryData(x, GuidRepresentation.Standard)),
            Samples.Instance.Integers.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Strings.Take(1).Select(x => (BsonValue)x),
            new BsonValue[] { BsonNull.Value, new BsonArray(Array.Empty<BsonValue>()) },
        }.SelectMany(x => x).ToArray();

        var operators = new[] {
            MongoOperators.Lt,
            MongoOperators.Lte,
            MongoOperators.Gt,
            MongoOperators.Gte,
            MongoOperators.Eq,
            MongoOperators.Neq,
        };

        foreach (var field in GetFieldNames())
        {
            foreach (var op in operators)
            {
                foreach (var value in values)
                {
                    yield return new Object[] { field, op, value };
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(CreateComparison_WorksAsExpectedAsync_Data))]
    public async Task CreateComparison_WorksAsExpectedAsync(String path, String @operator, BsonValue value)
    {
        // Arrange
        var positiveFilter = new MongoFilterBuilder(negate: false).CreateComparison(path, @operator, value);
        var negativeFilter = new MongoFilterBuilder(negate: true).CreateComparison(path, @operator, value);

        // Act
        var positiveIds = await m_fixture.Collection.Find(positiveFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var negativeIds = await m_fixture.Collection.Find(negativeFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var allIds = await m_fixture.Collection.Find(new BsonDocument()).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);

        // Assert
        allIds.Should().HaveCount(positiveIds.Count + negativeIds.Count);

        positiveIds.Should().NotIntersectWith(negativeIds);
        positiveIds.Union(negativeIds).Should().BeEquivalentTo(allIds);
    }

    public static IEnumerable<Object[]> CreateMatch_WorksAsExpectedAsync_Data()
    {
        var regexes = new BsonValue[] {
            new BsonString("."),
            new BsonString("."),
            new BsonRegularExpression("."),
            new BsonRegularExpression("."),
            new BsonRegularExpression(".", "i"),
            new BsonRegularExpression(".", "i"),
            new BsonRegularExpression(".", "ismx"),
            new BsonRegularExpression(".", "ismx"),
        };

        foreach (var field in GetFieldNames())
        {
            foreach (var regex in regexes)
            {
                yield return new Object[] { field, regex };
            }
        }
    }

    [Theory]
    [MemberData(nameof(CreateMatch_WorksAsExpectedAsync_Data))]
    public async Task CreateMatch_WorksAsExpectedAsync(String path, BsonValue regex)
    {
        // Arrange
        var positiveFilter = new MongoFilterBuilder(negate: false).CreateMatch(path, regex);
        var negativeFilter = new MongoFilterBuilder(negate: true).CreateMatch(path, regex);

        // Act
        var positiveIds = await m_fixture.Collection.Find(positiveFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var negativeIds = await m_fixture.Collection.Find(negativeFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var allIds = await m_fixture.Collection.Find(new BsonDocument()).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);

        // Assert
        allIds.Should().HaveCount(positiveIds.Count + negativeIds.Count);

        positiveIds.Should().NotIntersectWith(negativeIds);
        positiveIds.Union(negativeIds).Should().BeEquivalentTo(allIds);
    }

    public static IEnumerable<Object[]> CreateMatchOptions_WorksAsExpectedAsync_Data()
    {
        var regexes = new (BsonValue, BsonString)[] {
            (new BsonString("ABC"), BsonString.Empty),
            (new BsonString("ABC"), BsonString.Empty),
            (new BsonString("ABC"), new BsonString("i")),
            (new BsonString("ABC"), new BsonString("i")),
            (new BsonRegularExpression("ABC"), BsonString.Empty),
            (new BsonRegularExpression("ABC"), BsonString.Empty),
        };

        foreach (var field in GetFieldNames())
        {
            foreach (var regex in regexes)
            {
                yield return new Object[] { field, regex.Item1, regex.Item2 };
            }
        }
    }

    [Theory]
    [MemberData(nameof(CreateMatchOptions_WorksAsExpectedAsync_Data))]
    public async Task CreateMatchOptions_WorksAsExpectedAsync(String path, BsonValue regex, BsonValue options)
    {
        // Arrange
        var positiveFilter = new MongoFilterBuilder(negate: false).CreateMatchOptions(path, regex, options);
        var negativeFilter = new MongoFilterBuilder(negate: true).CreateMatchOptions(path, regex, options);

        // Act
        var positiveIds = await m_fixture.Collection.Find(positiveFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var negativeIds = await m_fixture.Collection.Find(negativeFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var allIds = await m_fixture.Collection.Find(new BsonDocument()).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);

        // Assert
        allIds.Should().HaveCount(positiveIds.Count + negativeIds.Count);

        positiveIds.Should().NotIntersectWith(negativeIds);
        positiveIds.Union(negativeIds).Should().BeEquivalentTo(allIds);
    }

    public static IEnumerable<Object[]> CreateExist_WorksAsExpectedAsync_Data()
    {
        foreach (var field in GetFieldNames())
        {
            yield return new Object[] { field };
        }
    }

    [Theory]
    [MemberData(nameof(CreateExist_WorksAsExpectedAsync_Data))]
    public async Task CreateExist_WorksAsExpectedAsync(String path)
    {
        // Arrange
        var positiveFilter = new MongoFilterBuilder(negate: false).CreateExist(path);
        var negativeFilter = new MongoFilterBuilder(negate: true).CreateExist(path);

        // Act
        var positiveIds = await m_fixture.Collection.Find(positiveFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var negativeIds = await m_fixture.Collection.Find(negativeFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var allIds = await m_fixture.Collection.Find(new BsonDocument()).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);

        // Assert
        allIds.Should().HaveCount(positiveIds.Count + negativeIds.Count);

        positiveIds.Should().NotIntersectWith(negativeIds);
        positiveIds.Union(negativeIds).Should().BeEquivalentTo(allIds);
    }

    public static IEnumerable<Object[]> CreateIn_WorksAsExpectedAsync_Data()
    {
        var values = new IEnumerable<BsonValue>[] {
            Samples.Instance.Data.Take(1).Select(x => new BsonObjectId(new ObjectId(x.Id))),
            Samples.Instance.Binaries.Take(1).Select(x => new BsonBinaryData(x)),
            Samples.Instance.Booleans.Select(x => (BsonValue)x),
            Samples.Instance.Dates.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Decimals.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Integers.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Guids.Take(1).Select(x => new BsonBinaryData(x, GuidRepresentation.Standard)),
            Samples.Instance.Integers.Take(1).Select(x => (BsonValue)x),
            Samples.Instance.Strings.Take(1).Select(x => (BsonValue)x),
            new BsonValue[] { BsonNull.Value, new BsonArray(Array.Empty<BsonValue>()) },
        }.SelectMany(x => x).ToArray();


        foreach (var field in GetFieldNames())
        {
            foreach (var value in values)
            {
                yield return new Object[] { field, new BsonArray { value } };
            }

            foreach (var (value1, value2) in values.Zip(values.Reverse()))
            {
                yield return new Object[] { field, new BsonArray { value1, value2 } };
            }
        }
    }

    [Theory]
    [MemberData(nameof(CreateIn_WorksAsExpectedAsync_Data))]
    public async Task CreateIn_WorksAsExpectedAsync(String path, BsonArray array)
    {
        // Arrange
        var positiveFilter = new MongoFilterBuilder(negate: false).CreateIn(path, array);
        var negativeFilter = new MongoFilterBuilder(negate: true).CreateIn(path, array);

        // Act
        var positiveIds = await m_fixture.Collection.Find(positiveFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var negativeIds = await m_fixture.Collection.Find(negativeFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var allIds = await m_fixture.Collection.Find(new BsonDocument()).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);

        // Assert
        allIds.Should().HaveCount(positiveIds.Count + negativeIds.Count);

        positiveIds.Should().NotIntersectWith(negativeIds);
        positiveIds.Union(negativeIds).Should().BeEquivalentTo(allIds);
    }

    public static IEnumerable<Object[]> CreateTypeofIs_WorksAsExpectedAsync_Data()
    {
        var types = GetTypes();

        foreach (var field in GetFieldNames())
        {
            foreach (var type in types)
            {
                yield return new Object[] { field, type };
            }
        }
    }

    [Theory]
    [MemberData(nameof(CreateTypeofIs_WorksAsExpectedAsync_Data))]
    public async Task CreateTypeofIs_WorksAsExpectedAsync(String path, BsonValue type)
    {
        // Arrange
        var positiveFilter = new MongoFilterBuilder(negate: false).CreateTypeEq(path, type);
        var negativeFilter = new MongoFilterBuilder(negate: true).CreateTypeEq(path, type);

        // Act
        var positiveIds = await m_fixture.Collection.Find(positiveFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var negativeIds = await m_fixture.Collection.Find(negativeFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var allIds = await m_fixture.Collection.Find(new BsonDocument()).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);

        // Assert
        allIds.Should().HaveCount(positiveIds.Count + negativeIds.Count);

        positiveIds.Should().NotIntersectWith(negativeIds);
        positiveIds.Union(negativeIds).Should().BeEquivalentTo(allIds);
    }

    public static IEnumerable<Object[]> CreateTypeofIn_WorksAsExpectedAsync_Data()
    {
        var types = GetTypes();

        foreach (var field in GetFieldNames())
        {
            foreach (var type in types)
            {
                yield return new Object[] { field, new BsonArray { type } };
            }

            foreach (var (type1, type2) in types.Zip(types.Reverse()))
            {
                yield return new Object[] { field, new BsonArray { type1, type2 } };
            }
        }
    }

    [Theory]
    [MemberData(nameof(CreateTypeofIn_WorksAsExpectedAsync_Data))]
    public async Task CreateTypeofIn_WorksAsExpectedAsync(String path, BsonArray types)
    {
        // Arrange
        var positiveFilter = new MongoFilterBuilder(negate: false).CreateTypeIn(path, types);
        var negativeFilter = new MongoFilterBuilder(negate: true).CreateTypeIn(path, types);

        // Act
        var positiveIds = await m_fixture.Collection.Find(positiveFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var negativeIds = await m_fixture.Collection.Find(negativeFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var allIds = await m_fixture.Collection.Find(new BsonDocument()).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);

        // Assert
        allIds.Should().HaveCount(positiveIds.Count + negativeIds.Count);

        positiveIds.Should().NotIntersectWith(negativeIds);
        positiveIds.Union(negativeIds).Should().BeEquivalentTo(allIds);
    }

    private static IEnumerable<String> GetFieldNames()
    {
        return new[] {
            nameof(TestModel.Binary),
            nameof(TestModel.BinaryArray),
            nameof(TestModel.Boolean),
            nameof(TestModel.BooleanArray),
            nameof(TestModel.Date),
            nameof(TestModel.DateArray),
            nameof(TestModel.Decimal),
            nameof(TestModel.DecimalArray),
            nameof(TestModel.Guid),
            nameof(TestModel.GuidArray),
            nameof(TestModel.Id),
            nameof(TestModel.Inner),
            nameof(TestModel.InnerArray),
            nameof(TestModel.Integer),
            nameof(TestModel.IntegerArray),
            nameof(TestModel.Missing),
            nameof(TestModel.Object),
            nameof(TestModel.ObjectArray),
            nameof(TestModel.String),
            nameof(TestModel.StringArray),

            $"{nameof(TestModel.BinaryArray)}.0",
            $"{nameof(TestModel.BooleanArray)}.0",
            $"{nameof(TestModel.DateArray)}.0",
            $"{nameof(TestModel.DecimalArray)}.0",
            $"{nameof(TestModel.GuidArray)}.0",
            $"{nameof(TestModel.InnerArray)}.0",
            $"{nameof(TestModel.IntegerArray)}.0",
            $"{nameof(TestModel.ObjectArray)}.0",
            $"{nameof(TestModel.StringArray)}.0",

            $"{nameof(TestModel.Inner)}.{nameof(ComplexModel.Binary)}",
            $"{nameof(TestModel.Inner)}.{nameof(ComplexModel.BinaryArray)}",
            $"{nameof(TestModel.Inner)}.{nameof(ComplexModel.Boolean)}",
            $"{nameof(TestModel.Inner)}.{nameof(ComplexModel.BooleanArray)}",
            $"{nameof(TestModel.Inner)}.{nameof(ComplexModel.Date)}",
            $"{nameof(TestModel.Inner)}.{nameof(ComplexModel.DateArray)}",
            $"{nameof(TestModel.Inner)}.{nameof(ComplexModel.Decimal)}",
            $"{nameof(TestModel.Inner)}.{nameof(ComplexModel.DecimalArray)}",
            $"{nameof(TestModel.Inner)}.{nameof(ComplexModel.Guid)}",
            $"{nameof(TestModel.Inner)}.{nameof(ComplexModel.GuidArray)}",
            $"{nameof(TestModel.Inner)}.{nameof(ComplexModel.Integer)}",
            $"{nameof(TestModel.Inner)}.{nameof(ComplexModel.IntegerArray)}",
            $"{nameof(TestModel.Inner)}.{nameof(ComplexModel.Missing)}",
            $"{nameof(TestModel.Inner)}.{nameof(ComplexModel.Object)}",
            $"{nameof(TestModel.Inner)}.{nameof(ComplexModel.ObjectArray)}",
            $"{nameof(TestModel.Inner)}.{nameof(ComplexModel.String)}",
            $"{nameof(TestModel.Inner)}.{nameof(ComplexModel.StringArray)}",

            $"{nameof(TestModel.InnerArray)}.{nameof(SimpleModel.Binary)}",
            $"{nameof(TestModel.InnerArray)}.{nameof(SimpleModel.Boolean)}",
            $"{nameof(TestModel.InnerArray)}.{nameof(SimpleModel.Date)}",
            $"{nameof(TestModel.InnerArray)}.{nameof(SimpleModel.Decimal)}",
            $"{nameof(TestModel.InnerArray)}.{nameof(SimpleModel.Guid)}",
            $"{nameof(TestModel.InnerArray)}.{nameof(SimpleModel.Integer)}",
            $"{nameof(TestModel.InnerArray)}.{nameof(SimpleModel.Missing)}",
            $"{nameof(TestModel.InnerArray)}.{nameof(SimpleModel.Object)}",
            $"{nameof(TestModel.InnerArray)}.{nameof(SimpleModel.String)}",

            $"{nameof(TestModel.InnerArray)}.0.{nameof(SimpleModel.Binary)}",
            $"{nameof(TestModel.InnerArray)}.0.{nameof(SimpleModel.Boolean)}",
            $"{nameof(TestModel.InnerArray)}.0.{nameof(SimpleModel.Date)}",
            $"{nameof(TestModel.InnerArray)}.0.{nameof(SimpleModel.Decimal)}",
            $"{nameof(TestModel.InnerArray)}.0.{nameof(SimpleModel.Guid)}",
            $"{nameof(TestModel.InnerArray)}.0.{nameof(SimpleModel.Integer)}",
            $"{nameof(TestModel.InnerArray)}.0.{nameof(SimpleModel.Missing)}",
            $"{nameof(TestModel.InnerArray)}.0.{nameof(SimpleModel.Object)}",
            $"{nameof(TestModel.InnerArray)}.0.{nameof(SimpleModel.String)}",
        };
    }

    private static IEnumerable<BsonValue> GetTypes()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var aliases = new[] {
            MongoTypeAliases.Array,
            MongoTypeAliases.BinaryData,
            MongoTypeAliases.Boolean,
            MongoTypeAliases.Date,
            MongoTypeAliases.DBPointer,
            MongoTypeAliases.Decimal128,
            MongoTypeAliases.Double,
            MongoTypeAliases.Int32,
            MongoTypeAliases.Int64,
            MongoTypeAliases.JavaScript,
            MongoTypeAliases.MaxKey,
            MongoTypeAliases.MinKey,
            MongoTypeAliases.Null,
            MongoTypeAliases.Number,
            MongoTypeAliases.Document,
            MongoTypeAliases.ObjectId,
            MongoTypeAliases.RegularExpression,
            MongoTypeAliases.String,
            MongoTypeAliases.Symbol,
            MongoTypeAliases.Timestamp,
            MongoTypeAliases.Undefined,
        };
#pragma warning restore CS0618 // Type or member is obsolete

        var types = new[] {
            BsonType.Double,
            BsonType.String,
            BsonType.Document,
            BsonType.Array,
            BsonType.Binary,
            BsonType.Undefined,
            BsonType.ObjectId,
            BsonType.Boolean,
            BsonType.DateTime,
            BsonType.Null,
            BsonType.RegularExpression,
            BsonType.JavaScript,
            BsonType.Symbol,
            BsonType.JavaScriptWithScope,
            BsonType.Int32,
            BsonType.Timestamp,
            BsonType.Int64,
            BsonType.Decimal128,
            (BsonType)(-1),
            BsonType.MaxKey,
        };

        return new IEnumerable<BsonValue>[] {
            aliases.Select(x => new BsonString(x)),
            types.Select(x => new BsonInt32((Int32)x)),
        }.SelectMany(x => x).ToArray();
    }
}
