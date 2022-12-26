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
    using PacketType = JustNet.Constant.PacketType;

    public static class PacketPacker
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
            uint clientID = BitConverter.ToUInt32(data, 0);
            PacketType packetType = (PacketType)BitConverter.ToChar(data, sizeof(uint));                        
            const int read = sizeof(char) + sizeof(uint);
            ArraySegment<byte> remained = new ArraySegment<byte>(data, read, readBytesCount - read);

            return new ReadablePacket(packetType, clientID, remained.ToArray());
        }       

        public static WritablePacket GetWritablePacket() => new WritablePacket(PacketType.CUSTOM, NetworkRunner.Singleton.ClientID);

        public static WritablePacket GetWritablePacket(byte[] data) => new WritablePacket(PacketType.CUSTOM, NetworkRunner.Singleton.ClientID, data);
    }    
}

#pragma warning restore CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable
