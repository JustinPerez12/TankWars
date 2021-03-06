using System;
using System.Xml;
using NetworkUtil;
using Model;
using System.Diagnostics;
using TankWars;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Sockets;
using ServerController;

namespace Server
{
    public class Server
    {
        private static servController controller;

        static void Main(string[] args)
        {

            controller = new servController();
            controller.ReadFile("..\\..\\..\\..\\Resources\\Settings.xml");
            StartServer();
            Console.WriteLine("server open to recieve");
            Stopwatch watch = new Stopwatch();
            while (true)
            {
                watch.Start();
                while (watch.ElapsedMilliseconds < controller.MSPerFrame)
                {
                    
                }
                watch.Restart();
                lock (controller.world)
                {
                    foreach (SocketState client in controller.Clients.Keys)
                    {
                        controller.sendMessage(client);
                    }
                }
            }
        }

        /// <summary>
        /// starts the server on the specified port 
        /// </summary>
        private static void StartServer()
        {
            Networking.StartServer(OnStart, 11000);
        }

        /// <summary>
        /// This method is invoked everytime a new client connects 
        /// </summary>
        /// <param name="state"></param>
        private static void OnStart(SocketState state)
        {
            if (state.ErrorOccured)
                return;

            Console.WriteLine("client connected");
            state.OnNetworkAction = RecieveStartupInfo;
            Networking.GetData(state);
        }

        /// <summary>
        /// This method gets invoked everytime the server recieves a command from a client 
        /// </summary>
        /// <param name="state"></param>
        private static void ReceiveMessageFromClient(SocketState state)
        {
            if (state.ErrorOccured)
            {
                PlayerDisconnected(state);
                return;
            }
            controller.UpdateWorld(state);
            Networking.GetData(state);
        }

        /// <summary>
        /// This method is called the first time a client connects
        /// </summary>
        /// <param name="state"></param>
        private static void RecieveStartupInfo(SocketState state)
        {
            controller.UpdateWorld(state);
            state.OnNetworkAction = ReceiveMessageFromClient;
            Networking.GetData(state);
        }

        /// <summary>
        /// private helper method to tell all clients that player has left the game 
        /// </summary>
        /// <param name="state"></param>
        private static void PlayerDisconnected(SocketState state)
        {
            controller.Clients.TryGetValue(state, out int TankID);
            controller.world.Tanks.TryGetValue(TankID, out Tank tank);
            tank.Deactivate();
            tank.SetDisconnect();
            controller.ClientName.Remove(state);
            controller.world.Tanks.Remove(TankID);
            controller.sendDisconnect(tank);
            controller.Clients.Remove(state);

        }

        
    }
}

