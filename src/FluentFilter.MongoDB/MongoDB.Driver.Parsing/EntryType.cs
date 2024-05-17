// <copyright file="EntryType.cs" company="Georgy Losenkov">
// Copyright (c) Georgy Losenkov. All rights reserved.
// </copyright>

namespace MongoDB.Driver.Parsing;

internal enum EntryType
{
    And,
    AnyIs,
    AnyNis,
    ArrayExpr,
    Between,
    Eq,
    Exist,
    Gt,
    Gte,
    In,
    List,
    Lt,
    Lte,
    Match,
    MatchOp,
    Nbetween,
    Neq,
    Nexist,
    Nin,
    Nmatch,
    NmatchOp,
    Not,
    Or,
    TypeEq,
    TypeIn,
    TypeNeq,
    TypeNin,
    Value,
    ValueExpr,
}
