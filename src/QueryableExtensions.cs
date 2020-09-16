using System.Linq;
using System.Linq.Expressions;
using Cypretex.Data.Filters.Parsers.Linq;

namespace Cypretex.Data.Filters
{
    /// <summary>
    /// Linq Extension Methods for filter and conditions
    /// </summary>
    public static class QueryableExtensions
    {
        public static IQueryable<T> Filter<T>(this IQueryable<T> source, IFilter filter) where T : class, new()
        {
            return filter.Apply<T>(source);
        }

        public static IQueryable<T> Filter<T>(this IQueryable<T> source, WhereCondition condition) where T : class, new()
        {
            return source.Filter<T>(new Filter()
            {
                Where = condition
            });
        }

        public static IQueryable<T> Where<T>(this IQueryable<T> source, WhereCondition condition) where T : class, new()
        {
            return source.Filter<T>(condition);
        }

        public static IQueryable<T> Select<T>(this IQueryable<T> source, string select) where T : class, new()
        {
            return LinqSelectParser.ParseSelect<T>(select, source);
        }
    }
}