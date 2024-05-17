// <copyright file="Entry.cs" company="Georgy Losenkov">
// Copyright (c) Georgy Losenkov. All rights reserved.
// </copyright>

using System;
using MongoDB.Bson;

namespace MongoDB.Driver.Parsing;

internal readonly struct Entry
{
    public Entry(EntryType type, String text, BsonValue value, Int32 index1, Int32 index2)
    {
        Type = type;
        Text = text;
        Value = value;
        Index1 = index1;
        Index2 = index2;
    }

    public EntryType Type { get; }

    public String Text { get; }

    public BsonValue Value { get; }

    public Int32 Index1 { get; }

    public Int32 Index2 { get; }
}
