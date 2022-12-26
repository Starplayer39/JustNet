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
    using static JustNet.Constant;    

    public sealed partial class NetworkRunner
    {
        public static NetworkRunner Singleton;

        public NetworkRunningType NetworkType { get; private set; }

        public bool IsServer { get => NetworkType == NetworkRunningType.Server; }
        public bool IsClient { get => NetworkType == NetworkRunningType.Client; }

        public uint ClientID
        {
            get
            {
                if (IsServer && ServerRunner != null)
                {
                    return ServerRunner.ClientID;
                }

                else if (IsClient && ClientRunner != null)
                {
                    return ClientRunner.ClientID;
                }

                else
                {
                    throw new Exception(); // TODO: Error message
                }
            }
        }

        private bool IsNetworkRunning { get => IsServer ? serverRunner.IsRunning : clientRunner.IsRunning; }                     

        public NetworkRunner(NetworkRunningType networkRunningType)
        {
            if (Singleton == null) Singleton = this;
            else throw new Exception($"Multiple objects of type {nameof(NetworkRunner)}");

            NetworkType = networkRunningType;
        }

        public Server ServerRunner
        {
            get
            {
                /*if (ServerRunner == null && !IsNetworkRunning && !IsClient)
                {
                    serverRunner = new Server();
                }

                if (serverRunner == null)
                {
                    throw new Exception(); // TODO: Error message
                }*/

                if (serverRunner == null) return serverRunner = new Server();

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
                /*if (clientRunner == null && !IsNetworkRunning && !IsServer)
                {
                    clientRunner = new Client();
                }

                if (clientRunner == null)
                {
                    throw new Exception(); // TODO: Error message
                }*/

                if (clientRunner == null) clientRunner = new Client();

                return clientRunner;
            }

            private set
            {
                clientRunner = value;
            }
        }

        private Client clientRunner;
    }                       
}

#pragma warning restore CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable
