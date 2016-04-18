// ----------------------------------------------------------------------
//  <copyright file="XapBondedTests.cs" company="Microsoft">
//        Copyright 2016 (c) Microsoft Corporation. All Rights Reserved.
//        Information Contained Herein is Proprietary and Confidential.
//  </copyright>
// ----------------------------------------------------------------------
namespace UnitTest.XapBondedStuff.tests
{
    using System;

    using Bond.IO.Safe;
    using Bond.Protocols;
    using Bond.xap;

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
                ((IXapReadonly)this.BondedB).SetReadonly();;
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
        public class B1 : PluginData, IXapReadonly
        {
            [Bond.Id(0), Bond.RequiredOptional]
            public string SchemaName { get; set; }

            public B1()
                : this("Xap.B", "B")
            {
            }

            protected B1(string fullName, string name)
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
            Assert.That(a.BondedB.Value.IsReadOnly, Is.False, "a.BondedB.Value should not be readonly");

            Assert.That(a.BondedB.Value.SchemaName, Is.EqualTo("Xap.B"));
        }

        [Test]
        public void LocalBC_Assignable()
        {
            var a = new A {BondedB = XapBondedImpl<B>.FromLocal(new C())};

            Assert.That(a.BondedB, Is.Not.Null);
            Assert.That(a.BondedB.Value, Is.TypeOf<C>(), "should be exactly of type C");
            Assert.That(a.BondedB.Value.SchemaName, Is.EqualTo("Xap.C"));
        }

        [Test]
        public void Local_AssignToSourcePropertyEndsUpInPayload()
        {
            var c = new C {c = 1};
            var a = new A {BondedB = XapBondedImpl<B>.FromLocal(c)};
            c.c = 42;

            var a1 = RoundTrip(a);
            Assert.That(a1.BondedB.Cast<C>().Value.c, Is.EqualTo(42));
        }

        [Test]
        public void Local_AssignToValueEndsUpInPayload()
        {
            var c = new C { c = 1 };
            var a = new A { BondedB = XapBondedImpl<B>.FromLocal(c) };
            a.BondedB.Cast<C>().Value.c = 42;

            var a1 = RoundTrip(a);
            Assert.That(a1.BondedB.Cast<C>().Value.c, Is.EqualTo(42));
        }

        [Test]
        public void LocalBC_AssignToValueOtherC()
        {
            var a = new A {BondedB = XapBondedImpl<B>.FromLocal(new C {c = 1})};
            a.BondedB.Value = new C { c = 2};

            Assert.That(a.BondedB.Cast<C>().Value.c, Is.EqualTo(2));
        }

        [Test]
        public void LocalBC_AssignToValueOtherB()
        {
            var a = new A { BondedB = XapBondedImpl<B>.FromLocal(new C { c = 1 }) };
            a.BondedB.Value = new B {SchemaName = "Blabla"};

            Assert.That(a.BondedB.Cast<B>().Value.SchemaName, Is.EqualTo("Blabla"));
        }

        [Test]
        public void LocalBC_AssignAfterReadonly()
        {
            var a = new A {BondedB = XapBondedImpl<B>.FromLocal(new C {c = 1})};
            a.SetReadonly();

            Assert.That(a.IsReadOnly, Is.True);
            Assert.That(a.BondedB.ReadOnly, Is.True);
            Assert.That(a.BondedB.Value.IsReadOnly, Is.True);
            Assert.That(a.BondedB.Cast<C>().Value.IsReadOnly, Is.True);
            Assert.Throws<InvalidOperationException>(() => a.BondedB.Value = new B {SchemaName = "Blabla"});
        }


        [Test]
        public void Remote_AssignAfterReadonly()
        {
            var a = new A { BondedB = XapBondedImpl<B>.FromLocal(new C { c = 1 }) };
            a = RoundTrip(a);

            Assert.That(a.IsReadOnly, Is.False);
            Assert.That(a.BondedB.ReadOnly, Is.False);
            Assert.That(a.BondedB.Value.IsReadOnly, Is.True);

            a.SetReadonly();

            Assert.That(a.IsReadOnly, Is.True);
            Assert.That(a.BondedB.ReadOnly, Is.True);
            Assert.That(a.BondedB.Value.IsReadOnly, Is.True);

            Assert.Throws<InvalidOperationException>(() => a.BondedB.Value = new B { SchemaName = "Blabla" });
        }

        [Test]
        public void LocalBC_CastToAssignable()
        {
            var a = new A {BondedB = XapBondedImpl<B>.FromLocal(new C())};
            
            var b1Bonded = a.BondedB.Cast<B1>();
            Assert.That(b1Bonded, Is.Not.Null);
            Assert.That(b1Bonded.ReadOnly, Is.False);
            Assert.That(b1Bonded.Value, Is.Not.Null);
            Assert.That(b1Bonded.Value.SchemaName, Is.EqualTo("Xap.C"));
        }

        [Test]
        public void Local_RoundTrip_RemoteT()
        {
            var a = new A
                    {
                        BondedB = XapBondedImpl<B>.FromLocal(new C {c = 42})
                    };

            var a1 = RoundTrip(a);
            
            Assert.That(a1.BondedB, Is.Not.Null);
            Assert.That(a1.BondedB.ReadOnly, Is.False);

            Assert.That(a1.BondedB.Value, Is.Not.Null);
            Assert.That(a1.BondedB.Value, Is.TypeOf<B>(), "should be exactly of type B");
            Assert.That(a1.BondedB.Value.IsReadOnly, Is.True); // should be recursively true

            Assert.That(a1.BondedB.Value.SchemaName, Is.EqualTo("Xap.C"));

            var cBonded = a1.BondedB.Cast<C>();

            Assert.That(cBonded, Is.Not.Null);
            Assert.That(cBonded.ReadOnly, Is.False);
            Assert.That(cBonded.Value, Is.Not.Null);
            Assert.That(cBonded.Value, Is.TypeOf<C>());
            Assert.That(cBonded.Value.c, Is.EqualTo(42));
        }

        [Test]
        public void Local_RoundTrip_Convert()
        {
            var a = new A
            {
                BondedB = XapBondedImpl<B>.FromLocal(new C { c = 42})
            };

            var a1 = RoundTrip(a);

            Assert.That(a1.BondedB, Is.Not.Null);
            Assert.That(a1.BondedB.ReadOnly, Is.False);

            Assert.That(a1.BondedB.Value, Is.Not.Null);
            Assert.That(a1.BondedB.Value, Is.TypeOf<B>(), "should be exactly of type B");
            Assert.That(a1.BondedB.Value.IsReadOnly, Is.True); // should be recursively true

            Assert.That(a1.BondedB.Value.SchemaName, Is.EqualTo("Xap.C"));
            Assert.That(a1.BondedB.Cast<C>().Value.c, Is.EqualTo(42));
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