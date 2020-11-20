﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TankWars;

namespace Model
{

    public class Beam
    {
        [JsonProperty(PropertyName = "beam")]
        private int beamID;

        [JsonProperty(PropertyName = "org")]
        private Vector2D origin;


        [JsonProperty(PropertyName = "dir")]
        private Vector2D direction;

        [JsonProperty(PropertyName = "owner")]
        private int owner;

        private bool isDead;

        public Beam()
        {
            isDead = false;
        }

        public Vector2D getOrigin()
        {
            return origin;
        }

        public int getID()
        {
            return beamID;
        }

        public Vector2D getDirection()
        {
            return direction;
        }

        public int getOwner()
        {
            return owner;
        }

        public void killBeam()
        {
            isDead = true;
        }

        public bool isAlive()
        {
            return isDead;
        }
    }
}
