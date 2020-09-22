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

    public class LinqSelectParser
    {



        //Expression visitor instance
        private readonly Visitor visitor;

        /// <summary>
        /// Parse the selection clause
        /// </summary>
        /// <param name="properties">The select properties clause</param>
        /// <param name="source"></param>
        /// <param name="suffix"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IQueryable<T> ParseSelect<T>(string properties, IQueryable<T> source, string paramName = null)
        where T : class, new()
        {
            if (String.IsNullOrEmpty(properties) || properties.Equals("*"))
            {
                return source;
            }
            LinqSelectParser parser = new LinqSelectParser();
            List<SelectEntry> props = parser.ParsePropertyNames(properties.Replace(" ", String.Empty));
            Expression<Func<T, T>> expression = (Expression<Func<T, T>>)parser.Process<T, T>(props, typeof(T), typeof(T), paramName);
            return source.Select<T, T>(expression);
        }

        public LinqSelectParser()
        {
            this.visitor = new Visitor();
        }


        /// <summary>
        /// Convert the string of the properties to a SelectEntry collection
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        private List<SelectEntry> ParsePropertyNames(string properties, string prefix = "")
        {
            string pattern = @"((?<complex>[A-Za-z0-9]+)\[(?<props>[[A-Za-z0-9,]+)\]?)+|(?<simple>\w+)";
            List<SelectEntry> ret = new List<SelectEntry>();
            MatchCollection matches = Regex.Matches(properties.Replace(" ", ""), pattern);
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

        private Expression Process<T, TReturn>(List<SelectEntry> props, Type sourceType, Type destType, string paramName = null)
            where T : class, new()
            where TReturn : class, new()
        {
            paramName = String.IsNullOrEmpty(paramName) ? sourceType.Name : paramName;

            List<MemberAssignment> bindings = new List<MemberAssignment>();
            ParameterExpression parameter = Expression.Parameter(sourceType, paramName);
            foreach (SelectEntry entry in props)
            {
                bindings.AddRange(ProcessEntry(entry, parameter));
            }
            NewExpression newData = Expression.New(destType);
            MemberInitExpression initExpression = Expression.MemberInit(newData, bindings);
            Expression finalExpression = MakeLambda(initExpression, parameter);
            return (Expression<Func<T, TReturn>>)finalExpression;

        }


        private IList<MemberAssignment> ProcessEntry(SelectEntry entry, ParameterExpression parameter, string suffix = "")
        {
            List<MemberAssignment> bindings = new List<MemberAssignment>();
            Type type = parameter.Type;

            //process the sub properties
            if (entry.Childs.Count > 0)
            {


                PropertyInfo propertyInfo = Utils.GetPropertyInfo(parameter.Type, entry.Property);
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
                    ParameterExpression elementParameter = Expression.Parameter(elementType, entry.Property + ".Element");

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
                    Expression notNullLambda = MakeLambda(Expression.Invoke(notNullExpression, originalMember), childParameter);

                    //Invocate the null-check expression
                    Expression invocation = Expression.Invoke(notNullLambda, originalMember);
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
        private Expression CreateNewObject(ParameterExpression parameter, Type objectType, List<MemberAssignment> bindings, MemberExpression originalMember)
        {
            // Create new object of type childType
            NewExpression newExpression = (NewExpression)Expression.New(objectType);
            // Initialize the members of the object
            MemberInitExpression initExpression = Expression.MemberInit(newExpression, bindings);
            // Check for not null original property (avoid the null pointer)
            Expression notNullConditionExpression = Expression.NotEqual(parameter, Expression.Constant(null, objectType));
            Expression trueExpression = initExpression;
            Expression falseExpression = Expression.Constant(null, objectType);
            Expression notNullExpression = Expression.Condition(notNullConditionExpression, trueExpression, falseExpression);

            // Create the lambda
            Expression initLambdaExpression = MakeLambda(notNullExpression, parameter);

            // Invoke the initialization expression and the not null expression
            Expression invocation = Expression.Invoke(initLambdaExpression, originalMember);
            return invocation;
        }


        private MemberAssignment AssignProperty(Type type, string propertyName, Expression parameter)
        {
            PropertyInfo propertyInfo = Utils.GetPropertyInfo(type, propertyName);
            MemberExpression originalMember = Expression.Property(parameter, propertyInfo);
            return Expression.Bind(propertyInfo, originalMember);
        }


        public Expression MakeLambda(Expression predicate, params ParameterExpression[] parameters)
        {

            List<ParameterExpression> resultParameters = new List<ParameterExpression>();
            //var resultParameterVisitor = new ParameterVisitor();

            foreach (ParameterExpression parameter in parameters)
            {

                resultParameters.Add(((ParameterExpression)visitor.Visit(parameter)));
            }
            LambdaExpression expression = Expression.Lambda(visitor.Visit(predicate), parameters);
            visitor.Visit(expression.Body);
            return visitor.Visit(expression);
        }



    }
}