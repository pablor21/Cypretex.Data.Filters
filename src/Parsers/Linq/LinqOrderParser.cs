using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Cypretex.Data.Filters.Parsers.Linq
{
    public static class LinqOrderParser
    {


        public static readonly MethodInfo QueryableOrderBy = Utils.QueryableType.GetRuntimeMethods().Single(
                method => method.Name == "OrderBy"
                        && method.IsGenericMethodDefinition
                        && method.GetGenericArguments().Length == 2
                        && method.GetParameters().Length == 2);

        public static readonly MethodInfo QueryableOrderByDescending = Utils.QueryableType.GetRuntimeMethods().Single(
         method => method.Name == "OrderByDescending"
                 && method.IsGenericMethodDefinition
                 && method.GetGenericArguments().Length == 2
                 && method.GetParameters().Length == 2);


        public static readonly MethodInfo QueryableThenBy = Utils.QueryableType.GetRuntimeMethods().Single(
                method => method.Name == "ThenBy"
                        && method.IsGenericMethodDefinition
                        && method.GetGenericArguments().Length == 2
                        && method.GetParameters().Length == 2);
        public static readonly MethodInfo QueryableThenByDescending = Utils.QueryableType.GetRuntimeMethods().Single(
                method => method.Name == "ThenByDescending"
                        && method.IsGenericMethodDefinition
                        && method.GetGenericArguments().Length == 2
                        && method.GetParameters().Length == 2);
        public static IOrderedQueryable<T> ParseOrder<T>(IList<string> order, IQueryable<T> source)
        {
            Type type = typeof(T);

            if (order != null && order.Any())
            {
                int index = 0;
                foreach (string o in order)
                {
                    source = ParseOrder<T>(o, source, index);
                    index++;
                }
            }
            return (IOrderedQueryable<T>)source;
        }

        public static IOrderedQueryable<T> ParseOrder<T>(string field, IQueryable<T> source, int index = 0)
        {
            Type type = typeof(T);
            bool isDesc = field.StartsWith("-");
            string fieldName = field;
            if (isDesc)
            {
                fieldName = field.Substring(1);
            }
            ParameterExpression arg = Expression.Parameter(type, "x" + fieldName);
            Expression expr = arg;
            string[] spl = fieldName.Split('.');

            for (int i = 0; i < spl.Length; i++)
            {
                PropertyInfo pi = Utils.GetDeclaringProperty(type, spl[i]);
                expr = Expression.Property(expr, pi);
                type = pi.PropertyType;
            }

            Type delegateType = typeof(Func<,>).MakeGenericType(typeof(T), type);
            Expression lambda = Expression.Lambda(delegateType, expr, arg);
            MethodInfo m = index == 0
               ? isDesc ? QueryableOrderByDescending : QueryableOrderBy
               : isDesc ? QueryableThenByDescending : QueryableThenBy;

            return ((IOrderedQueryable<T>)m.MakeGenericMethod(typeof(T), type)
                .Invoke(null, new object[] { source, lambda }));
        }
    }
}