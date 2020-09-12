using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cypretex.Data.Filters.Parsers.Linq
{
    public static class LinqWhereExpressionBuilder
    {
        /// <summary>
        /// IEnumerable Any method
        /// </summary>
        /// <returns></returns>
        public static MethodInfo EnumerableAnyMethod = typeof(Enumerable).GetMethods().Single(m => m.Name == "Any" && m.GetParameters().Length == 2);


        public static readonly Type StringType = typeof(string);
        public static readonly Type RegexType = typeof(Regex);

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


        public static Expression<Func<T, bool>> BuildPredicate<T>(WhereCondition condition, string suffix = "")
        {
            ParameterExpression e = Expression.Parameter(typeof(T), typeof(T).Name + suffix);
            Expression expression = AddExpression<T>(e, null, condition, suffix);
            return (Expression<Func<T, bool>>)MakeLambda(e, expression);
        }

        public static Expression AddExpression<T>(ParameterExpression e, Expression expression, WhereCondition condition, string suffix = "")
        {
            Expression newEpxression = expression;
            if (condition.HasFieldCondition)
            {
                newEpxression = GetExpressionForCondition(e, condition, suffix);
            }

            int i = 1;
            //AND conditions
            foreach (WhereCondition cond in condition.And)
            {
                Expression e1 = AddExpression<T>(e, newEpxression, cond, suffix + i);
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
                Expression e1 = AddExpression<T>(e, newEpxression, cond, suffix + i);
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
                Expression e1 = Expression.Not(AddExpression<T>(e, newEpxression, cond, suffix + i));
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

        private static Expression GetExpressionForCondition(Expression parameter, WhereCondition condition, string suffix = "")
        {
            Expression resultExpression = null;
            Expression childParameter = parameter, predicate = Expression.IsTrue(Expression.Constant(true));
            Type childType = null;

            string[] properties = condition.Field.Split(".");
            if (properties.Count() == 1)
            {
                resultExpression = BuildCondition(parameter, condition);
            }
            else
            {
                parameter = Expression.Property(parameter, properties[0]);
                var isCollection = Utils.IsEnumerable(parameter);
                if (isCollection)
                {
                    childType = parameter.Type.GetGenericArguments()[0];
                    childParameter = Expression.Parameter(childType, childType.Name);
                }
                else
                {
                    childParameter = parameter;
                }

                //skip current property and get navigation property expression recursivly
                var innerProperties = string.Join(".", properties.Skip(1).ToList());
                predicate = GetExpressionForCondition(childParameter, new WhereCondition()
                {
                    Field = innerProperties,
                    Comparator = condition.Comparator,
                    Value = condition.Value
                }, suffix);

                if (condition.Value != null)
                {
                    predicate = Expression.AndAlso(Expression.NotEqual(childParameter, Expression.Constant(null, childParameter.Type)), predicate);
                }

                if (isCollection)
                {
                    //build subquery
                    resultExpression = BuildSubQuery(parameter, childType, MakeLambda(childParameter, predicate));
                }
                else
                {
                    resultExpression = predicate;
                }

            }

            return resultExpression;
        }


        private static Expression BuildSubQuery(Expression parameter, Type childType, Expression predicate)
        {
            parameter = Utils.AsQueryable(parameter);
            var anyMethod = EnumerableAnyMethod.MakeGenericMethod(childType);
            predicate = Expression.Call(anyMethod, parameter, predicate);
            return predicate;
        }
        private static Expression BuildCondition(Expression parameter, WhereCondition condition)
        {
            var childProperty = parameter.Type.GetProperty(condition.Field);
            Expression prop = Expression.Property(parameter, childProperty);
            var predicate = BuildComparsion(prop, condition);
            if (predicate == null)
            {
                predicate = Expression.IsTrue(Expression.Constant(true));
            }
            return predicate;
        }

        private static Expression BuildComparsion(Expression prop, WhereCondition condition)
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
            Console.WriteLine(expression);
            return expression;
        }

        public static Expression ConvertParameter(Expression prop, WhereCondition condition)
        {
            return Utils.ToStaticParameterExpressionOfType(Utils.TryCastFieldValueType(condition.Value, prop.Type), prop.Type);
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
            return Expression.Equal(prop, Utils.ToStaticParameterExpressionOfType(string.Empty, prop.Type));
        }


        private static Expression IsNullOrEmpty(Expression prop, WhereCondition condition)
        {
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
            Console.WriteLine(expression);
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


        private static Expression MakeLambda(Expression parameter, Expression predicate)
        {
            var resultParameterVisitor = new ParameterVisitor();
            resultParameterVisitor.Visit(parameter);
            var resultParameter = resultParameterVisitor.Parameter;
            return Expression.Lambda(predicate, (ParameterExpression)resultParameter);
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