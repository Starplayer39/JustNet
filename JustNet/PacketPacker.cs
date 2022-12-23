/*
 * JustNet - Just some code for studying part of C# TCP networking
 * 
 * Copyright(c) 2022, Starplayer39
 * The project is under BSD 3-Clause License. Please see the LICENSE.txt 
*/

/*
 * Packet structure
 * 
 * Source Client ID
 * PacketType
 * Actual data
 * 
*/

#pragma warning disable CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable

namespace JustNet
{
    using System;    
    using System.Text;
    using System.Collections.Generic;    
    using PacketType = NetworkRunner.Constant.PacketType;

    public static partial class PacketPacker
    {        
        internal static uint ReadBufferSize = 1024;

        internal static byte[] PackOutgoingPacket(WritablePacket writablePacket)
        {
            List<byte> data = new List<byte>();

            data.AddRange(BitConverter.GetBytes(writablePacket.SourceClientID));
            data.AddRange(BitConverter.GetBytes((char)writablePacket.PacketType));            
            data.AddRange(writablePacket.ToArray());

            return data.ToArray();
        }

        internal static ReadablePacket PackIncomingPacket(int readBytesCount, byte[] data)
        {
            uint clientID = BitConverter.ToUInt32(data, sizeof(char));
            PacketType packetType = (PacketType)BitConverter.ToChar(data, 0);                        
            const int read = sizeof(char) + sizeof(uint);
            ArraySegment<byte> remained = new ArraySegment<byte>(data, read, readBytesCount - read);

            return new ReadablePacket(packetType, clientID, remained.ToArray());
        }       

        public static WritablePacket GetWritablePacket(uint sourceClientID) => new WritablePacket(PacketType.CUSTOM, sourceClientID);

        public static WritablePacket GetWritablePacket(uint sourceClientID, byte[] data) => new WritablePacket(PacketType.CUSTOM, sourceClientID, data);
    }

    public static partial class PacketPacker
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

        public class WritablePacket : Packet
        {            
            private List<byte> buffer;            

            public int Size { get => buffer.Count; }

            internal WritablePacket(PacketType packetType, uint sourceClientID) : base(packetType, sourceClientID)
            {
                buffer = new List<byte>();                
            }

            internal WritablePacket(PacketType packetType, uint sourceClientID, byte[] buffer) : base(packetType, sourceClientID)
            {
                this.buffer = new List<byte>();
                this.buffer.AddRange(buffer);                
            }

            public byte[] ToArray() => buffer.ToArray();

            public void Insert(int index, byte[] data) => buffer.InsertRange(index, data);

            public void Write(byte data)
            {
                buffer.Add(data);
            }

            public void Write(short data)
            {
                buffer.AddRange(BitConverter.GetBytes(data));
            }

            public void Write(ushort data)
            {
                buffer.AddRange(BitConverter.GetBytes(data));
            }

            public void Write(int data)
            {
                buffer.AddRange(BitConverter.GetBytes(data));
            }

            public void Write(uint data)
            {
                buffer.AddRange(BitConverter.GetBytes(data));
            }

            public void Write(long data)
            {
                buffer.AddRange(BitConverter.GetBytes(data));
            }

            public void Write(ulong data)
            {
                buffer.AddRange(BitConverter.GetBytes(data));
            }

            public void Write(float data)
            {
                buffer.AddRange(BitConverter.GetBytes(data));
            }

            public void Write(double data)
            {
                buffer.AddRange(BitConverter.GetBytes(data));
            }

            public void Write(bool data)
            {
                buffer.AddRange(BitConverter.GetBytes(data));
            }

            public void Write(string data)
            {
                byte[] converted = Encoding.UTF8.GetBytes(data);
                Write(converted.Length); // Write length of string data
                buffer.AddRange(converted);
            }

            public void Write(char data)
            {
                buffer.AddRange(BitConverter.GetBytes(data));
            }

            public void Write(INetworkData networkData)
            {
                networkData.WriteData(this);
            }

            protected override void DisposeFunction()
            {
                buffer = null;
            }
        }

        public class ReadablePacket : Packet
        {                    
            private byte[] receivedData;
            private uint readPosition;
            private uint lastReadSize;            

            internal ReadablePacket(PacketType packetType, uint sourceClientID, byte[] data) : base(packetType, sourceClientID)
            {
                if (data.Length > ReadBufferSize)
                {
                    return;
                }

                receivedData = data;
                readPosition = 0;
                lastReadSize = 0;                
            }

            internal ReadablePacket(PacketType packetType, uint sourceClientID, byte[] data, int startReadPosition) : base(packetType, sourceClientID)
            {
                if (data.Length > ReadBufferSize)
                {
                    return;
                }

                receivedData = data;
                readPosition = (uint)startReadPosition;
                lastReadSize = 0;                
            }

            public void Clear() => Array.Clear(receivedData, 0, receivedData.Length);

            public void Undo() => readPosition -= lastReadSize;

            public byte[] ToArray() => receivedData;

            public void ResetRead()
            {
                readPosition = 0;
                lastReadSize = 0;
            }

            public byte ReadByte()
            {
                if (receivedData.Length > readPosition)
                {
                    byte value = receivedData[readPosition];

                    readPosition += sizeof(byte);
                    lastReadSize = sizeof(byte);

                    return value;
                }

                else { throw new Exception($"Unable to read value of type {typeof(byte).Name}"); }
            }

            public short ReadShort()
            {
                if (receivedData.Length > readPosition)
                {
                    short value = BitConverter.ToInt16(receivedData, (int)readPosition);

                    readPosition += sizeof(short);
                    lastReadSize = sizeof(short);

                    return value;
                }

                else { throw new Exception($"Unable to read value of type {typeof(short).Name}"); }
            }

            public ushort ReadUShort()
            {
                if (receivedData.Length > readPosition)
                {
                    ushort value = BitConverter.ToUInt16(receivedData, (int)readPosition);

                    readPosition += sizeof(ushort);
                    lastReadSize = sizeof(ushort);

                    return value;
                }

                else { throw new Exception($"Unable to read value of type {typeof(ushort).Name}"); }
            }

            public int ReadInt()
            {
                if (receivedData.Length > readPosition)
                {
                    int value = BitConverter.ToInt32(receivedData, (int)readPosition);

                    readPosition += sizeof(int);
                    lastReadSize = sizeof(int);

                    return value;
                }

                else { throw new Exception($"Unable to read value of type {typeof(int).Name}"); }
            }

            public uint ReadUInt()
            {
                if (receivedData.Length > readPosition)
                {
                    uint value = BitConverter.ToUInt32(receivedData, (int)readPosition);

                    readPosition += sizeof(uint);
                    lastReadSize = sizeof(uint);

                    return value;
                }

                else { throw new Exception($"Unable to read value of type {typeof(uint).Name}"); }
            }

            public long ReadLong()
            {
                if (receivedData.Length > readPosition)
                {
                    long value = BitConverter.ToInt64(receivedData, (int)readPosition);

                    readPosition += sizeof(long);
                    lastReadSize = sizeof(long);

                    return value;
                }

                else { throw new Exception($"Unable to read value of type {typeof(long).Name}"); }

            }

            public ulong ReadULong()
            {
                if (receivedData.Length > readPosition)
                {
                    ulong value = BitConverter.ToUInt64(receivedData, (int)readPosition);

                    readPosition += sizeof(ulong);
                    lastReadSize = sizeof(ulong);

                    return value;
                }

                else { throw new Exception($"Unable to read value of type {typeof(ulong).Name}"); }
            }

            public float ReadFloat()
            {
                if (receivedData.Length > readPosition)
                {
                    float value = BitConverter.ToSingle(receivedData, (int)readPosition);

                    readPosition += sizeof(float);
                    lastReadSize = sizeof(float);

                    return value;
                }

                else { throw new Exception($"Unable to read value of type {typeof(float).Name}"); }
            }

            public double ReadDouble()
            {
                if (receivedData.Length > readPosition)
                {
                    double value = BitConverter.ToDouble(receivedData, (int)readPosition);

                    readPosition += sizeof(double);
                    lastReadSize = sizeof(double);

                    return value;
                }

                else { throw new Exception($"Unable to read value of type {typeof(double).Name}"); }
            }

            public bool ReadBool()
            {
                if (receivedData.Length > readPosition)
                {
                    bool value = BitConverter.ToBoolean(receivedData, (int)readPosition);

                    readPosition += sizeof(bool);
                    lastReadSize = sizeof(bool);

                    return value;
                }

                else { throw new Exception($"Unable to read value of type {typeof(bool).Name}"); }
            }

            public string ReadString()
            {
                if (receivedData.Length > readPosition)
                {
                    int length = ReadInt();
                    string value = Encoding.UTF8.GetString(receivedData, (int)readPosition, length);

                    readPosition += (uint)length;
                    lastReadSize = (uint)length + sizeof(int);

                    return value;
                }

                else { throw new Exception($"Unable to read value of type {typeof(string).Name}"); }
            }

            public char ReadChar()
            {
                if (receivedData.Length > readPosition)
                {
                    char value = BitConverter.ToChar(receivedData, (int)readPosition);

                    readPosition += sizeof(char);
                    lastReadSize = sizeof(char);

                    return value;
                }

                else { throw new Exception($"Unable to read value of type {typeof(char).Name}"); }
            }

            public void ReadNetworkData(INetworkData networkData)
            {
                networkData.ReadData(this);
            }

            protected override void DisposeFunction()
            {
                receivedData = null;
                readPosition = 0;
                lastReadSize = 0;
            }
        }
    }
}

#pragma warning restore CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable
