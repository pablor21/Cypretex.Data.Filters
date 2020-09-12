using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cypretex.Data.Filters
{
    /// <summary>
    /// Represents a Condition to be included in a filter
    /// </summary>
    public class WhereCondition
    {

        protected IList<WhereCondition> _andChain;
        protected IList<WhereCondition> _orChain;
        protected IList<WhereCondition> _notChain;


        /// <summary>
        /// The field to filter
        /// </summary>
        /// <value></value>
        public string? Field { get; set; }
        /// <summary>
        /// The comparer
        /// </summary>
        /// <value></value>
        public string? Comparator { get; set; }
        /// <summary>
        /// The vale to compare
        /// </summary>
        /// <value></value>
        public dynamic? Value { get; set; }

        /// <summary>
        /// AND chained conditions
        /// </summary>
        /// <value></value>
        [JsonConverter(typeof(Utils.InterfaceConverter<List<WhereCondition>, IList<WhereCondition>>))]
        public IList<WhereCondition> And
        {
            get
            {
                this._andChain = this._andChain ?? new List<WhereCondition>();
                return this._andChain;
            }
            set
            {
                this._andChain = value;
            }
        }

        /// <summary>
        /// OR chained conditions
        /// </summary>
        /// <value></value>
        [JsonConverter(typeof(Utils.InterfaceConverter<List<WhereCondition>, IList<WhereCondition>>))]
        public IList<WhereCondition> Or
        {
            get
            {
                this._orChain = this._orChain ?? new List<WhereCondition>();
                return this._orChain;
            }
            set
            {
                this._orChain = value;
            }
        }
        /// <summary>
        /// NOT chained conditions
        /// </summary>
        /// <value></value>
        [JsonConverter(typeof(Utils.InterfaceConverter<List<WhereCondition>, IList<WhereCondition>>))]
        public IList<WhereCondition> Not
        {
            get
            {
                this._notChain = this._notChain ?? new List<WhereCondition>();
                return this._notChain;
            }
            set
            {
                this._notChain = value;
            }
        }

        /// <summary>
        /// Has the condition a single field condition
        /// </summary>
        /// <value></value>
        public bool HasFieldCondition
        {
            get
            {
                return this.Field != null && this.Comparator != WhereComparator.NONE;
            }
        }



        /// <summary>
        /// Add conditions to the AND chain
        /// </summary>
        /// <param name="conds">The conditions</param>
        /// <returns>The WhereCondition</returns>
        public WhereCondition AndWhere(params WhereCondition[] conds)
        {
            foreach (WhereCondition cond in conds)
            {
                this.And.Add(cond);
            }

            return this;
        }

        /// <summary>
        /// Add a condition to the AND chain
        /// </summary>
        /// <param name="field">The field name</param>
        /// <param name="comparer">The where comparer</param>
        /// <param name="value">The value to compare</param>
        /// <returns>The WhereCondition</returns>
        public WhereCondition AndWhere(string field, string comparer, dynamic? value = null)
        {
            return this.AndWhere(new WhereCondition()
            {
                Field = field,
                Comparator = comparer,
                Value = value
            });
        }

        /// <summary>
        /// Add conditions to the OR chain
        /// </summary>
        /// <param name="conds">The conditions</param>
        /// <returns>The WhereCondition</returns>
        public WhereCondition OrWhere(params WhereCondition[] conds)
        {
            foreach (WhereCondition cond in conds)
            {
                this.Or.Add(cond);
            }
            return this;
        }

        /// <summary>
        /// Add a condition to the OR chain
        /// </summary>
        /// <param name="field">The field name</param>
        /// <param name="comparer">The where comparer</param>
        /// <param name="value">The value to compare</param>
        /// <returns>The WhereCondition</returns>
        public WhereCondition OrWhere(string field, string comparer, dynamic? value = null)
        {
            return this.OrWhere(new WhereCondition()
            {
                Field = field,
                Comparator = comparer,
                Value = value
            });
        }

        /// <summary>
        /// Add conditions to the NOT chain
        /// </summary>
        /// <param name="conds">The conditions</param>
        /// <returns>The WhereCondition</returns>
        public WhereCondition WhereNot(params WhereCondition[] conds)
        {
            foreach (WhereCondition cond in conds)
            {
                this.Not.Add(cond);
            }
            return this;
        }

        /// <summary>
        /// Add a condition to the NOT chain
        /// </summary>
        /// <param name="field">The field name</param>
        /// <param name="comparer">The where comparer</param>
        /// <param name="value">The value to compare</param>
        /// <returns>The WhereCondition</returns>
        public WhereCondition WhereNot(string field, string? comparer, dynamic? value = null)
        {
            return this.WhereNot(new WhereCondition()
            {
                Field = field,
                Comparator = comparer,
                Value = value
            });
        }
    }
}
