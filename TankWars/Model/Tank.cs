using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TankWars;

namespace Model {
    public class Tank {

        [JsonProperty(PropertyName = "tank")]
        private int ID;
        private int team;
        private Vector2D location;
        private Vector2D orientation;
        private bool active;

        public Tank()
        {
            Tank rebuilt = JsonConvert.DeserializeObject<Tank>("tank");
        }
        public Tank(int _ID, int _team, Vector2D l, Vector2D o)
        {
            ID = _ID;
            team = _team;
            location = l;
            orientation = o;
            active = true;
        }

        public void Deactivate()
        {
            active = false;
        }
        public bool GetActive()
        {
            return active;
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
        public int GetTeam()
        {
            return team;
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
