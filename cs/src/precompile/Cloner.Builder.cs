namespace Bond.Precompile
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using Bond.Expressions;

    public static class PrecompiledClonerBuilder
    {
        internal static IEnumerable<Expression> Build<T>(Func<Expression, Expression, Expression> deferredDeserialize)
        {
            return Cloner<T>.Generate(typeof(T), new DeserializerTransform<object>(deferredDeserialize, false), null);
        }

        public static TypeBuilder PrecompileCloner<TSource>(ModuleBuilder moduleBuilder)
        {
            var facadeBuilder = moduleBuilder.DefineType("PrecompiledBond." + typeof(TSource).Name + "ClonerHelper", TypeAttributes.NotPublic | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Abstract);
            var fieldBuilder = facadeBuilder.DefineField("CloneOp", typeof(Func<object, object>[]), FieldAttributes.Static | FieldAttributes.Assembly | FieldAttributes.InitOnly);
            var facadeType = facadeBuilder.CreateType();
            var fieldInfo = facadeType.GetField("CloneOp", BindingFlags.Static | BindingFlags.NonPublic);

            var typeBuilder = moduleBuilder.DefineType("PrecompiledBond." + typeof(TSource).Name + "Cloner", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Abstract);
            
            Func<Expression, Expression, Expression> deferredDeserialize = (o, i) =>
            {
                var arrayIndex = Expression.ArrayIndex(Expression.Field(null, fieldInfo), i);
                return Expression.Invoke(arrayIndex, o);
            };

            var expressions = Build<TSource>(deferredDeserialize);
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