namespace Bond.xap
{
    using System;

    internal interface IProjectable : IBonded
    {
        U GetProjection<U>();
        void SetReadOnly();
    }

    internal class XapBondedPayload<R> : IProjectable
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
            Facade.Transcoder<R, W>(this.schema).Transcode(this.reader.Clone(), writer);
        }

        public U Deserialize<U>()
        {
            return Facade.Deserializer<R, U>(this.schema).Deserialize<U>(this.reader.Clone());
        }

        IBonded<U> IBonded.Convert<U>()
        {
            throw new NotSupportedException();
        }

        public U GetProjection<U>()
        {
            var value = Deserialize<U>();
            var @readonly = (IXapReadonly) value;
            @readonly.SetReadonly();
            return value;
        }

        public void SetReadOnly()
        {
            // no-op
        }
    }
}