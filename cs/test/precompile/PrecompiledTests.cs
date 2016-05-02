namespace UnitTest
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    using Bond;
    using Bond.Precompiled;

    using NUnit.Framework;

    [TestFixture]
    public class PrecompiledTests
    {
        [Test]
        public static void Main()
        {
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DataSchemas.PrecompiledBond"), AssemblyBuilderAccess.RunAndSave);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("DataSchemas.PrecompiledBond.dll");
            
            var clonerCache = PrecompiledCloner.DefineCloneCacheType(moduleBuilder);

            var precompiledCloner = new PrecompiledCloner(moduleBuilder, clonerCache);
            foreach (var type in typeof(FieldOfStructWithAliases).Assembly.GetTypes())
            {
                if (!type.IsGenericType && type.IsBondStruct() && type.Namespace == "UnitTest" && !type.IsNested)
                {
                    precompiledCloner.CompileToModule(type).CreateType();
                }
            }

            assemblyBuilder.Save("DataSchemas.PrecompiledBond.dll");
        }
    }
}