/*
 * JustNet - Just some code for studying part of C# TCP networking
 * 
 * Copyright(c) 2022, Starplayer39
 * The project is under BSD 3-Clause License. Please see the LICENSE.txt
*/

#pragma warning disable CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable

namespace JustNet
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Collections.Generic;
    using static JustNet.NetworkRunner.Constant;    

    public sealed partial class NetworkRunner
    {
        public NetworkRunningType NetworkType { get; private set; }

        public bool IsServer { get => NetworkType == NetworkRunningType.Server; }
        public bool IsClient { get => NetworkType == NetworkRunningType.Client; }

        public bool IsNetworkRunning { get => IsServer ? serverRunner.IsRunning : clientRunner.IsRunning; }                     

        public Server ServerRunner
        {
            get
            {
                if (ServerRunner == null && !IsNetworkRunning && !IsClient)
                {
                    serverRunner = new Server();
                }

                if (serverRunner == null)
                {
                    throw new Exception();
                }

                return serverRunner;
            }

            private set
            {
                serverRunner = value;
            }
        }

        private Server serverRunner;

        public Client ClientRunner
        {
            get
            {
                if (clientRunner == null && !IsNetworkRunning && !IsServer)
                {
                    clientRunner = new Client();
                }

                if (clientRunner == null)
                {
                    throw new Exception();
                }

                return clientRunner;
            }

            private set
            {
                clientRunner = value;
            }
        }

        private Client clientRunner;
    }                   

    public sealed partial class NetworkRunner
    {
        public static class Constant
        {
            public enum NetworkRunningType
            {
                Server,
                Client
            }

            public enum PacketType
            {
                SYSTEM = 'S', // Packet sent by JustNet
                CUSTOM = 'C' // Packet sent by user
            }

            public enum ServerPacketInformation
            {
                SERVER_CLIENT_ID_SEND = 1,
                SERVER_HAS_STOPPED = 2
            }

            public enum ClientPacketInformation
            {
                CLIENT_RECEIVED_ID_WELL = -1,
                CLIENT_DISCONNECT_REQUEST = -2
            }

            public const uint SERVER_ID = 0;
            public const uint DEFAULT_PORT = 12345;
            public const int DEFAULT_READ_BUFFER_SIZE = 1024;            
        }

        internal static class Utility
        {
            public static IPAddress GetLocalIPAddress()
            {
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

                foreach (IPAddress ipAddress in host.AddressList)
                {
                    if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ipAddress;
                    }
                }

                return null;
            }

            public class UniqueQueue<T>
            {
                private readonly Queue<T> queue;

                public int Count { get => queue.Count; }

                public bool Contains(T data) => queue.Contains(data);

                public UniqueQueue()
                {
                    queue = new Queue<T>();
                }

                public void Enqueue(T data)
                {
                    if (queue.Contains(data))
                    {
                        return;
                    }

                    queue.Enqueue(data);
                }

                public T Dequeue()
                {
                    return queue.Dequeue();
                }

                public void OrderByAscending()
                {
                    Queue<T> temp = new Queue<T>();

                    foreach (T item in queue.OrderBy(x => x))
                    {
                        temp.Enqueue(item);
                    }

                    queue.Clear();

                    foreach (T item in temp)
                    {
                        queue.Enqueue(item);
                    }
                }
            }
        }
    }
}

#pragma warning restore CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable
