using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TankWars;

namespace Model {
    
    [JsonObject(MemberSerialization.OptIn)]
    public class Wall {

        [JsonProperty(PropertyName = "wall")]
        private int wallNum;

        [JsonProperty(PropertyName = "p1")]
        private Vector2D p1;

        [JsonProperty(PropertyName = "p2")]
        private Vector2D p2;

        public Wall()
        {

        }

        public int GetWallNum()
        {
            return wallNum;
        }

        public Vector2D GetP1()
        {
            return p1;
        }

        public Vector2D GetP2()
        {
            return p2;
        }

    }
}
