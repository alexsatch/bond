namespace Bond.Precompile
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Text;

    using Bond.Expressions;

    public class ClonerExpressionFactory<TSource>
    {
        private readonly Type type;
        private readonly IParser parser;
        private readonly Factory factory;
        private readonly bool inlineNested;

        public ClonerExpressionFactory(Type type, IParser parser, Factory factory, bool inlineNested)
        {
            this.type = type;
            this.parser = parser;
            this.factory = factory;
            this.inlineNested = inlineNested;
        }

        public virtual IEnumerable<Expression> BuildExpressions(Func<Expression, Expression, Expression> deferredDeserialize)
        {
            return Cloner<TSource>.Generate(type, new DeserializerTransform<object>(deferredDeserialize, factory, this.inlineNested), parser);
        }
    }

    public class PrecompiledCloner
    {
        private readonly Type clonerCache;
        private readonly Func<Expression, Expression, Expression> deferredDeserialize;

        public PrecompiledCloner(Type clonerCache, ClonerExpressionFactory clonerExpressionFactory)
        {
            this.clonerCache = clonerCache;
        }

        public TypeBuilder CompileToModule(ModuleBuilder moduleBuilder, Type sourceType, Type type)
        {
            var fieldInfo = clonerCache.MakeGenericType(sourceType, type).GetField("clone", BindingFlags.Static | BindingFlags.NonPublic);
            Func<Expression, Expression, Expression> deferredDeserialize = (o, i) => Expression.Invoke(Expression.ArrayIndex(Expression.Field(null, fieldInfo), i), o);
            
            var typeBuilder = moduleBuilder.DefineType("Bond.Precompiled." + EscapeTypeName(typeof(TSource)) + "Cloner.To" + EscapeTypeName(type), TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Abstract);
            
            var expressions = BuildExpressions();
            var methods = BuildMethods(typeBuilder, expressions.ToArray()).ToArray();

            var staticCtor = typeBuilder.DefineConstructor(MethodAttributes.Private | MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);
            var generator = staticCtor.GetILGenerator();

            generator.Emit(OpCodes.Ldc_I4, methods.Length);
            generator.Emit(OpCodes.Newarr, typeof(Func<object, object>));
            generator.Emit(OpCodes.Stsfld, fieldInfo);

            for (int k = 0; k < methods.Length; k++)
            {
                generator.Emit(OpCodes.Ldsfld, fieldInfo);
                generator.Emit(OpCodes.Ldc_I4, k);
                generator.Emit(OpCodes.Ldnull);
                generator.Emit(OpCodes.Ldftn, methods[k]);
                generator.Emit(OpCodes.Newobj, typeof(Func<object, object>).GetConstructor(new[] { typeof(object), typeof(IntPtr) }));
                generator.Emit(OpCodes.Stelem_Ref);
            }

            generator.Emit(OpCodes.Ret);

            return typeBuilder;
        }

        private static string EscapeTypeName(Type type)
        {
            var sb = new StringBuilder();
            foreach (var ch in type.FullName)
            {
                switch (ch)
                {
                case '.':
                case '<':
                case '>':
                case '`':
                case ',':
                    sb.Append("_");
                    break;
                default:
                    sb.Append(ch);
                    break;
                }
            }
            return sb.ToString();
        }

        private static MethodBuilder[] BuildMethods(TypeBuilder typeBuilder, Expression[] expressions)
        {
            var result = new MethodBuilder[expressions.Length];
            for (int i = 0; i < expressions.Length; i++)
            {
                var expression = expressions[i];
                var src = Expression.Parameter(typeof(object), "source");
                Expression<Func<object, object>> body = (Expression<Func<object, object>>) expression;

                if (i == 0)
                    result[0] = typeBuilder.DefineMethod("Clone", MethodAttributes.Public | MethodAttributes.Static, typeof(object), new[] {typeof(object)});
                else
                    result[i] = typeBuilder.DefineMethod("Clone" + i, MethodAttributes.Private | MethodAttributes.Static, typeof(object), new[] {typeof(object)});

                body.CompileToMethod(result[i]);
            }

            return result;
        }
    }
}