using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TankWars;

namespace Model {
    [JsonObject(MemberSerialization.OptIn)]
    public class Powerup {

        [JsonProperty(PropertyName = "power")]
        private int powerupNum;

        [JsonProperty(PropertyName = "loc")]
        private Vector2D location;

        [JsonProperty(PropertyName = "died")]
        private bool died;


        public Powerup()
        {

        }

        public int getPowerNum()
        {
            return powerupNum;
        }

        public Vector2D getLocation()
        {
            return location;
        }

        public double GetLocationX()
        {
            return location.GetX();
        }

        public double GetLocationY()
        {
            return location.GetY();
        }

    }
}

