using Model;
using NetworkUtil;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Xml;
using TankWars;

namespace ServerController
{
    public class servController 
    {
        public int MSPerFrame;
        public int UniverseSize;
        public int FramesPerShot;
        public int RespawnRate;
        public World world;
        public Dictionary<SocketState, int> Clients;
        public int clientID;
        public servController()
        {
            Clients = new Dictionary<SocketState, int>();
            clientID = -1;
            world = new World(0);
        }

        public void SetWorldSize(int universeSize)
        {
            world.SetWorldSize(universeSize);
        }

        public void UpdateWorld(string command)
        {

        }

        public void setUpWorld(SocketState state)
        {
            Clients.Add(state, clientID);
            clientID++;
            
            Networking.Send(state.TheSocket, clientID + "\n");
            Networking.Send(state.TheSocket, UniverseSize + "\n");
        }

        public void sendMesssage(SocketState state)
        {
            foreach (Tank tank in world.Tanks.Values)
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

        public void ReadFile(string path)
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
                                UniverseSize = int.Parse(reader.Value);
                                SetWorldSize(UniverseSize);
                                break;

                            case "MSPerFrame":
                                reader.Read();
                                MSPerFrame = int.Parse(reader.Value);
                                break;

                            case "FramesPerShot":
                                reader.Read();
                                FramesPerShot = int.Parse(reader.Value);
                                break;

                            case "RespawnRate":
                                reader.Read();
                                RespawnRate = int.Parse(reader.Value);
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
