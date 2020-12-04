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
        public int PowerUpRespawn;
        public int TankVelocity;
        public int ProjectileVelocity;
        public int NumofPowerUps;
        public int PowerUpDelay;
        public bool Gamemode;
        public World world;
        public Dictionary<SocketState, int> Clients;
        public Dictionary<SocketState, string> ClientName;
        public int clientID;
        public int projnum;
        public int powerNum;
        public int beamNum;

        public servController()
        {
            Clients = new Dictionary<SocketState, int>();
            ClientName = new Dictionary<SocketState, string>();
            clientID = 0;
            projnum = 0;
            powerNum = 0;
            beamNum = 0;
            world = new World(0);
        }

        /// <summary>
        /// sets world size
        /// </summary>
        /// <param name="universeSize"></param>
        public void SetWorldSize(int universeSize)
        {
            world.SetWorldSize(universeSize);
        }

        /// <summary>
        /// when data arrives from clients update the world 
        /// </summary>
        /// <param name="state"></param>
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

        /// <summary>
        /// Process through the commands recieved and send them to the clients 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="state"></param>
        private void UpdateArrived(string p, SocketState state)
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
                if (deadFrame(tank))//if tank is dead do not move or allow to fire
                {
                    Moving(moving, tdir, tank);
                    Firing(fire, tdir, tank);
                }
                loadPowerups();
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

        /// <summary>
        /// sets up the world for a new client that has just connected 
        /// </summary>
        /// <param name="state"></param>
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

                while (world.Powerups.Count < NumofPowerUps)
                {
                    Powerup power = new Powerup(powerNum, new Vector2D(RandomCoordinate(), RandomCoordinate()));
                    world.Powerups.Add(powerNum, power);
                    while (collided(power, new Vector2D(0, 0)))
                        power.setLocation(new Vector2D(RandomCoordinate(), RandomCoordinate()));
                    powerNum++;
                }

                SpawnTank(ID, name, state);
            }
        }

        /// <summary>
        /// sends the new updated world to all clients 
        /// </summary>
        /// <param name="state"></param>
        public void sendMessage(SocketState state)
        {
            lock (world)
            {
                foreach (Tank tank in world.Tanks.Values)
                    Networking.Send(state.TheSocket, JsonConvert.SerializeObject(tank) + "\n");

                foreach (Projectile proj in world.Projectiles.Values)
                    Networking.Send(state.TheSocket, JsonConvert.SerializeObject(proj) + "\n");

                foreach (Powerup power in world.Powerups.Values)
                    Networking.Send(state.TheSocket, JsonConvert.SerializeObject(power) + "\n");
            }
        }

        /// <summary>
        /// private helper method to move tank
        /// </summary>
        /// <param name="moving"></param>
        /// <param name="turretDirection"></param>
        /// <param name="tank"></param>
        private void Moving(JToken moving, Vector2D turretDirection, Tank tank)
        {
            lock (world)
            {
                Vector2D direction = new Vector2D(0, 0);
                Vector2D orientation = tank.GetOrientation();
                tank.SetOtherTurretOrientation(turretDirection);
                if (moving.ToString().Equals("up"))
                {
                    direction = new Vector2D(0, -TankVelocity);
                    orientation = new Vector2D(0, -1);
                }
                else if (moving.ToString().Equals("down"))
                {
                    direction = new Vector2D(0, TankVelocity);
                    orientation = new Vector2D(0, 1);
                }
                else if (moving.ToString().Equals("left"))
                {
                    direction = new Vector2D(-TankVelocity, 0);
                    orientation = new Vector2D(-1, 0);
                }
                else if (moving.ToString().Equals("right"))
                {
                    direction = new Vector2D(TankVelocity, 0);
                    orientation = new Vector2D(1, 0);
                }
                else if (moving.ToString().Equals("none"))
                {
                    direction = new Vector2D(0, 0);
                }

                if (collided(tank, direction))
                    direction = new Vector2D(0, 0);

                tank.MoveTank(direction);
                tank.SetOrientation(orientation);
            }
        }

        /// <summary>
        /// Private helper method to determine if a tank can fire and in what direction to send the projectile or beam
        /// </summary>
        /// <param name="fire"></param>
        /// <param name="turretDirection"></param>
        /// <param name="tank"></param>
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
                            if (collided(proj, new Vector2D(0, 0)))
                                SendDeadProjeciles(proj);
                            else
                            {
                                proj.moveProj(ProjectileVelocity);
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
                            tank.resetFrames();
                    }

                }
                else if (fire.ToString().Equals("alt"))
                {
                    if (tank.hasPower())
                    {
                        SendBeam(tank, turretDirection);
                        tank.takePower();
                        Console.WriteLine("shot beam");
                        foreach (Tank t in world.Tanks.Values)
                        {
                            if (Intersects(tank.GetLocation(), turretDirection, t.GetLocation(), 30))
                            {
                                t.Deactivate();
                                tank.incrementScore();
                            }
                        }
                    }
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
                            if (collided(proj, new Vector2D(0, 0)))
                                SendDeadProjeciles(proj);
                            else
                            {
                                proj.moveProj(ProjectileVelocity);
                                world.Projectiles.Add(tank.GetID(), proj);
                            }
                        }
                    }
                }
            }//lock
        }

        private void SendBeam(Tank tank, Vector2D turretDirection)
        {
            Beam beam = new Beam(beamNum, tank.GetLocation(), turretDirection, tank.GetID());
            beamNum++;
            foreach (SocketState state in Clients.Keys)
            {
                Networking.Send(state.TheSocket, JsonConvert.SerializeObject(beam) + "\n");
                Console.WriteLine(JsonConvert.SerializeObject(beam));
            }
        }

        /// <summary>
        /// Private helper method to determine if an item collides with another item 
        /// </summary>
        /// <param name="o"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        private bool collided(Object o, Vector2D direction)
        {
            bool isY = false;
            bool isX = false;
            if (o is Tank)
            {
                Tank tank = o as Tank;
                Vector2D location = tank.GetLocation();
                location += direction;
                foreach (Wall wall in world.Walls.Values)
                {
                    WallCollision(wall, location, ref isX, ref isY);
                    if (isX && isY)
                        return true;
                }
                foreach (Powerup power in world.Powerups.Values)
                {
                    if (tank.hasPower())
                        return false;
                    if (power.getLocation().GetX() < location.GetX() + 30 && power.getLocation().GetX() > location.GetX() - 30 &&
                        power.getLocation().GetY() < location.GetY() + 30 && power.getLocation().GetY() > location.GetY() - 30)
                    {
                        tank.givePower();
                        power.killPower();
                        sendDeadPowerup(power);
                        power.resetPowerFrame();
                    }
                }
            }
            else if (o is Projectile)
            {
                Projectile proj = o as Projectile;
                Vector2D location = proj.GetLocation();
                foreach (Wall wall in world.Walls.Values)
                {
                    WallCollision(wall, location, ref isX, ref isY);
                    if (isX && isY)
                        return true;
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

                        if (tank.decrementHP() == 0)
                        {
                            killTank(tank);
                            world.Tanks.TryGetValue(tankID, out Tank shooter);
                            shooter.incrementScore();
                        }
                        return true;
                    }
                }
            }

            else if (o is Powerup)
            {
                Powerup power = o as Powerup;
                Vector2D location = power.getLocation();
                foreach (Wall wall in world.Walls.Values)
                {
                    WallCollision(wall, location, ref isX, ref isY);
                    if (isX && isY)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// helper method to determine if an item is going to collide with a wall
        /// </summary>
        /// <param name="wall"></param>
        /// <param name="location"></param>
        /// <param name="isX"></param>
        /// <param name="isY"></param>
        private void WallCollision(Wall wall, Vector2D location, ref bool isX, ref bool isY)
        {
            isX = false;
            isY = false;
            wall.numofWalls(out bool isVertical, out bool p1IsGreater);
            if (isVertical)
            {
                if (location.GetX() < wall.getP1().GetX() + 55 && location.GetX() > wall.getP1().GetX() - 55) // projectile is within Y of wall
                    isX = true;
                if (p1IsGreater && (location.GetY() < wall.getP1().GetY() + 55 && location.GetY() > wall.getP2().GetY() - 55))
                    isY = true;
                if (!p1IsGreater && (location.GetY() > wall.getP1().GetY() - 55 && location.GetY() < wall.getP2().GetY() + 55))
                    isY = true;
            }
            else //horizontal
            {
                if (location.GetY() < wall.getP1().GetY() + 55 && location.GetY() > wall.getP1().GetY() - 55) // projectile is within Y of wall
                    isY = true;
                if (p1IsGreater && (location.GetX() < wall.getP1().GetX() + 55 && location.GetX() > wall.getP2().GetX() - 55))
                    isX = true;
                if (!p1IsGreater && (location.GetX() > wall.getP1().GetX() - 55 && location.GetX() < wall.getP2().GetX() + 55))
                    isX = true;
            }
        }

        /// <summary>
        /// Send the dead powerup to all clients
        /// </summary>
        /// <param name="power"></param>
        private void sendDeadPowerup(Powerup power)
        {
            foreach (SocketState state in Clients.Keys)
            {
                Networking.Send(state.TheSocket, JsonConvert.SerializeObject(power) + "\n");
            }
        }

        /// <summary>
        /// Helper method to kill a tank
        /// </summary>
        /// <param name="tank"></param>
        public void killTank(Tank tank)
        {
            tank.Deactivate();
            int deadFrames = 0;
            while (deadFrames <= RespawnRate)
                deadFrames++;
        }

        /// <summary>
        /// helper method to load in powerups
        /// </summary>
        private void loadPowerups()
        {
            Random random = new Random();
            double i = random.Next(500, PowerUpDelay);
            foreach (Powerup p in world.Powerups.Values)
            {
                if (!p.isDead())
                    continue;
                if (p.incrementPowerFrame() < i)
                    continue;
                p.setLocation(new Vector2D(RandomCoordinate(), RandomCoordinate()));
                while (collided(p, new Vector2D(0, 0)))
                    p.setLocation(new Vector2D(RandomCoordinate(), RandomCoordinate()));
                p.revive();
                Console.WriteLine("revived");
                //p.resetPowerFrame();
            }
        }

        /// <summary>
        /// helper method to spawn the tank at a random location when they first connect
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="name"></param>
        /// <param name="state"></param>
        private void SpawnTank(int ID, string name, SocketState state)
        {
            Tank tank = new Tank(ID, new Vector2D(RandomCoordinate(), RandomCoordinate()), new Vector2D(1, 0), new Vector2D(0, 0), name, 3, 0, false, false, true);
            while (collided(tank, new Vector2D(0, 0)))
            {
                tank = new Tank(ID, new Vector2D(RandomCoordinate(), RandomCoordinate()), new Vector2D(1, 0), new Vector2D(0, 0), name, 3, 0, false, false, true);
            }
            world.Tanks.Add(tank.GetID(), tank);
            Networking.Send(state.TheSocket, JsonConvert.SerializeObject(tank) + "\n");
        }

        /// <summary>
        /// helper method to respawn the tank at a random location on the map
        /// </summary>
        /// <param name="tank"></param>
        private void RespawnTank(Tank tank)
        {
            tank.setLocation(new Vector2D(RandomCoordinate(), RandomCoordinate()));
            while (collided(tank, new Vector2D(0, 0)))
            {
                tank.setLocation(new Vector2D(RandomCoordinate(), RandomCoordinate()));
            }
            tank.Activate();
        }

        /// <summary>
        /// helper method to send the disconnected tank to all the clients 
        /// </summary>
        /// <param name="tank"></param>
        public void sendDisconnect(Tank tank)
        {
            foreach (SocketState state in Clients.Keys)
            {
                Networking.Send(state.TheSocket, JsonConvert.SerializeObject(tank) + "\n");
                Console.WriteLine("disconnected");
            }
        }

        /// <summary>
        /// private helper method to create two random coordinates for a Vector2D object 
        /// </summary>
        /// <returns></returns>
        private double RandomCoordinate()
        {
            Random random = new Random();
            double i = random.Next(-world.size / 2, world.size / 2);
            return i;
        }

        /// <summary>
        /// private helper method to send dead projectiles to all the clients 
        /// </summary>
        /// <param name="proj"></param>
        private void SendDeadProjeciles(Projectile proj)
        {
            proj.Deactivate();
            foreach (SocketState state in Clients.Keys)
            {
                Networking.Send(state.TheSocket, JsonConvert.SerializeObject(proj) + "\n");
            }
        }

        /// <summary>
        /// private helper method to determine whether or not a tank is allowed to respawn yet 
        /// </summary>
        /// <param name="tank"></param>
        /// <returns></returns>
        private bool deadFrame(Tank tank)
        {
            if (tank.getDeadFrames() > -1 && tank.getDeadFrames() < RespawnRate)
            {
                tank.addDeadFrame();
                return false;
            }
            if (tank.getDeadFrames() == RespawnRate)
            {
                RespawnTank(tank);
            }
            return true;
        }

        /// <summary>
        /// private helper method to parse the turrets direction
        /// </summary>
        /// <param name="tdir"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Determines if a ray interescts a circle
        /// </summary>
        /// <param name="rayOrig">The origin of the ray</param>
        /// <param name="rayDir">The direction of the ray</param>
        /// <param name="center">The center of the circle</param>
        /// <param name="r">The radius of the circle</param>
        /// <returns></returns>
        public static bool Intersects(Vector2D rayOrig, Vector2D rayDir, Vector2D center, double r)
        {
            // ray-circle intersection test
            // P: hit point
            // ray: P = O + tV
            // circle: (P-C)dot(P-C)-r^2 = 0
            // substituting to solve for t gives a quadratic equation:
            // a = VdotV
            // b = 2(O-C)dotV
            // c = (O-C)dot(O-C)-r^2
            // if the discriminant is negative, miss (no solution for P)
            // otherwise, if both roots are positive, hit

            double a = rayDir.Dot(rayDir);
            double b = ((rayOrig - center) * 2.0).Dot(rayDir);
            double c = (rayOrig - center).Dot(rayOrig - center) - r * r;

            // discriminant
            double disc = b * b - 4.0 * a * c;

            if (disc < 0.0)
                return false;

            // find the signs of the roots
            // technically we should also divide by 2a
            // but all we care about is the sign, not the magnitude
            double root1 = -b + Math.Sqrt(disc);
            double root2 = -b - Math.Sqrt(disc);

            return (root1 > 0.0 && root2 > 0.0);
        }

        /// <summary>
        /// Read the settings file and collect all the data necessary
        /// </summary>
        /// <param name="path"></param>
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

                            case "PowerUpRespawn":
                                reader.Read();
                                PowerUpRespawn = int.Parse(reader.Value);
                                break;

                            case "TankVelocity":
                                reader.Read();
                                TankVelocity = int.Parse(reader.Value);
                                break;

                            case "ProjectileVelocity":
                                reader.Read();
                                ProjectileVelocity = int.Parse(reader.Value);
                                break;

                            case "NumofPowerUps":
                                reader.Read();
                                NumofPowerUps = int.Parse(reader.Value);
                                break;

                            case "PowerUpDelay":
                                reader.Read();
                                PowerUpDelay = int.Parse(reader.Value);
                                break;

                            case "GameMode":
                                reader.Read();
                                Gamemode = bool.Parse(reader.Value);
                                if (Gamemode)
                                    cracked();
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

        private void cracked()
        {
            FramesPerShot = 3;
            PowerUpRespawn = 1000;
            NumofPowerUps = 10;
            TankVelocity = 6;
        }
    }
}
