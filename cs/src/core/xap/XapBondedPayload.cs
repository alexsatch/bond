namespace Bond.xap
{
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
            Facade.Transcoder<R, W>(this.schema).Transcode(this.reader.Clone(), writer);
        }

        public U Deserialize<U>()
        {
            return Facade.Deserializer<R, U>(this.schema).Deserialize<U>(this.reader.Clone());
        }

        IBonded<U> IBonded.Convert<U>()
        {
            return new XapBondedPayload<U, R>(this.reader, this.schema);
        }
    }

    internal class XapBondedPayload<T, R> : IBonded<T>
        where R : IO.ICloneable<R>
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
            return this.Deserialize<T>();
        }

        public void Serialize<W>(W writer)
        {
            if (this.schema.HasValue)
                Facade.Transcoder<R, W>(this.schema).Transcode(this.reader.Clone(), writer);
            else
                Facade.Transcoder<R, W>(typeof(T)).Transcode(this.reader.Clone(), writer);
        }

        public U Deserialize<U>()
        {
            return Facade.Deserializer<R, U>(this.schema).Deserialize<U>(this.reader.Clone());
        }

        public IBonded<U> Convert<U>()
        {
            return new XapBondedPayload<U, R>(this.reader, this.schema);
        }
    }
}