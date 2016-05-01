namespace UnitTest
{
    using System;
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

            var clonerCache = moduleBuilder.DefineType("PrecompiledBond.ClonerCache", TypeAttributes.NotPublic | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Abstract);
            var genericParameters = clonerCache.DefineGenericParameters("TSource", "T");
            var fieldBuilder = clonerCache.DefineField("clone", typeof(Func<object, object>[]), FieldAttributes.Static | FieldAttributes.Assembly | FieldAttributes.InitOnly);
            
            var precompiledCloner = new PrecompiledCloner<FieldOfStructWithAliases>(typeof(FieldOfStructWithAliases), clonerCache.CreateType());

            var type = precompiledCloner.CompileToModule(moduleBuilder).CreateType();
            
            var clone = type.GetMethod("Clone", new[] { typeof(object)});
            var genericsResult = (FieldOfStructWithAliases)clone.Invoke(null, new object[] { new FieldOfStructWithAliases() });

            assemblyBuilder.Save("DataSchemas.PrecompiledBond.dll");
        }

    }
}