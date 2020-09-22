using System.Collections.Generic;
using System.Linq.Expressions;

namespace Cypretex.Data.Filters.Parsers.Linq
{
    internal class Visitor : ExpressionVisitor
    {
        private readonly List<Expression> expressions = new List<Expression>();

        protected override Expression VisitBinary(BinaryExpression node)
        {
            this.expressions.Add(node);
            return base.VisitBinary(node);
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            this.expressions.Add(node);
            return base.VisitBlock(node);
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            this.expressions.Add(node);
            return base.VisitConditional(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            this.expressions.Add(node);
            return base.VisitConstant(node);
        }

        protected override Expression VisitDebugInfo(DebugInfoExpression node)
        {
            this.expressions.Add(node);
            return base.VisitDebugInfo(node);
        }

        protected override Expression VisitDefault(DefaultExpression node)
        {
            this.expressions.Add(node);
            return base.VisitDefault(node);
        }

        protected override Expression VisitDynamic(DynamicExpression node)
        {
            this.expressions.Add(node);
            return base.VisitDynamic(node);
        }

        protected override Expression VisitExtension(Expression node)
        {
            this.expressions.Add(node);
            return base.VisitExtension(node);
        }

        protected override Expression VisitGoto(GotoExpression node)
        {
            this.expressions.Add(node);
            return base.VisitGoto(node);
        }

        protected override Expression VisitIndex(IndexExpression node)
        {
            this.expressions.Add(node);
            return base.VisitIndex(node);
        }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            this.expressions.Add(node);
            return base.VisitInvocation(node);
        }

        protected override Expression VisitLabel(LabelExpression node)
        {
            this.expressions.Add(node);
            return base.VisitLabel(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            this.expressions.Add(node);
            return base.VisitLambda(node);
        }

        protected override Expression VisitListInit(ListInitExpression node)
        {
            this.expressions.Add(node);
            return base.VisitListInit(node);
        }

        protected override Expression VisitLoop(LoopExpression node)
        {
            this.expressions.Add(node);
            return base.VisitLoop(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            this.expressions.Add(node);
            return base.VisitMember(node);
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            this.expressions.Add(node);
            return base.VisitMemberInit(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            this.expressions.Add(node);
            return base.VisitMethodCall(node);
        }

        protected override Expression VisitNew(NewExpression node)
        {
            this.expressions.Add(node);
            return base.VisitNew(node);
        }

        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            this.expressions.Add(node);
            return base.VisitNewArray(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            this.expressions.Add(node);
            Expression e = base.VisitParameter(node);
            return node;
        }

        protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        {
            this.expressions.Add(node);
            return base.VisitRuntimeVariables(node);
        }

        protected override Expression VisitSwitch(SwitchExpression node)
        {
            this.expressions.Add(node);
            return base.VisitSwitch(node);
        }

        protected override Expression VisitTry(TryExpression node)
        {
            this.expressions.Add(node);
            return base.VisitTry(node);
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            this.expressions.Add(node);
            return base.VisitTypeBinary(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            this.expressions.Add(node);
            return base.VisitUnary(node);
        }

        public IEnumerable<Expression> Explore(Expression node)
        {
            this.expressions.Clear();
            this.Visit(node);
            return expressions.ToArray();
        }
    }
}