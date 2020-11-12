﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Model {
    public class World {
        public Dictionary<int, Tank> Tanks;
        public Dictionary<int, Projectile> Projectiles;
        public Dictionary<int, Wall> Walls;
        public int size
        { get; private set; }

        public World(int _size)
        {
            Tanks = new Dictionary<int, Tank>();
            Projectiles = new Dictionary<int, Projectile>();
            Walls = new Dictionary<int, Wall>();
            size = _size;
        }
    }
}
