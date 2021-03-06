
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GameController;
using Model;
using TankWars;

namespace View
{
    public class DrawingPanel : Panel
    {
        private World theWorld;
        private ClientController controller;
        private int viewSize = 800;
        private int worldSize = 2000;
        private Dictionary<string, Image> images;

        private double deathLocationX = -1;
        private double deathLocationY = -1;
        public DrawingPanel(World w, ClientController controller1)
        {
            controller = controller1;
            DoubleBuffered = true;
            theWorld = w;
            CreateTankImages();
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
            e.Graphics.TranslateTransform(x, y);
            e.Graphics.RotateTransform((float)angle);
            drawer(o, e);

            // "pop" the transform
            e.Graphics.Transform = oldMatrix;
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
                    deathLocationY = player.GetLocation().GetY();
                    deathLocationX = player.GetLocation().GetX();
                    double ratio = (double)viewSize / (double)2000;
                    int halfSizeScaled = (int)(worldSize / 2.0 * ratio);

                    double inverseTranslateX = -WorldSpaceToImageSpace(worldSize, playerX) + halfSizeScaled;
                    double inverseTranslateY = -WorldSpaceToImageSpace(worldSize, playerY) + halfSizeScaled;

                    e.Graphics.TranslateTransform((float)inverseTranslateX, (float)inverseTranslateY);
                    BackgroundDrawer(null, e);

                }

                //If the user has not connected yet display title screen
                else if (controller.getID() == -1)
                {
                    images.TryGetValue("titleScreen", out Image titleScreen);
                    Rectangle destinationRect = new Rectangle(0, 0, titleScreen.Width, titleScreen.Height);
                    e.Graphics.DrawImage(titleScreen, destinationRect, 0, 0, titleScreen.Width, titleScreen.Height, GraphicsUnit.Pixel, new ImageAttributes(), null);
                }

                //this centers the screen over the death location rather than sending it to the top left of map
                else
                {
                    if (deathLocationY != -1)
                    {

                        double ratio = (double)viewSize / (double)2000;
                        int halfSizeScaled = (int)(worldSize / 2.0 * ratio);
                        double inverseTranslateX = -WorldSpaceToImageSpace(worldSize, deathLocationX) + halfSizeScaled;
                        double inverseTranslateY = -WorldSpaceToImageSpace(worldSize, deathLocationY) + halfSizeScaled;
                        e.Graphics.TranslateTransform((float)inverseTranslateX, (float)inverseTranslateY);
                    }
                    BackgroundDrawer(null, e);
                }


                foreach (Wall wall in theWorld.Walls.Values)
                    DrawWall(wall, e);

                foreach (Powerup pow in theWorld.Powerups.Values)
                    DrawObjectWithTransform(e, pow, worldSize, pow.getLocation().GetX(), pow.getLocation().GetY(), 0, PowerupDrawer);

                foreach (Projectile proj in theWorld.Projectiles.Values)
                    DrawObjectWithTransform(e, proj, worldSize, proj.GetLocation().GetX(), proj.GetLocation().GetY(), proj.GetDirectionAngle(), ProjectileDrawer);

                foreach (Tank tank in theWorld.DeadTanks.Values)
                    DrawObjectWithTransform(e, tank, worldSize, tank.GetLocation().GetX(), tank.GetLocation().GetY(), 0, DeadTankDrawer);

                int beamID = -1;
                foreach (Beam beam in theWorld.Beams.Values)
                {
                    double x = beam.getOrigin().GetX();
                    double y = beam.getOrigin().GetY();
                    DrawObjectWithTransform(e, beam, worldSize, x, y, beam.getDirection().ToAngle(), BeamDrawer);
                    beamID = beam.getID();
                }
                theWorld.Beams.Remove(beamID);

                // Draw the players
                foreach (Tank tank in theWorld.Tanks.Values)
                {
                    DrawObjectWithTransform(e, tank, worldSize, tank.GetLocation().GetX(), tank.GetLocation().GetY(), tank.GetOrientation().ToAngle(), TankDrawer);
                    DrawObjectWithTransform(e, tank, worldSize, tank.GetLocation().GetX() - (tank.getName().Length * 12) / 2, tank.GetLocation().GetY() + 30, 0, NameDrawer);
                    images.TryGetValue("lowHealth", out Image lowHealth);
                    DrawObjectWithTransform(e, tank, worldSize, tank.GetLocation().GetX() + lowHealth.Width / 2 - 36, tank.GetLocation().GetY() - 30, 0, HealthDrawer);

                    //Draw other players
                    if (tank.GetID() != controller.getID())
                        DrawObjectWithTransform(e, tank, worldSize, tank.GetLocation().GetX(), tank.GetLocation().GetY(), tank.TurretOrientation().ToAngle(), TurretDrawer);

                    //Draw the user
                    else if (controller.TurretOrientation != null && tank.GetID() == controller.getID())
                        DrawObjectWithTransform(e, tank, worldSize, tank.GetLocation().GetX(), tank.GetLocation().GetY(), controller.TurretOrientation.ToAngle(), TurretDrawer);

                    //if the user has not yet put their mouse on the screen
                    else if (controller.TurretOrientation == null && tank.GetID() == controller.getID())
                        DrawObjectWithTransform(e, tank, worldSize, tank.GetLocation().GetX(), tank.GetLocation().GetY(), 0, TurretDrawer);
                }
            }
            // Do anything that Panel(from which we inherit) needs to do
            base.OnPaint(e);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method to draw a tank
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void TankDrawer(object o, PaintEventArgs e)
        {
            Tank tank = o as Tank;
            if (tank.Color() != null)
            {
                if (images.TryGetValue(tank.Color(), out Image image))
                {
                    images.TryGetValue("oldTank", out Image oldTank);
                    Rectangle destinationRect = new Rectangle(-oldTank.Width / 2, -oldTank.Height / 2, oldTank.Width + 30, oldTank.Height + 30);
                    e.Graphics.DrawImage(image, destinationRect, 100, 100, image.Width, image.Height, GraphicsUnit.Pixel, new ImageAttributes(), null);
                }
            }
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
        /// DrawObjectWithTransform will invoke this method to draw a wall
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void WallDrawer(object o, PaintEventArgs e)
        {
            Wall wall = o as Wall;
            images.TryGetValue("wallImage", out Image wallImage);
            e.Graphics.DrawImage(wallImage, -wallImage.Width / 2, -wallImage.Height / 2);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method to draw a powerup
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
                Rectangle r = new Rectangle(-(width / 2), -(height / 2), width, height);
                e.Graphics.FillEllipse(blackBrush, r);
            }
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method to draw a projectile
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void ProjectileDrawer(object o, PaintEventArgs e)
        {
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
                e.Graphics.FillEllipse(redBrush, r);
            }

        }

        /// <summary>a
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method to draw a turret
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void TurretDrawer(object o, PaintEventArgs e)
        {
            Tank tank = o as Tank;
            if (tank.Color() != null)
            {
                images.TryGetValue(tank.Color() + "Turret", out Image image);
                images.TryGetValue("oldTurret", out Image oldTurret);
                Rectangle destinationRect = new Rectangle(-oldTurret.Width / 2, -oldTurret.Height / 2, oldTurret.Width + 30, oldTurret.Height + 30);
                e.Graphics.DrawImage(image, destinationRect, 100, 100, image.Width, image.Height, GraphicsUnit.Pixel, new ImageAttributes(), null);
            }
        }

        private void DeadTankDrawer(object o, PaintEventArgs e)
        {
            images.TryGetValue("destroy", out Image destroy);
            e.Graphics.DrawImage(destroy, -destroy.Width / 2, -destroy.Height / 2);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method to draw a beam
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void BeamDrawer(object o, PaintEventArgs e)
        {
            Beam beam = o as Beam;
            Pen pen = new Pen(Color.Black, 20);
            Point p1 = new Point(0, -worldSize * 2);
            Point p2 = new Point(0, 0);
            e.Graphics.DrawLine(pen, p1, p2);
            Debug.WriteLine(p1.ToString());
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method to draw a the name of a player
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void NameDrawer(object o, PaintEventArgs e)
        {
            Tank t = o as Tank;
            Point p = new Point(0, 0);
            Point p1 = new Point(0, 0);
            Brush b = Brushes.Black;
            FontFamily fontFamily = new FontFamily("Arial");
            Font font = new Font(fontFamily, 16, FontStyle.Bold, GraphicsUnit.Pixel);
            e.Graphics.DrawString(t.getName() + ": " + t.getScore(), font, b, p);
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method to draw a the health of a player
        /// </summary>
        /// <param name="o">The object to draw</param>
        /// <param name="e">The PaintEventArgs to access the graphics</param>
        private void HealthDrawer(object o, PaintEventArgs e)
        {
            Tank t = o as Tank;
            if (t.getHP() == 3)
            {
                images.TryGetValue("highHealth", out Image highHealth);
                e.Graphics.DrawImage(highHealth, -highHealth.Width / 2, -highHealth.Height / 2);
            }
            else if (t.getHP() == 2)
            {
                images.TryGetValue("medHealth", out Image medHealth);
                e.Graphics.DrawImage(medHealth, -medHealth.Width / 2, -medHealth.Height / 2);
            }
            else if (t.getHP() == 1)
            {
                images.TryGetValue("lowHealth", out Image lowHealth);
                e.Graphics.DrawImage(lowHealth, -lowHealth.Width / 2, -lowHealth.Height / 2);
            }
        }

        /// <summary>
        /// Acts as a drawing delegate for DrawObjectWithTransform
        /// After performing the necessary transformation (translate/rotate)
        /// DrawObjectWithTransform will invoke this method to draw the background
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void BackgroundDrawer(object o, PaintEventArgs e)
        {
            images.TryGetValue("backgroundImage", out Image backgroundImage);
            Rectangle destinationRect = new Rectangle(0, 0, worldSize, worldSize);
            e.Graphics.DrawImage(backgroundImage, destinationRect, 0, 0, backgroundImage.Width, backgroundImage.Height, GraphicsUnit.Pixel, new ImageAttributes(), null);
        }

        /// <summary>
        /// private helper method to create the list of images to pull from
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
            images.Add("backgroundImage", Image.FromFile("..\\..\\..\\Resources\\images\\Background2.png"));
            images.Add("wallImage", Image.FromFile("..\\..\\..\\Resources\\images\\WallSprite.png"));
            images.Add("oldTank", Image.FromFile("..\\..\\..\\Resources\\images\\LightGreenTank.png"));
            images.Add("oldTurret", Image.FromFile("..\\..\\..\\Resources\\images\\LightGreenTurret.png"));
            images.Add("lowHealth", Image.FromFile("..\\..\\..\\Resources\\images\\lowHealth.png"));
            images.Add("medHealth", Image.FromFile("..\\..\\..\\Resources\\images\\halfHealth.png"));
            images.Add("highHealth", Image.FromFile("..\\..\\..\\Resources\\images\\fullHealth.png"));
            images.Add("titleScreen", Image.FromFile("..\\..\\..\\Resources\\images\\tankwars-title.jpg"));
            images.Add("shot-green", Image.FromFile("..\\..\\..\\Resources\\images\\shot-green.png"));
            images.Add("destroy", Image.FromFile("..\\..\\..\\Resources\\images\\destroy.png"));
        }
    }
}

