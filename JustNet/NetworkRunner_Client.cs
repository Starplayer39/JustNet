/*
 * JustNet - Just some code for studying part of C# TCP networking
 * 
 * Copyright(c) 2022, Starplayer39
 * The project is under BSD 3-Clause License. Please see the LICENSE.txt
*/

using System.Net.Sockets;
using System.Net;
using static JustNet.Constant;

namespace JustNet
{
    public sealed partial class NetworkRunner
    {
        public sealed class Client : Common
        {
            public Action OnConnectedToServer;
            public Action OnDisconnectedFromServer;
            public Action<int, ReadablePacket> OnDataReceivedFromServer;

            public string IPAddress;
            public bool IsReading;

            private TcpClient tcpClient;
            private NetworkStream networkStream;

            private byte[] readBuffer;

            public Client() : base() => Init();

            protected override void Init()
            {
                ClientID = uint.MaxValue; // Default value
                readBufferSize = Constant.DEFAULT_READ_BUFFER_SIZE;
                port = DEFAULT_PORT;

                tcpClient = null;
                networkStream = null;

                OnConnectedToServer = null;
                OnDisconnectedFromServer = null;
                OnDataReceivedFromServer = null;
            }

            public override bool Run()
            {
                if (IsReady)
                {
                    return false;
                }

                IPAddress iPAddress = System.Net.IPAddress.Parse(this.IPAddress);
                IPEndPoint iPEndPoint = new IPEndPoint(iPAddress, (int)this.port);

                tcpClient = new TcpClient();

                IsReady = true;

                if (OnStart != null)
                {
                    OnStart();
                }

                tcpClient.BeginConnect(iPEndPoint.Address.ToString(), iPEndPoint.Port, new AsyncCallback(BeginConnectCallback), tcpClient);

                return true;
            }

            private void BeginConnectCallback(IAsyncResult asyncResult)
            {
                TcpClient tcpClient = (TcpClient)asyncResult.AsyncState;

                tcpClient.EndConnect(asyncResult);
                networkStream = tcpClient.GetStream();

                readBuffer = new byte[readBufferSize];

                networkStream.BeginRead(readBuffer, 0, (int)readBufferSize, new AsyncCallback(ClientIDReceiveCallback), null);
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

                    ServerPacketInformation information = (ServerPacketInformation)readablePacket.ReadByte();

                    if (information == ServerPacketInformation.SERVER_CLIENT_ID_SEND)
                    {
                        ClientID = readablePacket.ReadUInt();

                        using (WritablePacket writablePacket = new WritablePacket(PacketType.SYSTEM, ClientID))
                        {
                            writablePacket.Write((byte)ClientPacketInformation.CLIENT_RECEIVED_ID_WELL);

                            byte[] data = PacketPacker.PackOutgoingPacket(writablePacket);

                            networkStream.BeginWrite(data, 0, data.Length, null, null);
                        }

                        IsReadable = true;
                        IsWritable = true;

                        Read();

                        if (OnConnectedToServer != null)
                        {
                            OnConnectedToServer();
                        }
                    }

                    else
                    {
                        networkStream.BeginRead(readBuffer, 0, (int)readBufferSize, new AsyncCallback(ClientIDReceiveCallback), null);
                    }
                }
            }

            public void Send(WritablePacket writablePacket)
            {
                if (!IsWritable)
                {
                    return;
                }

                byte[] dataToSend = PacketPacker.PackOutgoingPacket(writablePacket);

                Send(dataToSend, 0, dataToSend.Length);
            }

            private void Send(byte[] dataToSend, int offset, int count)
            {
                networkStream.BeginWrite(dataToSend, offset, count, null, null);
            }

            public void Read()
            {
                if (!IsReadable)
                {
                    return;
                }

                Read(0, (int)readBufferSize);
            }

            private void Read(int offset, int count)
            {
                if (IsReading) return;

                Array.Clear(readBuffer, 0, (int)readBufferSize);

                IsReading = true;
                networkStream.BeginRead(readBuffer, offset, count, new AsyncCallback(BeginReadCallback), networkStream);
            }

            public void StopRead() => IsReading = false;

            private void BeginReadCallback(IAsyncResult asyncResult)
            {
                NetworkStream networkStream = (NetworkStream)asyncResult.AsyncState;

                if (networkStream == null || !IsReady || !IsReading)
                {
                    return;
                }

                int readBytesCount = networkStream.EndRead(asyncResult);

                using (ReadablePacket readablePacket = PacketPacker.PackIncomingPacket(readBytesCount, readBuffer))
                {
                    if (readablePacket.SourceClientID != Constant.SERVER_ID)
                    {
                        return;
                    }

                    if (readablePacket.PacketType == PacketType.SYSTEM)
                    {
                        ServerPacketInformation serverPacketInformation = (ServerPacketInformation)readablePacket.ReadByte();

                        if (serverPacketInformation == ServerPacketInformation.SERVER_HAS_STOPPED)
                        {
                            Stop(true);
                            return;
                        }
                    }

                    else if (readablePacket.PacketType == PacketType.CUSTOM && OnDataReceivedFromServer != null)
                    {
                        OnDataReceivedFromServer(readBytesCount, PacketPacker.PackIncomingPacket(readBytesCount, readBuffer));
                    }
                }

                Array.Clear(readBuffer, 0, (int)readBufferSize);

                networkStream.BeginRead(readBuffer, 0, (int)readBufferSize, new AsyncCallback(BeginReadCallback), networkStream);
            }

            public override bool Stop(bool isForced = false)
            {
                if (!IsReady)
                {
                    return false;
                }

                if (!isForced)
                {
                    using (WritablePacket writablePacket = new WritablePacket(PacketType.SYSTEM, ClientID))
                    {
                        writablePacket.Write((int)ClientPacketInformation.CLIENT_DISCONNECT_REQUEST);

                        networkStream.Write(PacketPacker.PackOutgoingPacket(writablePacket));
                    }
                }

                if (OnStop != null)
                {
                    OnStop();
                }

                networkStream?.Close();
                tcpClient?.Close();

                IsReadable = false;
                IsWritable = false;
                IsReady = false;

                return true;
            }
        }
    }
}
