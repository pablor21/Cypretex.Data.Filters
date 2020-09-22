using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Cypretex.Data.Filters.Parsers;

namespace Cypretex.Data.Filters
{
    public interface IFilter
    {

        /// <summary>
        /// The name of the parameter for the filter
        /// </summary>
        /// <value></value>
        public string As { get; set; }

        /// <summary>
        /// The where clause to apply
        /// </summary>
        /// <value></value>
        public WhereCondition Where { get; set; }

        /// <summary>
        /// The include clauses to apply
        /// </summary>
        /// <value></value>
        [JsonConverter(typeof(Utils.InterfaceConverter<List<IncludeFilter>, IList<IncludeFilter>>))]
        public IList<IncludeFilter> With { get; set; }

        /// <summary>
        /// The order by clauses to apply
        /// </summary>
        /// <value></value>
        [JsonConverter(typeof(Utils.InterfaceConverter<List<string>, IList<string>>))]
        public IList<string> OrderBy { get; set; }

        /// <summary>
        /// Skip
        /// </summary>
        /// <value></value>
        public int Skip { get; set; }

        /// <summary>
        /// Take
        /// </summary>
        /// <value></value>
        public int Take { get; set; }

        /// <summary>
        /// The select fields to include
        /// </summary>
        /// <value></value>
        public string Properties { get; set; }

        /// <summary>
        /// Gets if the filter has pagination
        /// </summary>
        /// <value></value>
        public bool IsPaginated
        {
            get
            {
                return Skip > -1 && Take > 0;
            }
        }

        /// <summary>
        /// The filter parser
        /// </summary>
        /// <value></value>
        [JsonIgnore]
        public IFIlterParser FilterParser { get; set; }


        /// <summary>
        /// Ad conditions to the AND chain
        /// </summary>
        /// <param name="conds">The conditions</param>
        /// <returns>The filter</returns>
        public IFilter AndWhere(params WhereCondition[] conds)
        {
            this.Where.AndWhere(conds);
            return this;
        }

        /// <summary>
        /// Ad conditions to the OR chain
        /// </summary>
        /// <param name="conds">The conditions</param>
        /// <returns>The filter</returns>
        public IFilter OrWhere(params WhereCondition[] conds)
        {
            this.Where.OrWhere(conds);
            return this;
        }

        /// <summary>
        /// Ad conditions to the NOT chain
        /// </summary>
        /// <param name="conds">The conditions</param>
        /// <returns>The filter</returns>
        public IFilter WhereNot(params WhereCondition[] conds)
        {
            this.Where.WhereNot(conds);
            return this;
        }

        /// <summary>
        /// Ad a include filter to the chain
        /// </summary>
        /// <param name="filters">The filters to include</param>
        /// <returns>The filter</returns>
        public IFilter Include(params IncludeFilter[] filters)
        {

            foreach (IncludeFilter f in filters)
            {
                this.With.Add(f);
            }
            return this;
        }

        /// <summary>
        /// Add  order by parameters
        /// </summary>
        /// <param name="fields">The fields to order (if it starts with `-` the order is descending, ascending otherwise)</param>
        /// <returns>The filter</returns>
        public IFilter Order(params string[] fields)
        {
            foreach (string field in fields)
            {
                this.OrderBy.Add(field);
            }
            return this;
        }


        /// <summary>
        /// Validates the filter
        /// </summary>
        /// <returns></returns>
        public bool Validate()
        {
            return true;
        }



        /// <summary>
        /// Apply the filter to a queryable list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public IQueryable<T> Apply<T>(IQueryable<T> source) where T : class, new()
        {
            Validate();
            return this.FilterParser.Parse(this, source);
        }

        /// <summary>
        /// Apply the filter to a queryable list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public Task<IQueryable<T>> ApplyAsync<T>(IQueryable<T> source) where T : class, new()
        {
            Validate();
            return this.FilterParser.ParseAsync(this, source);
        }
    }
}
