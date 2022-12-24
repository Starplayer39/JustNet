/*
 * JustNet - Just some code for studying part of C# TCP networking
 * 
 * Copyright(c) 2022, Starplayer39
 * The project is under BSD 3-Clause License. Please see the LICENSE.txt
*/

namespace JustNet
{
    public sealed partial class NetworkRunner
    {
        public abstract class Common
        {
            public uint ReadBufferSize
            {
                get => readBufferSize;
                set
                {
                    if (IsRunning)
                    {
                        return;
                    }

                    readBufferSize = value;
                    PacketPacker.ReadBufferSize = readBufferSize;
                }
            }

            public uint Port
            {
                get => port;
                set
                {
                    if (IsRunning)
                    {
                        return;
                    }

                    port = value;
                }
            }

            public uint ClientID { get; protected set; }

            public bool IsReady { get; protected set; }

            public bool IsReadable { get; protected set; }

            public bool IsWritable { get; protected set; }

            public bool IsRunning { get => IsReady && IsReadable && IsWritable; }

            public Action OnStart;
            public Action OnStop;

            protected uint port;
            protected uint readBufferSize;         

            internal Common()
            {
                ReadBufferSize = Constant.DEFAULT_READ_BUFFER_SIZE;
                port = Constant.DEFAULT_PORT;
                OnStart = null;
                OnStop = null;
            }

            internal Common(int readBufferSize)
            {
                ReadBufferSize = (uint)readBufferSize;
                port = Constant.DEFAULT_PORT;
                OnStart = null;
                OnStop = null;
            }

            internal Common(uint port)
            {
                ReadBufferSize = Constant.DEFAULT_READ_BUFFER_SIZE;
                this.port = port;
                OnStart = null;
                OnStop = null;
            }

            internal Common(int readBufferSize, uint port)
            {
                ReadBufferSize = (uint)readBufferSize;
                this.port = port;
                OnStart = null;
                OnStop = null;
            }

            protected abstract void Init();

            public abstract bool Run();

            public abstract bool Stop(bool isForced = false);
        }
    }
}
