using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TankWars;

namespace Model {
    public class Projectile {

        [JsonProperty(PropertyName = "proj")]
        private int projNum;

        [JsonProperty(PropertyName = "loc")]
        private Vector2D location;

        [JsonProperty(PropertyName = "dir")]
        private Vector2D direction;

        [JsonProperty(PropertyName = "died")]
        private bool died;

        [JsonProperty(PropertyName = "owner")]
        private int ownerNum;
        public Projectile()
        {

        }

        public int GetProjNum()
        {
            return projNum;
        }

        public void Deactivate()
        {
            died = false;
        }
        public bool GetActive()
        {
            return died;
        }
        public Vector2D GetLocation()
        {
            return location;
        }
        public Vector2D GetDirection()
        {
            return direction;
        }
        public void SetLocation(Vector2D l)
        {
            location = l;
        }
    }
}
