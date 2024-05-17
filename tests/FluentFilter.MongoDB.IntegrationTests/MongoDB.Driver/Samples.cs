using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Models;

namespace MongoDB.Driver;

public class Samples
{
    public static readonly Samples Instance = new Samples();

    private Samples()
    {
        const Int32 Count = 16;

        var range1 = Enumerable.Range(0, Count).ToArray();
        var range2 = Enumerable.Range(0, Count - 1).ToArray();
        var range3 = Enumerable.Range(0, Count - 2).ToArray();
        var range4 = Enumerable.Range(0, Count - 3).ToArray();

        var startDate = new DateTime(2013, 09, 18, 00, 00, 00, DateTimeKind.Utc);
        var chars = new String("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+/".ToCharArray().OrderBy(x => x).ToArray());
        var bytes = Enumerable.Range(0, 64).Select(x => (Byte)(4 * x)).ToArray();

        var booleans = new[] { false, true };
        var binaries = range1.Select(x => new ArraySegment<Byte>(bytes, 4 * x, 2 * (Count - x)).ToArray()).ToArray();
        var dates = range1.Select(x => startDate.AddMonths(x)).ToArray();
        var decimals = range1.Select(x => 1000m + 100m * x).ToArray();
        var integers = range1.Select(x => 100 + 10 * x).ToArray();
        var guids = range1.Select(x => new Guid(Enumerable.Range(0, 16).Select(y => (Byte)(15 * x)).ToArray())).ToArray();
        var strings = range1.Select(x => chars.Substring(4 * x, 2 * (Count - x))).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var objects = new IEnumerable<Object>[] {
            new Object[] { null },
            new Object[] { Array.Empty<Object>() },
            booleans.Select(x => (Object)x),
            binaries.Select(x => (Object)x).Take(1),
            dates.Select(x => (Object)x).Take(1),
            decimals.Select(x => (Object)x).Take(1),
            integers.Select(x =>(Object) x).Take(1),
            guids.Select(x =>(Object) x).Take(1),
            strings.Select(x =>(Object) x).Take(1),
        }
        .SelectMany(x => x).ToArray();
        objects = objects.Concat(objects.Select(x => new[] { x })).ToArray();

        var inners = range1.Select(x => new SimpleModel {
            Binary = (x < binaries.Length) ? binaries[x] : null,
            Boolean = (x < booleans.Length) ? booleans[x] : null,
            Date = dates[x],
            Decimal = decimals[x],
            Integer = integers[x],
            Guid = guids[x],
            Missing = (x < booleans.Length) ? booleans[x] : null,
            String = strings[x],
            Object = (x < objects.Length) ? objects[x] : null,
        }).ToArray();

        objects = objects.Append(inners[0]).ToArray();

        var booleanArrays = new[] {
            new[] { false },
            new[] { true },
            new[] { false, true },
            new[] { true, false },
        };

        var dateArrays = new[] {
            range1.Select(x => new[] { dates[x] }),
            range2.Select(x => new[] { dates[x], dates[x + 1] }),
            range3.Select(x => new[] { dates[x], dates[x + 1], dates[x + 2] }),
            range4.Select(x => new[] { dates[x], dates[x + 1], dates[x + 2], dates[x + 3] }),
        }.SelectMany(x => x).ToArray();

        var binaryArrays = new[] {
            range1.Select(x => new[] { binaries[x] }),
            range2.Select(x => new[] { binaries[x], binaries[x + 1] }),
            range3.Select(x => new[] { binaries[x], binaries[x + 1], binaries[x + 2] }),
            range4.Select(x => new[] { binaries[x], binaries[x + 1], binaries[x + 2], binaries[x + 3] }),
        }.SelectMany(x => x).ToArray();

        var decimalArrays = new[] {
            range1.Select(x => new[] { decimals[x] }),
            range2.Select(x => new[] { decimals[x], decimals[x + 1] }),
            range3.Select(x => new[] { decimals[x], decimals[x + 1], decimals[x + 2] }),
            range4.Select(x => new[] { decimals[x], decimals[x + 1], decimals[x + 2], decimals[x + 3] }),
        }.SelectMany(x => x).ToArray();

        var integerArrays = new[] {
            range1.Select(x => new[] { integers[x] }),
            range2.Select(x => new[] { integers[x], integers[x + 1] }),
            range3.Select(x => new[] { integers[x], integers[x + 1], integers[x + 2] }),
            range4.Select(x => new[] { integers[x], integers[x + 1], integers[x + 2], integers[x + 3] }),
        }.SelectMany(x => x).ToArray();

        var guidArrays = new[] {
            range1.Select(x => new[] { guids[x] }),
            range2.Select(x => new[] { guids[x], guids[x + 1] }),
            range3.Select(x => new[] { guids[x], guids[x + 1], guids[x + 2] }),
            range4.Select(x => new[] { guids[x], guids[x + 1], guids[x + 2], guids[x + 3] }),
        }.SelectMany(x => x).ToArray();

        var objectArrays = new[] {
            range1.Select(x => new[] { objects[x] }),
            range2.Select(x => new[] { objects[x], objects[x + 1] }),
            range3.Select(x => new[] { objects[x], objects[x + 1], objects[x + 2] }),
            range4.Select(x => new[] { objects[x], objects[x + 1], objects[x + 2], objects[x + 3] }),
        }.SelectMany(x => x).ToArray();

        var stringArrays = new[] {
            range1.Select(x => new[] { strings[x] }),
            range2.Select(x => new[] { strings[x], strings[x + 1] }),
            range3.Select(x => new[] { strings[x], strings[x + 1], strings[x + 2] }),
            range4.Select(x => new[] { strings[x], strings[x + 1], strings[x + 2], strings[x + 3] }),
        }.SelectMany(x => x).ToArray();

        var innerArrays = new[] {
            range1.Select(x => new[] { inners[x] }),
            range2.Select(x => new[] { inners[x], inners[x + 1] }),
            range3.Select(x => new[] { inners[x], inners[x + 1], inners[x + 2] }),
            range4.Select(x => new[] { inners[x], inners[x + 1], inners[x + 2], inners[x + 3] }),
        }.SelectMany(x => x).ToArray();

        var len = dateArrays.Length;
        var data = new List<TestModel>(len + 2);

        for (var i = 0; i < len; i++)
        {
            data.Add(new TestModel {
                Id = ObjectId.GenerateNewId().ToString(),
                Binary = GetClass(binaries, i),
                Boolean = GetStruct(booleans, i),
                Date = GetStruct(dates, i),
                Decimal = GetStruct(decimals, i),
                Integer = GetStruct(integers, i),
                Guid = GetStruct(guids, i),
                Missing = GetStruct(booleans, i),
                String = GetClass(strings, i),
                Object = GetClass(strings, i),
                BooleanArray = GetClass(booleanArrays, i),
                DateArray = GetClass(dateArrays, i),
                DecimalArray = GetClass(decimalArrays, i),
                IntegerArray = GetClass(integerArrays, i),
                GuidArray = GetClass(guidArrays, i),
                StringArray = GetClass(stringArrays, i),
                ObjectArray = GetClass(objectArrays, i),
                Inner = new ComplexModel {
                    Binary = GetClass(binaries, i),
                    Boolean = GetStruct(booleans, i),
                    Date = GetStruct(dates, i),
                    Decimal = GetStruct(decimals, i),
                    Integer = GetStruct(integers, i),
                    Guid = GetStruct(guids, i),
                    Missing = GetStruct(booleans, i),
                    String = GetClass(strings, i),
                    Object = GetClass(strings, i),
                    BinaryArray = GetClass(binaryArrays, i),
                    BooleanArray = GetClass(booleanArrays, i),
                    DateArray = GetClass(dateArrays, i),
                    DecimalArray = GetClass(decimalArrays, i),
                    IntegerArray = GetClass(integerArrays, i),
                    GuidArray = GetClass(guidArrays, i),
                    StringArray = GetClass(stringArrays, i),
                    ObjectArray = GetClass(objectArrays, i),
                },
                InnerArray = GetClass(innerArrays, i),
            });
        }

        data.Add(new TestModel {
            Id = ObjectId.GenerateNewId().ToString(),
        });

        data.Add(new TestModel {
            Id = ObjectId.GenerateNewId().ToString(),
            Binary = Array.Empty<Byte>(),
            Boolean = default(Boolean),
            Date = default(DateTime),
            Decimal = default(Decimal),
            Integer = default(Int32),
            Guid = default(Guid),
            Missing = default(Boolean),
            String = String.Empty,
            BooleanArray = Array.Empty<Boolean>(),
            DateArray = Array.Empty<DateTime>(),
            DecimalArray = Array.Empty<Decimal>(),
            IntegerArray = Array.Empty<Int32>(),
            StringArray = Array.Empty<String>(),
            Inner = new ComplexModel {
                Binary = Array.Empty<Byte>(),
                Boolean = default(Boolean),
                Date = default(DateTime),
                Decimal = default(Decimal),
                Integer = default(Int32),
                Guid = default(Guid),
                Missing = default(Boolean),
                String = String.Empty,
                BooleanArray = Array.Empty<Boolean>(),
                DateArray = Array.Empty<DateTime>(),
                DecimalArray = Array.Empty<Decimal>(),
                IntegerArray = Array.Empty<Int32>(),
                StringArray = Array.Empty<String>(),
            },
            InnerArray = Array.Empty<SimpleModel>(),
        });

        Data = Array.AsReadOnly(data.ToArray());
        Binaries = Array.AsReadOnly(binaries);
        Booleans = Array.AsReadOnly(booleans);
        Dates = Array.AsReadOnly(dates);
        Decimals = Array.AsReadOnly(decimals);
        Integers = Array.AsReadOnly(integers);
        Guids = Array.AsReadOnly(guids);
        Strings = Array.AsReadOnly(strings);
    }

    private static T? GetStruct<T>(T[] array, Int32 index)
        where T : struct
    {
        return (index < array.Length) ? array[index] : null;
    }

    private static T GetClass<T>(T[] array, Int32 index)
        where T : class
    {
        return (index < array.Length) ? array[index] : null;
    }

    public IReadOnlyList<TestModel> Data { get; }

    public IReadOnlyList<Byte[]> Binaries { get; }

    public IReadOnlyList<Boolean> Booleans { get; }

    public IReadOnlyList<DateTime> Dates { get; }

    public IReadOnlyList<Decimal> Decimals { get; }

    public IReadOnlyList<Int32> Integers { get; }

    public IReadOnlyList<Guid> Guids { get; }

    public IReadOnlyList<String> Strings { get; }
}
