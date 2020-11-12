using System;
using System.Collections.Generic;
using System.Text;
using TankWars;

namespace Model {
    public class Projectile {
        private Vector2D location;
        private Vector2D orientation;
        private bool active;

        public Projectile()
        {

        }
        public Projectile(Vector2D l, Vector2D o)
        {
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
