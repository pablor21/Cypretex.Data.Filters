using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Cypretex.Data.Filters.Utils;

namespace Cypretex.Data.Filters.Parsers
{
    public interface IOrderParser
    {
        

        public IOrderedQueryable<T> ParseOrder<T>(IList<string> order, IQueryable<T> source)
        {
            Type type = typeof(T);

            if (order != null && order.Any())
            {
                int index = 0;
                foreach (string o in order)
                {
                    source = this.ParseOrder<T>(o, source, index);
                    index++;
                }
            }
            return (IOrderedQueryable<T>)source;
        }

        public IOrderedQueryable<T> ParseOrder<T>(string field, IQueryable<T> source, int index = 0);
    }
}
