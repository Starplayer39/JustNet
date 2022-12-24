/*
 * JustNet - Just some code for studying part of C# TCP networking
 * 
 * Copyright(c) 2022, Starplayer39
 * The project is under BSD 3-Clause License. Please see the LICENSE.txt
*/

using System.Net.Sockets;
using System.Net;
using static JustNet.NetworkRunner.Constant;
using static JustNet.PacketPacker;

namespace JustNet
{
    public sealed partial class NetworkRunner
    {
        public sealed class Client
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
                    if (readablePacket.PacketType != PacketType.SYSTEM || readablePacket.SourceClientID != Constant.SERVER_ID)
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
}
