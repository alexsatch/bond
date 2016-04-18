namespace Bond.xap
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    ///     Local (in-memory) bonded payload. Contains an instance of exact(!) type <typeparamref name="T" /> within.
    /// </summary>
    /// <typeparam name="T"> </typeparam>
    internal class XapBondedLocal<T> : IProjectable
    {
        private static readonly MethodInfo fromValueMethod = Reflection.GenericMethodInfoOf(() => FromValue<T>(default(T)));
        private static readonly Dictionary<Type, Func<T, IProjectable>> localCache = new Dictionary<Type, Func<T, IProjectable>>();

        private readonly T instance;

        /// <summary>
        ///     ! No polymorphism here ! <br />
        ///     If you supply value of type U : T, it will be still serialized as T, not U.
        ///     Use <see cref="FromValue" /> method instead to get polymorphic behavior.
        /// </summary>
        private XapBondedLocal(T value)
        {
            instance = value;
        }

        /// <summary>
        /// Creates an empty <see cref="XapBondedLocal{T}"/> with non-null default value for type <typeparamref name="T"/>
        /// </summary>
        internal static IProjectable Empty()
        {
            return new XapBondedLocal<T>(GenericFactory.Create<T>());
        }

        /// <summary>
        /// Creates a  polymorphic <see cref="XapBondedLocal{T}"/> with specified value.
        /// </summary>
        /// <param name="value">Value to be stored.</param>
        internal static IProjectable FromValue(T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value", "Value cannot be null.");
            }

            Func<T, IProjectable> factory;
            var type = value.GetType();
            if (!localCache.TryGetValue(type, out factory))
                factory = localCache[type] = PerTypeValueFactory(type);

            return factory(value);
        }

        #region IBonded implementation

        void IBonded.Serialize<W>(W writer)
        {
            Facade.Serializer<W, T>().Serialize(instance, writer);
        }

        U IBonded.Deserialize<U>()
        {
            return Facade.Cloner<T, U>().Clone<U>(instance);
        }

        IBonded<U> IBonded.Convert<U>()
        {
            throw new NotSupportedException();
        }

        #endregion

        public U GetProjection<U>()
        {
            if (instance is U)
            {
                // "as-is" projection of the actual instance
                return (U) (object) instance;
            }

            
            var value = Facade.Cloner<T, U>().Clone<U>(instance);
            var @readonly = (IXapReadonly) value;
            @readonly.SetReadonly();

            return value;
        }

        void IXapReadonly.SetReadonly()
        {
            var pd = instance as PluginData;
            if (pd != null && !pd.IsReadOnly)
            {
                ((IXapReadonly) instance).SetReadonly();
            }
        }

        private static IProjectable FromValue<U>(U value)
            where U : T
        {
            return new XapBondedLocal<U>(value);
        }

        private static Func<T, IProjectable> PerTypeValueFactory(Type type)
        {
            var value = Expression.Parameter(typeof (T), "value");
            var method = fromValueMethod.MakeGenericMethod(type);
            var call = Expression.Call(method, Expression.ConvertChecked(value, type));

            var expression = Expression.Lambda<Func<T, IProjectable>>(call, value);

            return expression.Compile();
        }
    }
}