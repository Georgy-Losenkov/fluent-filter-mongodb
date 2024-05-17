using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver;

public class FluentFilterTests
{
    public static IEnumerable<String> ParameterNames()
    {
        yield return null;
        yield return "one";
        yield return "two";
        yield return "three";
    }

    public static IEnumerable<Object[]> DataFor_NotNullObject_ReturnsValue()
    {
        var values = new Object[] {
            new Object(),
            new Random(),
            "test"
        };

        foreach (var value in values)
        {
            foreach (var parameterName in ParameterNames())
            {
                yield return new Object[] { value, parameterName };
            }
        }
    }

    public static IEnumerable<Object[]> DataFor_NotNullString_ReturnsValue()
    {
        var values = new Object[] {
            "One",
            "Two",
            "Three"
        };

        foreach (var value in values)
        {
            foreach (var parameterName in ParameterNames())
            {
                yield return new Object[] { value, parameterName };
            }
        }
    }

    public static IEnumerable<Object[]> DataFor_Ensure_NullValue_ThrowsArgumentNotNullException()
    {
        foreach (var parameterName in ParameterNames())
        {
            yield return new Object[] { parameterName };
        }
    }

    [Theory]
    [MemberData(nameof(DataFor_NotNullObject_ReturnsValue))]
    internal void NotNull_Object_ReturnsValue(Object value, String parameterName)
    {
        Ensure.NotNull<Object>(value, parameterName).Should().BeSameAs(value);
    }

    [Theory]
    [MemberData(nameof(DataFor_NotNullString_ReturnsValue))]
    internal void NotNull_String_ReturnsValue(String value, String parameterName)
    {
        Ensure.NotNull<String>(value, parameterName).Should().BeSameAs(value);
    }

    [Theory]
    [MemberData(nameof(DataFor_Ensure_NullValue_ThrowsArgumentNotNullException))]
    internal void Ensure_NullObject_ThrowsArgumentNotNullException(String parameterName)
    {
        new Action(() => Ensure.NotNull<Object>(null, parameterName))
            .Should().ThrowExactly<ArgumentNullException>().WithParameterName(parameterName);
    }

    [Theory]
    [MemberData(nameof(DataFor_Ensure_NullValue_ThrowsArgumentNotNullException))]
    internal void Ensure_NullString_ThrowsArgumentNotNullException(String parameterName)
    {
        new Action(() => Ensure.NotNull<String>(null, parameterName))
            .Should().ThrowExactly<ArgumentNullException>().WithParameterName(parameterName);
    }
}