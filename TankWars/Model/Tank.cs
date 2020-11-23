﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TankWars;

namespace Model
{

    [JsonObject(MemberSerialization.OptIn)]
    public class Tank
    {

        [JsonProperty(PropertyName = "tank")]
        private int ID;

        [JsonProperty(PropertyName = "loc")]
        private Vector2D location;

        [JsonProperty(PropertyName = "bdir")]
        private Vector2D orientation;

        [JsonProperty(PropertyName = "tdir")]
        private Vector2D aiming;

        [JsonProperty(PropertyName = "name")]
        private string name;

        [JsonProperty(PropertyName = "hp")]
        private int hitPoints;

        [JsonProperty(PropertyName = "score")]
        private int score;

        [JsonProperty(PropertyName = "died")]
        private bool died;

        [JsonProperty(PropertyName = "dc")]
        private bool disconnected;

        [JsonProperty(PropertyName = "join")]
        private bool joined;

        private string color;
        public Tank()
        {
        }

        public void randomColor()
        {
            List<string> colors = new List<string>() { "Blue", "Green", "Red", "Purple", "Dark", "Yellow", "Orange" };
            Random random = new Random();
            int i = random.Next(0, 6);
            color = colors[i];
        }

        public void setColor(string newColor)
        {
            color = newColor;
        }

        public string Color()
        {
            return color;
        }

        public void Deactivate()
        {
            died = true;
        }
        public bool IsDead()
        {
            return died;
        }
        public int GetID()
        {
            return ID;
        }

        public bool Disconnected()
        {
            return disconnected;
        }

        public bool Joined()
        {
            return joined;
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

        public Vector2D TurretOrientation()
        {
            return aiming;
        }

        public int getHP()
        {
            return hitPoints;
        }

        public string getName()
        {
            return name;
        }

        public int getScore()
        {
            return score;
        }

        /// <summary>
        /// sets turret orientation with the given position of the Mouse on the screen
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SetTurretOrientation(double x, double y)
        {
            x -= 400;
            y -= 400;
            aiming = new Vector2D(x, y);
            aiming.Normalize();
        }
    }
}

