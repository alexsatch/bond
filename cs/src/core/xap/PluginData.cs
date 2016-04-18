namespace Bond.xap
{
    using System;

    public abstract class PluginData : IDisposable
    {
        public bool IsReadOnly { get; protected internal set; }

        public void Dispose()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}