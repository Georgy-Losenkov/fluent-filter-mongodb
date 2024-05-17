// <copyright file="ExpressionParser.cs" company="Georgy Losenkov">
// Copyright (c) Georgy Losenkov. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver.Filter.Fluent;

namespace MongoDB.Driver.Parsing;

internal partial class ExpressionParser
{
    private readonly List<Entry> m_entries = new List<Entry>(32);

    internal ExpressionParser(ExpressionScanner scanner)
        : base(scanner)
    {
    }

    internal FluentFilterFactory Result { get; private set; }

    private void Success(Int32 index)
    {
        Result = new FluentFilterFactory(m_entries, index);
    }

    private Int32 AddEntry(EntryType type, Int32 index1 = -1, Int32 index2 = -1)
    {
        m_entries.Add(new Entry(type, text: null, value: null, index1: index1, index2: index2));
        return m_entries.Count - 1;
    }

    private Int32 AddTextEntry(EntryType type, String text, Int32 index1 = -1, Int32 index2 = -1)
    {
        m_entries.Add(new Entry(type, text: text, value: null, index1: index1, index2: index2));
        return m_entries.Count - 1;
    }

    private Int32 AddValueEntry(EntryType type, BsonValue value)
    {
        m_entries.Add(new Entry(type, text: null, value: value, index1: -1, index2: -1));
        return m_entries.Count - 1;
    }
}
