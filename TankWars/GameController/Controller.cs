using NetworkUtil;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Model;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Windows.Forms;

namespace GameController {
    public class Controller {

        private World theWorld;
        private int worldSize = 2000;

        public delegate void InputHandler(IEnumerable<object> text);
        public event InputHandler InputArrived;

        public delegate void ErrorEvent(string message);
        public event ErrorEvent error;

        public delegate void SetName();
        public event SetName name;

        SocketState theServer = null;

        public Controller()
        {
            theWorld = new World(worldSize);
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
            //name();

            // Start an event loop to receive messages from the server
            state.OnNetworkAction = ReceiveMessage;

            Networking.GetData(state);
        }

        /// <summary>
        /// Method to be invoked by th e networking library when 
        /// a network action occurs (see lines 70-71)
        /// </summary>
        /// <param name="state"></param>
        private void ReceiveMessage(SocketState state)
        {
            if (state.ErrorOccured)
            {
                // TODO: Left as an exercise, allow the user to try to reconnect
                //MessageBox.Show("Error while receiving. Please restart the client.");
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

                    if (tankValue != null)
                    {
                        Tank tank = null;
                        tank = JsonConvert.DeserializeObject<Tank>(p);
                        theWorld.Tanks.Add(tank.GetID(), tank);
                        items.Add(tank);
                    }
                    else if (wallValue != null)
                    {
                        Wall wall = null;
                        wall = JsonConvert.DeserializeObject<Wall>(p);
                        theWorld.Walls.Add(wall.getWallNum(), wall);
                        items.Add(wall);
                    }
                    else if (projValue != null)
                    {
                        Projectile proj = null;
                        proj = JsonConvert.DeserializeObject<Projectile>(p);
                        theWorld.Projectiles.Add(proj.getProjnum(), proj);
                        items.Add(proj);
                    }

                    else if(powerupValue != null)
                    {
                        Powerup power = null;
                        power = JsonConvert.DeserializeObject<Powerup>(p);
                        theWorld.Powerups.Add(power.getPowerNum(), power);
                        items.Add(power);
                    }
                } 
                catch (Exception)
                {

                }
            }
            InputArrived(items);
        }

        public void MessageEntered(string message)
        {
            Networking.Send(theServer.TheSocket, message + "/n");
        }
    }
}
