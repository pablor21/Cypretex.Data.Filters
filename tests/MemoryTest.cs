using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Cypretex.Data.Filters;
using System.Linq.Expressions;

namespace Cypretex.Data.Filters.Tests
{
    public class MemoryTests
    {

        public MemoryTests()
        {
            var c = new List<User>();
            int documentIndex = 1;
            for (int i = 1; i < 101; i++)
            {
                User user = new User()
                {
                    Id = i,
                    FirstName = $"FirstName {i}",
                    LastName = $"LastName {i}",
                    AnualSalary = i,
                    Phone = i.ToString(),
                    Documents = new List<Document>(),
                    Parent = i > 1 ? c[i - 2] : null
                };

                for (var j = 1; j < 10; j++)
                {
                    user.Documents = user.Documents.Append(new Document()
                    {
                        Owner = user,
                        Name = $"Document {documentIndex}",
                        Id = documentIndex,
                    });
                    documentIndex++;
                }
                //Console.WriteLine(user.Documents.Count());
                //user.PrincipalDocument = user.Documents.FirstOrDefault();
                //user.Documents = null;
                c.Add(user);
            }
            users = c.AsQueryable();
        }

        protected IQueryable<User> users;



        [Fact]
        public void IntFilter()
        {

            IFilter filter = (new Filter());
            filter.AndWhere(new WhereCondition()
            {
                Field = "Id",
                Comparator = WhereComparator.EQUALS,
                Value = 50
            });
            filter.Properties = "Id, FirstName, LastName, Parent[Id,FirstName], PrincipalDocument[Id,Owner[Id]],Documents[Id]";
            var result = users.Filter(filter);
            result.ToList().ForEach((u) =>
            {
                Console.WriteLine(u.DisplayProperties(true));
            });
            /// id  eq
            filter = (new Filter());
            filter.AndWhere(new WhereCondition()
            {
                Field = "Id",
                Comparator = WhereComparator.EQUALS,
                Value = 1
            });
            var user = users.Filter<User>(filter).First<User>();
            Assert.Equal(1, user.Id);

            /// id  gt
            filter = (new Filter());
            filter.AndWhere(new WhereCondition()
            {
                Field = "Id",
                Comparator = WhereComparator.GREATHER_THAN,
                Value = 1
            });
            user = users.Filter<User>(filter).First<User>();
            Assert.Equal(2, user.Id);

            /// id  gte
            filter = (new Filter());
            filter.AndWhere(new WhereCondition()
            {
                Field = "Id",
                Comparator = WhereComparator.GE,
                Value = 1
            });
            user = users.Filter<User>(filter).First<User>();
            Assert.Equal(1, user.Id);
        }
    }
}
