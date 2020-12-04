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

        private int frame;

        public Powerup()
        {
        }

        public Powerup(int newNum, Vector2D newLocation)
        {
            powerupNum = newNum;
            location = newLocation;
            died = false;
        }

        public int getPowerNum()
        {
            return powerupNum;
        }
        public void killPower()
        {
            died = true;
        }

        public Vector2D getLocation()
        {
            return location;
        }

        public bool isDead()
        {
            return died;
        }

        public void resetPowerFrame()
        {
            frame = 0;
        }

        public int incrementPowerFrame()
        {
            return frame++;
        }

        public void revive()
        {
            died = false;
        }

        public void setLocation(Vector2D newLoc)
        {
            location = newLoc;
        }

    }
}

