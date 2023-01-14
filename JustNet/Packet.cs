/*
 * JustNet - Just some code for studying part of C# TCP networking
 * 
 * Copyright(c) 2022, Starplayer39
 * The project is under BSD 3-Clause License. Please see the LICENSE.txt
*/

using static JustNet.Constant;

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
