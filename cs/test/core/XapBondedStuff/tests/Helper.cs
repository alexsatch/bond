namespace UnitTest.XapBondedStuff.tests
{
    internal static class Helper
    {
        public static void Serialize<W, T>(W writer, T o)
        {
            Bond.Serialize.To(writer, o);
        }

        public static T Deserialize<R, T>(R reader)
        {
            return Bond.Deserialize<T>.From(reader);
        }
    }
}