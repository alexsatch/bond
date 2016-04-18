namespace Bond.xap
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    using Bond;
    using Bond.Expressions;

    public static class Facade
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
            var method = typeof(XapBondedLocal<>).MakeGenericType(objectType).FindMethod("FromValue", value.Type);
            return Expression.Call(method, value);
        }

        private static Expression PayloadBondedFactory(Expression reader, Expression schema)
        {
            var ctor = typeof(XapBondedPayload<>).MakeGenericType(reader.Type).GetConstructor(reader.Type, schema.Type);
            return Expression.New(ctor, reader, schema);
        }

        public static Transcoder<R, W> Transcoder<R, W>(RuntimeSchema schema)
        {
            return new Transcoder<R, W>(schema, ParserFactory<R>.Create(schema, PayloadBondedFactory));
        }

        public static Transcoder<R, W> Transcoder<R, W>(Type type)
        {
            return new Transcoder<R, W>(type, ParserFactory<R>.Create(type, PayloadBondedFactory));
        }

        private static Expression Factory(Type type, Type schemaType, params Expression[] arguments)
        {
            if (type.IsGenericType())
            {
                var typeDefinition = type.GetGenericTypeDefinition();
                if (typeDefinition == typeof(XapBonded<>))
                {
                    // target type: U
                    var typeU = type.GetGenericArguments();
                    var fromBondedMethod = typeof(XapBondedImpl<>).MakeGenericType(typeU).FindMethod("FromBonded", typeof(IBonded));
                    return Expression.Call(fromBondedMethod, arguments[0]);
                }
            }

            return null;
        }
    }
}