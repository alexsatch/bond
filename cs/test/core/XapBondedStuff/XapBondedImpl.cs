namespace UnitTest.XapBondedStuff
{
    using System;
    using System.Diagnostics;

    using Bond;

    public interface IXapReadonly
    {
        void SetReadonly();
    }

    internal class XapBondedLocal<TActual> : IBonded<TActual>
    {
        private readonly TActual instance;

        /// <summary>
        /// No polymorphism here
        /// </summary>
        private XapBondedLocal(TActual value)
        {
            this.instance = value;
        }

        public TActual Deserialize()
        {
            return Deserialize<TActual>();
        }

        public void Serialize<W>(W writer)
        {
            Facade.Serializer<W, TActual>().Serialize(instance, writer);
        }

        public T Deserialize<T>()
        {
            return Facade.Cloner<TActual, T>().Clone<T>(instance);
        }

        public IBonded<U> Convert<U>()
        {
            return this as IBonded<U>;
        }

        IBonded<U> IBonded.Convert<U>()
        {
            return this as IBonded<U>;
        }

        public static IBonded<TActual> FromValue<U>(U value)
        {
            return new XapBondedLocal<U>(value).Convert<TActual>();
        }

        public static IBonded<TActual> Empty()
        {
            return new XapBondedLocal<TActual>(GenericFactory.Create<TActual>());
        }
    }

    internal class XapBondedPayload<T, R> : IBonded<T>
        where R : Bond.IO.ICloneable<R>
    {
        private readonly R reader;
        private readonly RuntimeSchema schema;

        public XapBondedPayload(R reader, RuntimeSchema schema)
        {
            this.reader = reader.Clone();
            this.schema = schema;
        }

        public T Deserialize()
        {
            return Deserialize<T>();
        }

        public void Serialize<W>(W writer)
        {
            Facade.Transcoder<R, W>(schema).Transcode(reader.Clone(), writer);
        }

        public U Deserialize<U>()
        {
            return Facade.Deserializer<R, U>(schema).Deserialize<U>(reader.Clone());
        }

        public IBonded<U> Convert<U>()
        {
            return new XapBondedPayload<U, R>(reader, schema);
        }
    }

    internal class XapBondedPayload<R> : IBonded
        where R : Bond.IO.ICloneable<R>
    {
        private readonly R reader;
        private readonly RuntimeSchema schema;

        public XapBondedPayload(R reader, RuntimeSchema schema)
        {
            this.reader = reader.Clone();
            this.schema = schema;
        }

        public void Serialize<W>(W writer)
        {
            Facade.Transcoder<R, W>(schema).Transcode(reader.Clone(), writer);
        }

        public U Deserialize<U>()
        {
            return Facade.Deserializer<R, U>(schema).Deserialize<U>(reader.Clone());
        }

        IBonded<U> IBonded.Convert<U>()
        {
            return new XapBondedPayload<U, R>(reader, schema);
        }
    }

    internal class XapBondedImpl<T> : XapBonded<T>, IBonded<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Lazy<T> lazyValue;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IBonded<T> bonded;

        private IBonded<T> Bonded
        {
            get { return this.bonded; }
            set
            {
                this.bonded = value;
                this.InvalidateValue(value);
            }
        }

        private void InvalidateValue(IBonded<T> newHolder)
        {
            this.lazyValue = new Lazy<T>(() => CreateReadOnlyValue(newHolder));
        }

        private XapBondedImpl(IBonded<T> bonded)
        {
            this.Bonded = bonded;
        }

        public override T Value
        {
            get { return this.lazyValue.Value; }
            set { this.Bonded = XapBondedLocal<T>.FromValue(value); }
        }

        public override XapBonded<TR> Cast<TR>()
        {
            return new XapBondedImpl<TR>(this.Bonded.Convert<TR>());
        }

        public T Deserialize()
        {
            return this.Bonded.Deserialize<T>();
        }

        public void Serialize<W>(W writer)
        {
            this.Bonded.Serialize(writer);
        }

        public U Deserialize<U>()
        {
            return this.Bonded.Deserialize<U>();
        }

        public IBonded<U> Convert<U>()
        {
            return new XapBondedImpl<U>(this.bonded.Convert<U>());
        }

        public static XapBonded<T> FromBonded(IBonded<T> bonded)
        {
            return new XapBondedImpl<T>(bonded);
        }

        public static XapBonded<T> FromLocal<U>(U value)
        {
            return new XapBondedImpl<T>(XapBondedLocal<U>.FromValue(value).Convert<T>());
        }

        public static XapBonded<T> Empty()
        {
            return new XapBondedImpl<T>(XapBondedLocal<T>.Empty());
        }

        private static T CreateReadOnlyValue(IBonded<T> bonded)
        {
            var value = bonded.Deserialize();
            if (value is IXapReadonly)
            {
                ((IXapReadonly)value).SetReadonly();
            }
            return value;
        }
    }
}