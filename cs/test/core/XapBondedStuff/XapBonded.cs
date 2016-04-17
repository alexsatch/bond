namespace UnitTest.XapBondedStuff
{
    using System;

    public abstract class XapBonded<T>
    {
        public abstract T Value { get; set; }

        public bool ReadOnly { get; protected internal set; }

        static XapBonded()
        {
            XapBonded<T>.AssertTypeIsPluginData();
        }

        public abstract XapBonded<TR> Cast<TR>() where TR : PluginData;

        private static void AssertTypeIsPluginData()
        {
            if (!typeof (PluginData).IsAssignableFrom(typeof (T)))
            {
                throw new ArgumentException(string.Format("Type {0} should be assignable to PluginData", (object) typeof (T).FullName));
            }
        }
    }
}