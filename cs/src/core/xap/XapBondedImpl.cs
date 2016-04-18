namespace Bond.xap
{
    using System;
    using System.Diagnostics;

    using Bond;

    public class XapBondedImpl<T> : XapBonded<T>, IBonded<T>, IXapReadonly
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Lazy<T> lazyProjection;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IProjectable projectable;

        private XapBondedImpl(IProjectable projectable)
        {
            this.Projectable = projectable;
        }

        private IProjectable Projectable
        {
            get { return this.projectable; }
            set
            {
                this.projectable = value;
                this.lazyProjection = new Lazy<T>(() => CreateReadOnlyValue(value));
            }
        }

        /// <summary>
        /// Represents the "projection" of value stored in <see cref="XapBonded{T}"/> 
        /// </summary>
        /// <remarks>
        /// If <see cref="XapBonded{T}"/> was created from an object instance of type U and U : T,
        /// the instance would be returned as is. <br/>
        /// 
        /// Otherwise, the read-only projection ("deep-copy" of data) will be created.
        /// </remarks>
        /// <returns>Projection of value stored in the current instance.</returns>
        public override T Value
        {
            get
            {
                return this.lazyProjection.Value;
            }
            set
            {
                if (ReadOnly)
                {
                    throw new InvalidOperationException("The object is readonly.");
                }

                this.Projectable = XapBondedLocal<T>.FromValue(value);
            }
        }

        /// <summary>
        /// Converts the current instance of <see cref="XapBonded{T}"/> into <see cref="XapBonded{TR}"/>
        /// </summary>
        /// <typeparam name="TR">Type of which to convert XapBonded{T} to.</typeparam>
        /// <returns><see cref="XapBonded{T}"/> with the same state.</returns>
        public override XapBonded<TR> Cast<TR>()
        {
            return new XapBondedImpl<TR>(this.Projectable);
        }

        T IBonded<T>.Deserialize()
        {
            return this.Projectable.Deserialize<T>();
        }

        void IBonded.Serialize<W>(W writer)
        {
            this.Projectable.Serialize(writer);
        }

        U IBonded.Deserialize<U>()
        {
            return this.Projectable.Deserialize<U>();
        }

        IBonded<U> IBonded.Convert<U>()
        {
            return new XapBondedImpl<U>(this.projectable);
        }

        public static XapBonded<T> FromLocal(T value)
        {
            return FromProjectable(XapBondedLocal<T>.FromValue(value));
        }

        public static XapBonded<T> Empty()
        {
            return FromProjectable(XapBondedLocal<T>.Empty());
        }
        internal static XapBonded<T> FromProjectable(IProjectable projectable)
        {
            return new XapBondedImpl<T>(projectable);
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
            this.Projectable.SetReadonly();
        }
    }
}