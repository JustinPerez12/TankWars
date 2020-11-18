using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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


        public Tank()
        {
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

        //Might delete later
        public double GetLocationX()
        {
            return location.GetX();
        }

        //Might delete later
        public double GetLocationY()
        {
            return location.GetY();
        }
        public Vector2D GetOrientation()
        {
            return orientation;
        }

        //Might delete later
        public double GetOrientationAngle()
        {
            return orientation.ToAngle();
        }
        public void SetLocation(Vector2D l)
        {
            location = l;
        }
        public void SetOrientation(Vector2D o)
        {
            orientation = o;
        }
    }
}
