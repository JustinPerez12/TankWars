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

namespace GameController {
    public class Controller {

        private World theWorld;
        private int worldSize;
        private int ID;

        public string moving;
        public string fire;
        public string x;
        public string y;

        public delegate void InputHandler(IEnumerable<object> text);
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
                // TODO: Left as an exercise, allow the user to try to reconnect
                error(state.ErrorMessage);
                return;
            }
            theServer = state;

            // Start an event loop to receive messages from the server
            state.OnNetworkAction = ReceiveMessage;

            Networking.GetData(state);
        }

        /// <summary>
        /// Method to be invoked by the networking library when 
        /// a network action occurs (see lines 70-71)
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

            // Continue the event loop
            // state.OnNetworkAction has not been changed, 
            // so this same method (ReceiveMessage) 
            // will be invoked when more data arrives
            Networking.GetData(state);
        }

        /// <summary>
        /// Process any buffered messages separated by '\n'
        /// Display them, then remove them from the buffer.
        /// </summary>
        /// <param name="state"></param>
        private void ProcessMessages(SocketState state)
        {
            List<object> items = new List<object>();

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

                try
                {

                    JObject obj = JObject.Parse(p);
                    JToken tankValue = obj["tank"];
                    JToken wallValue = obj["wall"];
                    JToken projValue = obj["proj"];
                    JToken powerupValue = obj["power"];
                    lock (theWorld)
                    {
                        if (tankValue != null)
                        {
                            Tank tank = null;
                            tank = JsonConvert.DeserializeObject<Tank>(p);
                            if (theWorld.Tanks.ContainsKey(tank.GetID()))
                            {
                                theWorld.Tanks.Remove(tank.GetID());
                                theWorld.Tanks.Add(tank.GetID(), tank);
                                items.Add(tank);
                                continue;
                            }
                            theWorld.Tanks.Add(tank.GetID(), tank);
                            items.Add(tank);
                        }
                        else if (wallValue != null)
                        {
                            Wall wall = null;
                            wall = JsonConvert.DeserializeObject<Wall>(p);
                            if (theWorld.Walls.ContainsKey(wall.getWallNum()))
                                continue;
                            theWorld.Walls.Add(wall.getWallNum(), wall);
                            items.Add(wall);
                        }
                        else if (projValue != null)
                        {
                            Projectile proj = null;
                            proj = JsonConvert.DeserializeObject<Projectile>(p);
                            if (theWorld.Projectiles.ContainsKey(proj.getProjnum()))
                                continue;
                            theWorld.Projectiles.Add(proj.getProjnum(), proj);
                            items.Add(proj);
                        }

                        else if (powerupValue != null)
                        {
                            Powerup power = null;
                            power = JsonConvert.DeserializeObject<Powerup>(p);
                            if (theWorld.Powerups.ContainsKey(power.getPowerNum()))
                                continue;
                            theWorld.Powerups.Add(power.getPowerNum(), power);
                            items.Add(power);
                        }
                    }
                }
                catch (Exception)
                {
                    ID = int.Parse(parts[0]);
                    worldSize = int.Parse(parts[1]);
                }
            }
            sendMessage();
            InputArrived(items);
        }

        public void sendMessage()
        {
            MessageEntered("{\"moving\":\""+ moving +"\",\"fire\":\""+ fire +"\",\"tdir\":{\"x\":"+ x +",\"y\":"+ y +"}}");
        }


        public void HandleMoveRequest(KeyEventArgs e)
        {
            theWorld.Tanks.TryGetValue(ID, out Tank t);
            if (e.KeyCode == Keys.W)
            {
                Debug.WriteLine("moving up");
                moving = "up";
            }
            if (e.KeyCode == Keys.A)
            {
                Debug.WriteLine("moving left");
                moving = "left";
            }
            if (e.KeyCode == Keys.S)
            {
                Debug.WriteLine("moving down");
                moving = "down";
            }
            if (e.KeyCode == Keys.D)
            {
                Debug.WriteLine("moving right");
                moving = "right";
            }
        }


        public int getID()
        {
            return ID;
        }

        public void MessageEntered(string message)
        {
            Networking.Send(theServer.TheSocket, message + "\n");
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
    }
}
