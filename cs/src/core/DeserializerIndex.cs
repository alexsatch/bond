namespace Bond
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Threading;

    public interface IDeserializerIndex<R>
    {
        Func<R, object> GetFunction(int index);

        int SaveExpression(Type type, Func<Expression<Func<R, object>>> function);
    }


    internal class DeserializerIndex<R> : IDeserializerIndex<R>
    {
        private readonly List<Lazy<Func<R, object>>> deserialize;
        private readonly Dictionary<Type, int> bySchemaType;

        public DeserializerIndex()
        {
            this.deserialize = new List<Lazy<Func<R, object>>>();
            this.bySchemaType = new Dictionary<Type, int>();
        }

        public Func<R, object> GetFunction(int index)
        {
            return deserialize[index].Value;
        }

        public int SaveExpression(Type key, Func<Expression<Func<R, object>>> valueFactory)
        {
            int index;
            if (!bySchemaType.TryGetValue(key, out index))
            {
                index = bySchemaType.Count;
                bySchemaType[key] = index;
                deserialize.Add(new Lazy<Func<R, object>>(() => valueFactory().Compile(), LazyThreadSafetyMode.ExecutionAndPublication));
                return bySchemaType[key] = bySchemaType.Count;
            }

            return index;
        }
    }
}