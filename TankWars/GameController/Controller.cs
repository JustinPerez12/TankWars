using NetworkUtil;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GameController {
    public class Controller {

        public delegate void InputHandler(IEnumerable<string> text);
        public event InputHandler InputArrived;

        public delegate void ErrorEvent(string message);
        public event ErrorEvent error;

        SocketState theServer = null;

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
            List<string> input = new List<string>();

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
                input.Add(p);
            }
            InputArrived(input);
        }

        public void MessageEntered(string message)
        {
            Networking.Send(theServer.TheSocket, message + "/n");
        }
    }
}
