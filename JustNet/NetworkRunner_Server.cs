/*
 * JustNet - Just some code for studying part of C# TCP networking
 * 
 * Copyright(c) 2022, Starplayer39
 * The project is under BSD 3-Clause License. Please see the LICENSE.txt
*/

#pragma warning disable CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable

using System.Net;
using System.Net.Sockets;
using static JustNet.Constant;
using ClientIDQueue = JustNet.Utility.UniqueQueue<uint>;

namespace JustNet
{
    public sealed partial class NetworkRunner
    {
        public sealed class Server : Common
        {
            private sealed class NetTcpClient
            {
                public TcpClient TcpClient;
                public NetworkStream NetworkStream;

                public uint ClientID;
                public uint ReadBufferSize;

                public byte[] ReadBuffer;

                public bool IsReading;

                public void Init(TcpClient tcpClient, NetworkStream networkStream, uint clientID, uint readBufferSize)
                {
                    TcpClient = tcpClient;
                    NetworkStream = networkStream;
                    ClientID = clientID;
                    ReadBufferSize = readBufferSize;

                    ReadBuffer = new byte[ReadBufferSize];

                    IsReading = false;
                }
            }

            public uint MaxConnection
            {
                get => maxConnection;
                set
                {
                    if (IsReady)
                    {
                        return;
                    }

                    maxConnection = value;
                }
            }

            public bool IsFull { get => clientIDs.Count <= 0; }

            public int ConnectedClientsCount { get => connectedClients.Count; }

            public Action<uint> OnClientConnected;
            public Action<uint> OnClientDisconnected;
            public Action<int, ReadablePacket> OnDataReceivedFromClient;

            private TcpListener tcpListener;
            private ClientIDQueue clientIDs;
            private Dictionary<uint, NetTcpClient> connectedClients;

            private uint maxConnection;

            internal Server() : base() => Init();

            internal Server(int readBufferSize) : base(readBufferSize) => Init();

            internal Server(uint port) : base(port) => Init();

            internal Server(int readBufferSize, uint port) : base(readBufferSize, port) => Init();

            protected override void Init()
            {
                maxConnection = 5; // Default value
                ClientID = Constant.SERVER_ID;

                tcpListener = null;
                connectedClients = new Dictionary<uint, NetTcpClient>();
                clientIDs = new ClientIDQueue();

                IsReady = false;
                IsReadable = false;
                IsWritable = false;

                OnClientConnected = null;
                OnClientDisconnected = null;
                OnDataReceivedFromClient = null;
            }

            public bool IsValidClient(uint clientID)
            {
                return connectedClients.ContainsKey(clientID);
            }

            public override bool Run()
            {
                if (IsReady)
                {
                    return false;
                }

                IPAddress iPAddress = Utility.GetLocalIPAddress();

                if (iPAddress == null)
                {
                    return false;
                }

                IPEndPoint ipEndPoint = new IPEndPoint(iPAddress, (int)port);

                for (uint i = Constant.SERVER_ID + 1; i <= MaxConnection; i++)
                {
                    clientIDs.Enqueue(i);
                }

                tcpListener = new TcpListener(ipEndPoint);
                tcpListener.Start((int)maxConnection);

                if (OnStart != null)
                {
                    OnStart();
                }

                IsReady = true;

                if (!IsFull)
                {
                    tcpListener.BeginAcceptTcpClient(new AsyncCallback(BeginAcceptTcpClientCallback), null);
                }

                return true;
            }

            private void BeginAcceptTcpClientCallback(IAsyncResult asyncResult)
            {
                if (!IsReady)
                {
                    return;
                }

                uint clientID = clientIDs.Dequeue();

                TcpClient tcpClient = tcpListener.EndAcceptTcpClient(asyncResult);
                NetworkStream networkStream = tcpClient.GetStream();
                NetTcpClient netTcpClient = connectedClients.ContainsKey(clientID) ? connectedClients[clientID] : new NetTcpClient();

                netTcpClient.Init(tcpClient, networkStream, clientID, readBufferSize);

                using (WritablePacket writablePacket = new WritablePacket(PacketType.SYSTEM, Constant.SERVER_ID))
                {
                    writablePacket.Write((byte)ServerPacketInformation.SERVER_CLIENT_ID_SEND);
                    writablePacket.Write(clientID);

                    byte[] data = PacketPacker.PackOutgoingPacket(writablePacket);

                    networkStream.BeginWrite(data, 0, data.Length, new AsyncCallback(ClientIDSendCallback), netTcpClient);
                }

                if (!IsFull)
                {
                    tcpListener.BeginAcceptTcpClient(new AsyncCallback(BeginAcceptTcpClientCallback), null);
                }
            }

            private void ClientIDSendCallback(IAsyncResult asyncResult)
            {
                if (!IsReady)
                {
                    return;
                }

                NetTcpClient netTcpClient = (NetTcpClient)asyncResult.AsyncState;
                NetworkStream networkStream = netTcpClient.NetworkStream;

                networkStream.EndWrite(asyncResult);
                networkStream.BeginRead(netTcpClient.ReadBuffer, 0, (int)readBufferSize, new AsyncCallback(ClientIDReceivedConfirmCallback), netTcpClient);
            }

            private void ClientIDReceivedConfirmCallback(IAsyncResult asyncResult)
            {
                if (!IsReady)
                {
                    return;
                }

                NetTcpClient netTcpClient = (NetTcpClient)asyncResult.AsyncState;
                NetworkStream networkStream = netTcpClient.NetworkStream;

                int readBytesCount = networkStream.EndRead(asyncResult);

                using (ReadablePacket readablePacket = PacketPacker.PackIncomingPacket(readBytesCount, netTcpClient.ReadBuffer))
                {
                    if (readablePacket.PacketType != PacketType.SYSTEM || readablePacket.SourceClientID != netTcpClient.ClientID)
                    {
                        throw new Exception(); // TODO: Error message
                    }

                    ClientPacketInformation information = (ClientPacketInformation)readablePacket.ReadByte();

                    if (information == ClientPacketInformation.CLIENT_RECEIVED_ID_WELL)
                    {
                        IsReadable = true;
                        IsWritable = true;

                        if (!connectedClients.ContainsKey(netTcpClient.ClientID))
                        {
                            connectedClients.Add(netTcpClient.ClientID, netTcpClient);
                        }

                        Read(netTcpClient.ClientID);

                        if (OnClientConnected != null)
                        {
                            OnClientConnected(netTcpClient.ClientID);
                        }
                    }

                    else
                    {
                        using (WritablePacket writablePacket = new WritablePacket(PacketType.SYSTEM, Constant.SERVER_ID))
                        {
                            writablePacket.Write((int)ServerPacketInformation.SERVER_CLIENT_ID_SEND);
                            writablePacket.Write(netTcpClient.ClientID);

                            byte[] data = PacketPacker.PackOutgoingPacket(writablePacket);

                            networkStream.BeginWrite(data, 0, data.Length, new AsyncCallback(ClientIDSendCallback), netTcpClient);
                        }
                    }
                }
            }

            public void Send(uint targetClientID, WritablePacket writablePacket)
            {
                if (!IsWritable)
                {
                    return;
                }

                byte[] dataToSend = PacketPacker.PackOutgoingPacket(writablePacket);

                Send(targetClientID, dataToSend, 0, dataToSend.Length);
            }

            private void Send(uint targetClientID, byte[] dataToSend, int offset, int count)
            {
                if (!IsValidClient(targetClientID))
                {
                    return;
                }

                NetTcpClient netTcpClient = connectedClients[targetClientID];
                NetworkStream networkStream = netTcpClient.NetworkStream;

                networkStream.BeginWrite(dataToSend, offset, count, null, null);
            }

            public void Broadcast(WritablePacket writablePacket)
            {
                if (!IsWritable)
                {
                    return;
                }

                byte[] dataToSend = PacketPacker.PackOutgoingPacket(writablePacket);

                foreach (uint clientID in connectedClients.Keys)
                {
                    Send(clientID, dataToSend, 0, dataToSend.Length);
                }
            }

            public void Read(uint targetClientID)
            {
                if (!IsReadable || !IsValidClient(targetClientID))
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

                if (netTcpClient.IsReading)
                {
                    return;
                }

                netTcpClient.IsReading = true;

                NetworkStream networkStream = netTcpClient.NetworkStream;

                Array.Clear(netTcpClient.ReadBuffer, 0, (int)netTcpClient.ReadBufferSize);

                networkStream.BeginRead(netTcpClient.ReadBuffer, offset, count, new AsyncCallback(BeginReadCallback), netTcpClient);
            }

            public void StopRead(uint targetClientID)
            {
                if (!IsValidClient(targetClientID))
                {
                    return;
                }

                connectedClients[targetClientID].IsReading = false;
            }

            private void BeginReadCallback(IAsyncResult asyncResult)
            {
                NetTcpClient netTcpClient = (NetTcpClient)asyncResult.AsyncState;
                NetworkStream networkStream = netTcpClient.NetworkStream;

                if (netTcpClient == null || !IsRunning || !netTcpClient.IsReading)
                {
                    return;
                }

                int readBytesCount = networkStream.EndRead(asyncResult);

                using (ReadablePacket readablePacket = PacketPacker.PackIncomingPacket(readBytesCount, netTcpClient.ReadBuffer))
                {
                    if (!IsValidClient(readablePacket.SourceClientID))
                    {
                        return;
                    }

                    if (readablePacket.PacketType == PacketType.SYSTEM)
                    {
                        ClientPacketInformation clientPacketInformation = (ClientPacketInformation)readablePacket.ReadByte();

                        if (clientPacketInformation == ClientPacketInformation.CLIENT_DISCONNECT_REQUEST)
                        {
                            DisconnectClient(readablePacket.SourceClientID);
                            return;
                        }
                    }

                    else if (readablePacket.PacketType == PacketType.CUSTOM && OnDataReceivedFromClient != null)
                    {
                        OnDataReceivedFromClient(readBytesCount, readablePacket);
                    }
                }

                Array.Clear(netTcpClient.ReadBuffer, 0, (int)netTcpClient.ReadBufferSize);

                networkStream.BeginRead(netTcpClient.ReadBuffer, 0, (int)ReadBufferSize, new AsyncCallback(BeginReadCallback), netTcpClient);
            }

            public bool DisconnectClient(uint clientID)
            {
                bool success = DisconnectClientInternal(clientID);

                if (success && OnClientDisconnected != null)
                {
                    OnClientDisconnected(clientID);
                }

                return success;
            }

            private bool DisconnectClientInternal(uint clientID)
            {
                if (!IsValidClient(clientID))
                {
                    // No client exists with such ID
                    return false;
                }

                NetTcpClient netTcpClient = connectedClients[clientID];
                netTcpClient.NetworkStream?.Close();
                netTcpClient.TcpClient?.Close();
                netTcpClient.IsReading = false;

                netTcpClient.Init(null, null, clientID, readBufferSize);

                clientIDs.Enqueue(clientID);
                clientIDs.OrderByAscending();

                return true;
            }

            public override bool Stop(bool isForced = false)
            {
                if (!IsReady)
                {
                    return false;
                }

                foreach ((_, NetTcpClient netTcpClient) in connectedClients)
                {
                    if (!isForced)
                    {
                        using (WritablePacket writablePacket = new WritablePacket(PacketType.SYSTEM, Constant.SERVER_ID))
                        {
                            writablePacket.Write((int)ServerPacketInformation.SERVER_HAS_STOPPED);
                            netTcpClient.NetworkStream.Write(PacketPacker.PackOutgoingPacket(writablePacket));
                        }
                    }

                    netTcpClient.NetworkStream?.Close();
                    netTcpClient.TcpClient?.Close();

                    netTcpClient.IsReading = false;
                }

                if (OnStop != null)
                {
                    OnStop();
                }

                tcpListener.Stop();

                IsWritable = false;
                IsReadable = false;
                IsReady = false;

                return true;
            }
        }

    }
}

#pragma warning restore CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable
