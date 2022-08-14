/*
 * JustNet - Just some code for studying part of C# TCP networking
 * 
 * Copyright(c) 2022, Starplayer39
 * The project is under BSD 3-Clause License. Please see the LICENSE.txt
*/

/*
 * TODO:
 *  - client-to-client data transmission via server
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
    using WritablePacket = PacketPacker.WritablePacket;
    using ReadablePacket = PacketPacker.ReadablePacket;

    public sealed partial class NetworkRunner
    {
        public NetworkRunningType NetworkType { get; private set; }

        public bool IsServer { get => NetworkType == NetworkRunningType.Server; }
        public bool IsClient { get => NetworkType == NetworkRunningType.Client; }

        public bool IsNetworkRunning { get => isRunning && (IsServer ? server.IsServerRunning : client.IsClientRunning); }

        public uint ClientID
        {
            get
            {
                if (!IsNetworkRunning)
                {
                    throw new Exception("Network is not currently running");
                }

                if (IsServer)
                {
                    return Server.SERVER_ID;
                }

                if (IsClient)
                {
                    return client.ClientID;
                }

                else
                {
                    throw new Exception("Invalid Client ID");
                }
            }
        }

        public int Port;
        public uint ReadBufferSize;

        private bool isRunning;

        public NetworkRunner()
        {
            Port = 12345; // Default port
            ReadBufferSize = 1024; // Default size
            isRunning = false;
        }
    }

    public sealed partial class NetworkRunner
    {
        private Server server;

        public Action OnServerStart;
        public Action OnServerStop;
        public Action<int> OnClientConnected;
        public Action<int> OnClientDisconnected;
        public Action<int, ReadablePacket> OnDataReceivedFromClient;

        public uint MaxConnection;

        public bool RunAsServer()
        {
            NetworkType = NetworkRunningType.Server;
            server = new Server();

            server.OnServerStart = OnServerStart;
            server.OnServerStop = OnServerStop;
            server.OnClientConnected = OnClientConnected;
            server.OnClientDisconnected = OnClientDisconnected;
            server.OnDataReceivedFromClient = OnDataReceivedFromClient;

            server.MaxConnection = MaxConnection;
            server.ReadBufferSize = ReadBufferSize;

            PacketPacker.ReadBufferSize = ReadBufferSize;

            isRunning = true;

            return server.RunServer((uint)Port);
        }

        public void SendToClient(uint clientID, WritablePacket writablePacket)
        {
            if (!IsNetworkRunning && !IsServer)
            {
                return;
            }

            byte[] data = PacketPacker.PackOutgoingPacket(Server.SERVER_ID, writablePacket);

            server.Send(clientID, data);
        }

        public void Broadcast(WritablePacket writablePacket)
        {
            if (!IsNetworkRunning && !IsServer)
            {
                return;
            }

            byte[] data = PacketPacker.PackOutgoingPacket(Server.SERVER_ID, writablePacket);

            server.Broadcast(data);
        }

        public bool DisconnectClient(int clientID) => server.DisconnectClient(clientID);

        public bool StopServer() => server.StopServer();
    }

    public sealed partial class NetworkRunner
    {
        private Client client;

        public string IPAddress;

        public Action OnClientStart;
        public Action OnClientStop;
        public Action OnConnectedToServer;
        public Action<int, ReadablePacket> OnDataReceivedFromServer;

        public bool RunAsClient()
        {
            NetworkType = NetworkRunningType.Client;
            client = new Client();

            client.OnClientStart = OnClientStart;
            client.OnClientStop = OnClientStop;
            client.OnConnectedToServer = OnConnectedToServer;
            client.OnDataReceivedFromServer = OnDataReceivedFromServer;

            client.ReadBufferSize = ReadBufferSize;

            PacketPacker.ReadBufferSize = ReadBufferSize;

            isRunning = true;

            return client.RunClient(IPAddress, (uint)Port);
        }

        public void SendToServer(WritablePacket writablePacket)
        {
            if (!IsNetworkRunning && !IsClient)
            {
                return;
            }

            byte[] data = PacketPacker.PackOutgoingPacket(client.ClientID, writablePacket);

            client.Send(data);
        }

        public bool StopClient() => client.StopClient();
    }

    public sealed partial class NetworkRunner
    {
        internal sealed class Server
        {
            public uint ReadBufferSize
            {
                get => readBufferSize;
                set
                {
                    if (IsServerReady || value <= 0)
                    {
                        return;
                    }

                    readBufferSize = value;
                }
            }

            public uint Port
            {
                get => port;
                set
                {
                    if (IsServerReady)
                    {
                        return;
                    }

                    port = value;
                }
            }

            public uint MaxConnection
            {
                get => maxConnection;
                set
                {
                    if (IsServerReady || value <= 0)
                    {
                        return;
                    }

                    maxConnection = value;
                }
            }

            public bool IsServerReady { get; private set; }

            public bool IsServerReadable { get; private set; }

            public bool IsServerWritable { get; private set; }

            public bool IsServerRunning { get => IsServerReady && IsServerWritable && IsServerReadable; }

            public bool IsServerFull { get => clientIDs.Count <= 0; }

            public int ConnectedClientsCount { get => connectedClients.Count; }

            public Action OnServerStart;
            public Action OnServerStop;
            public Action<int> OnClientConnected;
            public Action<int> OnClientDisconnected;
            public Action<int, ReadablePacket> OnDataReceivedFromClient;

            public const int SERVER_ID = 0;

            private sealed class NetTcpClient
            {
                public TcpClient TcpClient;
                public NetworkStream NetworkStream;

                public uint ClientID;
                public uint ReadBufferSize;
                public byte[] ReadBuffer;

                public NetTcpClient(TcpClient tcpClient, NetworkStream networkStream,
                    uint clientID, uint readBufferSize)
                {
                    TcpClient = tcpClient;
                    NetworkStream = networkStream;
                    ClientID = clientID;
                    ReadBufferSize = readBufferSize;

                    ReadBuffer = new byte[ReadBufferSize];
                }
            }

            private TcpListener tcpListener;
            private Utility.UniqueQueue<uint> clientIDs;
            private Dictionary<uint, NetTcpClient> connectedClients;
            private uint port;
            private uint maxConnection;
            private uint readBufferSize;

            public Server()
            {
                ReadBufferSize = 1024; // Default value
                maxConnection = 1; // Default value
                port = 12345; // Default value

                tcpListener = null;
                connectedClients = new Dictionary<uint, NetTcpClient>();
                clientIDs = new Utility.UniqueQueue<uint>();

                IsServerReady = false;
                IsServerReadable = false;
                IsServerWritable = false;

                OnServerStart = null;
                OnClientConnected = null;
                OnClientDisconnected = null;
                OnDataReceivedFromClient = null;
            }

            public bool IsValidClient(uint clientID) => connectedClients.ContainsKey(clientID);

            public bool RunServer(uint port = 12345)
            {
                if (IsServerReady)
                {
                    return false;
                }

                this.port = port;

                IPAddress iPAddress = Utility.GetLocalIPAddress();

                if (iPAddress == null)
                {
                    return false;
                }

                IPEndPoint ipEndPoint = new IPEndPoint(iPAddress, (int)port);

                for (uint i = 1; i <= MaxConnection; i++)
                {
                    clientIDs.Enqueue(i);
                }

                tcpListener = new TcpListener(ipEndPoint);
                tcpListener.Start((int)maxConnection);

                if (OnServerStart != null)
                {
                    OnServerStart();
                }

                IsServerReady = true;

                if (!IsServerFull)
                {
                    tcpListener.BeginAcceptTcpClient(
                        new AsyncCallback(BeginAcceptTcpClientCallback), null);
                }

                return true;
            }

            private void BeginAcceptTcpClientCallback(IAsyncResult asyncResult)
            {
                if (!IsServerReady)
                {
                    return;
                }

                uint clientID = clientIDs.Dequeue();

                TcpClient tcpClient = tcpListener.EndAcceptTcpClient(asyncResult);
                NetworkStream networkStream = tcpClient.GetStream();
                NetTcpClient netTcpClient = new NetTcpClient(tcpClient, networkStream,
                    clientID, readBufferSize);

                using (WritablePacket writablePacket = new WritablePacket(PacketType.SYSTEM))
                {
                    writablePacket.Write((int)ServerPacketInformation.SERVER_CLIENT_ID_SEND);
                    writablePacket.Write((int)clientID);

                    byte[] data = PacketPacker.PackOutgoingPacket(SERVER_ID, writablePacket);

                    networkStream.BeginWrite(data, 0, data.Length,
                        new AsyncCallback(ClientIDSendCallback), netTcpClient);
                }

                if (!IsServerFull)
                {
                    tcpListener.BeginAcceptTcpClient(
                        new AsyncCallback(BeginAcceptTcpClientCallback), null);
                }
            }

            private void ClientIDSendCallback(IAsyncResult asyncResult)
            {
                NetTcpClient netTcpClient = (NetTcpClient)asyncResult.AsyncState;
                NetworkStream networkStream = netTcpClient.NetworkStream;

                networkStream.EndWrite(asyncResult);
                networkStream.BeginRead(netTcpClient.ReadBuffer, 0, (int)readBufferSize,
                    new AsyncCallback(ClientIDReceivedConfirmCallback), netTcpClient);
            }

            private void ClientIDReceivedConfirmCallback(IAsyncResult asyncResult)
            {
                NetTcpClient netTcpClient = (NetTcpClient)asyncResult.AsyncState;
                NetworkStream networkStream = netTcpClient.NetworkStream;

                int readBytesCount = networkStream.EndRead(asyncResult);

                using (ReadablePacket readablePacket = PacketPacker.PackIncomingPacket(readBytesCount, netTcpClient.ReadBuffer))
                {
                    if (readablePacket.PacketType != PacketType.SYSTEM || readablePacket.SourceClientID != netTcpClient.ClientID)
                    {
                        return;
                    }

                    ClientPacketInformation information = (ClientPacketInformation)readablePacket.ReadInt();

                    if (information == ClientPacketInformation.CLIENT_RECEIVED_ID_WELL)
                    {
                        IsServerReadable = true;
                        IsServerWritable = true;

                        connectedClients.Add(netTcpClient.ClientID, netTcpClient);
                        Read(netTcpClient.ClientID);

                        // lastClientID++;

                        if (OnClientConnected != null)
                        {
                            OnClientConnected((int)netTcpClient.ClientID);
                        }
                    }

                    else
                    {
                        using (WritablePacket writablePacket = new WritablePacket(PacketType.SYSTEM))
                        {
                            writablePacket.Write((int)ServerPacketInformation.SERVER_CLIENT_ID_SEND);
                            writablePacket.Write((int)netTcpClient.ClientID);

                            byte[] data = PacketPacker.PackOutgoingPacket(SERVER_ID, writablePacket);

                            networkStream.BeginWrite(data, 0, data.Length,
                                new AsyncCallback(ClientIDSendCallback), netTcpClient);
                        }
                    }
                }
            }

            public void Send(uint targetClientID, byte[] dataToSend)
            {
                if (!IsServerWritable)
                {
                    return;
                }

                Send(targetClientID, dataToSend, 0, dataToSend.Length);
            }

            private void Send(uint targetClientID, byte[] dataToSend, int offset,
                int count)
            {
                if (!IsValidClient(targetClientID))
                {
                    return;
                }

                NetTcpClient netTcpClient = connectedClients[targetClientID];
                NetworkStream networkStream = netTcpClient.NetworkStream;

                networkStream.BeginWrite(dataToSend, offset, count, null, null);
            }

            public void Broadcast(byte[] dataToSend)
            {
                if (!IsServerWritable)
                {
                    return;
                }

                foreach (uint clientID in connectedClients.Keys)
                {
                    Send(clientID, dataToSend, 0, dataToSend.Length);
                }
            }

            public void Read(uint targetClientID)
            {
                if (!IsServerReadable)
                {
                    return;
                }

                Read(targetClientID, 0, (int)readBufferSize);
            }

            private void Read(uint targetClientID, int offset, int count)
            {
                if (!IsValidClient(targetClientID))
                {
                    return;
                }

                NetTcpClient netTcpClient = connectedClients[targetClientID];
                NetworkStream networkStream = netTcpClient.NetworkStream;

                Array.Clear(netTcpClient.ReadBuffer, 0, (int)netTcpClient.ReadBufferSize);

                networkStream.BeginRead(netTcpClient.ReadBuffer, offset, count,
                    new AsyncCallback(BeginReadCallback), netTcpClient);
            }

            private void BeginReadCallback(IAsyncResult asyncResult)
            {
                NetTcpClient netTcpClient = (NetTcpClient)asyncResult.AsyncState;
                NetworkStream networkStream = netTcpClient.NetworkStream;

                if (netTcpClient == null || !IsServerRunning)
                {
                    return;
                }

                int readBytesCount = networkStream.EndRead(asyncResult);

                ReadablePacket readablePacket = PacketPacker.PackIncomingPacket(readBytesCount, netTcpClient.ReadBuffer);

                if (!IsValidClient((uint)readablePacket.SourceClientID))
                {
                    return;
                }

                if (readablePacket.PacketType == PacketType.SYSTEM)
                {
                    ClientPacketInformation clientPacketInformation = (ClientPacketInformation)readablePacket.ReadInt();

                    if (clientPacketInformation == ClientPacketInformation.CLIENT_DISCONNECT_REQUEST)
                    {
                        DisconnectClient(readablePacket.SourceClientID);
                        return;
                    }
                }

                if (readablePacket.PacketType == PacketType.CUSTOM || OnDataReceivedFromClient != null)
                {
                    OnDataReceivedFromClient(readBytesCount, readablePacket);
                }

                Array.Clear(netTcpClient.ReadBuffer, 0, (int)netTcpClient.ReadBufferSize);

                networkStream.BeginRead(netTcpClient.ReadBuffer, 0, (int)ReadBufferSize, new AsyncCallback(BeginReadCallback), netTcpClient);
            }

            public bool DisconnectClient(int clientID)
            {
                bool success = DisconnectClientInternal(clientID);

                if (success && OnClientDisconnected != null)
                {
                    OnClientDisconnected(clientID);
                }

                return success;
            }

            private bool DisconnectClientInternal(int clientID)
            {
                uint id = (uint)clientID;

                if (!IsValidClient(id))
                {
                    // No client exists with such ID
                    return false;
                }

                NetTcpClient netTcpClient = connectedClients[id];
                netTcpClient.NetworkStream?.Close();
                netTcpClient.TcpClient?.Close();

                clientIDs.Enqueue(id);
                clientIDs.OrderByAscending();
                connectedClients.Remove(id);

                return true;
            }

            public bool StopServer()
            {
                if (!IsServerReady)
                {
                    return false;
                }

                foreach ((_, NetTcpClient netTcpClient) in connectedClients)
                {
                    using (WritablePacket writablePacket = new WritablePacket(PacketType.SYSTEM))
                    {
                        writablePacket.Write((int)ServerPacketInformation.SERVER_HAS_STOPPED);

                        netTcpClient.NetworkStream.Write(PacketPacker.PackOutgoingPacket(SERVER_ID, writablePacket));
                    }

                    netTcpClient.NetworkStream?.Close();
                    netTcpClient.TcpClient?.Close();
                }

                if (OnServerStop != null)
                {
                    OnServerStop();
                }

                tcpListener.Stop();

                IsServerWritable = false;
                IsServerReadable = false;
                IsServerReady = false;

                return true;
            }
        }

    }

    public sealed partial class NetworkRunner
    {
        internal sealed class Client
        {
            public uint ReadBufferSize
            {
                get => readBufferSize;
                set
                {
                    if (IsClientReady || value <= 0)
                    {
                        return;
                    }

                    readBufferSize = value;
                }
            }

            public uint Port
            {
                get => port;
                set
                {
                    if (IsClientReady)
                    {
                        return;
                    }

                    port = value;
                }
            }

            public uint ClientID { get; private set; }

            public bool IsClientReady { get; private set; }

            public bool IsClientReadable { get; private set; }

            public bool IsClientWritable { get; private set; }

            public bool IsClientRunning { get => IsClientReady && IsClientWritable && IsClientReadable; }

            public uint port;
            public uint readBufferSize;

            public Action OnClientStart;
            public Action OnClientStop;
            public Action OnConnectedToServer;
            public Action<int, ReadablePacket> OnDataReceivedFromServer;

            private TcpClient tcpClient;
            private NetworkStream networkStream;

            private byte[] readBuffer;

            public Client()
            {
                ClientID = 123456789; // Default
                readBufferSize = 1024; // Default
                port = 12345; // Default

                tcpClient = null;
                networkStream = null;

                IsClientReady = false;
                IsClientReadable = false;
                IsClientWritable = false;

                OnClientStart = null;
                OnClientStop = null;
                OnConnectedToServer = null;
                OnDataReceivedFromServer = null;
            }

            public bool RunClient(string stringIPAddress, uint port = 12345)
            {
                if (IsClientReady)
                {
                    return false;
                }

                this.port = port;

                IPAddress iPAddress = System.Net.IPAddress.Parse(stringIPAddress);
                IPEndPoint iPEndPoint = new IPEndPoint(iPAddress, (int)this.port);

                tcpClient = new TcpClient();

                IsClientReady = true;

                if (OnClientStart != null)
                {
                    OnClientStart();
                }

                tcpClient.BeginConnect(iPEndPoint.Address.ToString(), iPEndPoint.Port,
                    new AsyncCallback(BeginConnectCallback), tcpClient);

                return true;
            }

            private void BeginConnectCallback(IAsyncResult asyncResult)
            {
                TcpClient tcpClient = (TcpClient)asyncResult.AsyncState;

                tcpClient.EndConnect(asyncResult);
                networkStream = tcpClient.GetStream();

                readBuffer = new byte[readBufferSize];

                networkStream.BeginRead(readBuffer, 0, (int)readBufferSize,
                    new AsyncCallback(ClientIDReceiveCallback), null);
            }

            private void ClientIDReceiveCallback(IAsyncResult asyncResult)
            {
                int readBytesCount = networkStream.EndRead(asyncResult);

                using (ReadablePacket readablePacket = PacketPacker.PackIncomingPacket(readBytesCount, readBuffer))
                {
                    if (readablePacket.PacketType != PacketType.SYSTEM || readablePacket.SourceClientID != Server.SERVER_ID)
                    {
                        return;
                    }

                    ServerPacketInformation information = (ServerPacketInformation)readablePacket.ReadInt();

                    if (information == ServerPacketInformation.SERVER_CLIENT_ID_SEND)
                    {
                        ClientID = readablePacket.ReadUInt();

                        using (WritablePacket writablePacket = new WritablePacket(PacketType.SYSTEM))
                        {
                            writablePacket.Write((int)ClientPacketInformation.CLIENT_RECEIVED_ID_WELL);

                            byte[] data = PacketPacker.PackOutgoingPacket(ClientID, writablePacket);

                            networkStream.BeginWrite(data, 0, data.Length, null, null);
                        }

                        IsClientReadable = true;
                        IsClientWritable = true;

                        Read();

                        if (OnConnectedToServer != null)
                        {
                            OnConnectedToServer();
                        }
                    }

                    else
                    {
                        networkStream.BeginRead(readBuffer, 0, (int)readBufferSize,
                        new AsyncCallback(ClientIDReceiveCallback), null);
                    }
                }
            }

            public void Send(byte[] dataToSend)
            {
                if (!IsClientReady)
                {
                    return;
                }

                Send(dataToSend, 0, dataToSend.Length);
            }

            private void Send(byte[] dataToSend, int offset, int count)
            {
                networkStream.BeginWrite(dataToSend, offset, count, null, null);
            }

            public void Read()
            {
                if (!IsClientReady)
                {
                    return;
                }

                Read(0, (int)readBufferSize);
            }

            private void Read(int offset, int count)
            {
                Array.Clear(readBuffer, 0, (int)readBufferSize);

                networkStream.BeginRead(readBuffer, offset, count, new AsyncCallback(BeginReadCallback), networkStream);
            }

            private void BeginReadCallback(IAsyncResult asyncResult)
            {
                NetworkStream networkStream = (NetworkStream)asyncResult.AsyncState;

                if (networkStream == null || !IsClientReady)
                {
                    return;
                }

                int readBytesCount = networkStream.EndRead(asyncResult);

                ReadablePacket readablePacket = PacketPacker.PackIncomingPacket(readBytesCount, readBuffer);

                if (readablePacket.SourceClientID != Server.SERVER_ID)
                {
                    return;
                }

                if (readablePacket.PacketType == PacketType.SYSTEM)
                {
                    ServerPacketInformation serverPacketInformation = (ServerPacketInformation)readablePacket.ReadInt();

                    if (serverPacketInformation == ServerPacketInformation.SERVER_HAS_STOPPED)
                    {
                        StopClient(true);
                        return;
                    }
                }

                if (readablePacket.PacketType == PacketType.CUSTOM || OnDataReceivedFromServer != null)
                {
                    OnDataReceivedFromServer(readBytesCount, PacketPacker.PackIncomingPacket(readBytesCount, readBuffer));
                }

                Array.Clear(readBuffer, 0, (int)readBufferSize);

                networkStream.BeginRead(readBuffer, 0, (int)readBufferSize, new AsyncCallback(BeginReadCallback), networkStream);
            }

            public bool StopClient(bool isForced = false)
            {
                if (!IsClientReady)
                {
                    return false;
                }

                if (!isForced)
                {
                    using (WritablePacket writablePacket = new WritablePacket(PacketType.SYSTEM))
                    {
                        writablePacket.Write((int)ClientPacketInformation.CLIENT_DISCONNECT_REQUEST);

                        networkStream.Write(PacketPacker.PackOutgoingPacket(ClientID, writablePacket));
                    }
                }

                if (OnClientStop != null)
                {
                    OnClientStop();
                }

                networkStream?.Close();
                tcpClient?.Close();

                IsClientReadable = false;
                IsClientWritable = false;
                IsClientReady = false;

                return true;
            }
        }
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
        }

        private static class Utility
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
