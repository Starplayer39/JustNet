/*
 * JustNet - Just some code for studying part of C# TCP networking
 * 
 * Copyright(c) 2022, Starplayer39
 * The project is under BSD 3-Clause License. Please see the LICENSE.txt
*/

namespace JustNet
{
    using WritablePacket = PacketPacker.WritablePacket;
    using ReadablePacket = PacketPacker.ReadablePacket;

    public interface INetworkData
    {
        public void WriteData(WritablePacket writablePacket);

        public void ReadData(ReadablePacket readablePacket);
    }
}
