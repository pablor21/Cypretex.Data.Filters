using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Cypretex.Data.Filters.Parsers;
using Cypretex.Data.Filters.Parsers.Linq;

namespace Cypretex.Data.Filters
{

    /// <summary>
    /// Represents a filter
    /// </summary>
    public class Filter : IFilter
    {
        protected WhereCondition _where;
        protected IList<IncludeFilter> _with;

        protected IList<string> _orderBy;


        public WhereCondition Where
        {
            get
            {
                this._where = this._where ?? new WhereCondition();
                return this._where;
            }
            set
            {
                this._where = value;
            }
        }
        public IList<IncludeFilter> With
        {
            get
            {
                this._with = this._with ?? new List<IncludeFilter>();
                return this._with;
            }
            set
            {
                this._with = value;
            }
        }
        public IList<string> OrderBy
        {
            get
            {
                this._orderBy = this._orderBy ?? new List<string>();
                return this._orderBy;
            }
            set
            {
                this._orderBy = value;
            }
        }
        [JsonIgnore]
        public IFIlterParser FilterParser { get; set; } = new LinqFilterParser();
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = -1;
        public string Properties { get; set; } = "*";

    }
}
