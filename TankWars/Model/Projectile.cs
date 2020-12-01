using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TankWars;

namespace Model {

    [JsonObject(MemberSerialization.OptIn)]
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
        private int tankID;

        private Vector2D velocity;

        private int frames;

        public Projectile()
        {
        }

        public Projectile(int Projnum, Vector2D Location, Vector2D Direction, bool Died, int TankID)
        {
            frames = 0;
            projNum = Projnum;
            location = Location;
            direction = Direction;
            died = Died;
            tankID = TankID; 
        }

        public void moveProj()
        {
            velocity = direction * 25;
            location += velocity;
        }

        public int getFrames()
        {
            return frames++;
        }
        public int getProjnum()
        {
            return projNum;
        }

        public void Deactivate()
        {
            died = true;
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

        public bool isDead()
        {
            return died;
        }

        public double GetDirectionAngle()
        {
            return direction.ToAngle();
        }
        public void SetLocation(Vector2D l)
        {
            location = l;
        }

        public int GetOwner()
        {
            return tankID;
        }

    }
}
