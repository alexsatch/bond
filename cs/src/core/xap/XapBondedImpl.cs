namespace Bond.xap
{
    using System;
    using System.Diagnostics;

    using Bond;

    public class XapBondedImpl<T> : XapBonded<T>, IBonded<T>, IXapReadonly
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Lazy<T> lazyValue;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IProjectable bonded;

        private XapBondedImpl(IProjectable bonded)
        {
            this.Projectable = bonded;
        }

        private IProjectable Projectable
        {
            get { return this.bonded; }
            set
            {
                this.bonded = value;
                this.lazyValue = new Lazy<T>(() => CreateReadOnlyValue(value));
            }
        }

        public override T Value
        {
            get { return this.lazyValue.Value; }
            set
            {
                if (ReadOnly)
                {
                    throw new InvalidOperationException("The object is readonly.");
                }

                this.Projectable = XapBondedLocal<T>.FromValue(value);
            }
        }

        public override XapBonded<TR> Cast<TR>()
        {
            // we keep the same instance of Bonded since it's immutable
            // we don't call Bonded.Convert<TR>() because it will return null in case of Local<T>
            // (it allows us to call Cast<TR> when TR does not inherit from T)

            return new XapBondedImpl<TR>(this.Projectable);
        }

        public T Deserialize()
        {
            return this.Projectable.Deserialize<T>();
        }

        public void Serialize<W>(W writer)
        {
            this.Projectable.Serialize(writer);
        }

        public U Deserialize<U>()
        {
            return this.Projectable.Deserialize<U>();
        }

        public IBonded<U> Convert<U>()
        {
            return new XapBondedImpl<U>(this.bonded);
        }

        internal static XapBonded<T> FromProjectable(IProjectable bonded)
        {
            return new XapBondedImpl<T>(bonded);
        }

        public static XapBonded<T> FromLocal(T value)
        {
            return new XapBondedImpl<T>(XapBondedLocal<T>.FromValue(value));
        }

        public static XapBonded<T> Empty()
        {
            return new XapBondedImpl<T>(XapBondedLocal<T>.Empty());
        }

        private static T CreateReadOnlyValue(IProjectable projectable)
        {
            return projectable.GetProjection<T>();
        }

        public void SetReadonly()
        {
            if (ReadOnly)
            {
                return;
            }

            this.ReadOnly = true;
            this.Projectable.SetReadOnly();
        }
    }
}