using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Cypretex.Data.Filters.Tests
{
    public class User
    {
        private ICollection<Document> _documents;
        public User()
        {
        }

        public User(Action<object, string> lazyLoader)
        {
            LazyLoader = lazyLoader;
        }

        [JsonIgnore]
        public Action<object, string> LazyLoader { get; set; }

        [Key]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public long? AnualSalary { get; set; }
        public string Phone { get; set; }
        [InverseProperty("Owner")]
        public ICollection<Document> Documents {get;set;}
        // {
        //     get
        //     {
        //         this._documents = LazyLoader.Load(this, ref _documents);
        //         return _documents;
        //     }
        //     set => _documents = value;
        // }

        public virtual Document PrincipalDocument { get; set; }

        public virtual User Parent { get; set; }
    }

    public class Document
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        //[ForeignKey("")]

        public virtual User Owner { get; set; }
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

    public static class PocoLoadingExtensions
    {
        public static TRelated Load<TRelated>(
            this Action<object, string> loader,
            object entity,
            ref TRelated navigationField,
            [CallerMemberName] string navigationName = null)
            where TRelated : class
        {
            loader?.Invoke(entity, navigationName);
            return navigationField;
        }
    }
}
