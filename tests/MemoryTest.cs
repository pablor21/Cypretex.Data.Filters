using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Cypretex.Data.Filters;
using System.Linq.Expressions;
using Cypretex.Data.Filters.Parsers.Linq;


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
                    user.Documents.Add(new Document()
                    {
                        //Owner = user,
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

            Console.WriteLine(Convert.ChangeType(((object)"1"), typeof(Int32)));


            IFilter filter = (new Filter());
            filter.As = "u";
            filter.AndWhere(new WhereCondition()
            {
                Field = "Parent.Id",
                Comparator = WhereComparator.IN,
                Value = "@Documents.Id"
            });


            // u => (Convert(u.Id, Nullable`1) == Convert(ChangeType(Convert(u.Phone, Object), System.Nullable`1[System.Int32]), Nullable`1))
            // u => (Convert(u.Id, Nullable`1) == Convert(ChangeType(Convert(u.Phone, Object), System.Nullable`1[System.Int32]), Nullable`1)))
            // filter.Properties = "Id, FirstName, LastName, Parent[Id,FirstName], PrincipalDocument[Id],Documents[Id]";
            filter.Properties = "Id,Parent[Id],Documents[id]";
            // filter.Include(new IncludeFilter()
            // {
            //     Field = "Parent",
            //     Filter = ((IFilter)new Filter()).AndWhere(new WhereCondition()
            //     {
            //         Field = "Id",
            //         Comparator = WhereComparator.EQUALS
            //     })
            // });

            // Expression<Func<User, bool>> expression1 = new LinqWhereExpressionBuilder().BuildPredicate<User>(filter.Where, "user");
            // Console.WriteLine(expression1);
            Expression<Func<User, bool>> expression2 = u => u.Documents.Select(d => d.Id).Contains(u.Id);
            expression2 = u => Convert.ChangeType(u.Id, typeof(Nullable<int>)) == (u.Parent != null ? Convert.ChangeType(u.Parent.Id, typeof(Nullable<Int32>)) : Convert.ChangeType(1, typeof(Nullable<Int32>)));
            //Console.WriteLine(expression2);
            ParameterExpression parameter = Expression.Parameter(typeof(User), "u");
            Expression idProperty = Expression.Convert(Expression.Property(parameter, "Id"), typeof(Nullable<int>));
            Expression phoneProperty = Expression.Property(parameter, "Phone");

            Expression condition = phoneProperty;
            condition = Cypretex.Data.Filters.Parsers.Linq.Utils.ToDynamicParameterExpressionOfType(condition, idProperty.Type);
            Expression equals = Expression.Equal(idProperty, condition);
            Expression<Func<User, bool>> expression = Expression.Lambda<Func<User, bool>>(equals, parameter);
            //Console.WriteLine(expression);

            //Expression expression1 = Expression.Call(typeof(Queryable), "Select", new[] { typeof(User), typeof(User) }, Expression.Convert(Expression.Constant(users), typeof(IQueryable<User>)), Expression.Lambda(Expression.Constant("Id"), parameter));
            // Expression expression1 = Expression.Call(Expression.Convert(Expression.Constant(users), typeof(IQueryable<User>)), "Select", null, Expression.Constant("Id", typeof(string)));
            // Console.WriteLine(expression1);

            var methods = typeof(IEnumerable<>).GetMethods().Where(n => n.Name=="Select");
            foreach(MethodInfo mi in methods){
                Console.WriteLine(mi);
            }

            //var result = users.Filter(filter);
            var result = users.Select("Documents.Id").Take(1);
            //Console.WriteLine(result);
            result = users.Filter(filter).Take(1);
            
            // Console.WriteLine(result);
            result.ToList().ForEach((u) =>
            {
                Console.WriteLine(u.DisplayProperties(true));
            });
            // /// id  eq
            // filter = (new Filter());
            // filter.AndWhere(new WhereCondition()
            // {
            //     Field = "Id",
            //     Comparator = WhereComparator.EQUALS,
            //     Value = 1
            // });
            // var user = users.Filter<User>(filter).First<User>();
            // Assert.Equal(1, user.Id);

            // /// id  gt
            // filter = (new Filter());
            // filter.AndWhere(new WhereCondition()
            // {
            //     Field = "Id",
            //     Comparator = WhereComparator.GREATHER_THAN,
            //     Value = 1
            // });
            // user = users.Filter<User>(filter).First<User>();
            // Assert.Equal(2, user.Id);

            // /// id  gte
            // filter = (new Filter());
            // filter.AndWhere(new WhereCondition()
            // {
            //     Field = "Id",
            //     Comparator = WhereComparator.GE,
            //     Value = 1
            // });
            // user = users.Filter<User>(filter).First<User>();
            // Assert.Equal(1, user.Id);
        }
    }
}
