using System;
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

        public static bool IsValidType(Type type){
            return AvailableCastTypes.Contains(type) || type.GetTypeInfo().IsEnum;
        }

        public static object TryCastFieldValueType(object value, Type type)
        {
            if (value == null || !IsValidType(type)){
                var v=(value!=null?value.GetType().Name:"Null");
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

        public static PropertyInfo GetDeclaringProperty(Type t, string name)
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

        public static PropertyInfo GetDeclaringProperty(Expression e, string name)
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



        public static bool IsEnumerable(Expression prop)
        {
            return prop.Type.GetTypeInfo().ImplementedInterfaces.FirstOrDefault(x => x.Name == "IEnumerable") != null;
        }

        public static Expression ToStaticParameterExpressionOfType(object obj, Type type)
            => Expression.Convert(
                Expression.Property(
                    Expression.Constant(new { obj }),
                    "obj"),
                type);

        public static Type GetNotNullableType(Type t)
        {
            if (Nullable.GetUnderlyingType(t) != null)
            {
                return Nullable.GetUnderlyingType(t);
            }
            return t;
        }

        public static void CheckType(Expression prop, WhereCondition condition, Type requiredType){
            if(!requiredType.IsAssignableFrom(prop.Type)){
               throw new InvalidCastException($"{condition.Field}: {condition.Comparator} can be applied to {requiredType.Name} only!");
            }
        }

    }
}