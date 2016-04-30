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

            PrecompiledClonerBuilder.PrecompileCloner<FieldOfStructWithAliases>(moduleBuilder).CreateType();

            assemblyBuilder.Save("DataSchemas.PrecompiledBond.dll");
        }

    }
}