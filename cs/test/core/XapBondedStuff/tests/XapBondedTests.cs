// ----------------------------------------------------------------------
//  <copyright file="XapBondedTests.cs" company="Microsoft">
//        Copyright 2016 (c) Microsoft Corporation. All Rights Reserved.
//        Information Contained Herein is Proprietary and Confidential.
//  </copyright>
// ----------------------------------------------------------------------
namespace UnitTest.XapBondedStuff.tests
{
    using Bond.IO.Safe;
    using Bond.Protocols;
    using NUnit.Framework;

    [TestFixture]
    public class XapBondedTests
    {
        [Bond.Schema]
        public class A : PluginData, IXapReadonly
        {
            [Bond.Id(0), Bond.Type(typeof(Bond.Tag.bonded<B>))]
            public XapBonded<B> BondedB { get; set; }

            public A()
                : this("Xap.A", "A")
            {
            }

            private A(string fullName, string name)
            {
                BondedB = XapBondedImpl<B>.Empty();
            }

            public void SetReadonly()
            {
                if (this.IsReadOnly)
                    return;

                this.IsReadOnly = true;
                this.BondedB.ReadOnly = true;
            }
        }

        [Bond.Schema]
        public class B : PluginData, IXapReadonly
        {
            [Bond.Id(0), Bond.RequiredOptional]
            public string SchemaName { get; set; }


            public B()
                : this("Xap.B", "B")
            {
            }

            protected B(string fullName, string name)
            {
                this.SchemaName = fullName;
            }

            public virtual void SetReadonly()
            {
                if (this.IsReadOnly)
                    return;

                this.IsReadOnly = true;
            }
        }

        [Bond.Schema]
        public class C : B
        {
            [Bond.Id(1)]
            public int c { get; set; }
          
            public C()
                : this("Xap.C", "C")
            {
            }

            protected C(string fullName, string name)
                : base(fullName, name)
            {
            }


            public override void SetReadonly()
            {
                if (this.IsReadOnly)
                    return;

                base.SetReadonly();
                this.IsReadOnly = true;
            }
        }

        [Test]
        public void EmptyIsNotNullAndHasDefaultValue()
        {
            var a = new A();
            
            Assert.That(a.BondedB, Is.Not.Null, "a.BondedB should be non-null");
            Assert.That(a.BondedB.ReadOnly, Is.False, "a.BondedB should be read-write");

            Assert.That(a.BondedB.Value, Is.Not.Null, "a.BondedB.Value should be non-null");
            Assert.That(a.BondedB.Value, Is.TypeOf<B>(), "a.BondedB.Value should be exactly of type B");
            Assert.That(a.BondedB.Value.IsReadOnly, Is.True, "a.BondedB.Value should be readonly");

            Assert.That(a.BondedB.Value.SchemaName, Is.EqualTo("Xap.B"));
        }

        [Test]
        public void LocalTT_DowncastToAssignable()
        {
            var a = new A {BondedB = XapBondedImpl<B>.FromLocal(new C())};

            Assert.That(a.BondedB, Is.Not.Null);

            Assert.That(a.BondedB.Value, Is.TypeOf<C>(), "should be exactly of type C");
            Assert.That(a.BondedB.Value.SchemaName, Is.EqualTo("Xap.C"));
        }

        [Test]
        public void LocalTU_RoundTrip_RemoteT()
        {
            var a = new A
            {
                BondedB = XapBondedImpl<B>.FromLocal(new C())
            };

            var a1 = RoundTrip(a);
            
            Assert.That(a1.BondedB, Is.Not.Null);
            Assert.That(a1.BondedB.ReadOnly, Is.False);

            Assert.That(a1.BondedB.Value, Is.Not.Null);
            Assert.That(a1.BondedB.Value, Is.TypeOf<B>(), "should be exactly of type B");
            Assert.That(a1.BondedB.Value.IsReadOnly, Is.True); // should be recursively true

            Assert.That(a1.BondedB.Value.SchemaName, Is.EqualTo("Xap.C"));
        }

        private T RoundTrip<T>(T a)
        {
            var output = new OutputBuffer();
            var writer = new CompactBinaryWriter<OutputBuffer>(output);
            Helper.Serialize(writer, a);

            var input = new CompactBinaryReader<InputBuffer>(new InputBuffer(output.Data));
            var reader = input;

            return Helper.Deserialize<CompactBinaryReader<InputBuffer>, T>(reader);
        }
    }

   
}