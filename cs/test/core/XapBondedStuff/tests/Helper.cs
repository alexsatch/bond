namespace UnitTest.XapBondedStuff.tests
{
    using Bond;
    using Bond.xap;

    internal static class Helper
    {
        public static void Serialize<W, T>(W writer, T o)
        {
            Facade.Serializer<W,T>().Serialize(o, writer);
        }

        public static T Deserialize<R, T>(R reader)
        {
            return Facade.Deserializer<R, T>(RuntimeSchema.Empty).Deserialize<T>(reader);
        }
    }
}