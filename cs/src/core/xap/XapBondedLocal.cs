namespace Bond.xap
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;

    internal class XapBondedLocal<TActual> : IBonded<TActual>
    {
        private static readonly Dictionary<Type, Func<TActual, IBonded<TActual>>> localCache
            = new Dictionary<Type, Func<TActual, IBonded<TActual>>>();

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
            return this.Deserialize<TActual>();
        }

        public void Serialize<W>(W writer)
        {
            Facade.Serializer<W, TActual>().Serialize(this.instance, writer);
        }

        public T Deserialize<T>()
        {
            return Facade.Cloner<TActual, T>().Clone<T>(this.instance);
        }

        public IBonded<U> Convert<U>()
        {
            return this as IBonded<U>;
        }

        public static IBonded<TActual> FromValue(TActual value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value", "Value cannot be null.");
            }

            Func<TActual, IBonded<TActual>> factory;
            var type = value.GetType();
            if (!localCache.TryGetValue(type, out factory))
                factory = localCache[type] = PerTypeValueFactory(type);

            return factory(value);
        }

        private static readonly MethodInfo fromValueMethod = Reflection.GenericMethodInfoOf(() => FromValue<TActual>(default(TActual)));

        private static Func<TActual, IBonded<TActual>> PerTypeValueFactory(Type type)
        {
            var value = Expression.Parameter(typeof(TActual), "value");
            var method = fromValueMethod.MakeGenericMethod(type);
            var call = Expression.Call(method, Expression.ConvertChecked(value, type));

            var expression = Expression.Lambda<Func<TActual, IBonded<TActual>>>(call, value);

            return expression.Compile();
        }

        private static IBonded<TActual> FromValue<U>(U value)
            where U : TActual
        {
            return new XapBondedLocal<U>(value).Convert<TActual>();
        }

        public static IBonded Empty()
        {
            return new XapBondedLocal<TActual>(GenericFactory.Create<TActual>());
        }
    }
}