namespace Bond.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;

    internal static class ExpressionExtensions
    {
        public static Expression GetBodyWithAppliedArguments<T>(this Expression<T> lambda, params Expression[] args)
        {
            var argToIndex = new Dictionary<ParameterExpression, int>();
            foreach (var pex in lambda.Parameters)
                argToIndex[pex] = argToIndex.Count;

            Func<ParameterExpression, Expression> apply = (pex) => args[argToIndex[pex]];

            var newBody = new ApplyArgumentsRewriter(apply).Visit(lambda.Body);

            return newBody;
        }

        private sealed class ApplyArgumentsRewriter : ExpressionVisitor
        {
            private readonly Func<ParameterExpression, Expression> apply;

            internal ApplyArgumentsRewriter(Func<ParameterExpression, Expression> apply)
            {
                this.apply = apply;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return apply(node) ?? base.VisitParameter(node);
            }
        }

        public static Expression RewriteSpecificField(this Expression expression, FieldInfo oldField, FieldInfo newField)
        {
            return new ChangeFieldInfoRewriter(oldField, newField).Visit(expression);
        }

        private class ChangeFieldInfoRewriter : ExpressionVisitor
        {
            private readonly FieldInfo oldField;
            private readonly FieldInfo newField;

            public ChangeFieldInfoRewriter(FieldInfo oldField, FieldInfo newField)
            {
                this.oldField = oldField;
                this.newField = newField;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                return node.Member == oldField
                    ? Expression.MakeMemberAccess(null, newField)
                    : base.VisitMember(node);
            }
        }
    }
}