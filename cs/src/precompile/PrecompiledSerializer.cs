namespace Bond.Precompiled
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Text;

    using Bond.Expressions;

    public class PrecompiledSerializer<W>
    {
        private readonly ModuleBuilder moduleBuilder;
        private readonly Type serializerCacheType;
        private readonly Func<Type, SerializerExpressionFactory> serializerExpressionFactory;

        public PrecompiledSerializer(ModuleBuilder moduleBuilder, Type serializerCacheType, Func<Type, SerializerExpressionFactory> serializerExpressionFactory = null)
        {
            this.moduleBuilder = moduleBuilder;
            this.serializerCacheType = serializerCacheType;
            this.serializerExpressionFactory = serializerExpressionFactory ?? ((schemaType) => new SerializerExpressionFactory(schemaType, null, true));
        }

        private static readonly Action<object, W>[] __placeHolder__ = null;
        private static readonly FieldInfo placeHolderField = typeof(PrecompiledSerializer<W>).GetField("__placeHolder__", BindingFlags.Static | BindingFlags.NonPublic);

        public TypeBuilder CompileToModule(Type schemaType)
        {
            var writerType = typeof(W);
            var fieldInfo = this.serializerCacheType.MakeGenericType(schemaType, writerType).GetField("serialize", BindingFlags.Static | BindingFlags.NonPublic);
            Expression<Action<object, W, int>> deferredSerialize = ((o, w, i) => __placeHolder__[i](o, w));

            deferredSerialize = (Expression<Action<object, W, int>>) deferredSerialize.RewriteSpecificField(PrecompiledSerializer<W>.placeHolderField, fieldInfo);

            var typeBuilder = this.moduleBuilder.DefineType("Bond.Precompiled." + EscapeTypeName(schemaType) + "Serialize.To" + EscapeTypeName(writerType), TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Abstract);
            var expressions = this.serializerExpressionFactory(schemaType).BuildExpressions<W>(deferredSerialize);
            var methods = BuildMethods(typeBuilder, expressions.ToArray()).ToArray();

            BuildStaticCtor(typeBuilder, fieldInfo, methods);
            this.BuildPublicMethod(typeBuilder, schemaType, fieldInfo);

            return typeBuilder;
        }

        private void BuildPublicMethod(TypeBuilder typeBuilder, Type type, FieldInfo fieldInfo)
        {
            var method = typeBuilder.DefineMethod("Serialize", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, CallingConventions.Standard, typeof(void),
                                                  new[] {type, typeof(W)});
            method.DefineParameter(1, ParameterAttributes.None, "o");
            method.DefineParameter(1, ParameterAttributes.None, "writer");
            

            var generator = method.GetILGenerator();
            
            // the following is equivalent to:
            // serialize[0](o, writer);

            generator.Emit(OpCodes.Ldsfld, fieldInfo);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Ldelem_Ref);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Callvirt, typeof(Action<object, W>).GetMethod("Invoke", new[] {typeof(object), typeof(W)}));
            generator.Emit(OpCodes.Ret);
        }

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

        public static Type DefineSerializeCacheType(ModuleBuilder moduleBuilder)
        {
            var clonerCache = moduleBuilder.DefineType("PrecompiledBond.SerializerCache", TypeAttributes.NotPublic | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Abstract);

            clonerCache.DefineGenericParameters("T", "W");
            clonerCache.DefineField("serialize", typeof(Action<object, W>[]), FieldAttributes.Static | FieldAttributes.Assembly | FieldAttributes.InitOnly);

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

        private static MethodBuilder[] BuildMethods(TypeBuilder typeBuilder, Expression<Action<object, W>>[] expressions)
        {
            var result = new MethodBuilder[expressions.Length];
            for (int i = 0; i < expressions.Length; i++)
            {
                result[i] = typeBuilder.DefineMethod("Clone" + i, MethodAttributes.Private | MethodAttributes.Static, typeof(void), new[] {typeof(object), typeof(W)});

                expressions[i].CompileToMethod(result[i]);
            }

            return result;
        }
    }

    public class SerializerExpressionFactory
    {
        private readonly Type type;
        private readonly IParser parser;
        private readonly bool inlineNested;

        public SerializerExpressionFactory(Type type, IParser parser, bool inlineNested)
        {
            this.type = type;
            this.parser = parser ?? new ObjectParser(type);
            this.inlineNested = inlineNested;
        }

        public IEnumerable<Expression<Action<object, W>>> BuildExpressions<W>(Expression<Action<object, W, int>> deferredDeserialize)
        {
            return SerializerGeneratorFactory<object, W>.Create(deferredDeserialize, type, inlineNested).Generate(parser);
        }
    }
}