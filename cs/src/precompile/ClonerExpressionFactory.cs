namespace Bond.Precompiled
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    using Bond.Expressions;

    public class ClonerExpressionFactory
    {
        private readonly Type type;
        private readonly IParser parser;
        private readonly Factory factory;
        private readonly bool inlineNested;

        public ClonerExpressionFactory(Type sourceType, Type type, IParser parser, Factory factory, bool inlineNested)
        {
            this.type = type;
            this.parser = parser ?? new ObjectParser(sourceType);
            this.factory = factory;
            this.inlineNested = inlineNested;
        }

        public virtual IEnumerable<Expression<Func<object, object>>> BuildExpressions(Expression<Func<object, int, object>>  deferredDeserialize)
        {
            var transform = new DeserializerTransform<object>(deferredDeserialize, this.factory, this.inlineNested);
            return transform.Generate(this.parser, this.type);
        }
    }
}