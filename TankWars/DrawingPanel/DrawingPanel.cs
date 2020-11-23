﻿
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GameController;
using Model;

namespace View
{
    public class DrawingPanel : Panel
    {
        private World theWorld;
        private Controller controller;
        private int viewSize = 800;
        private int worldSize = 2000;
        private Image backgroundImage;
        private Image wallImage;
        Dictionary<string, Image> images;


        public DrawingPanel(World w, Controller controller1)
        {
            controller = controller1;
            DoubleBuffered = true;
            theWorld = w;
            backgroundImage = Image.FromFile("..\\..\\..\\Resources\\images\\Background.png");
            wallImage = Image.FromFile("..\\..\\..\\Resources\\images\\WallSprite.png");
            CreateTankImages();
        }

        /// <summary>
        /// Helper method for DrawObjectWithTransform
        /// </summary>
        /// <param name="size">The world (and image) size</param>
        /// <param name="w">The worldspace coordinate</param>
        /// <returns></returns>
        private static int WorldSpaceToImageSpace(int size, double w)
        {
            return (int)w + size / 2;
        }

        // A delegate for DrawObjectWithTransform
        // Methods matching this delegate can draw whatever they want using e  
        public delegate void ObjectDrawer(object o, PaintEventArgs e);


        /// <summary>
        /// This method performs a translation and rotation to drawn an object in the world.
        /// </summary>
        /// <param name="e">PaintEventArgs to access the graphics (for drawing)</param>
        /// <param name="o">The object to draw</param>
        /// <param name="worldSize">The size of one edge of the world (assuming the world is square)</param>
        /// <param name="worldX">The X coordinate of the object in world space</param>
        /// <param name="worldY">The Y coordinate of the object in world space</param>
        /// <param name="angle">The orientation of the objec, measured in degrees clockwise from "up"</param>
        /// <param name="drawer">The drawer delegate. After the transformation is applied, the delegate is invoked to draw whatever it wants</param>
        private void DrawObjectWithTransform(PaintEventArgs e, object o, int worldSize, double worldX, double worldY, double angle, ObjectDrawer drawer)
        {
            // "push" the current transform
            System.Drawing.Drawing2D.Matrix oldMatrix = e.Graphics.Transform.Clone();

            int x = WorldSpaceToImageSpace(worldSize, worldX);
            int y = WorldSpaceToImageSpace(worldSize, worldY);
            /*            int x = 00;
                        int y = 0;*/
            e.Graphics.TranslateTransform(x, y);
            //Debug.WriteLine(angle.ToString());
            e.Graphics.RotateTransform((float)angle);
            drawer(o, e);

            // "pop" the transform
            e.Graphics.Transform = oldMatrix;
        }

        private Color teamColor(Tank o)
        {
            return Color.Red;
        }

        /// <summary>
        /// This method draws the objects on the world and centers the players view over their tank
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            lock (theWorld)
            {
                if (theWorld.Tanks.TryGetValue(controller.getID(), out Tank player))
                {
                    double playerY = player.GetLocation().GetY();
                    double playerX = player.GetLocation().GetX();

                    //double ratio = (double)viewSize / (double)theWorld.getSize();
                    double ratio = (double)viewSize / (double)2000;
                    int halfSizeScaled = (int)(worldSize / 2.0 * ratio);

                    double inverseTranslateX = -WorldSpaceToImageSpace(worldSize, playerX) + halfSizeScaled;
                    double inverseTranslateY = -WorldSpaceToImageSpace(worldSize, playerY) + halfSizeScaled;

                    e.Graphics.TranslateTransform((float)inverseTranslateX, (float)inverseTranslateY);
                }

                BackgroundDrawer(null, e);


                foreach (Wall wall in theWorld.Walls.Values)
                    DrawWall(wall, e);

                foreach (Powerup pow in theWorld.Powerups.Values)
                    DrawObjectWithTransform(e, pow, worldSize, pow.getLocation().GetX(), pow.getLocation().GetY(), 0, PowerupDrawer);

                foreach (Projectile proj in theWorld.Projectiles.Values)
                    DrawObjectWithTransform(e, proj, worldSize, proj.GetLocation().GetX(), proj.GetLocation().GetY(), proj.GetDirectionAngle(), ProjectileDrawer);

                if(theWorld.Beams.Count > 0)
                {
                    int beamID = -1;
                    foreach(Beam beam in theWorld.Beams.Values)
                    {
                        double x = beam.getOrigin().GetX();
                        double y = beam.getOrigin().GetY();
                        DrawObjectWithTransform(e, beam, worldSize, x, y, beam.getDirection().ToAngle(), BeamDrawer);
                        beamID = beam.getID();
                    }
                    theWorld.Beams.Remove(beamID);
                }

                // Draw the players
                foreach (Tank tank in theWorld.Tanks.Values)
                {
                    DrawObjectWithTransform(e, tank, worldSize, tank.GetLocation().GetX(), tank.GetLocation().GetY(), tank.GetOrientation().ToAngle(), TankDrawer);

                    if (controller.TurretOrientation != null && tank.GetID() == controller.getID())
                        DrawObjectWithTransform(e, tank, worldSize, tank.GetLocation().GetX(), tank.GetLocation().GetY(), controller.TurretOrientation.ToAngle(), TurretDrawer);


                    else if(controller.TurretOrientation != null)
                        DrawObjectWithTransform(e, tank, worldSize, tank.GetLocation().GetX(), tank.GetLocation().GetY(), tank.TurretOrientation().ToAngle(), TurretDrawer);
                    
                }
            }
            // Do anything that Panel(from which we inherit) needs to do
            base.OnPaint(e);
        }

        private void BeamDrawer(object o, PaintEventArgs e)
        {
            Beam b = o as Beam;
            Pen pen = new Pen(Color.Aqua, 3);
            Point p1 = new Point((int)b.getOrigin().GetX(), (int)b.getOrigin().GetY());
            int owner = b.getOwner();
            theWorld.Tanks.TryGetValue(owner, out Tank tank);

            Point p2 = new Point((int)tank.TurretOrientation().GetX(), (int)tank.TurretOrientation().GetY());

            e.Graphics.DrawLine(pen, p1, p2);

        }

        /// <summary>
        /// private helper method that tanks in each wall and then calls the WallDrawer delegate
        /// </summary>
        /// <param name="wall"></param>
        /// <param name="e"></param>
        private void DrawWall(Wall wall, PaintEventArgs e)
        {
            int numofwalls = wall.numofWalls(out bool isVertical, out bool p1Greater);
            int y = (int)wall.getP1().GetY();
            int x = (int)wall.getP1().GetX();
            for (int i = 0; i <= numofwalls; i++)
            {
                if (isVertical)
                {
                    DrawObjectWithTransform(e, wall, theWorld.size, wall.getP2().GetX(), y, 0, WallDrawer);
                    if (p1Greater)
                        y -= 50;
                    else
                        y += 50;
                }
                else
                {
                    DrawObjectWithTransform(e, wall, theWorld.size, x, wall.getP2().GetY(), 0, WallDrawer);
                    if (p1Greater)
                        x -= 50;
                    else
                        x += 50;
                }
            }
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void TankDrawer(object o, PaintEventArgs e)
        {
            Tank tank = o as Tank;
            images.TryGetValue(tank.Color(), out Image image);
            e.Graphics.DrawImage(image, -image.Width / 2, -image.Height / 2);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void WallDrawer(object o, PaintEventArgs e)
        {
            Wall wall = o as Wall;
            Image i = wallImage;
            e.Graphics.DrawImage(i, -i.Width / 2, -i.Height / 2);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void PowerupDrawer(object o, PaintEventArgs e)
        {
            Powerup p = o as Powerup;

            int width = 8;
            int height = 8;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (System.Drawing.SolidBrush redBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Red))
            using (System.Drawing.SolidBrush yellowBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Yellow))
            using (System.Drawing.SolidBrush blackBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
            {
                // Circles are drawn starting from the top-left corner.
                // So if we want the circle centered on the powerup's location, we have to offset it
                // by half its size to the left (-width/2) and up (-height/2)
                Rectangle r = new Rectangle(-(width / 2), -(height / 2), width, height);
                e.Graphics.FillEllipse(blackBrush, r);
            }
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void ProjectileDrawer(object o, PaintEventArgs e)
        {
            /*            Projectile p = o as Projectile;
                        int ID = p.GetOwner();
                        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        if (theWorld.Tanks.TryGetValue(ID, out Tank player))
                        {
                            string color = player.Color();
                            Image i = Image.FromFile("..\\..\\..\\Resources\\images\\shot-" + color + ".png");

                            e.Graphics.DrawImage(i, i.Width/2, i.Height/2);
                        }*/

            int width = 8;
            int height = 8;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (System.Drawing.SolidBrush redBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Red))
            using (System.Drawing.SolidBrush yellowBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Yellow))
            using (System.Drawing.SolidBrush blackBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
            {
                // Circles are drawn starting from the top-left corner.
                // So if we want the circle centered on the powerup's location, we have to offset it
                // by half its size to the left (-width/2) and up (-height/2)
                Rectangle r = new Rectangle(-(width / 2), -(height / 2), width, height);
                e.Graphics.FillEllipse(yellowBrush, r);
            }
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void TurretDrawer(object o, PaintEventArgs e)
        {
            Tank tank = o as Tank;
            images.TryGetValue(tank.Color() + "Turret", out Image image);
            e.Graphics.DrawImage(image, -image.Width / 2, -image.Height / 2);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void BackgroundDrawer(object o, PaintEventArgs e)
        {
            int width = backgroundImage.Width;
            int height = backgroundImage.Height;
            Rectangle destinationRect = new Rectangle(0, 0, worldSize, worldSize);
            e.Graphics.DrawImage(backgroundImage, destinationRect, 0, 0, backgroundImage.Width, backgroundImage.Height, GraphicsUnit.Pixel, new ImageAttributes(), null);
        }

        /// <summary>
        /// private helper method  
        /// </summary>
        private void CreateTankImages()
        {
            string filePath = "..\\..\\..\\Resources\\images\\";
            List<string> colors = new List<string>() { "Blue", "Green", "Red", "Purple", "Dark", "Yellow", "Orange" };
            images = new Dictionary<string, Image>();
            foreach (string color in colors)
            {
                images.Add(color, Image.FromFile(filePath + color + "Tank.png"));
                images.Add(color + "Turret", Image.FromFile(filePath + color + "Turret.png"));
            }
        }
    }
}

