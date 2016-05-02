namespace Bond.Precompiled
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Text;

    using Bond.Expressions;

    public class PrecompiledSerializer
    {
        private readonly ModuleBuilder moduleBuilder;
        private readonly Type serializerCache;
        private readonly Func<Type, Type, SerializerExpressionFactory> serializerExpressionFactory;
        
        public PrecompiledSerializer(ModuleBuilder moduleBuilder, Type serializerCacheType, Func<Type, Type, SerializerExpressionFactory> serializerExpressionFactory = null)
        {
            this.moduleBuilder = moduleBuilder;
            this.serializerCache = serializerCacheType;
            this.serializerExpressionFactory = serializerExpressionFactory ?? ((sourceType, type) => new SerializerExpressionFactory(sourceType, type, null, null, false));
        }

        public TypeBuilder CompileToModule(Type sourceType, Type type = null)
        {
            type = type ?? sourceType;
            var fieldInfo = this.serializerCache.MakeGenericType(sourceType, type).GetField("clone", BindingFlags.Static | BindingFlags.NonPublic);
            Func<Expression, Expression, Expression> deferredDeserialize = (o, i) => Expression.Invoke(Expression.ArrayIndex(Expression.Field(null, fieldInfo), i), Expression.Convert(o, typeof(object)));
        
            var typeBuilder = this.moduleBuilder.DefineType("Bond.Precompiled." + EscapeTypeName(type) + "Cloner.From" + EscapeTypeName(sourceType), TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Abstract);
            var expressions = this.serializerExpressionFactory(sourceType, type).BuildExpressions(deferredDeserialize);
            var methods = BuildMethods(typeBuilder, expressions.ToArray()).ToArray();

            BuildStaticCtor(typeBuilder, fieldInfo, methods);
            this.BuildPublicMethod(typeBuilder, sourceType, type, fieldInfo);

            return typeBuilder;
        }

        private void BuildPublicMethod(TypeBuilder typeBuilder, Type sourceType, Type type, FieldInfo fieldInfo)
        {
            var method = typeBuilder.DefineMethod("Clone", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, CallingConventions.Standard, type,
                                                  new[] {sourceType});
            method.DefineParameter(1, ParameterAttributes.None, "src");
            
            var generator = method.GetILGenerator();

            // the following is equivalent to:
            // (TResult) clone[0](src);

            generator.Emit(OpCodes.Ldsfld, fieldInfo);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Ldelem_Ref);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, typeof(Func<object, object>).GetMethod("Invoke", new[] {typeof(object)}));
            generator.Emit(OpCodes.Castclass, type);
            generator.Emit(OpCodes.Ret);
        }

        public static MethodInfo Clone(FieldInfo info)
        {
            return (MethodInfo)clone[0](info);
        }

        private static Func<object, object>[] clone = null;

        private static void BuildStaticCtor(TypeBuilder typeBuilder, FieldInfo fieldInfo, MethodBuilder[] methods)
        {
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
                generator.Emit(OpCodes.Newobj, typeof(Func<object, object>).GetConstructor(new[] {typeof(object), typeof(IntPtr)}));
                generator.Emit(OpCodes.Stelem_Ref);
            }

            generator.Emit(OpCodes.Ret);
        }

        public static Type DefineCloneCacheType(ModuleBuilder moduleBuilder)
        {
            var clonerCache = moduleBuilder.DefineType("PrecompiledBond.ClonerCache", TypeAttributes.NotPublic | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Abstract);

            clonerCache.DefineGenericParameters("TSource", "T");
            clonerCache.DefineField("clone", typeof(Func<object, object>[]), FieldAttributes.Static | FieldAttributes.Assembly | FieldAttributes.InitOnly);

            return clonerCache.CreateType();
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

        private static MethodBuilder[] BuildMethods(TypeBuilder typeBuilder, Expression<Func<object, object>>[] expressions)
        {
            var result = new MethodBuilder[expressions.Length];
            for (int i = 0; i < expressions.Length; i++)
            {
                result[i] = typeBuilder.DefineMethod("Clone" + i, MethodAttributes.Private | MethodAttributes.Static, typeof(object), new[] {typeof(object)});

                expressions[i].CompileToMethod(result[i]);
            }

            return result;
        }
    }

    public class SerializerExpressionFactory<W>
    {
        public SerializerExpressionFactory(Type type, IParser parser, bool inlineNested)
        {
            throw new NotImplementedException();
        }

        public object BuildExpressions(Func<Expression, Expression, Expression, Expression> deferredDeserialize)
        {
            SerializerGeneratorFactory<object, W>.Create(
                    deferredDeserialize, type, inlineNested)
                .Generate(parser)
        }
    }
}