using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Cypretex.Data.Filters.Parsers.Linq
{
    public class LinqWhereExpressionBuilder
    {
        /// <summary>
        /// IEnumerable Any method
        /// </summary>
        /// <returns></returns>
        public static MethodInfo EnumerableAnyMethod = typeof(Enumerable).GetMethods().Single(m => m.Name == "Any" && m.GetParameters().Length == 2);
        public static MethodInfo EnumerableAnyMethodNoParameters = typeof(Enumerable).GetMethods().Single(m => m.Name == "Any" && m.GetParameters().Length == 1);
        public static readonly Type StringType = typeof(string);
        public static readonly Type RegexType = typeof(Regex);
        public static readonly Type EnumerableType = typeof(IEnumerable<>);
        //Expression visitor instance
        private readonly Visitor visitor;

        public static readonly Dictionary<string, MethodInfo> StringMethods = new Dictionary<string, MethodInfo>(){
            {"StartsWith", StringType.GetRuntimeMethod("StartsWith", new[] { StringType, typeof(StringComparison) })},
            {"Equals", StringType.GetRuntimeMethod("Equals", new[] { StringType, typeof(StringComparison) })},
            {"Contains", StringType.GetRuntimeMethod("Contains", new[] { StringType, typeof(StringComparison) })},
            {"EndsWith", StringType.GetRuntimeMethod("EndsWith", new[] { StringType, typeof(StringComparison) })},
            {"Compare", StringType.GetRuntimeMethod("Compare", new[] { StringType, StringType, typeof(StringComparison) })},
            {"ToLower", StringType.GetRuntimeMethod("ToLower", new[] { StringType, typeof(StringComparison) })},
        };


        public static readonly Dictionary<string, MethodInfo> RegexMethods = new Dictionary<string, MethodInfo>(){
            {"IsMatch", RegexType.GetRuntimeMethod("IsMatch", new[]{StringType, typeof(RegexOptions)})}
        };

        public static IQueryable<T> Parse<T>(WhereCondition condition, IQueryable<T> source, string paramName = null)
        {
            Expression<Func<T, bool>> predicate = new LinqWhereExpressionBuilder().BuildPredicate<T>(condition, paramName);
            if (predicate != null)
            {
                source = source.Where(predicate);
            }
            else
            {
                source = source.AsQueryable<T>();
            }
            return source;
        }


        public LinqWhereExpressionBuilder()
        {
            this.visitor = new Visitor();
        }

        public Expression<Func<T, bool>> BuildPredicate<T>(WhereCondition condition, string paramName = null)
        {
            paramName = String.IsNullOrEmpty(paramName) ? typeof(T).Name : paramName;
            ParameterExpression parameter = Expression.Parameter(typeof(T), paramName);
            ParameterExpression original = Expression.Parameter(typeof(T), paramName);
            Expression expression = AddExpression<T>(parameter, parameter, null, condition);
            Expression<Func<T, bool>> lambda = null;
            if (parameter != null && expression != null)
            {
                lambda = (Expression<Func<T, bool>>)MakeLambda(expression, parameter);
            }
            return lambda;
        }

        public Expression AddExpression<T>(ParameterExpression parameter, ParameterExpression rootParameter, Expression expression, WhereCondition condition)
        {
            Expression newEpxression = expression;
            if (condition.HasFieldCondition)
            {
                newEpxression = GetExpressionForCondition(parameter, rootParameter, condition);
            }

            int i = 1;
            //AND conditions
            foreach (WhereCondition cond in condition.And)
            {
                Expression e1 = AddExpression<T>(parameter, rootParameter, newEpxression, cond);
                var args = new object[] { newEpxression, e1 };
                if (newEpxression != null)
                {

                    newEpxression = Expression.AndAlso(newEpxression, e1);
                }
                else
                {
                    newEpxression = e1;
                }
                i++;
            }


            //OR conditions
            foreach (WhereCondition cond in condition.Or)
            {
                Expression e1 = AddExpression<T>(parameter, rootParameter, newEpxression, cond);
                var args = new object[] { newEpxression, e1 };
                if (newEpxression != null)
                {
                    newEpxression = Expression.OrElse(newEpxression, e1);
                }
                else
                {
                    newEpxression = e1;
                }
                i++;
            }

            //NOT conditions
            foreach (WhereCondition cond in condition.Not)
            {
                Expression e1 = AddExpression<T>(parameter, rootParameter, newEpxression, cond);
                var args = new object[] { newEpxression, e1 };
                if (newEpxression != null)
                {
                    newEpxression = Expression.And(((LambdaExpression)newEpxression).Body, ((LambdaExpression)e1).Body);
                }
                else
                {
                    newEpxression = e1;
                }
                i++;
            }

            return newEpxression;
        }

        private Expression GetExpressionForCondition(Expression parameter, Expression rootParameter, WhereCondition condition)
        {
            Expression resultExpression = null;
            Expression childParameter = parameter;
            Expression predicate = Expression.IsTrue(Expression.Constant(true));
            Type childType = null;

            string[] properties = condition.Field.Split(".");
            if (properties.Count() == 1)
            {

                if (Utils.IsRefCondition(condition))
                {

                    Expression prop = Expression.Property(childParameter, properties[0]);
                    resultExpression = BuildSubqueryComparsion(prop, null, rootParameter, condition);

                }
                else
                {
                    resultExpression = BuildCondition(parameter, condition);
                    resultExpression = Expression.Condition(Expression.NotEqual(childParameter, Expression.Constant(null, childParameter.Type)), resultExpression, Expression.Equal(childParameter, Utils.ToStaticParameterExpressionOfType(condition.Value, childParameter.Type)));

                }
            }
            else
            {
                parameter = Expression.Property(parameter, properties[0]);
                var isCollection = Utils.IsEnumerable(parameter);
                if (isCollection)
                {
                    childType = Utils.GetEnumerableTypeArg(parameter.Type);
                    childParameter = Expression.Parameter(childType, childType.Name);
                }
                else
                {
                    childParameter = parameter;
                }

                //skip current property and get navigation property expression recursivly
                var innerProperties = string.Join(".", properties.Skip(1).ToList());
                predicate = GetExpressionForCondition(childParameter, rootParameter, new WhereCondition()
                {
                    Field = innerProperties,
                    Comparator = condition.Comparator,
                    Value = condition.Value
                });





                if (isCollection)
                {
                    //build subquery
                    resultExpression = BuildSubQuery(parameter, childType, MakeLambda(predicate, (ParameterExpression)childParameter));
                    //null-check
                    Expression falseExpression = Utils.ConditionAcceptNullValues(condition) ? Expression.Equal(parameter, Expression.Constant(null, parameter.Type)) : Expression.Equal(Expression.Constant(true), Expression.Constant(false));
                    resultExpression = Expression.Condition(Expression.NotEqual(parameter, Expression.Constant(null, parameter.Type)), resultExpression, falseExpression);

                }
                else
                {
                    resultExpression = predicate;
                    //null-check
                    Expression falseExpression = Utils.ConditionAcceptNullValues(condition) ? Expression.Equal(childParameter, Expression.Constant(null, childParameter.Type)) : Expression.Equal(Expression.Constant(true), Expression.Constant(false));
                    resultExpression = Expression.Condition(Expression.NotEqual(childParameter, Expression.Constant(null, childParameter.Type)), resultExpression, falseExpression);
                }

            }



            return resultExpression;
        }


        private Expression BuildSubQuery(Expression parameter, Type childType, Expression predicate)
        {
            parameter = Utils.AsQueryable(parameter);
            var anyMethod = EnumerableAnyMethod.MakeGenericMethod(childType);
            predicate = Expression.Call(anyMethod, parameter, predicate);
            return predicate;
        }
        private Expression BuildCondition(Expression parameter, WhereCondition condition)
        {

            Expression predicate = null;
            PropertyInfo childProperty = null;
            childProperty = Utils.GetPropertyInfo(parameter.Type, condition.Field);
            Expression prop = Utils.ConvertToNullable(Expression.Property(parameter, childProperty));

            //convert the property to nullable type
            predicate = BuildComparsion(prop, condition);


            if (predicate == null)
            {
                predicate = Expression.IsTrue(Expression.Constant(true));
            }

            return predicate;
        }

        private Expression BuildComparsion(Expression prop, WhereCondition condition)
        {
            switch (condition.Comparator.ToUpperInvariant())
            {
                case WhereComparator.EQ:
                case WhereComparator.EQUALS:
                    return IsEqual(prop, condition);
                case WhereComparator.NOT_EQUALS:
                case WhereComparator.NEQ:
                    return IsNotEqual(prop, condition);
                case WhereComparator.NOT_NULL:
                case WhereComparator.NN:
                    return IsNotNull(prop, condition);
                case WhereComparator.IS_NULL:
                    return IsNull(prop, condition);
                case WhereComparator.LESS_THAN:
                case WhereComparator.LT:
                    return IsLessThan(prop, condition);
                case WhereComparator.LESS_OR_EQUALS:
                case WhereComparator.LE:
                    return IsLessOrEqualThan(prop, condition);
                case WhereComparator.GREATHER_THAN:
                case WhereComparator.GT:
                    return IsGreaterThan(prop, condition);
                case WhereComparator.GREATHER_OR_EQUALS:
                case WhereComparator.GE:
                    return IsGreaterOrEqualThan(prop, condition);
                case WhereComparator.IN:
                    return In(prop, condition);
                case WhereComparator.NOT_IN:
                case WhereComparator.NIN:
                    return NotIn(prop, condition);
                case WhereComparator.BETWEEN:
                case WhereComparator.BTW:
                    return Between(prop, condition);
                case WhereComparator.NOT_BETWEEN:
                case WhereComparator.NBTW:
                    return NotBetween(prop, condition);
                //only for string
                case WhereComparator.STARTS_WITH:
                case WhereComparator.SW:
                    return StartsWith(prop, condition);
                case WhereComparator.NOT_STARTS_WITH:
                case WhereComparator.NSW:
                    return NotStartsWith(prop, condition);
                case WhereComparator.ENDS_WITH:
                case WhereComparator.EW:
                    return EndsWith(prop, condition);
                case WhereComparator.NOT_ENDS_WITH:
                case WhereComparator.NEW:
                    return NotEndsWith(prop, condition);
                case WhereComparator.EMPTY:
                case WhereComparator.EMP:
                    return IsEmpty(prop, condition);
                case WhereComparator.NOT_EMPTY:
                case WhereComparator.NEMP:
                    return IsNotEmpty(prop, condition);
                case WhereComparator.NULL_OR_EMPTY:
                case WhereComparator.NLEMP:
                    return IsNullOrEmpty(prop, condition);
                case WhereComparator.NOT_NULL_OR_EMPTY:
                case WhereComparator.NNLEMP:
                    return IsNotNullOrEmpty(prop, condition);
                case WhereComparator.REGEX:
                case WhereComparator.RE:
                    return Matches(prop, condition);
                case WhereComparator.NOT_REGEX:
                case WhereComparator.NRE:
                    return NotMatches(prop, condition);
                default:
                    throw new InvalidOperationException($"The operator {condition.Comparator} is not applicable to {condition.Field}!");
            }
        }

        private Expression BuildSubqueryComparsion(Expression leftProperty, Expression rightProperty, Expression rootParameter, WhereCondition condition)
        {
            Expression predicate = null;
            string rightFieldNames = (((string)condition.Value).StartsWith("@") ? ((string)condition.Value).Substring(1) : (string)condition.Value);
            string[] properties = rightFieldNames.Split(".");

            PropertyInfo rightProprertyInfo = null;
            rightProperty = rightProperty ?? rootParameter;
            Expression notNullExpression = Expression.NotEqual(rightProperty, Expression.Constant(null, rightProperty.Type));


            rightProprertyInfo = Utils.GetPropertyInfo(rightProperty.Type, properties[0]);
            rightProperty = Expression.Property(rightProperty, properties[0]);

            if (properties.Count() == 1)
            {

                rightProperty = Utils.ConvertToNullable(rightProperty);

                predicate = BuildComparsion(Utils.ConvertToNullable(leftProperty), new WhereCondition()
                {
                    Field = condition.Field,
                    Comparator = condition.Comparator,
                    Value = Expression.Condition(notNullExpression, rightProperty, Expression.Constant(null, rightProperty.Type))
                });
            }
            else
            {
                ParameterExpression childParameter = Expression.Parameter(rightProperty.Type, rightProperty.Type.Name);
                if (Utils.IsEnumerable(childParameter))
                {
                    throw new InvalidOperationException($"The referenced field:{properties[0]} cannot be a collection!");

                }
                else
                {
                    predicate = BuildSubqueryComparsion(leftProperty, rightProperty, rootParameter, new WhereCondition()
                    {
                        Field = condition.Field,
                        Comparator = condition.Comparator,
                        Value = string.Join(".", properties.Skip(1))
                    });
                }

            }


            return predicate;
        }

        private static Expression MakeStringExpression(Expression prop, MethodInfo method, WhereCondition condition)
        {
            Utils.CheckType(prop, condition, StringType);
            Expression parameter = ConvertParameter(prop, condition);
            return Expression.Call(prop, method, parameter, Expression.Constant(StringComparison.CurrentCultureIgnoreCase));
        }


        private static Expression MakeStringBetweenExpression(Expression prop, Expression cond1, Expression cond2, WhereCondition condition)
        {
            Utils.CheckType(prop, condition, StringType);
            MethodInfo method = StringMethods["Compare"];
            Expression low = Expression.Call(null, method, prop, cond1, Expression.Constant(StringComparison.CurrentCultureIgnoreCase));
            Expression high = Expression.Call(null, method, prop, cond2, Expression.Constant(StringComparison.CurrentCultureIgnoreCase));
            Expression expression = Expression.AndAlso(Expression.GreaterThanOrEqual(low, Expression.Constant(0)), Expression.LessThanOrEqual(high, Expression.Constant(0)));

            //negate the expression if is not between
            if (
                condition.Comparator == WhereComparator.NOT_BETWEEN ||
                condition.Comparator == WhereComparator.NBTW
                )
            {
                return Negate(expression);
            }
            return expression;
        }

        private static Expression MakeEnumerableComparatorExpression(Expression prop, WhereCondition condition)
        {
            Utils.CheckEnumerable(prop, condition);
            Type cType = Utils.GetIEnumerableImpl(prop.Type);
            Expression parameter = Expression.Convert(prop, cType);


            MethodInfo method = EnumerableAnyMethodNoParameters.MakeGenericMethod(Utils.GetEnumerableTypeArg(cType));
            Expression compareExpression = Expression.Call(method, parameter);
            Expression expression = null;
            switch (condition.Comparator.ToUpperInvariant())
            {
                case WhereComparator.NULL_OR_EMPTY:
                case WhereComparator.NLEMP:
                    expression = Expression.OrElse(IsNull(prop, condition), Expression.Not(compareExpression));
                    break;
                case WhereComparator.NOT_NULL_OR_EMPTY:
                case WhereComparator.NNLEMP:
                    expression = Expression.AndAlso(IsNotNull(prop, condition), compareExpression);
                    break;
                case WhereComparator.EMPTY:
                case WhereComparator.EMP:
                    expression = Expression.AndAlso(IsNotNull(prop, condition), Expression.Not(compareExpression));
                    break;
                case WhereComparator.NOT_EMPTY:
                case WhereComparator.NEMP:
                    expression = Expression.AndAlso(IsNotNull(prop, condition), compareExpression);
                    break;
                default:
                    throw new InvalidOperationException($"The operator {condition.Comparator} is not applicable to {condition.Field}!");
            }
            return expression;
        }


        private static Expression MakeStringComparatorExpression(Expression prop, WhereCondition condition)
        {
            Utils.CheckType(prop, condition, StringType);
            Expression parameter = ConvertParameter(prop, condition);
            MethodInfo method = StringMethods["Compare"];
            Expression compareExpression = Expression.Call(null, method, prop, parameter, Expression.Constant(StringComparison.CurrentCultureIgnoreCase));
            Expression expression = null;
            switch (condition.Comparator.ToUpperInvariant())
            {
                case WhereComparator.GREATHER_THAN:
                case WhereComparator.GT:
                    expression = Expression.Equal(compareExpression, Expression.Constant(1));
                    break;
                case WhereComparator.GREATHER_OR_EQUALS:
                case WhereComparator.GE:
                    expression = Expression.GreaterThanOrEqual(compareExpression, Expression.Constant(0));
                    break;
                case WhereComparator.LESS_THAN:
                case WhereComparator.LT:
                    expression = Expression.Equal(compareExpression, Expression.Constant(-1));
                    break;
                case WhereComparator.LESS_OR_EQUALS:
                case WhereComparator.LE:
                    expression = Expression.LessThanOrEqual(compareExpression, Expression.Constant(0));
                    break;
                case WhereComparator.REGEX:
                case WhereComparator.RE:
                    expression = Expression.LessThanOrEqual(compareExpression, Expression.Constant(0));
                    break;
                default:
                    throw new InvalidOperationException($"The operator {condition.Comparator} is not applicable to {condition.Field}!");
            }
            return expression;
        }

        public static Expression ConvertParameter(Expression prop, WhereCondition condition)
        {

            if (condition.Value != null && condition.Value is Expression)
            {
                //return condition.Value;
                return Utils.ToDynamicParameterExpressionOfType(condition.Value, prop.Type);
            }

            if (condition.Value == null)
            {
                return Utils.ToStaticParameterExpressionOfType(null, prop.Type);
            }


            return Utils.ToStaticParameterExpressionOfType(Utils.TryCastFieldValueType(Utils.ParseValue(condition.Value), prop.Type), prop.Type);
        }

        private static Expression IsEqual(Expression prop, WhereCondition condition)
        {

            if (prop.Type == StringType)
            {
                return MakeStringExpression(prop, StringMethods["Equals"], condition);
            }
            Expression parameter = ConvertParameter(prop, condition);
            return Expression.Equal(prop, parameter);
        }

        private static Expression IsNotEqual(Expression prop, WhereCondition condition)
        {
            return Negate(IsEqual(prop, condition));
        }

        private static Expression Negate(Expression expression)
        {
            return Expression.Not(expression);
        }

        private static Expression IsGreaterThan(Expression prop, WhereCondition condition)
        {
            if (prop.Type == StringType)
            {
                return MakeStringComparatorExpression(prop, condition);
            }
            Expression parameter = ConvertParameter(prop, condition);
            return Expression.GreaterThan(prop, parameter);
        }

        private static Expression IsGreaterOrEqualThan(Expression prop, WhereCondition condition)
        {
            if (prop.Type == StringType)
            {
                return MakeStringComparatorExpression(prop, condition);
            }
            Expression parameter = ConvertParameter(prop, condition);
            return Expression.GreaterThanOrEqual(prop, parameter);
        }

        private static Expression IsLessThan(Expression prop, WhereCondition condition)
        {
            if (prop.Type == StringType)
            {
                return MakeStringComparatorExpression(prop, condition);
            }
            Expression parameter = ConvertParameter(prop, condition);
            return Expression.LessThan(prop, parameter);
        }

        private static Expression IsLessOrEqualThan(Expression prop, WhereCondition condition)
        {
            if (prop.Type == StringType)
            {
                return MakeStringComparatorExpression(prop, condition);
            }
            Expression parameter = ConvertParameter(prop, condition);
            return Expression.LessThanOrEqual(prop, parameter);
        }

        private static Expression StartsWith(Expression prop, WhereCondition condition)
        {
            return MakeStringExpression(prop, StringMethods["StartsWith"], condition);
        }

        private static Expression NotStartsWith(Expression prop, WhereCondition condition)
        {
            return Negate(StartsWith(prop, condition));
        }

        private static Expression EndsWith(Expression prop, WhereCondition condition)
        {
            return MakeStringExpression(prop, StringMethods["EndsWith"], condition);
        }

        private static Expression NotEndsWith(Expression prop, WhereCondition condition)
        {
            return Negate(EndsWith(prop, condition));
        }

        private static Expression Contains(Expression prop, WhereCondition condition)
        {
            return MakeStringExpression(prop, StringMethods["Contains"], condition);
        }

        private static Expression IsEmpty(Expression prop, WhereCondition condition)
        {
            if (Utils.IsEnumerable(prop))
            {
                return MakeEnumerableComparatorExpression(prop, condition);
            }
            return Expression.Equal(prop, Utils.ToStaticParameterExpressionOfType(string.Empty, prop.Type));
        }


        private static Expression IsNullOrEmpty(Expression prop, WhereCondition condition)
        {
            if (Utils.IsEnumerable(prop))
            {
                return MakeEnumerableComparatorExpression(prop, condition);
            }
            return Expression.OrElse(IsNull(prop, condition), IsEmpty(prop, condition));
        }

        private static Expression IsNotEmpty(Expression prop, WhereCondition condition)
        {
            return Negate(IsEmpty(prop, condition));
        }

        private static Expression IsNotNullOrEmpty(Expression prop, WhereCondition condition)
        {

            return Expression.AndAlso(IsNotNull(prop, condition), IsNotEmpty(prop, condition));
        }

        private static Expression IsNull(Expression prop, WhereCondition condition)
        {
            return Expression.Equal(prop, Utils.ToStaticParameterExpressionOfType(null, prop.Type));
        }


        private static Expression IsNotNull(Expression prop, WhereCondition condition)
        {
            return Negate(IsNull(prop, condition));
        }

        private static Expression Matches(Expression prop, WhereCondition condition)
        {
            Utils.CheckType(prop, condition, StringType);
            Expression parameter = ConvertParameter(prop, condition);
            Expression expression = Expression.Call(typeof(Regex), "IsMatch", null, prop, parameter, Expression.Constant(RegexOptions.IgnoreCase));
            //negate the expression if is not between
            if (
                condition.Comparator == WhereComparator.NOT_REGEX ||
                condition.Comparator == WhereComparator.NRE
                )
            {
                return Negate(expression);
            }
            return expression;
        }

        private static Expression NotMatches(Expression prop, WhereCondition condition)
        {
            return Negate(Matches(prop, condition));
        }

        private static Expression Between(Expression prop, WhereCondition condition)
        {
            if (condition.Value.GetType() == StringType)
            {
                condition.Value = condition.Value.Split(",");
            }
            var d1 = condition.Value[0];
            var d2 = condition.Value[1];
            if (d1.GetType() == StringType)
            {
                if (String.Compare(d1, d2, StringComparison.InvariantCultureIgnoreCase) > 0)
                {
                    var temp = d2;
                    d2 = d1;
                    d1 = temp;
                }
            }
            else
            {
                if (d1 > d2)
                {
                    var temp = d2;
                    d2 = d1;
                    d1 = temp;
                }
            }
            Expression cond1 = Utils.ToStaticParameterExpressionOfType(d1, prop.Type);
            Expression cond2 = Utils.ToStaticParameterExpressionOfType(d2, prop.Type);

            if (prop.Type == StringType)
            {
                return MakeStringBetweenExpression(prop, cond1, cond2, condition);
            }

            return Expression.AndAlso(Expression.GreaterThanOrEqual(prop, cond1), Expression.LessThanOrEqual(prop, cond2));
        }

        private static Expression NotBetween(Expression prop, WhereCondition condition)
        {
            return Negate(Between(prop, condition));
        }

        private static Expression In(Expression prop, WhereCondition condition)
        {

            // TODO: Try to make it better, for example, move the methods to static context
            if (condition.Value.GetType() == StringType)
            {
                condition.Value = condition.Value.Split(",");
            }
            Type listType = typeof(List<>).MakeGenericType(new[] { prop.Type });
            var methodInfo = listType.GetRuntimeMethod("Contains", new Type[] { prop.Type });
            IList list = (IList)Activator.CreateInstance(listType);
            foreach (var e in condition.Value)
            {
                list.Add(Convert.ChangeType(e, Utils.GetNotNullableType(prop.Type)));
            }
            return Expression.Call(Expression.Constant(list), methodInfo, prop);
        }

        private static Expression NotIn(Expression prop, WhereCondition condition)
        {
            return Negate(In(prop, condition));
        }


        protected Expression MakeLambda(Expression predicate, params ParameterExpression[] parameters)
        {

            List<ParameterExpression> resultParameters = new List<ParameterExpression>();
            foreach (Expression parameter in parameters)
            {

                resultParameters.Add(((ParameterExpression)visitor.Visit(parameter)));
            }
            LambdaExpression expression = Expression.Lambda(visitor.Visit(predicate), parameters);
            visitor.Visit(expression.Body);
            return visitor.Visit(expression);
        }

    }
}