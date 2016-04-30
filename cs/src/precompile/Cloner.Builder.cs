namespace Bond.Precompile
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using Bond.Expressions;

    public class PrecompiledClonerBuilder
    {
        private readonly TypeBuilder typeBuilder;

        public PrecompiledClonerBuilder(TypeBuilder typeBuilder)
        {
            this.typeBuilder = typeBuilder;
        }

        public IEnumerable<Expression> Build<T>(Func<Expression, Expression, Expression> deferredDeserialize)
        {
            return Cloner<T>.Generate(typeof(T), new DeserializerTransform<object>(deferredDeserialize, false), null);
        }

        public MethodBuilder[] Precompile(Expression[] expressions)
        {
            var result = new MethodBuilder[expressions.Length];
            for (int i = 0; i < expressions.Length; i++)
            {
                var expression = expressions[i];
                var src = Expression.Parameter(typeof(object), "source");
                Expression<Func<object, object>> body = Expression.Lambda<Func<object, object>>(expression, src);

                result[i] = typeBuilder.DefineMethod("Clone_" + i, MethodAttributes.Private | MethodAttributes.Static, typeof(object), new[] {typeof(object)});

                body.CompileToMethod(result[i]);
            }

            return result;
        }
    }
}