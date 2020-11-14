using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TankWars;

namespace Model {


    [JsonObject(MemberSerialization.OptIn)]
    public class Powerup {

        [JsonProperty(PropertyName = "power")]
        public int wallNum;

        [JsonProperty(PropertyName = "loc")]
        public Vector2D location;

        [JsonProperty(PropertyName = "died")]
        public bool died;


        public Powerup()
        {

        }

    }
}

