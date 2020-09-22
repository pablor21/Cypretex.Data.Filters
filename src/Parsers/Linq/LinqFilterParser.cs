using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Cypretex.Data.Filters.Parsers.Linq
{
    public class LinqFilterParser : IFIlterParser
    {

        public IQueryable<T> Parse<T>(IFilter filter, IQueryable<T> source) where T : class, new()
        {
            //parse where clause
            source = LinqWhereExpressionBuilder.Parse<T>(filter.Where, source, filter.As);
            //parse the order clause
            source = LinqOrderParser.ParseOrder<T>(filter.OrderBy, source);
            //parse the pagination    
            source = LinqLimitParser.ParseLimit<T>(filter.Skip, filter.Take, source);
            //parse the select clause
            source = LinqSelectParser.ParseSelect<T>(filter.Properties, source, filter.As);
            //return the result
            return source;
        }

        public async Task<IQueryable<T>> ParseAsync<T>(IFilter filter, IQueryable<T> source) where T : class, new()
        {
            return await Task.Run(() => Parse(filter, source));
        }



    }


}
