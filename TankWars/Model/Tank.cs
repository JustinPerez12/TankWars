using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
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

        public Tank()
        {
            color = RandomColor();
        }

        private string RandomColor()
        {
            List<string> colors = new List<string>() { "blue", "brown", "green", "grey", "red", "violet", "white", "yellow" };
            Random random = new Random();
            int i = random.Next(0,8);

            return colors[i];
        }

        public string Color()
        {
            return color;
        }

        public void Deactivate()
        {
            died = true;
        }
        public bool IsDead()
        {
            return died;
        }
        public int GetID()
        {
            return ID;
        }

        public bool Disconnected()
        {
            return disconnected;
        }

        public bool Joined()
        {
            return joined;
        }
        public Vector2D GetLocation()
        {
            return location;
        }

        public Vector2D GetOrientation()
        {
            return orientation;
        }

        public void SetLocation(Vector2D l)
        {
            location = l;
        }
        public void SetOrientation(Vector2D o)
        {
            orientation = o;
        }

        public bool hasPowerup()
        {
            //need to implement. this is how we will tell if a tank has picked up a powerup. dont know how to do this yet tho
            return true;
        }

        public Vector2D TurretOrientation()
        {
            return aiming;
        }

        public void SetTurretOrientation(double x, double y)
        {
            aiming = new Vector2D(x, y);
        }
    }
}
