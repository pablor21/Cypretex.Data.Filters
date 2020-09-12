using System.Linq;
using System.Threading.Tasks;

namespace Cypretex.Data.Filters.Parsers.Linq
{
    public static class LinqLimitParser
    {
        public static IQueryable<T> ParseLimit<T>(int skip, int take, IQueryable<T> source)
        {
            if (skip > 0)
            {
                source = source.Skip(skip);
            }

            if (take > 0)
            {
                source = source.Take(take);
            }

            return source;
        }
    }
}