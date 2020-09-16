using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cypretex.Data.Filters.Parsers
{
    public interface IFIlterParser
    {

        /// <summary>
        /// Parse the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="source"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IQueryable<T> Parse<T>(IFilter filter, IQueryable<T> source) where T : class, new();


        /// <summary>
        /// Parse the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="source"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Task<IQueryable<T>> ParseAsync<T>(IFilter filter, IQueryable<T> source) where T : class, new();
    }
}
