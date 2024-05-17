// <copyright file="MongoTypeAliases.cs" company="Georgy Losenkov">
// Copyright (c) Georgy Losenkov. All rights reserved.
// </copyright>

using System;
using MongoDB.Bson;

namespace MongoDB.Driver.Filter.Fluent;

/// <summary>
/// String aliases for Bson types that may be used for type checks.
/// </summary>
public static class MongoTypeAliases
{
    /// <summary>
    /// String alias for <see cref="BsonType.Double"/>.
    /// </summary>
    public const String Double = "double";

    /// <summary>
    /// String alias for <see cref="BsonType.String"/>.
    /// </summary>
    public const String String = "string";

    /// <summary>
    /// String alias for <see cref="BsonType.Document"/>.
    /// </summary>
    public const String Document = "object";

    /// <summary>
    /// String alias for <see cref="BsonType.Array"/>.
    /// </summary>
    public const String Array = "array";

    /// <summary>
    /// String alias for <see cref="BsonType.Binary"/>.
    /// </summary>
    public const String BinaryData = "binData";

    /// <summary>
    /// String alias for <see cref="BsonType.Undefined"/>.
    /// </summary>
    [Obsolete("Deprecated")]
    public const String Undefined = "undefined";

    /// <summary>
    /// String alias for <see cref="BsonType.ObjectId"/>.
    /// </summary>
    public const String ObjectId = "objectId";

    /// <summary>
    /// String alias for <see cref="BsonType.Boolean"/>.
    /// </summary>
    public const String Boolean = "bool";

    /// <summary>
    /// String alias for <see cref="BsonType.DateTime"/>.
    /// </summary>
    public const String Date = "date";

    /// <summary>
    /// String alias for <see cref="BsonType.Null"/>.
    /// </summary>
    public const String Null = "null";

    /// <summary>
    /// String alias for <see cref="BsonType.RegularExpression"/>.
    /// </summary>
    public const String RegularExpression = "regex";

    /// <summary>
    /// String alias for no longer supported DbPointer.
    /// </summary>
    [Obsolete("Deprecated")]
    public const String DBPointer = "dbPointer";

    /// <summary>
    /// String alias for <see cref="BsonType.JavaScript"/>.
    /// </summary>
    public const String JavaScript = "javascript";

    /// <summary>
    /// String alias for <see cref="BsonType.Symbol"/>.
    /// </summary>
    [Obsolete("Deprecated")]
    public const String Symbol = "symbol";

    /// <summary>
    /// String alias for <see cref="BsonType.Int32"/>.
    /// </summary>
    public const String Int32 = "int";

    /// <summary>
    /// String alias for <see cref="BsonType.Timestamp"/>.
    /// </summary>
    public const String Timestamp = "timestamp";

    /// <summary>
    /// String alias for <see cref="BsonType.Int64"/>.
    /// </summary>
    public const String Int64 = "long";

    /// <summary>
    /// String alias for <see cref="BsonType.Decimal128"/>.
    /// </summary>
    public const String Decimal128 = "decimal";

    /// <summary>
    /// String alias for <see cref="BsonType.MinKey"/>.
    /// </summary>
    public const String MinKey = "minKey";

    /// <summary>
    /// String alias for <see cref="BsonType.MaxKey"/>.
    /// </summary>
    public const String MaxKey = "maxKey";

    /// <summary>
    /// String alias for any numeric value, i.e. <see cref="BsonType.Decimal128"/>, <see cref="BsonType.Double"/>, <see cref="BsonType.Int32"/>, <see cref="BsonType.Int64"/>.
    /// </summary>
    public const String Number = "number";
}
