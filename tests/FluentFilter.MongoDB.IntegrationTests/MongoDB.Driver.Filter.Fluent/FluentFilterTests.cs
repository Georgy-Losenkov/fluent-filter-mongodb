using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Driver.Models;
using Xunit;

namespace MongoDB.Driver.Filter.Fluent;

[Collection(nameof(SamplesCollection))]
public class FluentFilterTests
{
    private readonly SamplesFixture m_fixture;
    private readonly CancellationTokenSource m_cancellationTokenSource;
    private static readonly ProjectionDefinition<TestModel, String> s_projectId =
         new FindExpressionProjectionDefinition<TestModel, String>(x => x.Id);

    public FluentFilterTests(SamplesFixture fixture)
    {
        m_fixture = fixture;
        m_cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task SamplesAreValidAsync()
    {
        // Arrange
        var findFluent = m_fixture.Collection.Find(x => true);

        // Act
        var result = await findFluent.ToListAsync(m_cancellationTokenSource.Token);

        // Assert
        result.Should().BeEquivalentTo(Samples.Instance.Data);
    }

    public static IEnumerable<Object[]> Data0()
    {
        return new TheoryData<String, Func<TestModel, Boolean>> {
            { String.Empty, x => true },
            { " ", x => true },
            { "\r", x => true },
            { "\n", x => true },
            { "\t", x => true },
        };
    }

    public static IEnumerable<Object[]> Data1()
    {
        var dateSample = Samples.Instance.Dates[Samples.Instance.Dates.Count / 2];
        var dateField = nameof(TestModel.Date);

        var integerSample = Samples.Instance.Integers[Samples.Instance.Integers.Count / 2];
        var integerField = nameof(TestModel.Integer);

        var stringSample = Samples.Instance.Strings[Samples.Instance.Strings.Count / 2];
        var stringField = nameof(TestModel.String);

        var stringComparer = StringComparer.Ordinal;

        return new TheoryData<String, Func<TestModel, Boolean>> {
            { $"{dateField} <  {CreateLiteral(dateSample)}", x => x.Date != null && x.Date.Value <  dateSample },
            { $"{dateField} <= {CreateLiteral(dateSample)}", x => x.Date != null && x.Date.Value <= dateSample },
            { $"{dateField} >  {CreateLiteral(dateSample)}", x => x.Date != null && x.Date.Value >  dateSample },
            { $"{dateField} >= {CreateLiteral(dateSample)}", x => x.Date != null && x.Date.Value >= dateSample },
            { $"{dateField} != {CreateLiteral(dateSample)}", x => x.Date == null || x.Date.Value != dateSample },
            { $"{dateField} == {CreateLiteral(dateSample)}", x => x.Date != null && x.Date.Value == dateSample },

            { $"{integerField} <  {integerSample}", x => x.Integer != null && x.Integer.Value <  integerSample },
            { $"{integerField} <= {integerSample}", x => x.Integer != null && x.Integer.Value <= integerSample },
            { $"{integerField} >  {integerSample}", x => x.Integer != null && x.Integer.Value >  integerSample },
            { $"{integerField} >= {integerSample}", x => x.Integer != null && x.Integer.Value >= integerSample },
            { $"{integerField} != {integerSample}", x => x.Integer == null || x.Integer.Value != integerSample },
            { $"{integerField} == {integerSample}", x => x.Integer != null && x.Integer.Value == integerSample },

            { $"{stringField} <  {CreateLiteral(stringSample)}", x => x.String != null && stringComparer.Compare(x.String, stringSample) <  0 },
            { $"{stringField} <= {CreateLiteral(stringSample)}", x => x.String != null && stringComparer.Compare(x.String, stringSample) <= 0 },
            { $"{stringField} >  {CreateLiteral(stringSample)}", x => x.String != null && stringComparer.Compare(x.String, stringSample) >  0 },
            { $"{stringField} >= {CreateLiteral(stringSample)}", x => x.String != null && stringComparer.Compare(x.String, stringSample) >= 0 },
            { $"{stringField} != {CreateLiteral(stringSample)}", x => x.String == null || stringComparer.Compare(x.String, stringSample) != 0 },
            { $"{stringField} == {CreateLiteral(stringSample)}", x => x.String != null && stringComparer.Compare(x.String, stringSample) == 0 },
        };
    }

    public static IEnumerable<Object[]> Data2()
    {
        var dateSample1 = Samples.Instance.Dates[Samples.Instance.Dates.Count / 3];
        var dateSample2 = Samples.Instance.Dates[2 * Samples.Instance.Dates.Count / 3];
        var dateField = nameof(TestModel.Date);

        var integerSample1 = Samples.Instance.Integers[Samples.Instance.Integers.Count / 3];
        var integerSample2 = Samples.Instance.Integers[2 * Samples.Instance.Integers.Count / 3];
        var integerField = nameof(TestModel.Integer);

        var stringSample1 = Samples.Instance.Strings[Samples.Instance.Strings.Count / 3];
        var stringSample2 = Samples.Instance.Strings[2 * Samples.Instance.Strings.Count / 3];
        var stringField = nameof(TestModel.String);

        var stringComparer = StringComparer.Ordinal;

        return new TheoryData<String, Func<TestModel, Boolean>> {
            {
                $"{dateField} BETWEEN {CreateLiteral(dateSample1)} AND {CreateLiteral(dateSample2)}",
                x => x.Date != null && dateSample1 <= x.Date.Value && x.Date.Value <= dateSample2
            },
            {
                $"{dateField} NOT BETWEEN {CreateLiteral(dateSample1)} AND {CreateLiteral(dateSample2)}",
                x => !(x.Date != null && dateSample1 <= x.Date.Value && x.Date.Value <= dateSample2)
            },

            {
                $"{integerField} BETWEEN {integerSample1} AND {integerSample2}",
                x => x.Integer != null && integerSample1 <= x.Integer.Value && x.Integer.Value <= integerSample2
            },
            {
                $"{integerField} NOT BETWEEN {integerSample1} AND {integerSample2}",
                x => !(x.Integer != null && integerSample1 <= x.Integer.Value && x.Integer.Value <= integerSample2)
            },

            {
                $"{stringField} BETWEEN {CreateLiteral(stringSample1)} AND {CreateLiteral(stringSample2)}",
                x => x.String != null && stringComparer.Compare(stringSample1, x.String) <= 0 && stringComparer.Compare(x.String, stringSample2) <= 0
            },
            {
                $"{stringField} NOT BETWEEN {CreateLiteral(stringSample1)} AND {CreateLiteral(stringSample2)}",
                x => !(x.String != null && stringComparer.Compare(stringSample1, x.String) <= 0 && stringComparer.Compare(x.String, stringSample2) <= 0)
            },
        };
    }

    public static IEnumerable<Object[]> Data3()
    {
        var dateSample1 = Samples.Instance.Dates[1 * Samples.Instance.Dates.Count / 4];
        var dateSample2 = Samples.Instance.Dates[2 * Samples.Instance.Dates.Count / 4];
        var dateSample3 = Samples.Instance.Dates[3 * Samples.Instance.Dates.Count / 4];
        var dateField = nameof(TestModel.Date);

        var integerSample1 = Samples.Instance.Integers[1 * Samples.Instance.Integers.Count / 4];
        var integerSample2 = Samples.Instance.Integers[2 * Samples.Instance.Integers.Count / 4];
        var integerSample3 = Samples.Instance.Integers[3 * Samples.Instance.Integers.Count / 4];
        var integerField = nameof(TestModel.Integer);

        var stringSample1 = Samples.Instance.Strings[1 * Samples.Instance.Strings.Count / 4];
        var stringSample2 = Samples.Instance.Strings[2 * Samples.Instance.Strings.Count / 4];
        var stringSample3 = Samples.Instance.Strings[3 * Samples.Instance.Strings.Count / 4];
        var stringField = nameof(TestModel.String);

        var stringComparer = StringComparer.Ordinal;

        return new TheoryData<String, Func<TestModel, Boolean>> {
            {
                $"{dateField} BETWEEN {CreateLiteral(dateSample1)} AND {CreateLiteral(dateSample3)} AND {dateField} != {CreateLiteral(dateSample2)}",
                x => x.Date != null && dateSample1 <= x.Date.Value && x.Date.Value <= dateSample3 && x.Date.Value != dateSample2
            },
            {
                $"{dateField} NOT BETWEEN {CreateLiteral(dateSample1)} AND {CreateLiteral(dateSample3)} OR {dateField} == {CreateLiteral(dateSample2)}",
                x => !(x.Date != null && dateSample1 <= x.Date.Value && x.Date.Value <= dateSample3 && x.Date.Value != dateSample2)
            },

            {
                $"{integerField} BETWEEN {integerSample1} AND {integerSample3} AND {integerField} != {integerSample2}",
                x => x.Integer != null && integerSample1 <= x.Integer.Value && x.Integer.Value <= integerSample3 && x.Integer.Value != integerSample2
            },
            {
                $"{integerField} NOT BETWEEN {integerSample1} AND {integerSample3} OR {integerField} == {integerSample2}",
                x => !(x.Integer != null && integerSample1 <= x.Integer.Value && x.Integer.Value <= integerSample3 && x.Integer.Value != integerSample2)
            },

            {
                $"{stringField} BETWEEN {CreateLiteral(stringSample1)} AND {CreateLiteral(stringSample3)} AND {stringField} != {CreateLiteral(stringSample2)}",
                x => x.String != null && stringComparer.Compare(stringSample1, x.String) <= 0 && stringComparer.Compare(x.String, stringSample3) <= 0 && stringComparer.Compare(x.String, stringSample2) != 0
            },
            {
                $"{stringField} NOT BETWEEN {CreateLiteral(stringSample1)} AND {CreateLiteral(stringSample3)} OR {stringField} == {CreateLiteral(stringSample2)}",
                x => !(x.String != null && stringComparer.Compare(stringSample1, x.String) <= 0 && stringComparer.Compare(x.String, stringSample3) <= 0 && stringComparer.Compare(x.String, stringSample2) != 0)
            },
        };
    }

    public static IEnumerable<Object[]> Data4()
    {
        var dateSample1 = Samples.Instance.Dates[1 * Samples.Instance.Dates.Count / 4];
        var dateSample2 = Samples.Instance.Dates[2 * Samples.Instance.Dates.Count / 4];
        var dateSample3 = Samples.Instance.Dates[3 * Samples.Instance.Dates.Count / 4];
        var dateField = nameof(TestModel.Date);

        var integerSample1 = Samples.Instance.Integers[1 * Samples.Instance.Integers.Count / 4];
        var integerSample2 = Samples.Instance.Integers[2 * Samples.Instance.Integers.Count / 4];
        var integerSample3 = Samples.Instance.Integers[3 * Samples.Instance.Integers.Count / 4];
        var integerField = nameof(TestModel.Integer);

        var stringSample1 = Samples.Instance.Strings[1 * Samples.Instance.Strings.Count / 4];
        var stringSample2 = Samples.Instance.Strings[2 * Samples.Instance.Strings.Count / 4];
        var stringSample3 = Samples.Instance.Strings[3 * Samples.Instance.Strings.Count / 4];
        var stringField = nameof(TestModel.String);

        var stringComparer = StringComparer.Ordinal;

        return new TheoryData<String, Func<TestModel, Boolean>> {
            {
                $"{dateField} IN ({CreateLiteral(dateSample1)}, {CreateLiteral(dateSample2)}, {CreateLiteral(dateSample3)})",
                x => x.Date == dateSample1 || x.Date == dateSample2 || x.Date == dateSample3
            },
            {
                $"{dateField} NOT IN ({CreateLiteral(dateSample1)}, {CreateLiteral(dateSample2)}, {CreateLiteral(dateSample3)})",
                x => !(x.Date == dateSample1 || x.Date == dateSample2 || x.Date == dateSample3)
            },

            {
                $"{integerField} IN ({integerSample1}, {integerSample2}, {integerSample3})",
                x => x.Integer == integerSample1 || x.Integer == integerSample2 || x.Integer == integerSample3
            },
            {
                $"{integerField} NOT IN ({integerSample1}, {integerSample2}, {integerSample3})",
                x => !(x.Integer == integerSample1 || x.Integer == integerSample2 || x.Integer == integerSample3)
            },

            {
                $"{stringField} IN ({CreateLiteral(stringSample1)}, {CreateLiteral(stringSample2)}, {CreateLiteral(stringSample3)})",
                x => x.String == stringSample1 || x.String == stringSample2 || x.String == stringSample3
            },
            {
                $"{stringField} NOT IN ({CreateLiteral(stringSample1)}, {CreateLiteral(stringSample2)}, {CreateLiteral(stringSample3)})",
                x => !(x.String == stringSample1 || x.String == stringSample2 || x.String == stringSample3)
            },
        };
    }

    public static IEnumerable<Object[]> Data5()
    {
        var missingField = nameof(TestModel.Missing);

        return new TheoryData<String, Func<TestModel, Boolean>> {
            {
                $"{missingField} EXIST",
                x => x.Missing != null
            },
            {
                $"{missingField} NOT EXIST",
                x => x.Missing == null
            },
        };
    }

    public static IEnumerable<Object[]> Data6()
    {
        var stringField = nameof(TestModel.String);

        var regex = new Regex("abc", RegexOptions.IgnoreCase);

        return new TheoryData<String, Func<TestModel, Boolean>> {
            {
                $"{stringField} MATCH /abc/i",
                x => x.String != null && regex.IsMatch(x.String)
            },
            {
                $"{stringField} NOT MATCH /abc/i",
                x => !(x.String != null && regex.IsMatch(x.String))
            },

            {
                $"{stringField} MATCH \"abc\" OPTIONS \"i\"",
                x => x.String != null && regex.IsMatch(x.String)
            },
            {
                $"{stringField} NOT MATCH \"abc\" OPTIONS \"i\"",
                x => !(x.String != null && regex.IsMatch(x.String))
            },
        };
    }

    public static IEnumerable<Object[]> Data7()
    {
        var objectField = nameof(TestModel.Object);

        return new TheoryData<String, Func<TestModel, Boolean>> {
            {
                $"TYPEOF {objectField} == \"{MongoTypeAliases.Array}\"",
                x => x.Object is Array && x.Object is not Byte[]
            },
            {
                $"TYPEOF {objectField} != \"{MongoTypeAliases.Array}\"",
                x => !(x.Object is Array && x.Object is not Byte[])
            },

            {
                $"TYPEOF {objectField} == \"{MongoTypeAliases.BinaryData}\"",
                x => x.Object is Byte[]
            },
            {
                $"TYPEOF {objectField} != \"{MongoTypeAliases.BinaryData}\"",
                x => !(x.Object is Byte[])
            },

            {
                $"TYPEOF {objectField} == \"{MongoTypeAliases.Boolean}\"",
                x => x.Object is Boolean
            },
            {
                $"TYPEOF {objectField} != \"{MongoTypeAliases.Boolean}\"",
                x => !(x.Object is Boolean)
            },

            {
                $"TYPEOF {objectField} == \"{MongoTypeAliases.Date}\"",
                x => x.Object is DateTime
            },
            {
                $"TYPEOF {objectField} != \"{MongoTypeAliases.Date}\"",
                x => !(x.Object is DateTime)
            },

            {
                $"TYPEOF {objectField} == \"{MongoTypeAliases.Decimal128}\"",
                x => x.Object is Decimal
            },
            {
                $"TYPEOF {objectField} != \"{MongoTypeAliases.Decimal128}\"",
                x => !(x.Object is Decimal)
            },

            {
                $"TYPEOF {objectField} == \"{MongoTypeAliases.Document}\"",
                x => x.Object != null && x.Object is not String && x.Object.GetType().IsClass && !x.Object.GetType().IsArray
            },
            {
                $"TYPEOF {objectField} != \"{MongoTypeAliases.Document}\"",
                x => !(x.Object != null && x.Object is not String && x.Object.GetType().IsClass && !x.Object.GetType().IsArray)
            },

            {
                $"TYPEOF {objectField} == \"{MongoTypeAliases.Int32}\"",
                x => x.Object is Int32
            },
            {
                $"TYPEOF {objectField} != \"{MongoTypeAliases.Int32}\"",
                x => !(x.Object is Int32)
            },

            {
                $"TYPEOF {objectField} == \"{MongoTypeAliases.Null}\"",
                x => x.Object == null
            },
            {
                $"TYPEOF {objectField} != \"{MongoTypeAliases.Null}\"",
                x => !(x.Object == null)
            },

            {
                $"TYPEOF {objectField} == \"{MongoTypeAliases.Number}\"",
                x => x.Object is Decimal || x.Object is Double || x.Object is Int32 || x.Object is Int64
            },
            {
                $"TYPEOF {objectField} != \"{MongoTypeAliases.Number}\"",
                x => !(x.Object is Decimal || x.Object is Double || x.Object is Int32 || x.Object is Int64)
            },

            {
                $"TYPEOF {objectField} == \"{MongoTypeAliases.ObjectId}\"",
                x => x.Object is ObjectId
            },
            {
                $"TYPEOF {objectField} != \"{MongoTypeAliases.ObjectId}\"",
                x => !(x.Object is ObjectId)
            },

            {
                $"TYPEOF {objectField} == \"{MongoTypeAliases.String}\"",
                x => x.Object is String
            },
            {
                $"TYPEOF {objectField} != \"{MongoTypeAliases.String}\"",
                x => !(x.Object is String)
            },
        };
    }

    public static IEnumerable<Object[]> Data8()
    {
        var objectField = nameof(TestModel.Object);

        return new TheoryData<String, Func<TestModel, Boolean>> {
            {
                $"TYPEOF {objectField} IN (\"{MongoTypeAliases.Array}\")",
                x => x.Object is Array && x.Object is not Byte[]
            },
            {
                $"TYPEOF {objectField} NOT IN (\"{MongoTypeAliases.Array}\")",
                x => !(x.Object is Array && x.Object is not Byte[])
            },

            {
                $"TYPEOF {objectField} IN (\"{MongoTypeAliases.BinaryData}\")",
                x => x.Object is Byte[]
            },
            {
                $"TYPEOF {objectField} NOT IN (\"{MongoTypeAliases.BinaryData}\")",
                x => !(x.Object is Byte[])
            },

            {
                $"TYPEOF {objectField} IN (\"{MongoTypeAliases.Boolean}\")",
                x => x.Object is Boolean
            },
            {
                $"TYPEOF {objectField} NOT IN (\"{MongoTypeAliases.Boolean}\")",
                x => !(x.Object is Boolean)
            },

            {
                $"TYPEOF {objectField} IN (\"{MongoTypeAliases.Date}\")",
                x => x.Object is DateTime
            },
            {
                $"TYPEOF {objectField} NOT IN (\"{MongoTypeAliases.Date}\")",
                x => !(x.Object is DateTime)
            },

            {
                $"TYPEOF {objectField} IN (\"{MongoTypeAliases.Decimal128}\")",
                x => x.Object is Decimal
            },
            {
                $"TYPEOF {objectField} NOT IN (\"{MongoTypeAliases.Decimal128}\")",
                x => !(x.Object is Decimal)
            },

            {
                $"TYPEOF {objectField} IN (\"{MongoTypeAliases.Document}\")",
                x => x.Object != null && x.Object is not String && x.Object.GetType().IsClass && !x.Object.GetType().IsArray
            },
            {
                $"TYPEOF {objectField} NOT IN (\"{MongoTypeAliases.Document}\")",
                x => !(x.Object != null && x.Object is not String && x.Object.GetType().IsClass && !x.Object.GetType().IsArray)
            },

            {
                $"TYPEOF {objectField} IN (\"{MongoTypeAliases.Int32}\")",
                x => x.Object is Int32
            },
            {
                $"TYPEOF {objectField} NOT IN (\"{MongoTypeAliases.Int32}\")",
                x => !(x.Object is Int32)
            },

            {
                $"TYPEOF {objectField} IN (\"{MongoTypeAliases.Null}\")",
                x => x.Object == null
            },
            {
                $"TYPEOF {objectField} NOT IN (\"{MongoTypeAliases.Null}\")",
                x => !(x.Object == null)
            },

            {
                $"TYPEOF {objectField} IN (\"{MongoTypeAliases.Number}\")",
                x => x.Object is Decimal || x.Object is Double || x.Object is Int32 || x.Object is Int64
            },
            {
                $"TYPEOF {objectField} NOT IN (\"{MongoTypeAliases.Number}\")",
                x => !(x.Object is Decimal || x.Object is Double || x.Object is Int32 || x.Object is Int64)
            },

            {
                $"TYPEOF {objectField} IN (\"{MongoTypeAliases.ObjectId}\")",
                x => x.Object is ObjectId
            },
            {
                $"TYPEOF {objectField} NOT IN (\"{MongoTypeAliases.ObjectId}\")",
                x => !(x.Object is ObjectId)
            },

            {
                $"TYPEOF {objectField} IN (\"{MongoTypeAliases.String}\")",
                x => x.Object is String
            },
            {
                $"TYPEOF {objectField} NOT IN (\"{MongoTypeAliases.String}\")",
                x => !(x.Object is String)
            },
        };
    }

    public static IEnumerable<Object[]> Data9()
    {
        var dateSample1 = Samples.Instance.Dates[Samples.Instance.Dates.Count / 3];
        var dateSample2 = Samples.Instance.Dates[2 * Samples.Instance.Dates.Count / 3];
        var dateArrayField = nameof(TestModel.DateArray);

        var integerSample1 = Samples.Instance.Integers[Samples.Instance.Integers.Count / 3];
        var integerSample2 = Samples.Instance.Integers[2 * Samples.Instance.Integers.Count / 3];
        var integerArrayField = nameof(TestModel.IntegerArray);

        var stringSample1 = Samples.Instance.Strings[Samples.Instance.Strings.Count / 3];
        var stringSample2 = Samples.Instance.Strings[2 * Samples.Instance.Strings.Count / 3];
        var stringArrayField = nameof(TestModel.StringArray);

        var stringComparer = StringComparer.Ordinal;

        return new TheoryData<String, Func<TestModel, Boolean>> {
            {
                $"ANYOF {dateArrayField} IS ($ BETWEEN {CreateLiteral(dateSample1)} AND {CreateLiteral(dateSample2)})",
                x => x.DateArray != null && x.DateArray.Any(y => dateSample1 <= y && y <= dateSample2)
            },
            {
                $"ANYOF {dateArrayField} IS NOT ($ BETWEEN {CreateLiteral(dateSample1)} AND {CreateLiteral(dateSample2)})",
                x => !(x.DateArray != null && x.DateArray.Any(y => dateSample1 <= y && y <= dateSample2))
            },
            {
                $"ANYOF {dateArrayField} IS ($ NOT BETWEEN {CreateLiteral(dateSample1)} AND {CreateLiteral(dateSample2)})",
                x => x.DateArray != null && x.DateArray.Any(y => !(dateSample1 <= y && y <= dateSample2))
            },
            {
                $"ANYOF {dateArrayField} IS NOT ($ NOT BETWEEN {CreateLiteral(dateSample1)} AND {CreateLiteral(dateSample2)})",
                x => !(x.DateArray != null && x.DateArray.Any(y => !(dateSample1 <= y && y <= dateSample2)))
            },

            {
                $"ANYOF {integerArrayField} IS ($ BETWEEN {integerSample1} AND {integerSample2})",
                x => x.IntegerArray != null && x.IntegerArray.Any(y => integerSample1 <= y && y <= integerSample2)
            },
            {
                $"ANYOF {integerArrayField} IS NOT ($ BETWEEN {integerSample1} AND {integerSample2})",
                x => !(x.IntegerArray != null && x.IntegerArray.Any(y => integerSample1 <= y && y <= integerSample2))
            },
            {
                $"ANYOF {integerArrayField} IS ($ NOT BETWEEN {integerSample1} AND {integerSample2})",
                x => x.IntegerArray != null && x.IntegerArray.Any(y => !(integerSample1 <= y && y <= integerSample2))
            },
            {
                $"ANYOF {integerArrayField} IS NOT ($ NOT BETWEEN {integerSample1} AND {integerSample2})",
                x => !(x.IntegerArray != null && x.IntegerArray.Any(y => !(integerSample1 <= y && y <= integerSample2)))
            },

            {
                $"ANYOF {stringArrayField} IS ($ BETWEEN {CreateLiteral(stringSample1)} AND {CreateLiteral(stringSample2)})",
                x => x.StringArray != null && x.StringArray.Any(y => stringComparer.Compare(stringSample1, y) <= 0 && stringComparer.Compare(y, stringSample2) <= 0)
            },
            {
                $"ANYOF {stringArrayField} IS NOT ($ BETWEEN {CreateLiteral(stringSample1)} AND {CreateLiteral(stringSample2)})",
                x => !(x.StringArray != null && x.StringArray.Any(y => stringComparer.Compare(stringSample1, y) <= 0 && stringComparer.Compare(y, stringSample2) <= 0))
            },
            {
                $"ANYOF {stringArrayField} IS ($ NOT BETWEEN {CreateLiteral(stringSample1)} AND {CreateLiteral(stringSample2)})",
                x => x.StringArray != null && x.StringArray.Any(y => !(stringComparer.Compare(stringSample1, y) <= 0 && stringComparer.Compare(y, stringSample2) <= 0))
            },
            {
                $"ANYOF {stringArrayField} IS NOT ($ NOT BETWEEN {CreateLiteral(stringSample1)} AND {CreateLiteral(stringSample2)})",
                x => !(x.StringArray != null && x.StringArray.Any(y => !(stringComparer.Compare(stringSample1, y) <= 0 && stringComparer.Compare(y, stringSample2) <= 0)))
            },
        };
    }

    public static IEnumerable<Object[]> Data10()
    {
        var innerArrayField = nameof(TestModel.InnerArray);

        var dateSample1 = Samples.Instance.Dates[Samples.Instance.Dates.Count / 3];
        var dateSample2 = Samples.Instance.Dates[2 * Samples.Instance.Dates.Count / 3];
        var dateField = nameof(SimpleModel.Date);

        var integerSample1 = Samples.Instance.Integers[Samples.Instance.Integers.Count / 3];
        var integerSample2 = Samples.Instance.Integers[2 * Samples.Instance.Integers.Count / 3];
        var integerField = nameof(SimpleModel.Integer);

        var stringSample1 = Samples.Instance.Strings[Samples.Instance.Strings.Count / 3];
        var stringSample2 = Samples.Instance.Strings[2 * Samples.Instance.Strings.Count / 3];
        var stringField = nameof(SimpleModel.String);

        var stringComparer = StringComparer.Ordinal;

        return new TheoryData<String, Func<TestModel, Boolean>> {
            {
                $"ANYOF {innerArrayField} IS ({dateField} BETWEEN {CreateLiteral(dateSample1)} AND {CreateLiteral(dateSample2)})",
                x => x.InnerArray != null && x.InnerArray.Any(y => dateSample1 <= y.Date && y.Date <= dateSample2)
            },
            {
                $"ANYOF {innerArrayField} IS NOT ({dateField} BETWEEN {CreateLiteral(dateSample1)} AND {CreateLiteral(dateSample2)})",
                x => !(x.InnerArray != null && x.InnerArray.Any(y => dateSample1 <= y.Date && y.Date <= dateSample2))
            },
            {
                $"ANYOF {innerArrayField} IS ({dateField} NOT BETWEEN {CreateLiteral(dateSample1)} AND {CreateLiteral(dateSample2)})",
                x => x.InnerArray != null && x.InnerArray.Any(y => !(dateSample1 <= y.Date && y.Date <= dateSample2))
            },
            {
                $"ANYOF {innerArrayField} IS NOT ({dateField} NOT BETWEEN {CreateLiteral(dateSample1)} AND {CreateLiteral(dateSample2)})",
                x => !(x.InnerArray != null && x.InnerArray.Any(y => !(dateSample1 <= y.Date && y.Date <= dateSample2)))
            },

            {
                $"ANYOF {innerArrayField} IS ({integerField} BETWEEN {integerSample1} AND {integerSample2})",
                x => x.InnerArray != null && x.InnerArray.Any(y => integerSample1 <= y.Integer && y.Integer <= integerSample2)
            },
            {
                $"ANYOF {innerArrayField} IS NOT ({integerField} BETWEEN {integerSample1} AND {integerSample2})",
                x => !(x.InnerArray != null && x.InnerArray.Any(y => integerSample1 <= y.Integer && y.Integer <= integerSample2))
            },
            {
                $"ANYOF {innerArrayField} IS ({integerField} NOT BETWEEN {integerSample1} AND {integerSample2})",
                x => x.InnerArray != null && x.InnerArray.Any(y => !(integerSample1 <= y.Integer && y.Integer <= integerSample2))
            },
            {
                $"ANYOF {innerArrayField} IS NOT ({integerField} NOT BETWEEN {integerSample1} AND {integerSample2})",
                x => !(x.InnerArray != null && x.InnerArray.Any(y => !(integerSample1 <= y.Integer && y.Integer <= integerSample2)))
            },

            {
                $"ANYOF {innerArrayField} IS ({stringField} BETWEEN {CreateLiteral(stringSample1)} AND {CreateLiteral(stringSample2)})",
                x => x.InnerArray != null && x.InnerArray.Any(y => stringComparer.Compare(stringSample1, y.String) <= 0 && stringComparer.Compare(y.String, stringSample2) <= 0)
            },
            {
                $"ANYOF {innerArrayField} IS NOT ({stringField} BETWEEN {CreateLiteral(stringSample1)} AND {CreateLiteral(stringSample2)})",
                x => !(x.InnerArray != null && x.InnerArray.Any(y => stringComparer.Compare(stringSample1, y.String) <= 0 && stringComparer.Compare(y.String, stringSample2) <= 0))
            },
            {
                $"ANYOF {innerArrayField} IS ({stringField} NOT BETWEEN {CreateLiteral(stringSample1)} AND {CreateLiteral(stringSample2)})",
                x => x.InnerArray != null && x.InnerArray.Any(y => !(stringComparer.Compare(stringSample1, y.String) <= 0 && stringComparer.Compare(y.String, stringSample2) <= 0))
            },
            {
                $"ANYOF {innerArrayField} IS NOT ({stringField} NOT BETWEEN {CreateLiteral(stringSample1)} AND {CreateLiteral(stringSample2)})",
                x => !(x.InnerArray != null && x.InnerArray.Any(y => !(stringComparer.Compare(stringSample1, y.String) <= 0 && stringComparer.Compare(y.String, stringSample2) <= 0)))
            },
        };
    }

    [Theory]
    [MemberData(nameof(Data0))]
    [MemberData(nameof(Data1))]
    [MemberData(nameof(Data2))]
    [MemberData(nameof(Data3))]
    [MemberData(nameof(Data4))]
    [MemberData(nameof(Data5))]
    [MemberData(nameof(Data6))]
    [MemberData(nameof(Data7))]
    [MemberData(nameof(Data8))]
    [MemberData(nameof(Data9))]
    [MemberData(nameof(Data10))]
    public async Task ParseAndCreate_CreatesPredictableFilterAsync(String filterText, Func<TestModel, Boolean> samplesFilter)
    {
        // Arrange
        var filterDocument = FluentFilter.Parse(filterText).Create();

        // Act
        var foundIds = await m_fixture.Collection.Find(filterDocument).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);

        // Assert
        foundIds.Should().BeEquivalentTo(Samples.Instance.Data.Where(samplesFilter).Select(x => x.Id));
    }

    public static IEnumerable<Object[]> TwoFiltersData()
    {
        var integerSample1 = Samples.Instance.Integers[1 * Samples.Instance.Integers.Count / 4];
        var integerSample2 = Samples.Instance.Integers[2 * Samples.Instance.Integers.Count / 4];
        var integerSample3 = Samples.Instance.Integers[3 * Samples.Instance.Integers.Count / 4];
        var integerField = nameof(TestModel.Integer);

        var filters = new[] {
            $"{integerField} <= {integerSample1}",
            $"{integerField} <= {integerSample2}",
            $"{integerField} <= {integerSample3}",
            $"{integerField} >= {integerSample1}",
            $"{integerField} >= {integerSample2}",
            $"{integerField} >= {integerSample3}",
            $"NOT ({integerField} <= {integerSample1})",
            $"NOT ({integerField} <= {integerSample2})",
            $"NOT ({integerField} <= {integerSample3})",
            $"NOT ({integerField} >= {integerSample1})",
            $"NOT ({integerField} >= {integerSample2})",
            $"NOT ({integerField} >= {integerSample3})",
        };

        foreach (var filter1 in filters)
        {
            foreach (var filter2 in filters)
            {
                yield return new Object[] { filter1, filter2 };
            }
        }
    }

    [Theory]
    [MemberData(nameof(TwoFiltersData))]
    public async Task ParseAndCreate_Conjunction_CreatesPredictableFilterAsync(String filterText1, String filterText2)
    {
        // Arrange
        var filterDocument1 = FluentFilter.Parse(filterText1).Create();
        var filterDocument2 = FluentFilter.Parse(filterText2).Create();
        var conjunctionDocument = FluentFilter.Parse($"({filterText1}) AND ({filterText2})").Create();

        // Act
        var foundIds1 = await m_fixture.Collection.Find(filterDocument1).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var foundIds2 = await m_fixture.Collection.Find(filterDocument2).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var conjunctionIds = await m_fixture.Collection.Find(conjunctionDocument).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);

        // Assert
        conjunctionIds.Should().BeEquivalentTo(foundIds1.Intersect(foundIds2));
    }

    [Theory]
    [MemberData(nameof(TwoFiltersData))]
    public async Task ParseAndCreate_Disjunction_CreatesPredictableFilterAsync(String filterText1, String filterText2)
    {
        // Arrange
        var filterDocument1 = FluentFilter.Parse(filterText1).Create();
        var filterDocument2 = FluentFilter.Parse(filterText2).Create();
        var disjunctionDocument = FluentFilter.Parse($"({filterText1}) OR ({filterText2})").Create();

        // Act
        var foundIds1 = await m_fixture.Collection.Find(filterDocument1).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var foundIds2 = await m_fixture.Collection.Find(filterDocument2).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var disjunctionIds = await m_fixture.Collection.Find(disjunctionDocument).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);

        // Assert
        disjunctionIds.Should().BeEquivalentTo(foundIds1.Union(foundIds2));
    }

    public static IEnumerable<Object[]> OneFilterData()
    {
        var integerSample1 = Samples.Instance.Integers[1 * Samples.Instance.Integers.Count / 4];
        var integerSample2 = Samples.Instance.Integers[2 * Samples.Instance.Integers.Count / 4];
        var integerSample3 = Samples.Instance.Integers[3 * Samples.Instance.Integers.Count / 4];
        var integerField = nameof(TestModel.Integer);

        var filters = new[] {
            $"{integerField} <= {integerSample1}",
            $"{integerField} <= {integerSample2}",
            $"{integerField} <= {integerSample3}",
            $"{integerField} >= {integerSample1}",
            $"{integerField} >= {integerSample2}",
            $"{integerField} >= {integerSample3}",
            $"NOT ({integerField} <= {integerSample1})",
            $"NOT ({integerField} <= {integerSample2})",
            $"NOT ({integerField} <= {integerSample3})",
            $"NOT ({integerField} >= {integerSample1})",
            $"NOT ({integerField} >= {integerSample2})",
            $"NOT ({integerField} >= {integerSample3})",
        };

        foreach (var filter1 in filters)
        {
            yield return new Object[] { filter1 };

            foreach (var filter2 in filters)
            {
                yield return new Object[] { $"{filter1} AND {filter2}" };
                yield return new Object[] { $"{filter1} OR {filter2}" };
            }
        }
    }

    [Theory]
    [MemberData(nameof(OneFilterData))]
    public async Task ParseAndCreate_Negation_CreatesPredictableFilterAsync(String filterText)
    {
        // Arrange
        var positiveFilter = FluentFilter.Parse(filterText).Create();
        var negativeFilter = FluentFilter.Parse($"NOT ({filterText})").Create();

        // Act
        var positiveIds = await m_fixture.Collection.Find(positiveFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var negativeIds = await m_fixture.Collection.Find(negativeFilter).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);
        var allIds = await m_fixture.Collection.Find(new BsonDocument()).Project(s_projectId).ToListAsync(m_cancellationTokenSource.Token);

        // Assert
        allIds.Should().HaveCount(positiveIds.Count + negativeIds.Count);

        positiveIds.Should().NotIntersectWith(negativeIds);
        positiveIds.Union(negativeIds).Should().BeEquivalentTo(allIds);
    }

    private static String CreateLiteral(DateTime value)
    {
        return value.ToString("#yyyy-MM-dd HH:mm:ss.fff#");
    }

    private static String CreateLiteral(String value)
    {
        return String.Concat("\"", value.Replace("\"", "\"\""), "\"");
    }
}
