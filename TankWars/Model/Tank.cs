using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TankWars;

namespace Model {
    public class Tank {

        [JsonProperty(PropertyName = "tank")]
        private int ID;

        [JsonProperty(PropertyName = "loc")]
        private Vector2D location;

        [JsonProperty(PropertyName = "bdir")]
        private Vector2D orientation;

        [JsonProperty(PropertyName = "tdir")]
        private Vector2D aiming = new Vector2D(0, -1);

        [JsonProperty(PropertyName = "name")]
        private string name;

        [JsonProperty(PropertyName = "hp")]
        private int hitPoints = 3;

        [JsonProperty(PropertyName = "score")]
        private int score = 0;

        [JsonProperty(PropertyName = "died")]
        private bool died = false;

        [JsonProperty(PropertyName = "dc")]
        private bool disconnected = false;

        [JsonProperty(PropertyName = "join")]
        private bool joined = false;


        public Tank()
        {
           // Tank rebuilt = JsonConvert.DeserializeObject<Tank>("tank");
        }
        public Tank(int _ID, Vector2D l, Vector2D o)
        {
            ID = _ID;
            location = l;
            orientation = o;
            died = true;
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
    }
}
