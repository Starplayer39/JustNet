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
    using static JustNet.Constant;    

    public sealed partial class NetworkRunner
    {
        public NetworkRunningType NetworkType { get; private set; }

        public bool IsServer { get => NetworkType == NetworkRunningType.Server; }
        public bool IsClient { get => NetworkType == NetworkRunningType.Client; }

        private bool IsNetworkRunning { get => IsServer ? serverRunner.IsRunning : clientRunner.IsRunning; }                     

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
                    throw new Exception(); // TODO: Error message
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
                    throw new Exception(); // TODO: Error message
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
}

#pragma warning restore CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable
