/*
 * JustNet - Just some code for studying part of C# TCP networking
 * 
 * Copyright(c) 2022, Starplayer39
 * The project is under BSD 3-Clause License. Please see the LICENSE.txt
*/

using System;

namespace JustNet
{
    public static class Constant
    {
        public enum NetworkRunningType
        {
            Server,
            Client
        }

        public enum PacketType : byte
        {            
            CUSTOM = 0x63, // Packet sent by user
            SYSTEM = 0x73 // Packet sent by JustNet
        }
        
        public enum ServerPacketInformation : byte
        {
            SERVER_CLIENT_ID_SEND = 0x01, // 2k + 1 (k >= 0)
            SERVER_HAS_STOPPED = 0x03
        }

        public enum ClientPacketInformation : byte
        {
            CLIENT_RECEIVED_ID_WELL = 0x00, // 2k (k >= 0)
            CLIENT_DISCONNECT_REQUEST = 0x02
        }

        public const uint SERVER_ID = 0;
        public const uint DEFAULT_PORT = 12345;
        public const uint DEFAULT_READ_BUFFER_SIZE = 1024;
    }
}

