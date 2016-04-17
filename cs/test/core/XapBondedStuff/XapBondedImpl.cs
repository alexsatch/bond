namespace UnitTest.XapBondedStuff
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Bond;
    using Bond.Expressions;

    public interface IXapReadonly
    {
        void SetReadonly();
    }

    internal interface IXapBonded<out T> : IBonded<T>
    {
        T GhostValue();
    }

    internal class XapBondedLocal<TActual> : XapBonded<TActual>, IXapBonded<TActual>
    {
        private readonly TActual instance;

        /// <summary>
        /// No polymorphism here
        /// </summary>
        public XapBondedLocal(TActual value)
        {
            this.instance = value;
        }

        public TActual GhostValue()
        {
            return instance;
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

        public new IXapBonded<U> Convert<U>()
        {
            return this as IXapBonded<U>;
        }
        IBonded<U> IBonded.Convert<U>()
        {
            return this as IBonded<U>;
        }

        public static IXapBonded<TActual> Empty()
        {
            return new XapBondedLocal<TActual>(GenericFactory.Create<TActual>());
        }

        public override TActual Value { get; set; }
        public override XapBonded<TR> Cast<TR>()
        {
            throw new NotImplementedException();
        }
    }

    internal class Remote<T, R> : IXapBonded<T>
        where R : Bond.IO.ICloneable<R>
    {
        private readonly R reader;
        private readonly RuntimeSchema schema;

        private readonly Lazy<T> ghost;

        public Remote(R reader, RuntimeSchema schema)
        {
            this.reader = reader.Clone();
            this.schema = schema;

            this.ghost = new Lazy<T>(() =>
            {
                var result = Deserialize();
                ((IXapReadonly)result).SetReadonly();
                return result;
            });
        }

        public T Deserialize()
        {
            return this.ghost.Value;
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
            return new Remote<U, R>(reader, schema);
        }

        public T GhostValue()
        {
            return ghost.Value;
        }
    }

    internal class XapBondedImplPayload<R> : IBonded
        where R : Bond.IO.ICloneable<R>
    {
        private readonly R reader;
        private readonly RuntimeSchema schema;

        public XapBondedImplPayload(R reader, RuntimeSchema schema)
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

        public IXapBonded<U> Convert<U>()
        {
            return new Remote<U, R>(reader, schema);
        }

        IBonded<U> IBonded.Convert<U>()
        {
            return Convert<U>();
        }
    }

    internal static class Facade
    {
        public static Cloner<TSource> Cloner<TSource, T>()
        {
            return new Cloner<TSource>(typeof(T), new ObjectParser(typeof(TSource), ObjectBondedFactory), Factory);
        }

        public static Serializer<W> Serializer<W, T>()
        {
            return new Serializer<W>(typeof(T), new ObjectParser(typeof(T), ObjectBondedFactory), false);
        }

        public static Deserializer<R> Deserializer<R, T>(RuntimeSchema schema)
        {
            var parser = schema.HasValue
                             ? ParserFactory<R>.Create(schema, PayloadBondedFactory)
                             : ParserFactory<R>.Create(typeof(T), PayloadBondedFactory);

            return new Deserializer<R>(typeof(T), parser, Factory, false);
        }

        private static Expression ObjectBondedFactory(Type objectType, Expression value)
        {
            var method = typeof(XapBondedImpl<>).MakeGenericType(objectType).GetMethod("FromLocal", new[] { value.Type });

            return Expression.Call(method, value);
        }

        private static Expression PayloadBondedFactory(Expression reader, Expression schema)
        {
            var ctor = typeof(XapBondedImplPayload<>).MakeGenericType(reader.Type).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new[] { reader.Type, schema.Type }, null);
            return Expression.New(ctor, reader, schema);
        }

        public static Transcoder<R, W> Transcoder<R, W>(RuntimeSchema schema)
        {
            return new Transcoder<R, W>(schema, ParserFactory<R>.Create(schema, PayloadBondedFactory));
        }

        private static Expression Factory(Type type, Type schemaType, params Expression[] arguments)
        {
            if (type.IsGenericType)
            {
                var typeDefinition = type.GetGenericTypeDefinition();
                if (typeDefinition == typeof(XapBondedImpl<>))
                {
                    var arg = arguments[0]; // CustomBondedVoid<R>
                    var bondedConvert = typeof(IBonded).GetMethod("Convert").MakeGenericMethod(type.GetGenericArguments());

                    return Expression.ConvertChecked(Expression.Call(arg, bondedConvert), type);
                }
            }

            return null;
        }
    }

    public static class XapBondedFactory<T>
    {
        public static XapBonded<T> FromLocal<U>(U value)
            where U : T
        {
            return new XapBondedLocal<U>(value).Convert<T>();
        }

        public static XapBonded<T> Empty()
        {
            return XapBondedLocal<T>.Empty();
        }
    }

    internal class XapBondedImpl<T> : XapBonded<T>, IBonded<T>
    {
        private IXapBonded<T> holder;

        private XapBondedImpl(IXapBonded<T> holder)
        {
            this.holder = holder;
        }

        

        public override T Value
        {
            get
            {
                return holder.GetValue<T>();
            }
            set
            {
                // questionable
                holder = new XapBondedLocal<T>(value);
            }
        }

        public override XapBonded<TR> Cast<TR>()
        {
            return new XapBondedImpl<TR>(this.holder.Convert<TR>());
        }

        

        public T Deserialize()
        {
            return this.holder.Deserialize<T>();
        }

        public void Serialize<W>(W writer)
        {
            return this.holder.Serialize(writer);
        }

        public U Deserialize<U>()
        {
            throw new System.NotImplementedException();
        }

        public IBonded<U> Convert<U>()
        {
            return new XapBondedImpl<U>(this.holder.Convert<U>());
        }
    }
}