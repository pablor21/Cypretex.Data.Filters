using System;
using System.Linq;
using System.Linq.Expressions;
using Cypretex.Data.Filters.Utils;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;

namespace Cypretex.Data.Filters.Parsers
{
    public interface IWhereParser
    {

        public IQueryable<T> ParseWhere<T>(WhereCondition condition, IQueryable<T> source, string suffix = "");


    }
}
