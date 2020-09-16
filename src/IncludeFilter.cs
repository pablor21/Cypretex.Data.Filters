using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cypretex.Data.Filters
{
    public class IncludeFilter
    {
        public string Field { get; set; }
        public string? As { get; set; }
        public WhereCondition Where { get; set; } = new WhereCondition();
        [JsonConverter(typeof(Utils.InterfaceConverter<List<IncludeFilter>, IList<IncludeFilter>>))]
        public IList<IncludeFilter> With { get; set; } = new List<IncludeFilter>();

    }
}
