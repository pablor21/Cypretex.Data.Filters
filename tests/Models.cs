using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Cypretex.Data.Filters.Tests
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public long? AnualSalary { get; set; }
        public string Phone { get; set; }
        public IEnumerable<Document> Documents { get; set; }

        public Document PrincipalDocument { get; set; }

        public User Parent { get; set; }
    }

    public class Document
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public User Owner { get; set; }
    }

    public static class PropertyDescriptor
    {
        public static string DisplayProperties(this object obj, JsonSerializerSettings options = null)
        {
            options = options ?? new JsonSerializerSettings();
            options.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            return JsonConvert.SerializeObject(obj, options);
        }

        public static string DisplayProperties(this object obj, bool formatted = false)
        {
            return obj.DisplayProperties(new JsonSerializerSettings()
            {
                Formatting = formatted ? Formatting.Indented : Formatting.None
            });
        }
    }
}
