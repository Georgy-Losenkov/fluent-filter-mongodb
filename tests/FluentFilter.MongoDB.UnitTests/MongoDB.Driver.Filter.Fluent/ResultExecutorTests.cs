using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Parsing;
using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace MongoDB.Driver.Filter.Fluent;

public class ResultExecutorTests
{
    [Fact]
    public void Ctor_ThrowsArgumentNullException()
    {
        var mockExpressionEvaluator = new Mock<Func<String, BsonValue>>(MockBehavior.Strict);

        new Func<Object>(() => new ResultExecutor(null, 0, null))
            .Should().Throw<ArgumentNullException>().WithParameterName("entries");

        new Func<Object>(() => new ResultExecutor(null, 0, mockExpressionEvaluator.Object))
            .Should().Throw<ArgumentNullException>().WithParameterName("entries");

        new Func<Object>(() => new ResultExecutor(new List<Entry>(), 0, null))
            .Should().Throw<ArgumentNullException>().WithParameterName("expressionEvaluator");

        mockExpressionEvaluator.VerifyAll();
    }

    public static IEnumerable<Object[]> Execute_ThrowsInvalidOperationException_Data()
    {
        // CreateDocumentArray
        yield return new Object[] {
            new[] {
                new Entry(EntryType.Or, null, null, -1, -1),
                new Entry(EntryType.And, null, null, 0, -1),
            },
            new Dictionary<String, BsonValue>(),
        };

        // CreateValueArray
        yield return new Object[] {
            new[] {
                new Entry(EntryType.Value, null, BsonNull.Value, -1, -1),
                new Entry(EntryType.In, "A", null, 0, -1),
            },
            new Dictionary<String, BsonValue>(),
        };

        // CreateValueArray
        yield return new Object[] {
            new[] {
                new Entry(EntryType.Value, null, BsonNull.Value, -1, -1),
                new Entry(EntryType.List, null, null, 0, -1),
                new Entry(EntryType.In, "A", null, 1, -1),
            },
            new Dictionary<String, BsonValue>(),
        };

        // CreateValue
        yield return new Object[] {
            new[] {
                new Entry(EntryType.List, null, null, -1, -1),
                new Entry(EntryType.Eq, "A", null, 1, -1),
            },
            new Dictionary<String, BsonValue>(),
        };

        // CreateValue
        yield return new Object[] {
            new[] {
                new Entry(EntryType.List, null, null, -1, -1),
            },
            new Dictionary<String, BsonValue>(),
        };

        // CreateValueArray
        yield return new Object[] {
            new[] {
                new Entry(EntryType.ArrayExpr, "2 + 2", null, -1, -1),
                new Entry(EntryType.In, "A", null, 0, -1),
            },
            new Dictionary<String, BsonValue> { { "2 + 2", BsonNull.Value } },
        };
    }

    [Theory]
    [MemberData(nameof(Execute_ThrowsInvalidOperationException_Data))]
    internal void Execute_ThrowsInvalidOperationException(
        IReadOnlyList<Entry> entries,
        IEnumerable<KeyValuePair<String, BsonValue>> expressions)
    {
        // Arrange
        var mockExpressionEvaluator = new Mock<Func<String, BsonValue>>(MockBehavior.Strict);

        foreach (var expression in expressions)
        {
            mockExpressionEvaluator
                .Setup(x => x(expression.Key))
                .Returns(expression.Value);
        }

        var executor = new ResultExecutor(entries, entries.Count - 1, mockExpressionEvaluator.Object);

        // Act & Assert
        executor.Invoking(x => x.Execute()).Should().ThrowExactly<InvalidOperationException>();

        mockExpressionEvaluator.VerifyAll();
    }
}
