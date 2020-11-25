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

namespace Server
{
    public class Server
    {
        private static World world;
        private static string MSPerFrame;
        private static string UniverseSize;
        private static string FramesPerShot;
        private static string RespawnRate;
        private static Dictionary<int, SocketState> Clients;
        private static int clientID;
        static void Main(string[] args)
        {
            ReadFile("..\\..\\..\\..\\Resources\\Settings.xml");
            StartServer();
            Clients = new Dictionary<int, SocketState>();
            clientID = 0;

            Stopwatch watch = new Stopwatch();
            while (true)
            {
                watch.Start();
                while (watch.ElapsedMilliseconds < long.Parse(MSPerFrame))
                {
                    //Console.WriteLine(MSPerFrame + "");
                }
                watch.Reset();
                UpdateWorld();
                foreach (SocketState client in Clients.Values)
                {
                    //sendMesssage(client);
                }
            }


        }

        private static void UpdateWorld()
        {
            
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
            Clients.Add(clientID, state);
            clientID++;
            state.OnNetworkAction = ReceiveMessageFromClient;
            Networking.Send(state.TheSocket, clientID + "\n");
            Networking.Send(state.TheSocket, UniverseSize + "\n");
            Console.WriteLine("sending");
            Networking.GetData(state);
        }

        private static void ReceiveMessageFromClient(SocketState state)
        {
            if (state.ErrorOccured)
            {
                Console.WriteLine("client left dawg. stop simping dawg");
                Clients.Remove(clientID);
                return;
            }
            UpdateWorld();
            sendMesssage(state);
            Networking.GetData(state);
        }

        private static void sendMesssage(SocketState state)
        {
            foreach(Tank tank in world.Tanks.Values)
            {
                Networking.Send(state.TheSocket, JsonConvert.SerializeObject(tank));
            }
            foreach (Projectile proj in world.Projectiles.Values)
            {
                Networking.Send(state.TheSocket, JsonConvert.SerializeObject(proj));
            }
            foreach (Powerup power in world.Powerups.Values)
            {
                Networking.Send(state.TheSocket, JsonConvert.SerializeObject(power));
            }
        }

        private static void ReadFile(string path)
        {
            using (XmlReader reader = XmlReader.Create(path))
            {
                Vector2D p1 = null;
                Vector2D p2 = null;
                string x1 = null;
                string y1 = null;
                string x2 = null;
                string y2 = null;
                int i = 0;
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        switch (reader.Name)
                        {
                            case "UniverseSize":
                                reader.Read();
                                UniverseSize = reader.Value;
                                world = new World(int.Parse(UniverseSize));
                                break;

                            case "MSPerFrame":
                                reader.Read();
                                MSPerFrame = reader.Value;
                                break;

                            case "FramesPerShot":
                                reader.Read();
                                FramesPerShot = reader.Value;
                                break;

                            case "RespawnRate":
                                reader.Read();
                                RespawnRate = reader.Value;
                                break;

                            case "Wall":
                                break;

                            case "p1":
                                p1 = new Vector2D();
                                break;

                            case "p2":
                                p2 = new Vector2D();
                                break;

                            case "x":
                                reader.Read();
                                if (x1 is null)
                                    x1 = reader.Value;
                                else
                                    x2 = reader.Value;
                                break;

                            case "y":
                                reader.Read();
                                if (y1 is null)
                                    y1 = reader.Value;
                                else
                                    y2 = reader.Value;
                                break;
                        }

                    }
                    else // If it's not a start element, it's probably an end element
                    {
                        if (reader.Name == "Wall")
                        {
                            Wall wall = new Wall();
                            p1 = new Vector2D(double.Parse(x1), double.Parse(y1));
                            p2 = new Vector2D(double.Parse(x2), double.Parse(y2));
                            wall.SetP1(p1);
                            wall.SetP2(p2);
                            wall.setWallID(i);
                            world.Walls.Add(wall.getWallNum(), wall);
                            i++;
                            p1 = null;
                            p2 = null;
                        }
                        continue;
                    }
                }
            }
        }
    }
}

