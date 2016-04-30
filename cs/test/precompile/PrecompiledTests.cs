namespace UnitTest
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using Bond.Precompile;
    using NUnit.Framework;

    [TestFixture]
    public class PrecompiledTests
    {
        [Test]
        public static void Main()
        {
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DataSchemas.PrecompiledBond"), AssemblyBuilderAccess.RunAndSave);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("DataSchemas.PrecompiledBond.dll");

//            PrecompileCloner<BasicTypes>(moduleBuilder).CreateType();
            PrecompileCloner<FieldOfStructWithAliases>(moduleBuilder).CreateType();

            assemblyBuilder.Save("DataSchemas.PrecompiledBond.dll");
        }

        private static TypeBuilder PrecompileCloner<TSource>(ModuleBuilder moduleBuilder)
        {
            var facadeBuilder = moduleBuilder.DefineType("PrecompiledBond." + typeof(TSource).Name + "ClonerHelper", TypeAttributes.NotPublic | TypeAttributes.Class);
            var fieldBuilder = facadeBuilder.DefineField("CloneOp", typeof(Func<object, object>[]), FieldAttributes.Static | FieldAttributes.Assembly | FieldAttributes.InitOnly);
            var facadeType = facadeBuilder.CreateType();
            var fieldInfo = facadeType.GetField("CloneOp", BindingFlags.Static | BindingFlags.NonPublic);

            var typeBuilder = moduleBuilder.DefineType("PrecompiledBond." + typeof(TSource).Name + "Cloner", TypeAttributes.Public | TypeAttributes.Class);
            var builder = new PrecompiledClonerBuilder(typeBuilder);
            Func<Expression, Expression, Expression> deferredDeserialize = (o, i) =>
            {
                var arrayIndex = Expression.ArrayIndex(Expression.Field(null, fieldInfo), i);
                return Expression.Invoke(arrayIndex, o);
            };

            var expressions = builder.Build<TSource>(deferredDeserialize);
            var methods = builder.Precompile(expressions.ToArray()).ToArray();

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
                generator.Emit(OpCodes.Newobj, typeof(Func<object, object>).GetConstructor(new [] { typeof(object), typeof(IntPtr)}));
                generator.Emit(OpCodes.Stelem_Ref);
            }
            
            generator.Emit(OpCodes.Ret);

            return typeBuilder;
        }

        private static Func<object, object>[] clone;

        private static object Clone_X(object o)
        {
            return o;
        }

        public static void M()
        {
            clone = new Func<object, object>[2];
            clone[0] = Clone_X;
            clone[1] = Clone_X;
        }
    }
}