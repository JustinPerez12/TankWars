using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TankWars;

namespace Model
{

    [JsonObject(MemberSerialization.OptIn)]
    public class Tank
    {

        [JsonProperty(PropertyName = "tank")]
        private int ID;

        [JsonProperty(PropertyName = "loc")]
        private Vector2D location;

        [JsonProperty(PropertyName = "bdir")]
        private Vector2D orientation;

        [JsonProperty(PropertyName = "tdir")]
        private Vector2D aiming;

        [JsonProperty(PropertyName = "name")]
        private string name;

        [JsonProperty(PropertyName = "hp")]
        private int hitPoints;

        [JsonProperty(PropertyName = "score")]
        private int score;

        [JsonProperty(PropertyName = "died")]
        private bool died;

        [JsonProperty(PropertyName = "dc")]
        private bool disconnected;

        [JsonProperty(PropertyName = "join")]
        private bool joined;

        private string color;

        private int shotFrames;
        private int deadFrames;
        private bool power;

        public Tank()
        {
        }

        public Tank(int ID1, Vector2D Location, Vector2D Orientation, Vector2D Aiming, string Name, int HP, int Score, bool Died, bool Disconnected, bool Joined)
        {
            ID = ID1;
            location = Location;
            orientation = Orientation;
            aiming = Aiming;
            name = Name;
            hitPoints = HP;
            score = Score;
            died = Died;
            disconnected = Disconnected;
            joined = Joined;
            shotFrames = 0;
            deadFrames = -1;
            power = false;
        }
        public void randomColor()
        {
            List<string> colors = new List<string>() { "Blue", "Green", "Red", "Purple", "Dark", "Yellow", "Orange" };
            Random random = new Random();
            int i = random.Next(0, 6);
            color = colors[i];
        }

        public void setColor(string newColor)
        {
            color = newColor;
        }

        public string Color()
        {
            return color;
        }

        public void Deactivate()
        {
            died = true;
            hitPoints = 0;
            deadFrames = 0;
        }
        public bool IsDead()
        {
            return died;
        }

        public void givePower()
        {
            power = true;
        } 

        public bool hasPower()
        {
            return power;
        }

        public void Activate()
        {
            died = false;
            deadFrames = -1;
            hitPoints = 3;
        }
        public int GetID()
        {
            return ID;
        }

        public bool Disconnected()
        {
            return disconnected;
        }

        public void SetDisconnect()
        {
            disconnected = true;
        }

        public bool Joined()
        {
            return joined;
        }

        public Vector2D GetLocation()
        {
            return location;
        }

        public int getDeadFrames()
        {
            return deadFrames;
        }

        public void addDeadFrame()
        {
            deadFrames++;
        }

        public Vector2D GetOrientation()
        {
            return orientation;
        }

        public void MoveTank(Vector2D direction)
        {
            location += direction;
        }

        public void SetOrientation(Vector2D o)
        {
            orientation = o;
        }

        public Vector2D TurretOrientation()
        {
            return aiming;
        }

        public int getHP()
        {
            return hitPoints; 
        }

        public string getName()
        {
            return name;
        }

        public int getScore()
        {
            return score;
        }

        /// <summary>
        /// sets turret orientation with the given position of the Mouse on the screen
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SetTurretOrientation(double x, double y)
        {
            x -= 400;
            y -= 400;
            aiming = new Vector2D(x, y);
            aiming.Normalize();
        }

        public void SetOtherTurretOrientation(Vector2D orientation)
        {
            aiming = orientation;
        }

        public void resetFrames()
        {
            shotFrames = 0;
        }

        public int getFrames()
        {
            return shotFrames;
        }

        public void addFrame()
        {
            shotFrames++;
        }

        public int decrementHP()
        {
            hitPoints--;
            return hitPoints;
        }

        public void setLocation(Vector2D newLocation)
        {
            location = newLocation;
        }

        public void takePower()
        {
            power = false;
        }

        public void incrementScore()
        {
            score++;
        }
    }
}

