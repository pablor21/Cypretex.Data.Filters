using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cypretex.Data.Filters
{
    public interface IIncludeFilter
    {
        public string Field { get; set; }
        public string As { get; set; }
        // waiting for ef 5.0 (will support where clause inside include filter)
        //public IFilter Filter { get; set; }
        
        [JsonConverter(typeof(Utils.InterfaceConverter<List<IIncludeFilter>, IList<IIncludeFilter>>))]
        public IList<IIncludeFilter> With { get; set; }
    }
}
