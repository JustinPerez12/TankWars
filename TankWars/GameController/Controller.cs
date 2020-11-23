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
        private bool leftPressed;
        private bool rightPressed;
        private bool upPressed;
        private bool downPressed;

        //commands to send back to server
        public string moving;
        public string fire;
        public string turretX;
        public string turretY;

        public delegate void InputHandler();
        public event InputHandler InputArrived;

        public delegate void ErrorEvent(string message);
        public event ErrorEvent error;

        SocketState theServer = null;

        public Vector2D TurretOrientation;

        public Controller()
        {
            theWorld = new World(worldSize);
            moving = "none";
            fire = "none";
            turretX = "0";
            turretY = "0";
            ID = -1;
            worldSize = 0;
        }

        /// <summary>
        /// returns the world
        /// </summary>
        /// <returns></returns>
        public World getWorld()
        {
            return theWorld;
        }

        /// <summary>
        /// wrapper for the networking.connecttoserver
        /// </summary>
        /// <param name="address"></param>
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
            sendMessage();
            ProcessMessages(state);
            InputArrived();
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
            lock (theServer)
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
        }


        /// <summary>
        /// when a move button is clicked
        /// </summary>
        /// <param name="e"></param>
        public void HandleMoveRequest(KeyEventArgs e)
        {
            theWorld.Tanks.TryGetValue(ID, out Tank t);
            if (e.KeyCode == Keys.W)
            {
                upPressed = true;
                moving = "up";
            }
            else if (e.KeyCode == Keys.A)
            {
                leftPressed = true;
                moving = "left";
            }
            else if (e.KeyCode == Keys.S)
            {
                downPressed = true;
                moving = "down";
            }
            else if (e.KeyCode == Keys.D)
            {
                rightPressed = true;
                moving = "right";
            }
        }

        /// <summary>
        /// when a move button is released
        /// </summary>
        /// <param name="e"></param>
        public void HandleMoveCancel(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W)
            {
                upPressed = false;
                OtherButtonPressed(out string key);
                moving = key;
            }
            else if (e.KeyCode == Keys.A)
            {
                leftPressed = false;
                OtherButtonPressed(out string key);
                moving = key;
            }
            else if (e.KeyCode == Keys.S)
            {
                downPressed = false;
                OtherButtonPressed(out string key);
                moving = key;
            }
            else if (e.KeyCode == Keys.D)
            {
                rightPressed = false;
                OtherButtonPressed(out string key);
                moving = key;
            }
        }


        /// <summary>
        /// when a mouse button is clicked
        /// </summary>
        /// <param name="e"></param>
        public void HandleMouseRequest(MouseEventArgs e)
        {
            theWorld.Tanks.TryGetValue(ID, out Tank t);
            if (e.Button == MouseButtons.Left)
            {

                fire = "main";
            }
            else if (e.Button == MouseButtons.Right)
            {
                fire = "alt";
            }
            else
            {
                fire = "none";
            }
        }

        /// <summary>
        /// when a mouse button is released
        /// </summary>
        /// <param name="e"></param>
        public void HandleMouseCancel(MouseEventArgs e)
        {
            fire = "none";
        }

        /// <summary>
        /// when the mouse in on the panel and it moves
        /// </summary>
        /// <param name="e"></param>
        public void HandleMouseMove(MouseEventArgs e)
        {
            if (theWorld.Tanks.TryGetValue(ID, out Tank t))
            {
                t.SetTurretOrientation(e.X, e.Y);
                int x1 = e.X;
                int y1 = e.Y;
                x1 -= 400;
                y1 -= 400;

                //set this clients turret orientation
                TurretOrientation = new Vector2D(x1, y1);
                TurretOrientation.Normalize();
                turretX = TurretOrientation.GetX().ToString();
                turretY = TurretOrientation.GetY().ToString();
            }
        }

        /// <summary>
        /// wrapper class for networking.send
        /// </summary>
        /// <param name="message"></param>
        public void MessageEntered(string message)
        {
            Networking.Send(theServer.TheSocket, message + "\n");
        }

        /// <summary>
        /// wrapper for our wrapper that has the command ready to go
        /// </summary>
        public void sendMessage()
        {
            MessageEntered("{\"moving\":\"" + moving + "\",\"fire\":\"" + fire + "\",\"tdir\":{\"x\":" + turretX + ",\"y\":" + turretY + "}}");
        }

        /// <summary>
        /// helper method to get the ID of this client
        /// </summary>
        /// <returns></returns>
        public int getID()
        {
            return ID;
        }

        /// <summary>
        /// helper method to Handle the form closing by shutting down the socket cleanly
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

        /// <summary>
        /// private helper method to determine if a JSONstring is a tank, wall, projectile, or powerup
        /// </summary>
        /// <param name="JSONString"></param>
        private void UpdateArrived(string JSONString)
        {
            try
            {
                JObject obj = JObject.Parse(JSONString);
                JToken tankValue = obj["tank"];
                JToken wallValue = obj["wall"];
                JToken projValue = obj["proj"];
                JToken beamValue = obj["beam"];
                JToken powerupValue = obj["power"];
                lock (theWorld)
                {
                    if (tankValue != null)
                    {
                        Tank tank = JsonConvert.DeserializeObject<Tank>(JSONString);
                        AddTank(tank);
                    }
                    else if (wallValue != null)
                    {
                        Wall wall = JsonConvert.DeserializeObject<Wall>(JSONString);
                        AddWall(wall);
                    }
                    else if (projValue != null)
                    {
                        Projectile proj = JsonConvert.DeserializeObject<Projectile>(JSONString);
                        AddProj(proj);
                    }

                    else if (powerupValue != null)
                    {
                        Powerup power = JsonConvert.DeserializeObject<Powerup>(JSONString);
                        AddPower(power);
                    }

                    else if (beamValue != null)
                    {
                        Beam beam = JsonConvert.DeserializeObject<Beam>(JSONString);
                        AddBeam(beam);
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

        private void AddBeam(Beam beam)
        {
            theWorld.Beams.Add(beam.getID(), beam);
        }

        /// <summary>
        /// private helper method to add to the powerup dictionary in theWorld
        /// </summary>
        /// <param name="power"></param>
        private void AddPower(Powerup power)
        {
            if (power.isDead())
                theWorld.Powerups.Remove(power.getPowerNum());

            else if (theWorld.Powerups.ContainsKey(power.getPowerNum()))
                return;

            else
                theWorld.Powerups.Add(power.getPowerNum(), power);
        }

        /// <summary>
        /// private helper method to add to the projectile dictionary in theWorld
        /// </summary>
        /// <param name="proj"></param>
        private void AddProj(Projectile proj)
        {
            if (proj.isDead())
                theWorld.Projectiles.Remove(proj.getProjnum());

            else if (theWorld.Projectiles.ContainsKey(proj.getProjnum()))
            {
                theWorld.Projectiles.Remove(proj.getProjnum());
                theWorld.Projectiles.Add(proj.getProjnum(), proj);
                return;
            }

            else
                theWorld.Projectiles.Add(proj.getProjnum(), proj);

        }

        /// <summary>
        /// private helper method to add to the wall dictionary in theWorld
        /// </summary>
        /// <param name="wall"></param>
        private void AddWall(Wall wall)
        {
            if (theWorld.Walls.ContainsKey(wall.getWallNum()))
                return;
            theWorld.Walls.Add(wall.getWallNum(), wall);
        }

        /// <summary>
        /// private helper method to add to the tank dictionary in theWorld
        /// </summary>
        /// <param name="tank"></param>
        public void AddTank(Tank tank)
        {
            if (tank.Disconnected())
            {
                theWorld.Tanks.Remove(tank.GetID());
                //theWorld.playerColors.Remove(tank.GetID());
            }
            if (tank.getHP() == 0)
                theWorld.Tanks.Remove(tank.GetID());


            else if (theWorld.Tanks.ContainsKey(tank.GetID()) && tank.getHP() > 0)
            {
                theWorld.playerColors.TryGetValue(tank.GetID(), out string color);
                theWorld.Tanks.Remove(tank.GetID());
                tank.setColor(color);
                theWorld.Tanks.Add(tank.GetID(), tank);
                return;
            }

            else
            {
                tank.randomColor();
                theWorld.Tanks.Add(tank.GetID(), tank);
                theWorld.playerColors.Add(tank.GetID(), tank.Color());
            }
        }

        /// <summary>
        /// Private helper method to determine if another key is being pressed. Creates smooth movement
        /// </summary>
        /// <param name="key"></param>
        private void OtherButtonPressed(out string key)
        {
            if (leftPressed)
                key = "left";

            else if (rightPressed)
                key = "right";

            else if (upPressed)
                key = "up";

            else if (downPressed)
                key = "down";

            else
                key = "none";
        }

    }
}
