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
            Stopwatch watch = new Stopwatch();
            while (true)
            {
                watch.Start();
                while (watch.ElapsedMilliseconds < controller.MSPerFrame)
                {
                    //Console.WriteLine(MSPerFrame + "");
                }
                watch.Reset();
                lock (controller.world)
                {
                    foreach (SocketState client in controller.Clients.Keys)
                    {
                        controller.sendMesssage(client);
                    }
                }
            }
        }

        private static void StartServer()
        {
            Networking.StartServer(OnStart, 11000);
        }

        private static void OnStart(SocketState state)
        {
            if (state.ErrorOccured)
                return;

            Console.WriteLine("client connected");
            state.OnNetworkAction = ReceiveMessageFromClient;
            Console.WriteLine("sending");
            Networking.GetData(state);
        }

        private static void ReceiveMessageFromClient(SocketState state)
        {
            if (state.ErrorOccured)
            {
                Console.WriteLine("client left dawg. stop simping dawg");
                controller.Clients.TryGetValue(state, out int TankID);
                controller.Clients.Remove(state);
                controller.ClientName.Remove(state);
                controller.world.Tanks.Remove(TankID);
                return;
            }

            controller.UpdateWorld(state);
            controller.sendMesssage(state);
            Networking.GetData(state);
        }
    }
}

