// <copyright file="MongoOperators.cs" company="Georgy Losenkov">
// Copyright (c) Georgy Losenkov. All rights reserved.
// </copyright>

using System;

namespace MongoDB.Driver.Filter.Fluent;

internal static class MongoOperators
{
    internal const String And = "$and";
    internal const String ElemMatch = "$elemMatch";
    internal const String Eq = "$eq";
    internal const String Exists = "$exists";
    internal const String Gt = "$gt";
    internal const String Gte = "$gte";
    internal const String In = "$in";
    internal const String Lt = "$lt";
    internal const String Lte = "$lte";
    internal const String Regex = "$regex";
    internal const String Neq = "$ne";
    internal const String Nin = "$nin";
    internal const String Nor = "$nor";
    internal const String Not = "$not";
    internal const String Options = "$options";
    internal const String Or = "$or";
    internal const String Type = "$type";
}
