using NetworkUtil;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Model;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Diagnostics;
using TankWars;

namespace GameController
{
    public class Controller
    {

        private World theWorld;
        private int worldSize;
        private int ID;

        public string moving;
        public string fire;
        public string x;
        public string y;

        public delegate void InputHandler();
        public event InputHandler InputArrived;

        public delegate void ErrorEvent(string message);
        public event ErrorEvent error;

        SocketState theServer = null;

        public Controller()
        {
            theWorld = new World(worldSize);
            moving = "none";
            fire = "none";
            x = "0";
            y = "0";
            ID = -1;
            worldSize = 0;
        }

        public World getWorld()
        {
            return theWorld;
        }

        public void Connect(string address)
        {
            Networking.ConnectToServer(OnConnect, address, 11000);
        }


        /// <summary>
        /// Method to be invoked by the networking library when a connection is made
        /// (see line 49)
        /// </summary>
        /// <param name="state"></param>
        private void OnConnect(SocketState state)
        {
            if (state.ErrorOccured)
            {
                error(state.ErrorMessage);
                return;
            }
            theServer = state;

            // Start an event loop to receive messages from the server
            state.OnNetworkAction = ReceiveMessage;

            InputArrived();
            Networking.GetData(state);
        }

        /// <summary>
        /// Method to be invoked by the networking library when 
        /// a network action occurs 
        /// </summary>
        /// <param name="state"></param>
        private void ReceiveMessage(SocketState state)
        {
            if (state.ErrorOccured)
            {
                error(state.ErrorMessage);
                return;
            }
            ProcessMessages(state);
            sendMessage();
            InputArrived();
            // Continue the event loop
            // state.OnNetworkAction has not been changed, 
            // so this same method (ReceiveMessage) 
            // will be invoked when more data arrives
            Networking.GetData(state);

            //not sure if this is where this goes but it was working on the provided TankWars Client
            sendMessage();

        }

        /// <summary>
        /// Process any buffered messages separated by '\n'
        /// Display them, then remove them from the buffer.
        /// </summary>
        /// <param name="state"></param>
        private void ProcessMessages(SocketState state)
        {
            string totalData = state.GetData();
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            // Loop until we have processed all messages.
            // We may have received more than one.
            foreach (string p in parts)
            {

                // Ignore empty strings added by the regex splitter
                if (p.Length == 0)
                    continue;
                // The regex splitter will include the last string even if it doesn't end with a '\n',
                // So we need to ignore it if this happens. 
                if (p[p.Length - 1] != '\n')
                    break;

                // Display the message
                // "messages" is the big message text box in the form.
                // We must use a MethodInvoker, because only the thread 
                // that created the GUI can modify it.

                // Then remove it from the SocketState's growable buffer
                state.RemoveData(0, p.Length);

                //checks JSONstring and determines if its a tank, wall, proj, or powerup
                UpdateArrived(p);
            }
            //display the new inputs
            InputArrived();
        }

        private void UpdateArrived(string JSONString)
        {
            try
            {
                JObject obj = JObject.Parse(JSONString);
                JToken tankValue = obj["tank"];
                JToken wallValue = obj["wall"];
                JToken projValue = obj["proj"];
                JToken powerupValue = obj["power"];
                lock (theWorld)
                {
                    if (tankValue != null)
                    {
                        Tank tank = null;
                        tank = JsonConvert.DeserializeObject<Tank>(JSONString);
                        AddTank(tank);
                    }
                    else if (wallValue != null)
                    {
                        Wall wall = null;
                        wall = JsonConvert.DeserializeObject<Wall>(JSONString);
                        AddWall(wall);
                    }
                    else if (projValue != null)
                    {
                        Projectile proj = null;
                        proj = JsonConvert.DeserializeObject<Projectile>(JSONString);
                        AddProj(proj);
                    }

                    else if (powerupValue != null)
                    {
                        Powerup power = null;
                        power = JsonConvert.DeserializeObject<Powerup>(JSONString);
                        AddPower(power);
                    }
                }
            }
            catch (Exception)
            {
                //first time around. Dont want to set ID and worldSize again if another exception is caught 
                if (ID == -1)
                {
                    ID = int.Parse(JSONString);
                    worldSize = -1;
                }

                else if (worldSize == -1)
                {
                    worldSize = int.Parse(JSONString);
                    theWorld.SetWorldSize(worldSize);
                }
            }
        }

        private void AddPower(Powerup power)
        {
            if (theWorld.Powerups.ContainsKey(power.getPowerNum()))
                return;
            theWorld.Powerups.Add(power.getPowerNum(), power);
        }

        private void AddProj(Projectile proj)
        {
            if (theWorld.Projectiles.ContainsKey(proj.getProjnum()))
                return;
            theWorld.Projectiles.Add(proj.getProjnum(), proj);
        }

        private void AddWall(Wall wall)
        {
            if (theWorld.Walls.ContainsKey(wall.getWallNum()))
                return;
            theWorld.Walls.Add(wall.getWallNum(), wall);
        }

        private void AddTank(Tank tank)
        {
            if (theWorld.Tanks.ContainsKey(tank.GetID()))
            {
                theWorld.Tanks.Remove(tank.GetID());
                theWorld.Tanks.Add(tank.GetID(), tank);
                return;
            }
            theWorld.Tanks.Add(tank.GetID(), tank);
        }



        /// <summary>
        /// Private helper method to Handle the form closing by shutting down the socket cleanly
        /// </summary>
        /// <returns></returns>
        public bool Exit()
        {
            if (theServer != null)
            {
                theServer.TheSocket.Shutdown(SocketShutdown.Both);
                return true;
            }

            return false;
        }

        public void HandleMoveRequest(KeyEventArgs e)
        {
            theWorld.Tanks.TryGetValue(ID, out Tank t);
            if (e.KeyCode == Keys.W)
            {
                //Debug.WriteLine("moving up");
                moving = "up";
            }
            else if (e.KeyCode == Keys.A)
            {
                //Debug.WriteLine("moving left");
                moving = "left";
            }
            else if (e.KeyCode == Keys.S)
            {
                //Debug.WriteLine("moving down");
                moving = "down";
            }
            else if (e.KeyCode == Keys.D)
            {
                // Debug.WriteLine("moving right");
                moving = "right";
            }
        }

        public void HandleMoveCancel(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W)
            {
                //Debug.WriteLine("moving up");
                moving = "none";
            }
            else if (e.KeyCode == Keys.A)
            {
                //Debug.WriteLine("moving left");
                moving = "none";
            }
            else if (e.KeyCode == Keys.S)
            {
                //Debug.WriteLine("moving down");
                moving = "none";
            }
            else if (e.KeyCode == Keys.D)
            {
                //Debug.WriteLine("moving right");
                moving = "none";
            }
        }

        public void HandleMouseRequest(MouseEventArgs e)
        {
            theWorld.Tanks.TryGetValue(ID, out Tank t);
            if (e.Button == MouseButtons.Left)
            {
                fire = "main";
            }
            else if (e.Button == MouseButtons.Right && t.hasPowerup())
            {
                fire = "alt";
            }
            else
            {
                fire = "none";
            }
        }

        public void HandleMouseCancel(MouseEventArgs e)
        {
            fire = "none";
        }


        public void HandleMouseMove(MouseEventArgs e)
        {
            if (theWorld.Tanks.TryGetValue(ID, out Tank t))
            {
                t.SetTurretOrientation(e.X, e.Y);
                int x1 = e.X;
                int y1 = e.Y;
                x1 -= 400;
                y1 -= 400;

                Vector2D vector = new Vector2D(x1, y1);
                vector.Normalize();
                x = vector.GetX().ToString();
                y = vector.GetY().ToString();
            }
        }


        public void MessageEntered(string message)
        {
            Networking.Send(theServer.TheSocket, message + "\n");
            // Debug.WriteLine(message);
        }

        public int getID()
        {
            return ID;
        }

        public void sendMessage()
        {
            MessageEntered("{\"moving\":\"" + moving + "\",\"fire\":\"" + fire + "\",\"tdir\":{\"x\":" + x + ",\"y\":" + y + "}}");
        }
    }
}
