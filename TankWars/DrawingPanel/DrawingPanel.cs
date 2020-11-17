using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameController;
using Model;

namespace View {
    public class DrawingPanel : Panel 
    {

        private World theWorld;
        private Controller controller;
        private int scale = 4;
        private int viewSize = 800;
        private int worldSize = 2000;

        public DrawingPanel(World w, Controller controller1)
        {
            controller = controller1;
            DoubleBuffered = true;
            theWorld = w;
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
            e.Graphics.RotateTransform((float)angle);
            drawer(o, e);

            // "pop" the transform
            e.Graphics.Transform = oldMatrix;
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
            Image i = Image.FromFile("..\\..\\..\\Resources\\images\\RedTank.png");
            e.Graphics.DrawImage(i, 0, 0);
            int width = i.Width;
            int height = i.Height;
            Rectangle destinationRect = new Rectangle(0, 0, worldSize, worldSize);
        }

        private Color teamColor(Tank o)
        {
            return Color.Red;
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


        private void BackgroundDrawer(object o, PaintEventArgs e)
        {
            Image i = Image.FromFile("..\\..\\..\\Resources\\images\\Background.png");
            int width = i.Width;
            int height = i.Height;
            Rectangle destinationRect = new Rectangle(0, 0, worldSize, worldSize);
            e.Graphics.DrawImage(i, destinationRect, 0, 0, i.Width, i.Height, GraphicsUnit.Pixel, new ImageAttributes(), null);
        }

        // This method is invoked when the DrawingPanel needs to be re-drawn
        protected override void OnPaint(PaintEventArgs e)
        {
            if(theWorld.Tanks.TryGetValue(controller.getID(), out Tank player))
            {
                double playerY = player.GetLocationY();
                double playerX = player.GetLocationX();

                //double ratio = (double)viewSize / (double)theWorld.getSize();
                double ratio = (double)viewSize / (double)2000;
                int halfSizeScaled = (int)(worldSize / 2.0 * ratio);

                double inverseTranslateX = -WorldSpaceToImageSpace(worldSize, playerX) + halfSizeScaled;
                double inverseTranslateY = -WorldSpaceToImageSpace(worldSize, playerY) + halfSizeScaled;

                e.Graphics.TranslateTransform((float)inverseTranslateX, (float)inverseTranslateY);
            }

            //DrawObjectWithTransform(e, play, theWorld.size, play.GetLocation().GetX(), play.GetLocation().GetY(), play.GetOrientation().ToAngle(), DrawMine); lock (theWorld)
            lock(theWorld)
            {
                BackgroundDrawer(null, e);

                // Draw the players
                foreach (Tank tank in theWorld.Tanks.Values)
                {
                    DrawObjectWithTransform(e, tank, worldSize, tank.GetLocationX(), tank.GetLocationY(), tank.GetOrientationAngle(), TankDrawer);
                }
                // Draw the powerups
                foreach (Powerup pow in theWorld.Powerups.Values)
                {
                    DrawObjectWithTransform(e, pow, worldSize, pow.GetLocationX(), pow.GetLocationY(), 0, PowerupDrawer);
                }

                foreach (Projectile proj in theWorld.Projectiles.Values)
                {
                    DrawObjectWithTransform(e, proj, worldSize, proj.GetLocationX(), proj.GetLocationY(), proj.GetDirectionAngle(), TankDrawer);
                }


                /*foreach (Wall wall in theWorld.Walls.Values)
                {
                    DrawObjectWithTransform(e, wall, theWorld.size, tank.GetLocationX(), tank.GetLocationY(), tank.GetOrientationAngle(), TankDrawer);
                }*/

                // Do anything that Panel (from which we inherit) needs to do
                base.OnPaint(e);
            }
        }

    }
}

