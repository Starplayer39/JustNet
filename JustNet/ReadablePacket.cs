/*
 * JustNet - Just some code for studying part of C# TCP networking
 * 
 * Copyright(c) 2022, Starplayer39
 * The project is under BSD 3-Clause License. Please see the LICENSE.txt
*/

using System.Text;
using static JustNet.Constant;

namespace JustNet
{
    public class ReadablePacket : Packet
    {
        private byte[] receivedData;
        private uint readPosition;
        private uint lastReadSize;

        internal ReadablePacket(PacketType packetType, uint sourceClientID, byte[] data) : base(packetType, sourceClientID)
        {
            if (data.Length > PacketPacker.ReadBufferSize)
            {
                return;
            }

            receivedData = data;
            readPosition = 0;
            lastReadSize = 0;
        }

        internal ReadablePacket(PacketType packetType, uint sourceClientID, byte[] data, int startReadPosition) : base(packetType, sourceClientID)
        {
            if (data.Length > PacketPacker.ReadBufferSize)
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
