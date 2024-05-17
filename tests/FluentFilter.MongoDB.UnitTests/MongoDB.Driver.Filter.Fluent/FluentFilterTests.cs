using System;
using System.Collections.Generic;
using FluentAssertions;
using MongoDB.Bson;
using Moq;
using Xunit;

namespace MongoDB.Driver.Filter.Fluent;

#pragma warning disable IDE0042 // Deconstruct variable declaration
public class FluentFilterTests
{
    public static IEnumerable<(String text, String value)> ValidPathes()
    {
        yield return ("$", null);
        yield return ("A.0.C", "A.0.C");
        yield return ("`$.A`", "$.A");
    }

    public static IEnumerable<(String text, String value)> ComparisonOperators()
    {
        yield return ("<", MongoOperators.Lt);
        yield return ("<=", MongoOperators.Lte);
        yield return (">", MongoOperators.Gt);
        yield return (">=", MongoOperators.Gte);
        yield return ("==", MongoOperators.Eq);
        yield return ("!=", MongoOperators.Neq);
    }

    public static IEnumerable<(Func<String, String> apply, Boolean value)> OuterNegations()
    {
        yield return (x => x, false);
        yield return (x => String.Concat("not (", x, ")"), true);
        yield return (x => String.Concat("not (not (", x, "))"), false);
    }

    public static IEnumerable<(String text, Boolean value)> InnerNegations()
    {
        yield return ("", false);
        yield return ("not", true);
    }

    public static IEnumerable<(String text, BsonValue value)> ValidValues()
    {
        // no need to test all possible literals since we verified that in scanner tests
        yield return ("\"ABC\"", new BsonString("ABC"));
    }

    public static IEnumerable<(String text, BsonValue value)> ValidValues2()
    {
        // no need to test all possible literals since we verified that in scanner tests
        yield return ("\"BCD\"", new BsonString("BCD"));
    }

    public static IEnumerable<(String text, IEnumerable<KeyValuePair<String, BsonValue>> expressions, BsonDocument document)> ValidSubFilters()
    {
        const String ExpressionText0 = "Expression^Text";
        const String ExpressionText1 = "Expression+Text";
        const String ExpressionText2 = "Expression-Text";
        const String ExpressionText3 = "Text^Expression";
        const String ExpressionText4 = "Text+Expression";
        const String ExpressionText5 = "Text-Expression";

        yield return (
            "A <= 123",
            new Dictionary<String, BsonValue>(),
            MongoFilterBuilder.Positive.CreateComparison("A", MongoOperators.Lte, new BsonDecimal128(123)));

        yield return (
            $"A <= ${{{ExpressionText0}}}",
            new Dictionary<String, BsonValue> { { ExpressionText0, new BsonDecimal128(123) } },
            MongoFilterBuilder.Positive.CreateComparison("A", MongoOperators.Lte, new BsonDecimal128(123)));

        yield return (
            "$ BETWEEN 12 AND 13",
            new Dictionary<String, BsonValue>(),
            MongoFilterBuilder.Positive.CreateBetween(null, new BsonDecimal128(12), new BsonDecimal128(13)));

        yield return (
            $"$ BETWEEN 12 AND ${{{ExpressionText2}}}",
            new Dictionary<String, BsonValue> { { ExpressionText2, new BsonDecimal128(13) } },
            MongoFilterBuilder.Positive.CreateBetween(null, new BsonDecimal128(12), new BsonDecimal128(13)));

        yield return (
            $"$ BETWEEN ${{{ExpressionText1}}} AND 13",
            new Dictionary<String, BsonValue> { { ExpressionText1, new BsonDecimal128(12) } },
            MongoFilterBuilder.Positive.CreateBetween(null, new BsonDecimal128(12), new BsonDecimal128(13)));

        yield return (
            $"$ BETWEEN ${{{ExpressionText1}}} AND ${{{ExpressionText2}}}",
            new Dictionary<String, BsonValue> { { ExpressionText1, new BsonDecimal128(12) }, { ExpressionText2, new BsonDecimal128(13) } },
            MongoFilterBuilder.Positive.CreateBetween(null, new BsonDecimal128(12), new BsonDecimal128(13)));

        yield return (
            "C MATCH \"123\" OPTIONS \"ims\"",
            new Dictionary<String, BsonValue>(),
            MongoFilterBuilder.Positive.CreateMatchOptions("C", new BsonString("123"), new BsonString("ims")));

        yield return (
            "D NOT EXIST",
            new Dictionary<String, BsonValue>(),
            MongoFilterBuilder.Negative.CreateExist("D"));

        yield return (
            $"E IN ${{{ExpressionText3}}}",
            new Dictionary<String, BsonValue> { { ExpressionText3, new BsonArray { "abc", "def" } } },
            MongoFilterBuilder.Positive.CreateIn("E", new BsonArray { "abc", "def" }));

        yield return (
            "E IN (\"abc\", \"def\")",
            new Dictionary<String, BsonValue>(),
            MongoFilterBuilder.Positive.CreateIn("E", new BsonArray { "abc", "def" }));

        yield return (
            $"E IN (\"abc\", ${{{ExpressionText5}}})",
            new Dictionary<String, BsonValue> { { ExpressionText5, "def" } },
            MongoFilterBuilder.Positive.CreateIn("E", new BsonArray { "abc", "def" }));

        yield return (
            $"E IN (${{{ExpressionText4}}}, \"def\")",
            new Dictionary<String, BsonValue> { { ExpressionText4, "abc" } },
            MongoFilterBuilder.Positive.CreateIn("E", new BsonArray { "abc", "def" }));

        yield return (
            $"E IN (${{{ExpressionText4}}}, ${{{ExpressionText5}}})",
            new Dictionary<String, BsonValue> { { ExpressionText4, "abc" }, { ExpressionText5, "def" } },
            MongoFilterBuilder.Positive.CreateIn("E", new BsonArray { "abc", "def" }));
    }

    public static IEnumerable<Object[]> ComparisonData()
    {
        const String ExpressionText = "Expression+Text";

        var result = new TheoryData<String, IEnumerable<KeyValuePair<String, BsonValue>>, BsonDocument>();

        foreach (var path in ValidPathes())
        {
            foreach (var @operator in ComparisonOperators())
            {
                foreach (var value in ValidValues())
                {
                    foreach (var outerNegation in OuterNegations())
                    {
                        var document = new MongoFilterBuilder(negate: outerNegation.value)
                            .CreateComparison(path.value, @operator.value, value.value);

                        result.Add(
                            outerNegation.apply($"{path.text} {@operator.text} {value.text}"),
                            new Dictionary<String, BsonValue>(),
                            document);

                        result.Add(
                            outerNegation.apply($"{path.text} {@operator.text} ${{{ExpressionText}}}"),
                            new Dictionary<String, BsonValue> { { ExpressionText, value.value } },
                            document);
                    }
                }
            }
        }

        return result;
    }

    public static IEnumerable<Object[]> BetweenData()
    {
        const String ExpressionText1 = "Expression+Text";
        const String ExpressionText2 = "Expression-Text";

        var result = new TheoryData<String, IEnumerable<KeyValuePair<String, BsonValue>>, BsonDocument>();

        foreach (var path in ValidPathes())
        {
            foreach (var value1 in ValidValues())
            {
                foreach (var value2 in ValidValues2())
                {
                    foreach (var innerNegation in InnerNegations())
                    {
                        foreach (var outerNegation in OuterNegations())
                        {
                            var document = new MongoFilterBuilder(negate: outerNegation.value != innerNegation.value)
                                .CreateBetween(path.value, value1.value, value2.value);

                            result.Add(
                                outerNegation.apply($"{path.text} {innerNegation.text} BETWEEN {value1.text} AND {value2.text}"),
                                Array.Empty<KeyValuePair<String, BsonValue>>(),
                                document);

                            result.Add(
                                outerNegation.apply($"{path.text} {innerNegation.text} BETWEEN {value1.text} AND ${{{ExpressionText2}}}"),
                                new Dictionary<String, BsonValue> { { ExpressionText2, value2.value } },
                                document);

                            result.Add(
                                outerNegation.apply($"{path.text} {innerNegation.text} BETWEEN ${{{ExpressionText1}}} AND {value2.text}"),
                                new Dictionary<String, BsonValue> { { ExpressionText1, value1.value } },
                                document);

                            result.Add(
                                outerNegation.apply($"{path.text} {innerNegation.text} BETWEEN ${{{ExpressionText1}}} AND ${{{ExpressionText2}}}"),
                                new Dictionary<String, BsonValue> { { ExpressionText1, value1.value }, { ExpressionText2, value2.value } },
                                document);
                        }
                    }
                }
            }
        }

        return result;
    }

    public static IEnumerable<Object[]> MatchData()
    {
        const String ExpressionText0 = "Expression^Text";
        const String ExpressionText1 = "Expression+Text";
        const String ExpressionText2 = "Expression-Text";

        var result = new TheoryData<String, IEnumerable<KeyValuePair<String, BsonValue>>, BsonDocument>();

        foreach (var path in ValidPathes())
        {
            foreach (var regex in ValidValues())
            {
                foreach (var innerNegation in InnerNegations())
                {
                    foreach (var outerNegation in OuterNegations())
                    {
                        var document = new MongoFilterBuilder(negate: outerNegation.value != innerNegation.value).CreateMatch(path.value, regex.value);

                        result.Add(
                            outerNegation.apply($"{path.text} {innerNegation.text} MATCH {regex.text}"),
                            new Dictionary<String, BsonValue>(),
                            document);

                        result.Add(
                            outerNegation.apply($"{path.text} {innerNegation.text} MATCH ${{{ExpressionText0}}}"),
                            new Dictionary<String, BsonValue> { { ExpressionText0, regex.value } },
                            document);
                    }
                }
            }
        }

        foreach (var path in ValidPathes())
        {
            foreach (var regex in ValidValues())
            {
                foreach (var options in ValidValues2())
                {
                    foreach (var innerNegation in InnerNegations())
                    {
                        foreach (var outerNegation in OuterNegations())
                        {
                            var document = new MongoFilterBuilder(negate: outerNegation.value != innerNegation.value).CreateMatchOptions(path.value, regex.value, options.value);

                            result.Add(
                                outerNegation.apply($"{path.text} {innerNegation.text} MATCH {regex.text} OPTIONS {options.text}"),
                                new Dictionary<String, BsonValue>(),
                                document);

                            result.Add(
                                outerNegation.apply($"{path.text} {innerNegation.text} MATCH {regex.text} OPTIONS ${{{ExpressionText2}}}"),
                                new Dictionary<String, BsonValue> { { ExpressionText2, options.value } },
                                document);

                            result.Add(
                                outerNegation.apply($"{path.text} {innerNegation.text} MATCH ${{{ExpressionText1}}} OPTIONS {options.text}"),
                                new Dictionary<String, BsonValue> { { ExpressionText1, regex.value } },
                                document);

                            result.Add(
                                outerNegation.apply($"{path.text} {innerNegation.text} MATCH ${{{ExpressionText1}}} OPTIONS ${{{ExpressionText2}}}"),
                                new Dictionary<String, BsonValue> { { ExpressionText1, regex.value }, { ExpressionText2, options.value } },
                                document);
                        }
                    }
                }
            }
        }

        return result;
    }

    public static IEnumerable<Object[]> InListData()
    {
        const String ExpressionText0 = "Expression^Text";
        const String ExpressionText1 = "Expression+Text";
        const String ExpressionText2 = "Expression-Text";

        var result = new TheoryData<String, IEnumerable<KeyValuePair<String, BsonValue>>, BsonDocument>();

        foreach (var path in ValidPathes())
        {
            foreach (var value in ValidValues())
            {
                foreach (var innerNegation in InnerNegations())
                {
                    foreach (var outerNegation in OuterNegations())
                    {
                        var document = new MongoFilterBuilder(negate: outerNegation.value != innerNegation.value).CreateIn(
                            path.value,
                            new BsonArray { value.value });

                        result.Add(
                            outerNegation.apply($"{path.text} {innerNegation.text} IN ( {value.text} )"),
                            new Dictionary<String, BsonValue>(),
                            document);

                        result.Add(
                            outerNegation.apply($"{path.text} {innerNegation.text} IN (${{{ExpressionText0}}})"),
                            new Dictionary<String, BsonValue> { { ExpressionText0, value.value } },
                            document);
                    }
                }
            }
        }

        foreach (var path in ValidPathes())
        {
            foreach (var value1 in ValidValues())
            {
                foreach (var value2 in ValidValues2())
                {
                    foreach (var innerNegation in InnerNegations())
                    {
                        foreach (var outerNegation in OuterNegations())
                        {
                            var document = new MongoFilterBuilder(negate: outerNegation.value != innerNegation.value)
                                .CreateIn(path.value, new BsonArray { value1.value, value2.value });

                            result.Add(
                                outerNegation.apply($"{path.text} {innerNegation.text} IN ( {value1.text}, {value2.text} )"),
                                new Dictionary<String, BsonValue>(),
                                document);

                            result.Add(
                                outerNegation.apply($"{path.text} {innerNegation.text} IN ( {value1.text}, ${{{ExpressionText2}}} )"),
                                new Dictionary<String, BsonValue> { { ExpressionText2, value2.value } },
                                document);

                            result.Add(
                                outerNegation.apply($"{path.text} {innerNegation.text} IN ( ${{{ExpressionText1}}}, {value2.text} )"),
                                new Dictionary<String, BsonValue> { { ExpressionText1, value1.value } },
                                document);

                            result.Add(
                                outerNegation.apply($"{path.text} {innerNegation.text} IN ( ${{{ExpressionText1}}}, ${{{ExpressionText2}}} )"),
                                new Dictionary<String, BsonValue> { { ExpressionText1, value1.value }, { ExpressionText2, value2.value } },
                                document);
                        }
                    }
                }
            }
        }

        return result;
    }

    public static IEnumerable<Object[]> InExpressionData()
    {
        const String ExpressionText = "Expression+Text";

        var result = new TheoryData<String, IEnumerable<KeyValuePair<String, BsonValue>>, BsonDocument>();

        foreach (var path in ValidPathes())
        {
            foreach (var value in ValidValues())
            {
                var array = new BsonArray { value.value };

                foreach (var innerNegation in InnerNegations())
                {
                    foreach (var outerNegation in OuterNegations())
                    {
                        var document = new MongoFilterBuilder(negate: outerNegation.value != innerNegation.value)
                            .CreateIn(path.value, array);

                        result.Add(
                            outerNegation.apply($"{path.text} {innerNegation.text} IN ${{{ExpressionText}}}"),
                            new Dictionary<String, BsonValue> { { ExpressionText, array } },
                            document);
                    }
                }
            }
        }

        return result;
    }

    public static IEnumerable<Object[]> ExistData()
    {
        var result = new TheoryData<String, IEnumerable<KeyValuePair<String, BsonValue>>, BsonDocument>();

        foreach (var path in ValidPathes())
        {
            foreach (var innerNegation in InnerNegations())
            {
                foreach (var outerNegation in OuterNegations())
                {
                    var document = new MongoFilterBuilder(negate: outerNegation.value != innerNegation.value)
                        .CreateExist(path.value);

                    result.Add(
                        outerNegation.apply($"{path.text} {innerNegation.text} EXIST"),
                        new Dictionary<String, BsonValue>(),
                        document);
                }
            }
        }

        return result;
    }

    public static IEnumerable<Object[]> AnyData()
    {
        var result = new TheoryData<String, IEnumerable<KeyValuePair<String, BsonValue>>, BsonDocument>();

        foreach (var path in ValidPathes())
        {
            foreach (var innerNegation in InnerNegations())
            {
                foreach (var subFilter in ValidSubFilters())
                {
                    foreach (var outerNegation in OuterNegations())
                    {
                        var document = new MongoFilterBuilder(negate: outerNegation.value != innerNegation.value)
                            .CreateAny(path.value, subFilter.document);

                        result.Add(
                            outerNegation.apply($"ANYOF {path.text} IS {innerNegation.text} ({subFilter.text})"),
                            subFilter.expressions,
                            document);
                    }
                }
            }
        }

        return result;
    }

    public static IEnumerable<Object[]> TypeEqData()
    {
        const String ExpressionText = "Expression+Text";

        var result = new TheoryData<String, IEnumerable<KeyValuePair<String, BsonValue>>, BsonDocument>();

        foreach (var path in ValidPathes())
        {
            foreach (var value in ValidValues())
            {
                foreach (var outerNegation in OuterNegations())
                {
                    var document = new MongoFilterBuilder(negate: outerNegation.value != false)
                        .CreateTypeEq(path.value, value.value);
            
                    result.Add(
                        outerNegation.apply($"TYPEOF {path.text} == {value.text}"),
                        new Dictionary<String, BsonValue>(),
                        document);

                    result.Add(
                        outerNegation.apply($"TYPEOF {path.text} == ${{{ExpressionText}}}"),
                        new Dictionary<String, BsonValue> { { ExpressionText, value.value } },
                        document);
                }
            }
        }

        foreach (var path in ValidPathes())
        {
            foreach (var value in ValidValues())
            {
                foreach (var outerNegation in OuterNegations())
                {
                    var document = new MongoFilterBuilder(negate: outerNegation.value != true)
                        .CreateTypeEq(path.value, value.value);

                    result.Add(
                        outerNegation.apply($"TYPEOF {path.text} != {value.text}"),
                        new Dictionary<String, BsonValue>(),
                        document);

                    result.Add(
                        outerNegation.apply($"TYPEOF {path.text} != ${{{ExpressionText}}}"),
                        new Dictionary<String, BsonValue> { { ExpressionText, value.value } },
                        document);
                }
            }
        }

        return result;
    }

    public static IEnumerable<Object[]> TypeInListData()
    {
        const String ExpressionText0 = "Expression^Text";
        const String ExpressionText1 = "Expression+Text";
        const String ExpressionText2 = "Expression-Text";

        var result = new TheoryData<String, IEnumerable<KeyValuePair<String, BsonValue>>, BsonDocument>();

        foreach (var path in ValidPathes())
        {
            foreach (var value in ValidValues())
            {
                foreach (var innerNegation in InnerNegations())
                {
                    foreach (var outerNegation in OuterNegations())
                    {
                        var document = new MongoFilterBuilder(negate: outerNegation.value != innerNegation.value)
                            .CreateTypeIn(path.value, new BsonArray { value.value });

                        result.Add(
                            outerNegation.apply($"TYPEOF {path.text} {innerNegation.text} IN ( {value.text} )"),
                            new Dictionary<String, BsonValue>(),
                            document);

                        result.Add(
                            outerNegation.apply($"TYPEOF {path.text} {innerNegation.text} IN (${{{ExpressionText0}}})"),
                            new Dictionary<String, BsonValue> { { ExpressionText0, value.value } },
                            document);
                    }
                }
            }
        }

        foreach (var path in ValidPathes())
        {
            foreach (var value1 in ValidValues())
            {
                foreach (var value2 in ValidValues2())
                {
                    foreach (var innerNegation in InnerNegations())
                    {
                        foreach (var outerNegation in OuterNegations())
                        {
                            var document = new MongoFilterBuilder(negate: outerNegation.value != innerNegation.value)
                                .CreateTypeIn(path.value, new BsonArray { value1.value, value2.value });

                            result.Add(
                                outerNegation.apply($"TYPEOF {path.text} {innerNegation.text} IN ( {value1.text}, {value2.text} )"),
                                new Dictionary<String, BsonValue>(),
                                document);

                            result.Add(
                                outerNegation.apply($"TYPEOF {path.text} {innerNegation.text} IN ( {value1.text}, ${{{ExpressionText2}}} )"),
                                new Dictionary<String, BsonValue> { { ExpressionText2, value2.value } },
                                document);

                            result.Add(
                                outerNegation.apply($"TYPEOF {path.text} {innerNegation.text} IN ( ${{{ExpressionText1}}}, {value2.text} )"),
                                new Dictionary<String, BsonValue> { { ExpressionText1, value1.value } },
                                document);

                            result.Add(
                                outerNegation.apply($"TYPEOF {path.text} {innerNegation.text} IN ( ${{{ExpressionText1}}}, ${{{ExpressionText2}}} )"),
                                new Dictionary<String, BsonValue> { { ExpressionText1, value1.value }, { ExpressionText2, value2.value } },
                                document);
                        }
                    }
                }
            }
        }

        return result;
    }

    public static IEnumerable<Object[]> TypeInExpressionData()
    {
        const String ExpressionText = "Expression+Text";

        var result = new TheoryData<String, IEnumerable<KeyValuePair<String, BsonValue>>, BsonDocument>();

        foreach (var path in ValidPathes())
        {
            foreach (var value in ValidValues())
            {
                var array = new BsonArray { value.value };

                foreach (var innerNegation in InnerNegations())
                {
                    foreach (var outerNegation in OuterNegations())
                    {
                        var document = new MongoFilterBuilder(negate: outerNegation.value != innerNegation.value)
                            .CreateTypeIn(path.value, array);

                        result.Add(
                            outerNegation.apply($"TYPEOF {path.text} {innerNegation.text} IN ${{{ExpressionText}}}"),
                            new Dictionary<String, BsonValue> { { ExpressionText, array } },
                            document);
                    }
                }
            }
        }

        return result;
    }

    public static IEnumerable<Object[]> ParenData()
    {
        var result = new TheoryData<String, IEnumerable<KeyValuePair<String, BsonValue>>, BsonDocument>();

        foreach (var subFilter in ValidSubFilters())
        {
            result.Add(
                $"({subFilter.text})",
                subFilter.expressions,
                subFilter.document);
        }

        return result;
    }

    public static IEnumerable<Object[]> GroupData()
    {
        (String text, BsonDocument document) filter1 = (
            "A <= 100",
            MongoFilterBuilder.Positive.CreateComparison("A", MongoOperators.Lte, new BsonDecimal128(100)));

        (String text, BsonDocument document) filter2 = (
            "A >= 0",
            MongoFilterBuilder.Positive.CreateComparison("A", MongoOperators.Gte, new BsonDecimal128(0)));

        (String text, BsonDocument document) filter3 = (
            "B == 123",
            MongoFilterBuilder.Positive.CreateComparison("B", MongoOperators.Eq, new BsonDecimal128(123)));

        (String text, BsonDocument document) filter4 = (
            "B != 124",
            MongoFilterBuilder.Positive.CreateComparison("B", MongoOperators.Neq, new BsonDecimal128(124)));

        var result = new TheoryData<String, IEnumerable<KeyValuePair<String, BsonValue>>, BsonDocument>();

        foreach (var outerNegation in OuterNegations())
        {
            result.Add(
                outerNegation.apply($"{filter1.text} AND {filter2.text}"),
                new Dictionary<String, BsonValue>(),
                new MongoFilterBuilder(negate: outerNegation.value != false)
                    .CreateAnd(new BsonArray {
                        filter1.document,
                        filter2.document,
                    }));

            result.Add(
                outerNegation.apply($"{filter1.text} OR {filter2.text}"),
                new Dictionary<String, BsonValue>(),
                new MongoFilterBuilder(negate: outerNegation.value != false)
                    .CreateOr(new BsonArray {
                        filter1.document,
                        filter2.document,
                    }));

            result.Add(
                outerNegation.apply($"{filter1.text} AND {filter2.text} AND {filter3.text}"),
                new Dictionary<String, BsonValue>(),
                new MongoFilterBuilder(negate: outerNegation.value != false)
                    .CreateAnd(new BsonArray {
                        filter1.document,
                        filter2.document,
                        filter3.document,
                    }));

            result.Add(
                outerNegation.apply($"{filter1.text} AND {filter2.text} OR {filter3.text}"),
                new Dictionary<String, BsonValue>(),
                new MongoFilterBuilder(negate: outerNegation.value != false)
                    .CreateOr(new BsonArray {
                        MongoFilterBuilder.Positive.CreateAnd(new BsonArray { filter1.document, filter2.document }),
                        filter3.document,
                    }));

            result.Add(
                outerNegation.apply($"{filter1.text} OR {filter2.text} AND {filter3.text}"),
                new Dictionary<String, BsonValue>(),
                new MongoFilterBuilder(negate: outerNegation.value != false)
                    .CreateOr(new BsonArray {
                        filter1.document,
                        MongoFilterBuilder.Positive.CreateAnd(new BsonArray { filter2.document, filter3.document }),
                    }));

            result.Add(
                outerNegation.apply($"{filter1.text} OR {filter2.text} OR {filter3.text}"),
                new Dictionary<String, BsonValue>(),
                new MongoFilterBuilder(negate: outerNegation.value != false)
                    .CreateOr(new BsonArray {
                        filter1.document,
                        filter2.document,
                        filter3.document,
                    }));

            result.Add(
                outerNegation.apply($"{filter1.text} AND {filter2.text} AND {filter3.text} AND {filter4.text}"),
                new Dictionary<String, BsonValue>(),
                new MongoFilterBuilder(negate: outerNegation.value != false)
                    .CreateAnd(new BsonArray {
                        filter1.document,
                        filter2.document,
                        filter3.document,
                        filter4.document,
                    }));

            result.Add(
                outerNegation.apply($"{filter1.text} AND {filter2.text} AND {filter3.text} OR {filter4.text}"),
                new Dictionary<String, BsonValue>(),
                new MongoFilterBuilder(negate: outerNegation.value != false)
                    .CreateOr(new BsonArray {
                        MongoFilterBuilder.Positive.CreateAnd(new BsonArray { filter1.document, filter2.document, filter3.document }),
                        filter4.document,
                    }));

            result.Add(
                outerNegation.apply($"{filter1.text} AND {filter2.text} OR {filter3.text} AND {filter4.text}"),
                new Dictionary<String, BsonValue>(),
                new MongoFilterBuilder(negate: outerNegation.value != false)
                    .CreateOr(new BsonArray {
                        MongoFilterBuilder.Positive.CreateAnd(new BsonArray { filter1.document, filter2.document }),
                        MongoFilterBuilder.Positive.CreateAnd(new BsonArray { filter3.document, filter4.document }),
                    }));

            result.Add(
                outerNegation.apply($"{filter1.text} AND {filter2.text} OR {filter3.text} OR {filter4.text}"),
                new Dictionary<String, BsonValue>(),
                new MongoFilterBuilder(negate: outerNegation.value != false)
                    .CreateOr(new BsonArray {
                        MongoFilterBuilder.Positive.CreateAnd(new BsonArray { filter1.document, filter2.document }),
                        filter3.document,
                        filter4.document,
                    }));


            result.Add(
                outerNegation.apply($"{filter1.text} OR {filter2.text} AND {filter3.text} AND {filter4.text}"),
                new Dictionary<String, BsonValue>(),
                new MongoFilterBuilder(negate: outerNegation.value != false)
                    .CreateOr(new BsonArray {
                        filter1.document,
                        MongoFilterBuilder.Positive.CreateAnd(new BsonArray { filter2.document, filter3.document, filter4.document }),
                    }));

            result.Add(
                outerNegation.apply($"{filter1.text} OR {filter2.text} AND {filter3.text} OR {filter4.text}"),
                new Dictionary<String, BsonValue>(),
                new MongoFilterBuilder(negate: outerNegation.value != false)
                    .CreateOr(new BsonArray {
                        filter1.document,
                        MongoFilterBuilder.Positive.CreateAnd(new BsonArray { filter2.document, filter3.document }),
                        filter4.document,
                    }));

            result.Add(
                outerNegation.apply($"{filter1.text} OR {filter2.text} OR {filter3.text} AND {filter4.text}"),
                new Dictionary<String, BsonValue>(),
                new MongoFilterBuilder(negate: outerNegation.value != false)
                    .CreateOr(new BsonArray {
                        filter1.document,
                        filter2.document,
                        MongoFilterBuilder.Positive.CreateAnd(new BsonArray { filter3.document, filter4.document }),
                    }));

            result.Add(
                outerNegation.apply($"{filter1.text} OR {filter2.text} OR {filter3.text} OR {filter4.text}"),
                new Dictionary<String, BsonValue>(),
                new MongoFilterBuilder(negate: outerNegation.value != false)
                    .CreateOr(new BsonArray {
                        filter1.document,
                        filter2.document,
                        filter3.document,
                        filter4.document,
                    }));
        }

        return result;
    }

    [Theory]
    [MemberData(nameof(ComparisonData))]
    [MemberData(nameof(BetweenData))]
    [MemberData(nameof(MatchData))]
    [MemberData(nameof(InExpressionData))]
    [MemberData(nameof(InListData))]
    [MemberData(nameof(ExistData))]
    [MemberData(nameof(TypeEqData))]
    [MemberData(nameof(TypeInExpressionData))]
    [MemberData(nameof(TypeInListData))]
    [MemberData(nameof(AnyData))]
    [MemberData(nameof(ParenData))]
    [MemberData(nameof(GroupData))]
    internal void ParseAndCreate_ValidFilterText_ReturnsExpected(
        String filterText,
        IEnumerable<KeyValuePair<String, BsonValue>> expressions,
        BsonDocument expected)
    {
        // Arrange
        var mockExpressionEvaluator = new Mock<Func<String, BsonValue>>(MockBehavior.Strict);

        foreach (var expression in expressions)
        {
            mockExpressionEvaluator
                .Setup(x => x(expression.Key))
                .Returns(expression.Value);
        }

        // Act
        var filter = FluentFilter.Parse(filterText).Create(mockExpressionEvaluator.Object);

        // Assert
        filter.Should().BeEquivalentTo(expected);

        mockExpressionEvaluator.VerifyAll();
        mockExpressionEvaluator.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("A <= ${2 + 2}")]
    internal void ParseAndCreate_ValidFilterText_WithExpression_ThrowsNotSupportedException(String filterText)
    {
        // Act
        var action = new Action(() => FluentFilter.Parse(filterText).Create());

        // Assert
        action.Should().ThrowExactly<NotSupportedException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("A ==")]
    [InlineData("A == B")]
    [InlineData("A == #12-12-2040#")]
    internal void Parse_InvalidFilterText_ThrowsArgumentException(String filterText)
    {
        // Act
        var action = new Action(() => FluentFilter.Parse(filterText));

        // Assert
        action.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    internal void Parse_NullFilterText_ThrowsArgumentNullException()
    {
        // Arrange
        var action = new Action(() => FluentFilter.Parse(null));

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>();
    }
}
#pragma warning restore IDE0042 // Deconstruct variable declaration
