using GameController;
using Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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
    public partial class Form1 : Form 
    {

        Controller controller;
        DrawingPanel dPanel;
        World theWorld;

        private const int viewSize = 800;
        private const int menuSize = 40;


        public Form1()
        {
            InitializeComponent();

            controller = new Controller();
            controller.InputArrived += DisplayInput;
            controller.error += ErrorEvent;

            controller.name += setName;

            theWorld = controller.getWorld();
            panel = new DrawingPanel(theWorld, controller);
            //panel.Location = new Point(0, menuSize);
            panel.Location = new Point(0, menuSize);
            panel.Size = new Size(viewSize, viewSize);
            this.Controls.Add(panel);

            //need to take this out later
            serverAddress.Text = "localhost";

            FormClosed += OnExit;
        }

        private void ErrorEvent(string message)
        {
            MessageBox.Show("Error connecting to server. Please restart the client.");
        }

        private void DisplayInput(IEnumerable<object> newInput)
        {
            lock (theWorld)
            {
                foreach (object p in newInput)
                {
                    MethodInvoker mi = new MethodInvoker(() => this.Invalidate(true));
                    Invoke(mi);
                }
            }
        }

        private void setName()
        {
            string name = nameBox.Text;
            nameBox.Enabled = false;
            this.Invoke(new MethodInvoker(() => controller.MessageEntered(name)));

        }

        /// <summary>
        /// Handle the form closing by shutting down the socket cleanly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnExit(object sender, FormClosedEventArgs e)
        {
            controller.Exit();
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
            if (nameBox.Text == "")
            {
                MessageBox.Show("Please enter a name");
                return;
            }

            // Disable the controls and try to connect
            connectButton.Enabled = false;
            serverAddress.Enabled = false;

            controller.Connect(serverAddress.Text);

            string name = nameBox.Text;
            nameBox.Enabled = false;
            controller.MessageEntered(name);
            //Networking.ConnectToServer(OnConnect, serverAddress.Text, 11000);
        }
    }
}
