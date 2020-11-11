using GameController;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace View {
    public partial class Form1 : Form {

        Controller controller;

        public Form1()
        {
            InitializeComponent();
            controller = new Controller();
            controller.InputArrived += DisplayInput;
            controller.error += ErrorEvent;
            messageToSendBox.KeyDown += new KeyEventHandler(MessageEnterHandler);

            FormClosed += OnExit;
        }

        private void ErrorEvent(string message)
        {
            MessageBox.Show("Error connecting to server. Please restart the client.");
        }

        private void DisplayInput(IEnumerable<string> newInput)
        {
            foreach(string p in newInput)
            {
                this.Invoke(new MethodInvoker(() => messages.AppendText(p + Environment.NewLine)));
            }
        }

        /// <summary>
        /// Handle the form closing by shutting down the socket cleanly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnExit(object sender, FormClosedEventArgs e)
        {
            /*if (theServer != null)
                theServer.TheSocket.Shutdown(SocketShutdown.Both);*/
        }

        /// <summary>
        /// Connect button event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void connectButton_Click_1(object sender, EventArgs e)
        {
            if (serverAddress.Text == "")
            {
                MessageBox.Show("Please enter a server address");
                return;
            }

            // Disable the controls and try to connect
            connectButton.Enabled = false;
            serverAddress.Enabled = false;

            controller.Connect(serverAddress.Text);
            //Networking.ConnectToServer(OnConnect, serverAddress.Text, 11000);
        }


        /// <summary>
        /// This is the event handler when the enter key is pressed in the messageToSend box
        /// </summary>
        /// <param name="sender">The Form control that fired the event</param>
        /// <param name="e">The key event arguments</param>
        private void MessageEnterHandler(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // prevent the windows "ding" sound
                e.Handled = true;
                e.SuppressKeyPress = true;

                // Append a newline, since that is our protocol's terminating character for a message.
               string message = /*messageToSendBox.Text +*/ "";

                // Reset the textbox
                //essageToSendBox.Text = "";

                // Send the message to the server
                //Networking.Send(theServer.TheSocket, message);
                controller.MessageEntered(message);
            }
        }
    }
}
