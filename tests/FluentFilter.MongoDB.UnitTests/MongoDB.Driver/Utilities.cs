using System;

namespace MongoDB.Driver;

public static class Utilities
{
    public static void Invoke(Type type1, Action action)
    {
        var method = action
            .Method
            .GetGenericMethodDefinition()
            .MakeGenericMethod(type1);

        method.Invoke(action.Target, Array.Empty<Object>());
    }

    public static void Invoke(Type type1, Type type2, Action action)
    {
        var method = action
            .Method
            .GetGenericMethodDefinition()
            .MakeGenericMethod(type1, type2);

        method.Invoke(action.Target, Array.Empty<Object>());
    }

    public static void Invoke(Type type1, Type type2, Type type3, Action action)
    {
        var method = action
            .Method
            .GetGenericMethodDefinition()
            .MakeGenericMethod(type1, type2, type3);

        method.Invoke(action.Target, Array.Empty<Object>());
    }

    public static void Invoke(Type type1, Type type2, Type type3, Type type4, Action action)
    {
        var method = action
            .Method
            .GetGenericMethodDefinition()
            .MakeGenericMethod(type1, type2, type3, type4);

        method.Invoke(action.Target, Array.Empty<Object>());
    }
}