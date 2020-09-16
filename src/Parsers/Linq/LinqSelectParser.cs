using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cypretex.Data.Filters.Parsers.Linq
{
    internal class SelectEntry
    {
        public string Property { get; set; }
        public IList<SelectEntry> Childs { get; set; } = new List<SelectEntry>();

        public SelectEntry AddChildProperty(SelectEntry entry)
        {
            Childs.Add(entry);
            return this;
        }
    }

    public static class LinqSelectParser
    {

        /// <summary>
        /// Parse the selection clause
        /// </summary>
        /// <param name="properties">The select properties clause</param>
        /// <param name="source"></param>
        /// <param name="suffix"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IQueryable<T> ParseSelect<T>(string properties, IQueryable<T> source, string suffix = "")
        where T : class, new()
        {
            if (String.IsNullOrEmpty(properties) || properties.Equals("*"))
            {
                return source;
            }
            List<SelectEntry> props = ParsePropertyNames(properties.Replace(" ", String.Empty));
            Expression<Func<T, T>> expression = (Expression<Func<T, T>>)Process<T, T>(props, typeof(T), typeof(T), suffix);
            return source.Select<T, T>(expression);
        }

        /// <summary>
        /// Convert the string of the properties to a SelectEntry collection
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        private static List<SelectEntry> ParsePropertyNames(string properties, string prefix = "")
        {
            string pattern = @"((?<complex>[A-Za-z0-9]+)\[(?<props>[[A-Za-z0-9,]+)\]?)+|(?<simple>\w+)";
            List<SelectEntry> ret = new List<SelectEntry>();
            MatchCollection matches = Regex.Matches(properties, pattern);
            if (matches.Any())
            {
                matches.ToList().ForEach(o =>
                {
                    if (!String.IsNullOrEmpty(o.Groups["simple"].Value))
                    {
                        ret.Add(new SelectEntry()
                        {
                            Property = o.Value
                        });
                    }
                    else
                    {
                        SelectEntry entry = new SelectEntry()
                        {
                            Property = o.Groups["complex"].Value,
                            Childs = ParsePropertyNames(o.Groups["props"].Value)
                        };
                        ret.Add(entry);
                    }
                });
            }

            return ret;
        }

        private static Expression Process<T, TReturn>(List<SelectEntry> props, Type sourceType, Type destType, string suffix = "")
            where T : class, new()
            where TReturn : class, new()
        {

            List<MemberAssignment> bindings = new List<MemberAssignment>();
            ParameterExpression parameter = Expression.Parameter(sourceType, sourceType.Name);
            foreach (SelectEntry entry in props)
            {
                bindings.AddRange(ProcessEntry(entry, parameter));
            }
            NewExpression newData = Expression.New(destType);
            MemberInitExpression initExpression = Expression.MemberInit(newData, bindings);
            Expression finalExpression = MakeLambda(initExpression, parameter);
            //Console.WriteLine(finalExpression);
            return (Expression<Func<T, TReturn>>)finalExpression;

        }


        private static IList<MemberAssignment> ProcessEntry(SelectEntry entry, ParameterExpression parameter, string suffix = "")
        {
            List<MemberAssignment> bindings = new List<MemberAssignment>();
            Type type = parameter.Type;

            //process the sub properties
            if (entry.Childs.Count > 0)
            {


                PropertyInfo propertyInfo = parameter.Type.GetProperty(entry.Property);
                MemberExpression originalMember = Expression.Property(parameter, propertyInfo);

                Type childType = propertyInfo.PropertyType;
                ParameterExpression childParameter = Expression.Parameter(childType, entry.Property);
                List<MemberAssignment> subBindings = new List<MemberAssignment>();



                var isCollection = Utils.IsEnumerable(childParameter);
                //The property is a Enumerable
                if (isCollection)
                {
                    // Get the type of the child elements
                    Type elementType = childType.GetGenericArguments()[0];
                    // Create an expression for the parameter
                    ParameterExpression elementParameter = Expression.Parameter(elementType, elementType.Name);
                    foreach (SelectEntry e in entry.Childs)
                    {
                        subBindings.AddRange(ProcessEntry(e, elementParameter));
                    }

                    // Convert the list to Queryable
                    Expression asQueryable = Utils.AsQueryable(childParameter);
                    //Expression to generate a new element of the list
                    NewExpression newElementExpression = Expression.New(elementType);
                    MemberInitExpression initElementExpression = Expression.MemberInit(newElementExpression, subBindings);
                    //Iterate over the original elements (Queryable.Select)
                    MethodCallExpression selectExpr = Expression.Call(typeof(Queryable), "Select", new[] { elementType, elementType }, asQueryable, MakeLambda(initElementExpression, elementParameter));
                    //Convert the result to list
                    Expression toListCall = Expression.Call(typeof(Enumerable), "ToList", selectExpr.Type.GetGenericArguments(), selectExpr);
                    // Check for null original collection (avoid null pointer)
                    Expression notNullConditionExpression = Expression.NotEqual(childParameter, Expression.Constant(null, childParameter.Type));
                    Expression trueExpression = MakeLambda(Expression.Convert(toListCall, childParameter.Type), childParameter);
                    Expression falseExpression = MakeLambda(Expression.Constant(null, childParameter.Type), childParameter);
                    Expression notNullExpression = Expression.Condition(notNullConditionExpression, trueExpression, falseExpression);
                    //Invocate the null-check expression
                    Expression invocation = Expression.Invoke(MakeLambda(Expression.Invoke(notNullExpression, originalMember), childParameter), originalMember);
                    // Add the invocation to the bindings on the original element
                    bindings.Add(Expression.Bind(propertyInfo, invocation));
                }
                else
                {
                    // Add the child entities to the initialization bindings of the object
                    foreach (SelectEntry e in entry.Childs)
                    {
                        subBindings.AddRange(ProcessEntry(e, childParameter));
                    }
                    // Add the lambda to the bindings of the property in the parent object
                    bindings.Add(Expression.Bind(propertyInfo, CreateNewObject(childParameter, childType, subBindings, originalMember)));
                }

            }
            else
            {
                // Add the property to the init bindings
                bindings.Add(AssignProperty(parameter.Type, entry.Property, parameter));
            }
            return bindings;
        }

        /// <summary>
        /// Create a new object for assignement on the member of the result object
        /// </summary>
        /// <param name="parameter">The child parameter</param>
        /// <param name="objectType">The type of the object</param>
        /// <param name="bindings">The bindings for the initialization</param>
        /// <param name="originalMember">The member on the original (parent) object</param>
        /// <returns></returns>
        private static Expression CreateNewObject(ParameterExpression parameter, Type objectType, List<MemberAssignment> bindings, MemberExpression originalMember)
        {
            // Create new object of type childType
            NewExpression newExpression = Expression.New(objectType);
            // Initialize the members of the object
            MemberInitExpression initExpression = Expression.MemberInit(newExpression, bindings);
            // Check for not null original property (avoid the null pointer)
            Expression notNullConditionExpression = Expression.NotEqual(parameter, Expression.Constant(null, objectType));
            Expression trueExpression = MakeLambda(initExpression, parameter);
            Expression falseExpression = MakeLambda(Expression.Constant(null, objectType), parameter);
            Expression notNullExpression = Expression.Condition(notNullConditionExpression, trueExpression, falseExpression);
            // Create the lambda
            Expression initLambdaExpression = MakeLambda(notNullExpression, parameter);
            // Invoke the initialization expression and the not null expression
            Expression invocation = Expression.Invoke(Expression.Invoke(initLambdaExpression, originalMember), originalMember);
            return invocation;
        }


        private static MemberAssignment AssignProperty(Type type, string propertyName, Expression parameter)
        {
            PropertyInfo propertyInfo = type.GetProperty(propertyName);
            MemberExpression originalMember = Expression.Property(parameter, propertyInfo);
            return Expression.Bind(propertyInfo, originalMember);
        }


        private static Expression MakeLambda(Expression predicate, params ParameterExpression[] parameters)
        {

            List<ParameterExpression> resultParameters = new List<ParameterExpression>();
            foreach (ParameterExpression parameter in parameters)
            {
                var resultParameterVisitor = new ParameterVisitor();
                resultParameterVisitor.Visit(parameter);
                resultParameters.Add((ParameterExpression)resultParameterVisitor.Parameter);
            }
            return Expression.Lambda(predicate, resultParameters);
        }


        private class ParameterVisitor : ExpressionVisitor
        {
            public Expression Parameter
            {
                get;
                private set;
            }
            protected override Expression VisitParameter(ParameterExpression node)
            {
                Parameter = node;
                return node;
            }
        }
    }
}