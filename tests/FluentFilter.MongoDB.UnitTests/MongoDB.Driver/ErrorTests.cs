using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver;

public class ErrorTests
{
    public static IEnumerable<String> ParameterNames()
    {
        yield return null;
        yield return "one";
        yield return "two";
        yield return "three";
    }

    public static IEnumerable<Object[]> DataFor_ArgumentNull_ReturnsValue()
    {
        foreach (var parameterName in ParameterNames())
        {
            yield return new Object[] { parameterName };
        }
    }

    [Theory]
    [MemberData(nameof(DataFor_ArgumentNull_ReturnsValue))]
    internal void ArgumentNull_ReturnsValue(String parameterName)
    {
        Error.ArgumentNull(parameterName)
            .Should().BeOfType<ArgumentNullException>()
            .Which.ParamName.Should().Be(parameterName);
    }
}