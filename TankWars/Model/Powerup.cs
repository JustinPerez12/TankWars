using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TankWars;

namespace Model {


    [JsonObject(MemberSerialization.OptIn)]
    public class Powerup {

        [JsonProperty(PropertyName = "power")]
        private int powerNum;

        [JsonProperty(PropertyName = "loc")]
        private Vector2D location;

        [JsonProperty(PropertyName = "died")]
        private bool died;


        public Powerup()
        {

        }

        public int GetPowerNum()
        {
            return powerNum;
        }

        public Vector2D GetLocation()
        {
            return location;
        }

        public bool Died()
        {
            return died;
        }

    }
}

