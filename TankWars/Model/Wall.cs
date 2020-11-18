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

        public int getWallNum()
        {
            return wallNum;
        }

        public int numofWalls(out bool isVertical, out bool p1Greater)
        {
            if (p1.GetX() == p2.GetX())
            {
                if (p1.GetY() > p2.GetY())
                    p1Greater = true;
                else
                    p1Greater = false;

                double distance = Math.Abs(p1.GetY() - p2.GetY());
                int numofWall = (int)distance / 50;
                isVertical = true;
                return numofWall;

            }

            else if (p1.GetY() == p2.GetY())
            {
                if (p1.GetX() > p2.GetX())
                    p1Greater = true;
                else
                    p1Greater = false;
                double distance = Math.Abs(p1.GetX() - p2.GetX());
                int numofWall = (int)distance / 50;
                isVertical = false;
                return numofWall;
            }

            p1Greater = false;
            isVertical = false;
            return -1;

        }

        public Vector2D getP1()
        {
            return p1;
        }

        public Vector2D getP2()
        {
            return p2;
        }

    }
}
