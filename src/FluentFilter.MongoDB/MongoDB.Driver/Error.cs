// <copyright file="Error.cs" company="Georgy Losenkov">
// Copyright (c) Georgy Losenkov. All rights reserved.
// </copyright>

using System;

namespace MongoDB.Driver;

internal static class Error
{
    internal static Exception ArgumentNull(String paramName)
    {
        return new ArgumentNullException(paramName);
    }
}