using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Cypretex.Data.Filters;
using System.Linq.Expressions;
using Cypretex.Data.Filters.Tests.Database;
using Microsoft.EntityFrameworkCore;
using Cypretex.Data.Filters.Parsers.Linq;
using System.Reflection;

namespace Cypretex.Data.Filters.Tests
{
    public class DatabaeTests
    {
        TestDbContext context;
        public DatabaeTests()
        {
            //Arrange    
            var factory = new ConnectionFactory();

            //Get the instance of BlogDBContext  
            context = factory.CreateContextForInMemory();

            // var c = new List<User>();
            // int documentIndex = 1;
            // for (int i = 1; i < 101; i++)
            // {
            //     User user = new User()
            //     {
            //         Id = i,
            //         FirstName = $"FirstName {i}",
            //         LastName = $"LastName {i}",
            //         AnualSalary = i,
            //         Phone = i.ToString(),
            //         Documents = new List<Document>(),
            //         Parent = i > 1 ? context.Users.Where(x => x.Id == i - 1).First() : null
            //     };
            //     context.Add(user);
            //     context.SaveChanges();
            //     for (var j = 1; j < 10; j++)
            //     {
            //         Document d = new Document()
            //         {
            //             Name = $"Document {documentIndex}",
            //             Id = documentIndex,
            //             Owner = user
            //         };
            //         context.Documents.Add(d);
            //         context.SaveChanges();
            //         user.Documents.Append(d);

            //         documentIndex++;
            //     }
            //     context.SaveChanges();
            //     //Console.WriteLine(user.Documents.Count());
            //     //user.PrincipalDocument = user.Documents.First();
            //     //user.Documents = null;
            //     //context.Users.Add(user);
            //     //context.SaveChanges();
            // }
            // context.SaveChanges();
            // users = context.Users;
        }

        protected IQueryable<User> users;

        [Fact]
        public void TestDatabase()
        {
            IFilter filter = (new Filter());
            // filter.AndWhere(new WhereCondition()
            // {
            //     Field = "Documents.Id",
            //     Comparator = WhereComparator.EQUALS,
            //     Value = "10"
            // });
            //filter.Properties = "Id,Parent,Documents";
            //filter.Order("-Documents.Id");

            filter.Include(new IncludeFilter()
            {
                Field = "Documents",
                With = new List<IncludeFilter>(){
                    new IncludeFilter(){
                        Field="Owner"
                    }
                }

            });


            // Expression<Func<User, bool>> lambda = x => (x.Parent != null ? x.AnualSalary > x.Parent.AnualSalary : true) && x.Id == 2;
            // Console.WriteLine(lambda);
            // var result = context.Users.Where(lambda).Include("Documents").Include("Parent");
            //Console.WriteLine(Expression.Invoke(expression, parameter));
            var result = context.Users.Filter(filter).Take(1);
            //Console.WriteLine(result);
            // var result = users.Where(User => User.Id == 50 && User.Parent != null && User.Parent.Id == 49).Select(User => new User()
            // {
            //     Id = User.Id,
            //     Parent = (User.Parent != null) ? new User()
            //     {
            //         Id = User.Parent.Id
            //     } : null
            // });
            result.ToList().ForEach((u) =>
            {
                Console.WriteLine(u.DisplayProperties(true));
            });
        }
    }
}