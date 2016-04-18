namespace Bond.xap
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;

    internal class XapBondedLocal<TActual> : IProjectable
    {
        private static readonly MethodInfo fromValueMethod = Reflection.GenericMethodInfoOf(() => FromValue<TActual>(default(TActual)));

        private static readonly Dictionary<Type, Func<TActual, IProjectable>> localCache
            = new Dictionary<Type, Func<TActual, IProjectable>>();

        private readonly TActual instance;

        /// <summary>
        /// No polymorphism here
        /// </summary>
        private XapBondedLocal(TActual value)
        {
            this.instance = value;
        }

        public void Serialize<W>(W writer)
        {
            Facade.Serializer<W, TActual>().Serialize(this.instance, writer);
        }

        public T Deserialize<T>()
        {
            return Facade.Cloner<TActual, T>().Clone<T>(this.instance);
        }

        IBonded<U> IBonded.Convert<U>()
        {
            throw new NotSupportedException();
        }

        public U GetProjection<U>()
        {
            if (instance is U)
            {
                // non-readonly projection of actual instance
                return (U) (object) instance;
            }

            var value = Deserialize<U>();
            var @readonly = (IXapReadonly) value;
            @readonly.SetReadonly();

            return value;
        }

        void IXapReadonly.SetReadonly()
        {
            var pd = instance as PluginData;
            if (pd != null && !pd.IsReadOnly)
            {
                ((IXapReadonly)instance).SetReadonly();
            }
        }

        internal static IProjectable Empty()
        {
            return new XapBondedLocal<TActual>(GenericFactory.Create<TActual>());
        }

        internal static IProjectable FromValue(TActual value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value", "Value cannot be null.");
            }

            Func<TActual, IProjectable> factory;
            var type = value.GetType();
            if (!localCache.TryGetValue(type, out factory))
                factory = localCache[type] = PerTypeValueFactory(type);

            return factory(value);
        }

        private static IProjectable FromValue<U>(U value)
            where U : TActual
        {
            return new XapBondedLocal<U>(value);
        }

        private static Func<TActual, IProjectable> PerTypeValueFactory(Type type)
        {
            var value = Expression.Parameter(typeof(TActual), "value");
            var method = fromValueMethod.MakeGenericMethod(type);
            var call = Expression.Call(method, Expression.ConvertChecked(value, type));

            var expression = Expression.Lambda<Func<TActual, IProjectable>>(call, value);

            return expression.Compile();
        }
    }
}