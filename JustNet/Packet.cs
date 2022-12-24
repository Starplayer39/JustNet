using static JustNet.NetworkRunner.Constant;

namespace JustNet
{
    public abstract class Packet : IDisposable
    {
        public readonly uint SourceClientID;

        internal PacketType PacketType;

        protected bool disposed = false;

        internal Packet(PacketType packetType, uint sourceClientID)
        {
            this.PacketType = packetType;
            this.SourceClientID = sourceClientID;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected abstract void DisposeFunction();

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    DisposeFunction();
                }

                disposed = true;
            }
        }
    }
}
