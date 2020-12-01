using Model;
using NetworkUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using TankWars;

namespace ServerController {
    public class servController {
        public int MSPerFrame;
        public int UniverseSize;
        public int FramesPerShot;
        public int RespawnRate;
        public World world;
        public Dictionary<SocketState, int> Clients;
        public Dictionary<SocketState, string> ClientName;
        public int clientID;
        public int projnum;
        public servController()
        {
            Clients = new Dictionary<SocketState, int>();
            ClientName = new Dictionary<SocketState, string>();
            clientID = 0;
            projnum = 0;
            world = new World(0);
        }

        public void SetWorldSize(int universeSize)
        {
            world.SetWorldSize(universeSize);
        }

        public void UpdateWorld(SocketState state)
        {
            string data = state.GetData();
            string[] parts = Regex.Split(data, @"(?<=[\n])");
            state.ClearData();
            foreach (string p in parts)
            {
                if (p.Length == 0)
                    continue;
                if (p[p.Length - 1] != '\n')
                    break;
                UpdateArrived(p, state);
            }
        }

        private void UpdateArrived(string p, SocketState state)
        {
            lock (world)
            {
                try
                {
                    JObject obj = JObject.Parse(p);
                    JToken moving = obj["moving"];
                    JToken fire = obj["fire"];
                    JToken turretDirection = obj["tdir"];
                    Vector2D tdir = parseTdir(turretDirection);
                    Clients.TryGetValue(state, out int TankID);
                    world.Tanks.TryGetValue(TankID, out Tank tank);
                    tank.addFrame();
                    Moving(moving, tdir, tank);
                    Firing(fire, tdir, tank);
                }
                catch (Exception)
                {
                    string name = p.Remove(p.Length - 1, 1);
                    if (!ClientName.ContainsKey(state))
                    {
                        ClientName.Add(state, name);
                        setUpWorld(state);
                    }
                }
            }
        }

        private Vector2D parseTdir(JToken tdir)
        {
            string data = tdir.ToString();
            string[] parts = Regex.Split(data, @"(?<=[\n])");

            // substring x coordinate from JToken
            int x1 = parts[1].IndexOf(':');
            int x2 = parts[1].IndexOf(',');
            string x = parts[1].Substring(x1 + 2, x2 - x1 - 2);

            //substring y coordinate from JToken
            int y1 = parts[2].IndexOf(':');
            int y2 = parts[2].IndexOf('\r');
            string y = parts[2].Substring(y1 + 2, y2 - y1 - 2);

            return new Vector2D(double.Parse(x), double.Parse(y));
        }

        private void Firing(JToken fire, Vector2D turretDirection, Tank tank)
        {
            lock (world)
            {
                if (fire.ToString().Equals("main"))
                {
                    if (world.Projectiles.TryGetValue(tank.GetID(), out Projectile proj)) // proj exists
                    {
                        world.Projectiles.Remove(tank.GetID());
                        if (proj.isDead())
                            return;
                        else
                        {
                            if (collided(proj))
                            {
                                proj.Deactivate();
                                world.DeadProj.Add(proj.getProjnum(), proj);
                            }
                            else
                            {
                                proj.moveProj();
                                world.Projectiles.Add(tank.GetID(), proj);
                            }
                        }
                    }
                    else // need to add proj
                    {
                        if (tank.getFrames() == 1)
                        {
                            Projectile newProj = new Projectile(projnum, tank.GetLocation(), turretDirection, false, tank.GetID());
                            projnum++;
                            world.Projectiles.Add(tank.GetID(), newProj);
                        }
                        else if (tank.getFrames() >= FramesPerShot)
                        {
                            tank.resetFrames();
                        }

                    }

                }
                else if (fire.ToString().Equals("alt"))
                {

                }
                else if (fire.ToString().Equals("none"))
                {
                    if (world.Projectiles.TryGetValue(tank.GetID(), out Projectile proj))
                    {
                        world.Projectiles.Remove(tank.GetID());
                        if (proj.isDead())
                            return;
                        else
                        {
                            if (collided(proj))
                            {
                                proj.Deactivate();
                                world.DeadProj.Add(proj.getProjnum(), proj);
                            }
                            else
                            {
                                proj.moveProj();
                                world.Projectiles.Add(tank.GetID(), proj);
                            }
                        }
                    }
                }
            }
        }

        private bool collided(Object o)
        {
            bool isY = false;
            bool isX = false;
            if (o is Tank)
            {

            }
            else if (o is Projectile)
            {
                Projectile proj = o as Projectile;
                Vector2D location = proj.GetLocation();
                foreach (Wall wall in world.Walls.Values)
                {
                    wall.numofWalls(out bool isVertical, out bool p1IsGreater);
                    if (isVertical)
                    {
                        if (location.GetX() < wall.getP1().GetX() + 25 && location.GetX() > wall.getP1().GetX() - 25) // projectile is within Y of wall
                            isX = true;
                        if (p1IsGreater && (location.GetY() < wall.getP1().GetY() + 25 && location.GetY() > wall.getP2().GetY() - 25))
                            isY = true;
                        if (!p1IsGreater && (location.GetY() > wall.getP1().GetY() - 25 && location.GetY() < wall.getP2().GetY() + 25))
                            isY = true;
                    }
                    else //horizontal
                    {
                        if (location.GetY() < wall.getP1().GetY() + 25 && location.GetY() > wall.getP1().GetY() - 25) // projectile is within Y of wall
                            isY = true;
                        if (p1IsGreater && (location.GetX() < wall.getP1().GetX() + 25 && location.GetX() > wall.getP2().GetX() - 25))
                            isX = true;
                        if (!p1IsGreater && (location.GetX() > wall.getP1().GetX() - 25 && location.GetX() < wall.getP2().GetX() + 25))
                            isX = true;
                    }
                    if (isX && isY)
                        return true;

                    isX = false;
                    isY = false;
                }
                int tankID = proj.GetOwner();
                foreach (Tank tank in world.Tanks.Values)
                {
                    world.Tanks.TryGetValue(tank.GetID(), out Tank currentTank);
                    if (currentTank.GetID().Equals(tankID))
                        continue;
                    if (location.GetX() < tank.GetLocation().GetX() + 30 && location.GetX() > tank.GetLocation().GetX() - 30 && location.GetY()
                        < tank.GetLocation().GetY() + 30 && location.GetY() > tank.GetLocation().GetY() - 30)
                    {
                        proj.Deactivate();
                        world.DeadProj.Add(proj.getProjnum(), proj);
                        if(tank.decrementHP() == 0)
                        {
                            tank.Deactivate();
                        }
                        return true;
                    }

                }
            }
            return false;
        }

        private void Moving(JToken moving, Vector2D turretDirection, Tank tank)
        {
            lock (world)
            {
                tank.SetOtherTurretOrientation(turretDirection);
                if (moving.ToString().Equals("up"))
                {
                    tank.MoveTank(new Vector2D(0, -3));
                    tank.SetOrientation(new Vector2D(0, -1));
                }
                else if (moving.ToString().Equals("down"))
                {
                    tank.MoveTank(new Vector2D(0, 3));
                    tank.SetOrientation(new Vector2D(0, 1));
                }
                else if (moving.ToString().Equals("left"))
                {
                    tank.MoveTank(new Vector2D(-3, 0));
                    tank.SetOrientation(new Vector2D(-1, 0));
                }
                else if (moving.ToString().Equals("right"))
                {
                    tank.MoveTank(new Vector2D(3, 0));
                    tank.SetOrientation(new Vector2D(1, 0));
                }
                else if (moving.ToString().Equals("none"))
                    tank.MoveTank(new Vector2D(0, 0));
            }
        }

        public void setUpWorld(SocketState state)
        {
            lock (world)
            {
                Clients.Add(state, clientID);
                Clients.TryGetValue(state, out int ID);
                ClientName.TryGetValue(state, out string name);
                clientID++;
                Networking.Send(state.TheSocket, ID + "\n");
                Networking.Send(state.TheSocket, UniverseSize + "\n");

                foreach (Wall wall in world.Walls.Values)
                    Networking.Send(state.TheSocket, JsonConvert.SerializeObject(wall) + "\n");

                Tank tank = new Tank(ID, new Vector2D(-300, -300), new Vector2D(1, 0), new Vector2D(0, 0), name, 3, 0, false, false, true);
                world.Tanks.Add(tank.GetID(), tank);
                Networking.Send(state.TheSocket, JsonConvert.SerializeObject(tank) + "\n");
            }
        }

        public void sendMessage(SocketState state)
        {
            lock (world)
            {

                foreach (Tank tank in world.Tanks.Values)
                {
                    Networking.Send(state.TheSocket, JsonConvert.SerializeObject(tank) + "\n");
                }

                foreach (Projectile proj in world.Projectiles.Values)
                {
                    Networking.Send(state.TheSocket, JsonConvert.SerializeObject(proj) + "\n");

                    Console.WriteLine(JsonConvert.SerializeObject(proj));
                }

                foreach (Projectile proj in world.DeadProj.Values)
                {
                    Networking.Send(state.TheSocket, JsonConvert.SerializeObject(proj) + "\n");
                }


                foreach (Powerup power in world.Powerups.Values)
                    Networking.Send(state.TheSocket, JsonConvert.SerializeObject(power) + "\n");
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
                            lock (world)
                            {
                                world.Walls.Add(wall.getWallNum(), wall);
                            }
                            i++;
                            p1 = null;
                            p2 = null;
                            x1 = null;
                            x2 = null;
                            y1 = null;
                            y2 = null;
                        }
                        continue;
                    }
                }
            }
        }
    }
}
