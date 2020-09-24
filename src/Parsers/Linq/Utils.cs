using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Cypretex.Data.Filters.Parsers.Linq
{
    public static class Utils
    {

        public static readonly Type StringType = typeof(string);

        public static readonly Type QueryableType = typeof(Queryable);

        public static readonly MethodInfo AsQueryableMethod = QueryableType.GetRuntimeMethods().FirstOrDefault(
        method => method.Name == "AsQueryable" && method.IsStatic);
        public static readonly MethodInfo AsListMethod = QueryableType.GetRuntimeMethods().FirstOrDefault(
        method => method.Name == "ToList" && method.IsStatic);
        private static readonly MethodInfo changeTypeMethod = typeof(Convert).GetMethod("ChangeType",
                    new Type[] { typeof(object), typeof(Type) });

        public static Type[] AvailableCastTypes =
        {
            typeof(DateTime),
            typeof(DateTime?),
            typeof(DateTimeOffset),
            typeof(DateTimeOffset?),
            typeof(TimeSpan),
            typeof(TimeSpan?),
            typeof(bool),
            typeof(bool?),
            typeof(byte?),
            typeof(sbyte?),
            typeof(short),
            typeof(short?),
            typeof(ushort),
            typeof(ushort?),
            typeof(int),
            typeof(int?),
            typeof(uint),
            typeof(uint?),
            typeof(long),
            typeof(long?),
            typeof(ulong),
            typeof(ulong?),
            typeof(Guid),
            typeof(Guid?),
            typeof(double),
            typeof(double?),
            typeof(float),
            typeof(float?),
            typeof(decimal),
            typeof(decimal?),
            typeof(char),
            typeof(char?),
            typeof(string)
        };

        public static bool IsValidType(Type type)
        {
            return AvailableCastTypes.Contains(type) || type.GetTypeInfo().IsEnum;
        }

        public static object TryCastFieldValueType(object value, Type type)
        {
            if (value == null || !IsValidType(type))
            {
                var v = (value != null ? value.GetType().Name : "Null");
                throw new InvalidCastException($"Cannot convert {v} to type {type.Name}.");
            }

            var valueType = value.GetType();

            if (valueType == type)
                return value;

            if (type.GetTypeInfo().BaseType == typeof(Enum))
                return Enum.Parse(type, Convert.ToString(value));


            var s = Convert.ToString(value);
            object res;

            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GenericTypeArguments[0];
                res = Activator.CreateInstance(typeof(Nullable<>).MakeGenericType(type));
            }
            else
            {
                res = Activator.CreateInstance(type);
            }

            var argTypes = new[] { StringType, type.MakeByRefType() };
            object[] args = { s, res };
            var tryParse = type.GetRuntimeMethod("TryParse", argTypes);

            if (!(bool)(tryParse?.Invoke(null, args) ?? false))
                throw new InvalidCastException($"Cannot convert value to type {type.Name}.");

            return args[1];
        }

        public static PropertyInfo GetPropertyInfo(Type t, string name)
        {
            var p = t.GetRuntimeProperties().SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (p == null)
            {
                throw new InvalidOperationException(string.Format("Property '{0}' not found on type '{1}'", name, t));
            }

            if (t != p.DeclaringType)
            {
                p = p.DeclaringType.GetRuntimeProperties().SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
            return p;
        }

        public static PropertyInfo GetPropertyInfo(Expression e, string name)
        {
            var t = e.Type;
            var p = t.GetRuntimeProperties().SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (p == null)
            {
                throw new InvalidOperationException(string.Format("Property '{0}' not found on type '{1}'", name, t));
            }

            if (t != p.DeclaringType)
            {
                p = p.DeclaringType.GetRuntimeProperties().SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
            return p;
        }

        /// <summary>
        /// Cast IEnumerable to IQueryable.
        /// </summary>
        /// <param name="prop">IEnumerable expression</param>
        /// <returns>IQueryable expression.</returns>
        public static Expression AsQueryable(Expression prop)
        {
            return Expression.Call(
                        AsQueryableMethod.MakeGenericMethod(prop.Type.GenericTypeArguments.Single()),
                        prop);
        }

        public static Expression AsList(Expression prop)
        {
            return Expression.Call(
                        AsListMethod.MakeGenericMethod(prop.Type.GenericTypeArguments.Single()),
                        prop);
        }


        public static bool IsEnumerable(Expression prop)
        {
            return prop.Type.GetTypeInfo().ImplementedInterfaces.FirstOrDefault(x => x.Name == "IEnumerable") != null;
        }

        public static bool IsRefCondition(WhereCondition condition)
        {

            return (condition.Value != null && condition.Value.GetType() == typeof(string) && condition.Value.ToString().StartsWith("@") && (!condition.Value.ToString().StartsWith("@@")));
        }

        public static Expression ConvertToNullable(Expression prop)
        {
            //convert the property to nullable type
            Type type = prop.Type;
            try
            {
                type = typeof(Nullable<>).MakeGenericType(prop.Type);
                prop = Expression.Convert(prop, type);
            }
            catch { }
            return prop;
        }

        public static object ParseValue(object value)
        {
            if (value != null)
            {
                if (value.GetType() == StringType)
                {
                    if (((string)value).StartsWith("@@"))
                    {
                        return ((string)value).Substring(1);
                    }
                }
            }
            return value;
        }

        public static bool ConditionAcceptNullValues(WhereCondition condition)
        {
            if (condition == null || condition.Value == null || IsRefCondition(condition))
            {
                return true;
            }
            switch (condition.Comparator.ToUpperInvariant())
            {
                case WhereComparator.N:
                case WhereComparator.IS_NULL:
                case WhereComparator.NULL_OR_EMPTY:
                case WhereComparator.NLEMP:
                    return true;
                default:
                    return false;
            }
        }

        public static Expression ToDynamicParameterExpressionOfType(Expression expression, Type type)
        {
            if (type == expression.Type)
            {
                return expression;
            }

            Type conversionType = Nullable.GetUnderlyingType(type) ?? type;

            // if (conversionType == expression.Type)
            // {
            //     return expression;
            // }

            Expression targetType = Expression.Constant(conversionType);
            Expression convertedValue = Expression.Convert(expression, typeof(object));
            return Expression.Convert(Expression.Call(changeTypeMethod, convertedValue, targetType), type);
        }


        public static Expression ToStaticParameterExpressionOfType(object obj, Type type)
        {
            return Expression.Convert(Expression.Property(Expression.Constant(new { obj }), "obj"), type);
        }
        // => obj == null || obj.GetType() != type ? Expression.Convert(
        //     Expression.Property(
        //         Expression.Constant(new { obj }),
        //         "obj"),
        //     type) : Expression.Constant(obj);

        public static Type GetNotNullableType(Type t)
        {
            if (Nullable.GetUnderlyingType(t) != null)
            {
                return Nullable.GetUnderlyingType(t);
            }
            return t;
        }

        public static Type GetEnumerableTypeArg(Type type)
        {
            return type.GetGenericArguments()[0];
        }

        public static bool IsEnumerable(Type type)
        {
            // Console.WriteLine(type);
            // return type.IsGenericType
            //     && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
            return type.GetTypeInfo().ImplementedInterfaces.FirstOrDefault(x => x.Name == "IEnumerable") != null;
        }

        public static Type GetIEnumerableImpl(Type type)
        {
            // Get IEnumerable implementation. Either type is IEnumerable<T> for some T, 
            // or it implements IEnumerable<T> for some T. We need to find the interface.
            if (IsEnumerable(type))
                return type;
            Type[] t = type.FindInterfaces((m, o) => IsEnumerable(m), null);
            return t[0];
        }

        public static void CheckEnumerable(Expression prop, WhereCondition condition)
        {
            if (!IsEnumerable(prop))
            {
                throw new InvalidCastException($"{condition.Field}: {condition.Comparator} can be applied to Enumerable only!");
            }
        }

        public static void CheckType(Expression prop, WhereCondition condition, Type requiredType)
        {
            if (!requiredType.IsAssignableFrom(prop.Type))
            {
                throw new InvalidCastException($"{condition.Field}: {condition.Comparator} can be applied to {requiredType.Name} only!");
            }
        }

    }
}