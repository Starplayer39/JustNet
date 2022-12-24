using System.Text;
using static JustNet.NetworkRunner.Constant;

namespace JustNet
{
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
}
