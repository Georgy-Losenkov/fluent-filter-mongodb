using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using FluentAssertions;
using MongoDB.Bson;
using Xunit;
using static System.Net.Mime.MediaTypeNames;

namespace MongoDB.Driver.Parsing;

public class ExpressionScannerTests
{
    public static IEnumerable<Object[]> DataFor_yylex_ReturnsToken()
    {
        var tuples = new[] {
            ("And", Token.AND),
            ("Anyof", Token.ANYOF),
            ("Between", Token.BETWEEN),
            ("Exist", Token.EXIST),
            ("In", Token.IN),
            ("Is", Token.IS),
            ("Match", Token.MATCH),
            ("Not", Token.NOT),
            ("Options", Token.OPTIONS),
            ("Or", Token.OR),
            ("Typeof", Token.TYPEOF),
        };

        foreach (var tuple in tuples)
        {
            yield return new Object[] { tuple.Item1, tuple.Item2 };
            yield return new Object[] { tuple.Item1.ToLower(), tuple.Item2 };
            yield return new Object[] { tuple.Item1.ToUpper(), tuple.Item2 };
        }

        yield return new Object[] { ",", Token.COMMA };
        yield return new Object[] { "(", Token.LPAREN };
        yield return new Object[] { ")", Token.RPAREN };

        yield return new Object[] { "<", Token.LT };
        yield return new Object[] { "<=", Token.LTE };
        yield return new Object[] { ">", Token.GT };
        yield return new Object[] { ">=", Token.GTE };
        yield return new Object[] { "==", Token.EQ };
        yield return new Object[] { "!=", Token.NEQ };

    }

    [Theory]
    [MemberData(nameof(DataFor_yylex_ReturnsToken))]
    internal void yylex_ReturnsToken(String input, Token expectedToken)
    {
        // Arrange
        var scanner = new ExpressionScanner();
        scanner.SetSource(input, 0);

        // Act
        var token = scanner.yylex();

        // Assert
        token.Should().Be((Int32)expectedToken);
    }

    // EXPRESSION "${...}", LITERAL
    public static IEnumerable<Object[]> DataFor_yylex_ReturnsPathWithText()
    {
        yield return new Object[] { "$", null };
        yield return new Object[] { "A", "A" };
        yield return new Object[] { "A.B", "A.B" };
        yield return new Object[] { "A.0", "A.0" };
        yield return new Object[] { "A.0.C", "A.0.C" };
        yield return new Object[] { "`A`", "A" };
        yield return new Object[] { "`A.B`", "A.B" };
        yield return new Object[] { "`A.0`", "A.0" };
        yield return new Object[] { "`A.0.C`", "A.0.C" };
        yield return new Object[] { "`123.1.2`", "123.1.2" };
        yield return new Object[] { "`A``B`", "A`B" };
    }

    [Theory]
    [MemberData(nameof(DataFor_yylex_ReturnsPathWithText))]
    internal void yylex_ReturnsPathWithText(String input, String expectedText)
    {
        // Arrange
        var scanner = new ExpressionScanner();
        scanner.SetSource(input, 0);

        // Act
        var token = scanner.yylex();

        // Assert
        token.Should().Be((Int32)Token.PATH);
        scanner.yylval.Text.Should().Be(expectedText);
    }

    public static IEnumerable<Object[]> DataFor_yylex_ReturnsExpressionWithText()
    {
        yield return new[] { "${A}", "A" };
        yield return new[] { "${DateTime.Today}", "DateTime.Today" };
        yield return new[] { "${7 + 8 - 9}", "7 + 8 - 9" };
    }

    [Theory]
    [MemberData(nameof(DataFor_yylex_ReturnsExpressionWithText))]
    internal void yylex_ReturnsExpressionWithText(String input, String expectedText)
    {
        // Arrange
        var scanner = new ExpressionScanner();
        scanner.SetSource(input, 0);

        // Act
        var token = scanner.yylex();

        // Assert
        token.Should().Be((Int32)Token.EXPRESSION);
        scanner.yylval.Text.Should().Be(expectedText);
    }

    public static IEnumerable<Object[]> DataFor_yylex_ReturnsLiteralWithValue()
    {
        // strings
        yield return new Object[] { "\"A\"", new BsonString("A") };
        yield return new Object[] { "\"A\"\"BC\"", new BsonString("A\"BC") };
        yield return new Object[] { "\"Long text\"", new BsonString("Long text") };
        yield return new Object[] { "\"Very\"\"long\"\"text\"", new BsonString("Very\"long\"text") };

        // regular expressions
        yield return new Object[] { "/ABC/", new BsonRegularExpression("ABC") };
        yield return new Object[] { "/ABC/ims", new BsonRegularExpression("ABC", "ims") };
        yield return new Object[] { "/ABC//DE/", new BsonRegularExpression("ABC/DE") };
        yield return new Object[] { "/ABC//DE/ims", new BsonRegularExpression("ABC/DE", "ims") };

        // numbers
        foreach (var sign in new[] { ("", 1m), ("+", 1m), ("-", -1m) })
        {
            foreach (var number in new[] { ("123", 123m), ("123.456", 123.456m) })
            {
                foreach (var exponent in new[] { ("", 1m), ("e0", 1m), ("e+0", 1m), ("e-0", 1m), ("e3", 1000m), ("e+3", 1000m), ("e-3", 0.001m), ("E0", 1m), ("E+0", 1m), ("E-0", 1m), ("E3", 1000m), ("E+3", 1000m), ("E-3", 0.001m) })
                {
                    yield return new Object[] {
                        $"{sign.Item1}{number.Item1}{exponent.Item1}",
                        new BsonDecimal128(sign.Item2 * number.Item2 * exponent.Item2)
                    };
                }
            }
        }

        // dates
        yield return new Object[] { "#2045-12-21#", new BsonDateTime(new DateTime(2045, 12, 21, 00, 00, 00, DateTimeKind.Utc)) };
        yield return new Object[] { "#2045-12-21 15:45#", new BsonDateTime(new DateTime(2045, 12, 21, 15, 45, 0, DateTimeKind.Utc)) };
        yield return new Object[] { "#2045-12-21 15:45:36#", new BsonDateTime(new DateTime(2045, 12, 21, 15, 45, 36, DateTimeKind.Utc)) };
        yield return new Object[] { "#2045-12-21 15:45:36.1#", new BsonDateTime(new DateTime(2045, 12, 21, 15, 45, 36, 100, DateTimeKind.Utc)) };
        yield return new Object[] { "#2045-12-21 15:45:36.12#", new BsonDateTime(new DateTime(2045, 12, 21, 15, 45, 36, 120, DateTimeKind.Utc)) };
        yield return new Object[] { "#2045-12-21 15:45:36.123#", new BsonDateTime(new DateTime(2045, 12, 21, 15, 45, 36, 123, DateTimeKind.Utc)) };

#if NET7_0_OR_GREATER
        yield return new Object[] { "#2045-12-21 15:45:36.1234#", new BsonDateTime(new DateTime(2045, 12, 21, 15, 45, 36, 123, 400, DateTimeKind.Utc)) };
        yield return new Object[] { "#2045-12-21 15:45:36.12345#", new BsonDateTime(new DateTime(2045, 12, 21, 15, 45, 36, 123, 450, DateTimeKind.Utc)) };
        yield return new Object[] { "#2045-12-21 15:45:36.123456#", new BsonDateTime(new DateTime(2045, 12, 21, 15, 45, 36, 123, 456, DateTimeKind.Utc)) };
        yield return new Object[] { "#2045-12-21 15:45:36.1234567#", new BsonDateTime(new DateTime(2045, 12, 21, 15, 45, 36, 123, 457, DateTimeKind.Utc)) };
        yield return new Object[] { "#2045-12-21 15:45:36.12345678#", new BsonDateTime(new DateTime(2045, 12, 21, 15, 45, 36, 123, 457, DateTimeKind.Utc)) };
#endif

        // null
        yield return new Object[] { "null", BsonNull.Value };
        yield return new Object[] { "true", BsonBoolean.True };
        yield return new Object[] { "false", BsonBoolean.False };

        // object id
        yield return new Object[] { "ObjectId(\"0A1B2C3D4E5F6a7b8c9d0e1f\")", new BsonObjectId(new ObjectId(new Byte[] { 0x0a, 0x1b, 0x2c, 0x3d, 0x4e, 0x5f, 0x6a, 0x7b, 0x8c, 0x9d, 0x0e, 0x1f })) };
        yield return new Object[] { "ObjectId(\"0A1B2C3D4E5F6a7b8c9d0e1f\" )", new BsonObjectId(new ObjectId(new Byte[] { 0x0a, 0x1b, 0x2c, 0x3d, 0x4e, 0x5f, 0x6a, 0x7b, 0x8c, 0x9d, 0x0e, 0x1f })) };
        yield return new Object[] { "ObjectId( \"0A1B2C3D4E5F6a7b8c9d0e1f\")", new BsonObjectId(new ObjectId(new Byte[] { 0x0a, 0x1b, 0x2c, 0x3d, 0x4e, 0x5f, 0x6a, 0x7b, 0x8c, 0x9d, 0x0e, 0x1f })) };
        yield return new Object[] { "ObjectId( \"0A1B2C3D4E5F6a7b8c9d0e1f\" )", new BsonObjectId(new ObjectId(new Byte[] { 0x0a, 0x1b, 0x2c, 0x3d, 0x4e, 0x5f, 0x6a, 0x7b, 0x8c, 0x9d, 0x0e, 0x1f })) };

        // uuid
        var guid = Guid.NewGuid();
        yield return new Object[] { $"Uuid(\"{guid:D}\")", new BsonBinaryData(guid, GuidRepresentation.Standard) };
        yield return new Object[] { $"Uuid(\"{guid:D}\" )", new BsonBinaryData(guid, GuidRepresentation.Standard) };
        yield return new Object[] { $"Uuid( \"{guid:D}\")", new BsonBinaryData(guid, GuidRepresentation.Standard) };
        yield return new Object[] { $"Uuid( \"{guid:D}\" )", new BsonBinaryData(guid, GuidRepresentation.Standard) };

        foreach (var repr in new[] { GuidRepresentation.CSharpLegacy, GuidRepresentation.JavaLegacy, GuidRepresentation.PythonLegacy, GuidRepresentation.Standard })
        {
            yield return new Object[] { $"Uuid(\"{repr}\",\"{guid:D}\")", new BsonBinaryData(guid, repr) };
            yield return new Object[] { $"Uuid(\"{repr}\",\"{guid:D}\" )", new BsonBinaryData(guid, repr) };
            yield return new Object[] { $"Uuid(\"{repr}\", \"{guid:D}\")", new BsonBinaryData(guid, repr) };
            yield return new Object[] { $"Uuid(\"{repr}\", \"{guid:D}\" )", new BsonBinaryData(guid, repr) };
            yield return new Object[] { $"Uuid(\"{repr}\" ,\"{guid:D}\")", new BsonBinaryData(guid, repr) };
            yield return new Object[] { $"Uuid(\"{repr}\" ,\"{guid:D}\" )", new BsonBinaryData(guid, repr) };
            yield return new Object[] { $"Uuid(\"{repr}\" , \"{guid:D}\")", new BsonBinaryData(guid, repr) };
            yield return new Object[] { $"Uuid(\"{repr}\" , \"{guid:D}\" )", new BsonBinaryData(guid, repr) };
            yield return new Object[] { $"Uuid( \"{repr}\",\"{guid:D}\")", new BsonBinaryData(guid, repr) };
            yield return new Object[] { $"Uuid( \"{repr}\",\"{guid:D}\" )", new BsonBinaryData(guid, repr) };
            yield return new Object[] { $"Uuid( \"{repr}\", \"{guid:D}\")", new BsonBinaryData(guid, repr) };
            yield return new Object[] { $"Uuid( \"{repr}\", \"{guid:D}\" )", new BsonBinaryData(guid, repr) };
            yield return new Object[] { $"Uuid( \"{repr}\" ,\"{guid:D}\")", new BsonBinaryData(guid, repr) };
            yield return new Object[] { $"Uuid( \"{repr}\" ,\"{guid:D}\" )", new BsonBinaryData(guid, repr) };
            yield return new Object[] { $"Uuid( \"{repr}\" , \"{guid:D}\")", new BsonBinaryData(guid, repr) };
            yield return new Object[] { $"Uuid( \"{repr}\" , \"{guid:D}\" )", new BsonBinaryData(guid, repr) };
        }

        // binary
        var samples = new[] {
            new Byte[] { },
            new Byte[] { 89 },
            new Byte[] { 86, 103 },
            new Byte[] { 83, 100, 117 }
        };

#pragma warning disable CS0618 // Type or member is obsolete
        var subTypes = new[] {
            BsonBinarySubType.Binary,
            BsonBinarySubType.Encrypted,
            BsonBinarySubType.Function,
            BsonBinarySubType.MD5,
            BsonBinarySubType.OldBinary,
            BsonBinarySubType.UserDefined,
            (BsonBinarySubType)12,
        };
#pragma warning restore CS0618 // Type or member is obsolete

        foreach (var binary in samples)
        {
            var text = Convert.ToBase64String(binary);

            yield return new Object[] { $"Binary(\"{text}\")", new BsonBinaryData(binary) };
            yield return new Object[] { $"Binary(\"{text}\" )", new BsonBinaryData(binary) };
            yield return new Object[] { $"Binary( \"{text}\")", new BsonBinaryData(binary) };
            yield return new Object[] { $"Binary( \"{text}\" )", new BsonBinaryData(binary) };

            foreach (var subType in subTypes)
            {
                yield return new Object[] { $"Binary(\"{subType}\",\"{text}\")", new BsonBinaryData(binary, subType) };
                yield return new Object[] { $"Binary(\"{subType}\",\"{text}\" )", new BsonBinaryData(binary, subType) };
                yield return new Object[] { $"Binary(\"{subType}\", \"{text}\")", new BsonBinaryData(binary, subType) };
                yield return new Object[] { $"Binary(\"{subType}\", \"{text}\" )", new BsonBinaryData(binary, subType) };
                yield return new Object[] { $"Binary(\"{subType}\" ,\"{text}\")", new BsonBinaryData(binary, subType) };
                yield return new Object[] { $"Binary(\"{subType}\" ,\"{text}\" )", new BsonBinaryData(binary, subType) };
                yield return new Object[] { $"Binary(\"{subType}\" , \"{text}\")", new BsonBinaryData(binary, subType) };
                yield return new Object[] { $"Binary(\"{subType}\" , \"{text}\" )", new BsonBinaryData(binary, subType) };
                yield return new Object[] { $"Binary( \"{subType}\",\"{text}\")", new BsonBinaryData(binary, subType) };
                yield return new Object[] { $"Binary( \"{subType}\",\"{text}\" )", new BsonBinaryData(binary, subType) };
                yield return new Object[] { $"Binary( \"{subType}\", \"{text}\")", new BsonBinaryData(binary, subType) };
                yield return new Object[] { $"Binary( \"{subType}\", \"{text}\" )", new BsonBinaryData(binary, subType) };
                yield return new Object[] { $"Binary( \"{subType}\" ,\"{text}\")", new BsonBinaryData(binary, subType) };
                yield return new Object[] { $"Binary( \"{subType}\" ,\"{text}\" )", new BsonBinaryData(binary, subType) };
                yield return new Object[] { $"Binary( \"{subType}\" , \"{text}\")", new BsonBinaryData(binary, subType) };
                yield return new Object[] { $"Binary( \"{subType}\" , \"{text}\" )", new BsonBinaryData(binary, subType) };
            }
        }

        {
            var bytes = new byte[512];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            var text = Convert.ToBase64String(bytes, Base64FormattingOptions.InsertLineBreaks);
            yield return new Object[] { $"Binary(\"{text}\")", new BsonBinaryData(bytes) };
        }
    }

    [Theory]
    [MemberData(nameof(DataFor_yylex_ReturnsLiteralWithValue))]
    internal void yylex_ReturnsLiteralWithValue(String input, BsonValue expectedValue)
    {
        // Arrange
        var scanner = new ExpressionScanner();
        scanner.SetSource(input, 0);

        // Act
        var token = (Token)scanner.yylex();
        var text = scanner.yytext;

        // Assert
        text.Should().Be(input);
        token.Should().Be(Token.LITERAL);
        scanner.yylval.Value.Should().Be(expectedValue);
    }

    public static IEnumerable<Object[]> DataFor_yylex_ThrowsFormatException()
    {
        // dates
        yield return new Object[] { "#2045-13-21#" };
        yield return new Object[] { "#2045-13-32#" };
        yield return new Object[] { "#2045-12-21 25:45#" };
        yield return new Object[] { "#2045-12-21 15:65#" };
        yield return new Object[] { "#2045-12-21 15:45:66#" };
        yield return new Object[] { "#2045-12-21 15:45:66.1#" };
        yield return new Object[] { "#2045-12-21 15:45:66.12#" };
        yield return new Object[] { "#2045-12-21 15:45:66.123#" };

        // object id
        yield return new Object[] { "ObjectId(\"0A1B2C3D4E5F7b8c9d0e1f\")" };
        yield return new Object[] { "ObjectId(\"0A1B2C3D4E5F6R7b8c9d0e1f\" )" };

        // uuid
        yield return new Object[] { $"Uuid(\"2C62A140-E79E-4C8E-94E1-C9C6E18BF13\")" };
        yield return new Object[] { $"Uuid(\"2C62A140-E79E-4C8E-94E1-C9C6E18BF13R\")" };
        yield return new Object[] { $"Uuid(\"{GuidRepresentation.Unspecified}\", \"2C62A140-E79E-4C8E-94E1-C9C6E18BF13E\")" };
        yield return new Object[] { $"Uuid(\"xxxx\", \"2C62A140-E79E-4C8E-94E1-C9C6E18BF13E\")" };
        yield return new Object[] { $"Uuid(\"10\", \"2C62A140-E79E-4C8E-94E1-C9C6E18BF13E\")" };

        // binary
        yield return new Object[] { $"Binary(\"AAA*\")" };
        yield return new Object[] { $"Binary(\"AAA\")" };
        yield return new Object[] { $"Binary(\"{BsonBinarySubType.UuidStandard}\", \"AAAA\")" };
        yield return new Object[] { $"Binary(\"{BsonBinarySubType.UuidLegacy}\", \"AAAA\")" };
        yield return new Object[] { $"Binary(\"xxxx\", \"AAAA\")" };
    }

    [Theory]
    [MemberData(nameof(DataFor_yylex_ThrowsFormatException))]
    internal void yylex_ThrowsFormatException(String input)
    {
        // Arrange
        var scanner = new ExpressionScanner();
        scanner.SetSource(input, 0);

        // Act & Assert
        scanner.Invoking(x => x.yylex()).Should().ThrowExactly<FormatException>();
    }

    [Fact]
    internal void Base64()
    {
        System.Diagnostics.Debug.WriteLine(
            String.Join(", ", Enumerable.Range(0, 256).Select(x => Convert.ToBase64String(new[] { (Byte)x })))
        );

        System.Diagnostics.Debug.WriteLine(
            String.Join(", ", Enumerable.Range(0, 256).Select(x => Convert.ToBase64String(new[] { Byte.MinValue, (Byte)x })))
        );

        System.Diagnostics.Debug.WriteLine(
            String.Join(", ", Enumerable.Range(0, 256).Select(x => Convert.ToBase64String(new[] { Byte.MaxValue, (Byte)x })))
        );
    }
}