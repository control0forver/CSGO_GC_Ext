using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CSGO_GC_Ext.Utils;

internal static class Reflection
{
    public const string SelfPrefix = "this";

    public static IEnumerable<TResult> GetPropertyValues<TData, TResult>(
        TData data,
        IEnumerable<string> propertyPaths)
    {
        var parameter = Expression.Parameter(typeof(TData), "x");

        foreach (var path in propertyPaths)
        {
            var cleanPath = path.StartsWith($"{SelfPrefix}.") ? path.AsSpan(5) : path.AsSpan();

            if (cleanPath.IsEmpty)
            {
                yield return data is TResult result ? result : default!;
                continue;
            }

            Expression expression = parameter;
            Type currentType = typeof(TData);
            bool isValid = true;

            foreach (var propertyName in cleanPath.ToString().Split('.'))
            {
                var property = currentType.GetProperty(propertyName);
                if (property == null)
                {
                    isValid = false;
                    break;
                }

                expression = Expression.Property(expression, property);
                currentType = property.PropertyType;
            }

            if (!isValid)
            {
                yield return default!;
                continue;
            }

            if (typeof(TResult) != currentType)
            {
                expression = Expression.Convert(expression, typeof(TResult));
            }

            var lambda = Expression.Lambda<Func<TData, TResult>>(expression, parameter).Compile();
            yield return lambda(data);
        }
    }
}
