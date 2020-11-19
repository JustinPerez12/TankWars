using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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

        public DrawingPanel(World w, Controller controller1)
        {
            controller = controller1;
            DoubleBuffered = true;
            theWorld = w;
            backgroundImage = Image.FromFile("..\\..\\..\\Resources\\images\\Background.png");
            Dictionary<string, Image> images = new Dictionary<string, Image>();
            var files = new DirectoryInfo(@"C:\Source\");
            foreach (var file in files.GetFiles())
            {
                //Debug.writeLine(file.Directory);
                
            }
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
                // Draw the players
                foreach (Tank tank in theWorld.Tanks.Values)
                {
                    if(!tank.Disconnected() || !tank.IsDead())
                        DrawObjectWithTransform(e, tank, worldSize, tank.GetLocation().GetX(), tank.GetLocation().GetY(), tank.GetOrientation().ToAngle(), TankDrawer);

                    //normalize the vector then pass into turretdrawer
                    tank.TurretOrientation().Normalize();
                    //DrawObjectWithTransform(e, tank, worldSize, tank.GetLocation().GetX(), tank.GetLocation().GetY(), tank.TurretOrientation().ToAngle(), TurretDrawer);
                }

                // Draw the powerups
                foreach (Powerup pow in theWorld.Powerups.Values)
                    DrawObjectWithTransform(e, pow, worldSize, pow.getLocation().GetX(), pow.getLocation().GetY(), 0, PowerupDrawer);

                foreach (Projectile proj in theWorld.Projectiles.Values)
                    DrawObjectWithTransform(e, proj, worldSize, proj.GetLocation().GetX(), proj.GetLocation().GetY(), proj.GetDirectionAngle(), ProjectileDrawer);

                foreach (Wall wall in theWorld.Walls.Values)
                    DrawWall(wall, e);
            }
            // Do anything that Panel(from which we inherit) needs to do
            base.OnPaint(e);
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
            Image i = Image.FromFile("..\\..\\..\\Resources\\images\\RedTank.png");
            e.Graphics.DrawImage(i, -i.Width / 2, -i.Height / 2);
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
            Image i = Image.FromFile("..\\..\\..\\Resources\\images\\WallSprite.png");
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
            Projectile p = o as Projectile;
            int ID = p.GetOwner();
            if (theWorld.Tanks.TryGetValue(ID, out Tank player))
            {
                string color = player.Color();
                Image i = Image.FromFile("..\\..\\..\\Resources\\images\\shot-" + color + ".png");
                int width = i.Width;
                int height = i.Height;

                e.Graphics.DrawImage(i, 0, 0);
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
            Image i = Image.FromFile("..\\..\\..\\Resources\\images\\RedTurret.png");
            e.Graphics.DrawImage(i, -i.Width / 2, -i.Height / 2);
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
    }
}

