// <copyright file="ExpressionScanner.cs" company="Georgy Losenkov">
// Copyright (c) Georgy Losenkov. All rights reserved.
// </copyright>

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using MongoDB.Bson;
using MongoDB.Driver.Filter.Fluent;

namespace MongoDB.Driver.Parsing;

internal partial class ExpressionScanner
{
    private const Char Comma = ',';
    private const Char DoubleQuote = '"';

    internal String ErrorMessage { get; private set; }

    /// <inheritdoc/>
    public override void yyerror(String format, params Object[] args)
    {
        if (args != null && 0 < args.Length)
        {
            ErrorMessage = String.Format(format, args);
        }
        else
        {
            ErrorMessage = format;
        }
    }

    private static ReadOnlySpan<Char> StripDoubleQuotes(ReadOnlySpan<Char> chars)
    {
        // {WS}*\"{something}\"{WS}*
        var startIndex = chars.IndexOf(DoubleQuote);
        var lastIndex = chars.LastIndexOf(DoubleQuote);

        return chars[(startIndex + 1) .. lastIndex];
    }

    private static Boolean TryParseBase64(ReadOnlySpan<Char> chars, out Byte[] bytes)
    {
        if (chars.Length == 0)
        {
            bytes = Array.Empty<Byte>();
            return true;
        }

        var len = chars.Length / 4 * 3;

        if (0 < len)
        {
            if (chars[^1] == '=')
            {
                len--;
            }

            if (chars[^2] == '=')
            {
                len--;
            }
        }

        var result = new Byte[len];
        if (Convert.TryFromBase64Chars(chars, result, out var bytesWritten))
        {
            if (bytesWritten < result.Length)
            {
                Array.Resize(ref result, bytesWritten);
            }

            bytes = result;
            return true;
        }

        bytes = null;
        return false;
    }

    private static BsonDateTime ParseDateTime(ReadOnlySpan<Char> s, String format)
    {
        var dateTime = DateTime.ParseExact(s, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

        return new BsonDateTime(dateTime);
    }

    private ReadOnlySpan<Char> GetSpan()
    {
        return buffer.GetSpan(tokPos, tokEPos);
    }

    private void HandleDateTime3()
    {
        var chars = GetSpan();

        yylval.Value = ParseDateTime(chars[1..^1], "yyyy-MM-dd");
    }

    private void HandleDateTime5()
    {
        var chars = GetSpan();

        yylval.Value = ParseDateTime(chars[1..^1], "yyyy-MM-dd HH:mm");
    }

    private void HandleDateTime6()
    {
        var chars = GetSpan();

        yylval.Value = ParseDateTime(chars[1..^1], "yyyy-MM-dd HH:mm:ss");
    }

    private void HandleDateTime7()
    {
        const String format1 = "yyyy-MM-dd HH:mm:ss.f";
        const String format2 = "yyyy-MM-dd HH:mm:ss.ff";
        const String format3 = "yyyy-MM-dd HH:mm:ss.fff";
        const String format4 = "yyyy-MM-dd HH:mm:ss.ffff";
        const String format5 = "yyyy-MM-dd HH:mm:ss.fffff";
        const String format6 = "yyyy-MM-dd HH:mm:ss.ffffff";
        const String format7 = "yyyy-MM-dd HH:mm:ss.fffffff";

        var chars = GetSpan()[1..^1];

        if (chars.Length == format1.Length)
        {
            yylval.Value = ParseDateTime(chars, format1);
        }
        else if (chars.Length == format2.Length)
        {
            yylval.Value = ParseDateTime(chars, format2);
        }
        else if (chars.Length == format3.Length)
        {
            yylval.Value = ParseDateTime(chars, format3);
        }
        else if (chars.Length == format4.Length)
        {
            yylval.Value = ParseDateTime(chars, format4);
        }
        else if (chars.Length == format5.Length)
        {
            yylval.Value = ParseDateTime(chars, format5);
        }
        else if (chars.Length == format6.Length)
        {
            yylval.Value = ParseDateTime(chars, format6);
        }
        else
        {
            yylval.Value = ParseDateTime(chars[0..format7.Length], format7);
        }
    }

    private void HandleNumber()
    {
        var chars = GetSpan();

        yylval.Value = new BsonDecimal128(Decimal128.Parse(chars.ToString()));
    }

    private void HandleTrue()
    {
        yylval.Value = BsonBoolean.True;
    }

    private void HandleFalse()
    {
        yylval.Value = BsonBoolean.False;
    }

    private void HandleNull()
    {
        yylval.Value = BsonNull.Value;
    }

    private void HandleObjectId()
    {
        // OBJECTID\({WS}*{STRING}{WS}*\)
        const String Prefix = "OBJECTID(";
        const String Suffix = ")";

        var literalChars = GetSpan();

        var oidChars = StripDoubleQuotes(literalChars[Prefix.Length .. ^Suffix.Length]);

        if (!ObjectId.TryParse(oidChars.ToString(), out var objectId))
        {
            throw new FormatException($"String {literalChars.ToString()} is not valid OBJECTID literal");
        }

        yylval.Value = new BsonObjectId(objectId);
    }

    private void HandleRegex()
    {
        var chars = GetSpan();

        var index = chars.LastIndexOf('/');

        yylval.Value = new BsonRegularExpression(
            chars[1 .. index].ToString().Replace("//", "/"),
            chars[(index + 1) .. ^0].ToString());
    }

    private void HandleString()
    {
        var chars = GetSpan();

        yylval.Value = new BsonString(chars[1..^1].ToString().Replace("\"\"", "\""));
    }

    private void HandleSimpleUuid()
    {
        // UUID\({WS}*{STRING}{WS}*\)
        const String Prefix = "UUID(";
        const String Suffix = ")";

        var literalChars = GetSpan();

        var guidChars = StripDoubleQuotes(literalChars[Prefix.Length .. ^Suffix.Length]);

        if (!Guid.TryParseExact(guidChars, "D", out var guid))
        {
            throw new FormatException($"String {literalChars.ToString()} is not valid UUID literal");
        }

        yylval.Value = new BsonBinaryData(guid, GuidRepresentation.Standard);
    }

    private void HandleExtendedUuid()
    {
        // UUID\({WS}*{STRING}{WS}*{COMMA}{WS}*{STRING}{WS}*\)
        const String Prefix = "UUID(";
        const String Suffix = ")";

        var literalChars = GetSpan();

        var chars = literalChars[Prefix.Length..^Suffix.Length];

        var commaIndex = chars.IndexOf(Comma);

        var reprChars = StripDoubleQuotes(chars[0 .. commaIndex]);
        var guidChars = StripDoubleQuotes(chars[(commaIndex + 1) ..]);

        if (!Enum.TryParse<GuidRepresentation>(reprChars.ToString(), out var representation)
            || !Guid.TryParseExact(guidChars, "D", out var guid))
        {
            throw new FormatException($"String {literalChars.ToString()} is not valid UUID literal");
        }

        try
        {
            yylval.Value = new BsonBinaryData(guid, representation);
        }
        catch (InvalidOperationException ex)
        {
            throw new FormatException($"String {literalChars.ToString()} is not valid UUID literal", ex);
        }
        catch (ArgumentException ex)
        {
            throw new FormatException($"String {literalChars.ToString()} is not valid UUID literal", ex);
        }
    }

    private void HandleSimpleBinary()
    {
        // BINARY\({WS}*{STRING}{WS}*\)
        const String Prefix = "BINARY(";
        const String Suffix = ")";

        var literalChars = GetSpan();

        var base64Chars = StripDoubleQuotes(literalChars[Prefix.Length .. ^Suffix.Length]);

        if (!TryParseBase64(base64Chars, out var bytes))
        {
            throw new FormatException($"String {literalChars.ToString()} is not valid Binary literal");
        }

        yylval.Value = new BsonBinaryData(bytes, BsonBinarySubType.Binary);
    }

    private void HandleExtendedBinary()
    {
        // BINARY\({WS}*{STRING}{WS}*{COMMA}{WS}*{STRING}{WS}*\)
        const String Prefix = "BINARY(";
        const String Suffix = ")";

        var literalChars = GetSpan();

        var chars = literalChars[Prefix.Length..^Suffix.Length];

        var commaIndex = chars.IndexOf(Comma);

        var subTypeChars = StripDoubleQuotes(chars[0 .. commaIndex]);
        var base64Chars = StripDoubleQuotes(chars[(commaIndex + 1) ..]);

        if (!Enum.TryParse<BsonBinarySubType>(subTypeChars.ToString(), out var subType)
            || !TryParseBase64(base64Chars, out var bytes))
        {
            throw new FormatException($"String {literalChars.ToString()} is not valid Binary literal");
        }

        try
        {
            yylval.Value = new BsonBinaryData(bytes, subType);
        }
        catch (ArgumentException ex)
        {
            throw new FormatException($"String {literalChars.ToString()} is not valid Binary literal", ex);
        }
    }

    private void HandleExpression()
    {
        var chars = GetSpan();

        yylval.Text = chars[2..^1].ToString();
    }

    private void HandlePlainPath()
    {
        var chars = GetSpan();

        yylval.Text = chars.ToString();
    }

    private void HandleQuotedPath()
    {
        var chars = GetSpan();

        yylval.Text = chars[1..^1].ToString().Replace("``", "`");
    }

    private void HandleSelfPath()
    {
        yylval.Text = null;
    }
}
