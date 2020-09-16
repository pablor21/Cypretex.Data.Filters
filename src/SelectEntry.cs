using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cypretex.Data.Filters
{
    public class SelectEntry
    {
        public string Property { get; set; }
        public IList<SelectEntry> Childs { get; set; } = new List<SelectEntry>();

        public SelectEntry AddChildProperty(SelectEntry entry)
        {
            Childs.Add(entry);
            return this;
        }
    }

}