using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
//using System.Windows.;

namespace FoundationCodeForFractalMountains
{
    public partial class fractalMountainDemoForm : Form
    {
        #region Data Members of "fractalMountainDemo"

        private int _screenWidth, _screenHeight;

        /*
         * Co-ordinates of initial points on "pointDraggingPictureBox." 
         * The user can drag these points to select a custom initial triangle.
         * 
         * initialA -> initial point on z-axis
         * initialB -> initial point on x-axis
         * initialC -> initial point on y-axis
         * 
         * Points on x-axis: (50, 260), (119, 189) -> Equation Y = (-71/69)*X + (21490/69) (relative to "pointDraggingPictureBox")
         */

        private Point initialA = new Point(119, 55), initialB = new Point(51, 261), initialC = new Point(250, 190);

        // Some constants required for mapping points on "pointDraggingPictureBox" to 3D space.
        private const int ORIGIN_X = 119, ORIGIN_Y = 189; //Co-ordinates of origin relative to "pointDraggingPictureBox"
        private const int A_INDEX = 0, B_INDEX = 1, C_INDEX = 2;

        // Required for the feature that allows the user to select the initial vertices simply by dragging.
        private Bitmap pointDraggingBitmap = new Bitmap(300, 300);
        private Point[] dotsLocationRelativeToPicBox = new Point[3]; //Stores the co-ordinates of the centres of the dragging dots relative to picture box 
        private PictureBox[] dotPic = new PictureBox[3]; //Stores references to the picture boxes uses for the dot pictures.

        // Objects needed for GDI+ graphics.
        private Bitmap fractalMountainBitmap;
        private Graphics fractalMountainDrawingSurface;

        Random a = new Random();
        Random b = new Random();
        Random c = new Random();
        Brush red = new SolidBrush(Color.Red);


        private Pen drawMountainPen = new Pen(Color.Black);
        private Pen drawInitialTrianglePen = new Pen(Color.Blue, 2); // The second argument specifies the line width.
        private Brush drawInitialTriangleBrush = new SolidBrush(Color.FromArgb(100, Color.LightSteelBlue));

        // Create an empty list of triangles
        private List<Triangle> triangleList = new List<Triangle>();


        // Needed to traverse the list of triangles when drawing  the triangles
        private System.Collections.IEnumerator triangleListEnumerator;

        //Vertices and edges of initial triangle.
        private Vertex A, B, C;
        private Edge AB, BC, CA;

        private int lastX_Angle, lastY_Angle, lastZ_Angle;

        // The light source vector is initialized to be from (0, 0, 0) to (1, 1, 1)
        private Vector3 lightSourceVector = new Vector3(1, 1, 1);

        private void rotateTriangles()
        {
            fractalMountainDrawingSurface.Clear(mountainPictureBox.BackColor);
            mountainPictureBox.Refresh();
            System.Collections.IEnumerator TriangleListEnumerator = triangleList.GetEnumerator();
            Graphics g = Graphics.FromImage(fractalMountainBitmap);

            while (TriangleListEnumerator.MoveNext())
            { Triangle t = (Triangle)TriangleListEnumerator.Current;
                Edge AB = new Edge(t.AB);
                Vertex A = new Vertex(AB.V1);
                Vertex B = new Vertex(AB.V2);
                Vertex C = new Vertex(t.BC.V2);
                Vector3 OA = new Vector3(A.X, A.Y, A.Z);
                Vector3 OB = new Vector3(B.X, B.Y, B.Z);
                Vector3 OC = new Vector3(C.X, C.Y, C.Z);
                Point A_Screen = camera.toScreen(OA);
                Point B_Screen = camera.toScreen(OB);
                Point C_Screen = camera.toScreen(OC);
                g.DrawPolygon(Pens.Black, new Point[] { A_Screen, B_Screen, C_Screen }); }
        }

        /**     A
                /\
               /  \
              /    \
             /      \
            /        \  
           /          \
         B ------------ C
         
        */


        // Required for perspective projection
        private Camera camera;


        #endregion

        #region Constructor

        public fractalMountainDemoForm()
        {
            InitializeComponent();

            /**
             * Initializations
             */

            _screenWidth = mountainPictureBox.Width;
            _screenHeight = mountainPictureBox.Height;
            fractalMountainBitmap = new Bitmap(_screenWidth, _screenHeight);

            fractalMountainDrawingSurface = Graphics.FromImage(fractalMountainBitmap);

            triangleListEnumerator = triangleList.GetEnumerator();

            Graphics pointDraggingSurface = Graphics.FromImage(pointDraggingBitmap);

            dotPic[0] = movePointDotPictureBoxA;
            dotPic[1] = movePointDotPictureBoxB;
            dotPic[2] = movePointDotPictureBoxC;

            //Initialize "dotLocation," the array of "Point" objects.
            dotsLocationRelativeToPicBox[0] = initialA;
            dotsLocationRelativeToPicBox[1] = initialB;
            dotsLocationRelativeToPicBox[2] = initialC;

            //Draw initial triangle.
            pointDraggingSurface.DrawPolygon(drawInitialTrianglePen, new Point[] { initialA, initialB, initialC });


            //Fill triangle
            pointDraggingSurface.FillPolygon(drawInitialTriangleBrush, new Point[] { initialA, initialB, initialC });


            //Place the "dragging" dots at their initial positions
            for (int i = 0; i < dotPic.Length; i++)
            {
                dotPic[i].Location = new Point(dotsLocationRelativeToPicBox[i].X + pointDraggingPictureBox.Left - dotPic[i].Width / 2,
                                        dotsLocationRelativeToPicBox[i].Y + pointDraggingPictureBox.Top - dotPic[i].Height / 2);
            }

            // Fire the "Paint" event on "pointDraggingPictureBox"
            pointDraggingPictureBox.Refresh();

            //Set vertices of initial triangle.
            A = transformTo3D(initialA, A_INDEX);
            B = transformTo3D(initialB, B_INDEX);
            C = transformTo3D(initialC, C_INDEX);

            //Set edges of initial triangle.
            AB = new Edge(A, B);
            BC = new Edge(B, C);
            CA = new Edge(C, A);

            //Create the camera (direction, right, cameraPosition)
            camera = new Camera(new Vector3(-1, 0, -1), new Vector3(0, 1, 0), new Vector3(1, 0.2, 1), 90, new Point(_screenWidth, _screenHeight));


            lastX_Angle = XtrackBar.Value;
            lastY_Angle = YtrackBar.Value;
            lastZ_Angle = ZtrackBar.Value;

        }

        #endregion

        #region Event Handlers

        // This event handler handles rotations along the x-axis
        private void XtrackBar_ValueChanged(object sender, EventArgs e)
        {

            //int direction = Math.Sign(XtrackBar.Value - lastX_Angle);
            triangleList = rotateMountain(triangleList, XtrackBar.Value - lastX_Angle, 'x');
            fractalMountainDrawingSurface.Clear(mountainPictureBox.BackColor);
            lastX_Angle = XtrackBar.Value;
            if (noShadingRadioButton.Checked == true)
            {
                drawTriangles();
                mountainPictureBox.Refresh();
            }
            else if (positiveShadingRadioButton.Checked == true)
            {
                positiveShading();
                mountainPictureBox.Refresh();
            }
            else if (negativeShadingRadioButton.Checked == true)
            {
                negativeShading();
                mountainPictureBox.Refresh();
            }
            else if (colorShadingRadioButton.Checked == true)
            {
                colorShading(colorComboBox.Text);
                mountainPictureBox.Refresh();
            }
        }

        // This event handler handles rotations along the y-axis
        private void YtrackBar_ValueChanged(object sender, EventArgs e)
        {
            triangleList = rotateMountain(triangleList, YtrackBar.Value - lastY_Angle, 'y');
            fractalMountainDrawingSurface.Clear(mountainPictureBox.BackColor);
            lastY_Angle = YtrackBar.Value;
            if (noShadingRadioButton.Checked == true)
            {
                drawTriangles();
                mountainPictureBox.Refresh();
            }
            else if (positiveShadingRadioButton.Checked == true)
            {
                positiveShading();
                mountainPictureBox.Refresh();
            }
            else if (negativeShadingRadioButton.Checked == true)
            {
                negativeShading();
                mountainPictureBox.Refresh();
            }
            else if (colorShadingRadioButton.Checked == true)
            {
                colorShading(colorComboBox.Text);
                mountainPictureBox.Refresh();
            }
        }

        // This event handler handles rotations along the z-axis
        private void ZtrackBar_ValueChanged(object sender, EventArgs e)
        {
            triangleList = rotateMountain(triangleList, ZtrackBar.Value - lastZ_Angle, 'z');
            fractalMountainDrawingSurface.Clear(mountainPictureBox.BackColor);
            lastZ_Angle = ZtrackBar.Value;
            if (noShadingRadioButton.Checked == true)
            {
                drawTriangles();
                mountainPictureBox.Refresh();
            }
            else if (positiveShadingRadioButton.Checked == true)
            {
                positiveShading();
                mountainPictureBox.Refresh();
            }
            else if (negativeShadingRadioButton.Checked == true)
            {
                negativeShading();
                mountainPictureBox.Refresh();
            }
            else if (colorShadingRadioButton.Checked == true)
            {
                colorShading(colorComboBox.Text);
                mountainPictureBox.Refresh();
            }
        }

        // This event handler checks whether the colorShadingRadioButton
        // is checked to enable colorComboBox
        private void colorShadingRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (colorShadingRadioButton.Checked == true)
                colorComboBox.Enabled = true;
            else
                colorComboBox.Enabled = false;
        }

        // This button clears the drawing surface, picture box, and disables all track bars
        private void clearButton_Click(object sender, EventArgs e)
        {
            XtrackBar.Enabled = false;
            YtrackBar.Enabled = false;
            ZtrackBar.Enabled = false;
            lightSourceTrackBar.Enabled = false;
            fieldOfViewTrackBar.Enabled = false;
            XtrackBar.Value = 0;
            YtrackBar.Value = 0;
            ZtrackBar.Value = 0;
            lightSourceTrackBar.Value = 0;
            fieldOfViewTrackBar.Value = 90;
            fractalMountainDrawingSurface.Clear(mountainPictureBox.BackColor);
            mountainPictureBox.Refresh();
        }

        // This track bar changes the x-coordinate as a function of the z-coordinate of the light source
        private void lightSourceTrackBar_Scroll(object sender, EventArgs e)
        {

            lightSourceVector.X = lightSourceTrackBar.Value;
            
            if (positiveShadingRadioButton.Checked == true)
            {
                positiveShading();
                mountainPictureBox.Refresh();
            }
            else if (negativeShadingRadioButton.Checked == true)
            {
                negativeShading();
                mountainPictureBox.Refresh();
            }
            else if (colorShadingRadioButton.Checked == true)
            {
                colorShading(colorComboBox.Text);
                mountainPictureBox.Refresh();
            }
        }
    
        private void drawMountainButton_Click(object sender, EventArgs e)
        {
            int maxIterations = Convert.ToInt32(iterationsNumericUpDown.Value);

            triangleList.Clear();
            fractalMountainDrawingSurface.Clear(mountainPictureBox.BackColor);
            mountainPictureBox.Refresh();

            //Add the initial Triangle to the list "TriangleList."
            triangleList.Add(new Triangle(AB, BC, CA));

            if (noShadingRadioButton.Checked == true)
            {
                fieldOfViewTrackBar.Enabled = true;
                lightSourceTrackBar.Enabled = true;
                XtrackBar.Enabled = true;
                YtrackBar.Enabled = true;
                ZtrackBar.Enabled = true;
                for (int iteration = 1; iteration <= maxIterations; iteration++)
                {
                    fractalMountainDrawingSurface.Clear(mountainPictureBox.BackColor);
                    mountainPictureBox.Refresh();

                    triangleList = subdivideTriangles();

                    drawTriangles();
                    mountainPictureBox.Refresh();

                    System.Threading.Thread.Sleep(500); //Pause for 500 ms = 0.1 s
                }
            }
            else if (positiveShadingRadioButton.Checked == true)
            {
                fieldOfViewTrackBar.Enabled = true;
                lightSourceTrackBar.Enabled = true;
                XtrackBar.Enabled = true;
                YtrackBar.Enabled = true;
                ZtrackBar.Enabled = true;
                for (int iteration = 1; iteration <= maxIterations; iteration++)
                {
                    fractalMountainDrawingSurface.Clear(mountainPictureBox.BackColor);
                    mountainPictureBox.Refresh();

                    triangleList = subdivideTriangles();

                    positiveShading();
                    mountainPictureBox.Refresh();

                    System.Threading.Thread.Sleep(500); //Pause for 500 ms = 0.1 s
                }
            }
            else if (negativeShadingRadioButton.Checked == true)
            {
                fieldOfViewTrackBar.Enabled = true;
                lightSourceTrackBar.Enabled = true;
                XtrackBar.Enabled = true;
                YtrackBar.Enabled = true;
                ZtrackBar.Enabled = true;
                for (int iteration = 1; iteration <= maxIterations; iteration++)
                {
                    fractalMountainDrawingSurface.Clear(mountainPictureBox.BackColor);
                    mountainPictureBox.Refresh();

                    triangleList = subdivideTriangles();

                    negativeShading();
                    mountainPictureBox.Refresh();

                    System.Threading.Thread.Sleep(500); //Pause for 500 ms = 0.1 s
                }
            }
            else if (colorShadingRadioButton.Checked == true)
            {
                fieldOfViewTrackBar.Enabled = true;
                string colorChosen = colorComboBox.Text;
                lightSourceTrackBar.Enabled = true;
                XtrackBar.Enabled = true;
                YtrackBar.Enabled = true;
                ZtrackBar.Enabled = true;
                for (int iteration = 1; iteration <= maxIterations; iteration++)
                {
                    fractalMountainDrawingSurface.Clear(mountainPictureBox.BackColor);
                    mountainPictureBox.Refresh();

                    triangleList = subdivideTriangles();

                    colorShading(colorChosen);
                    mountainPictureBox.Refresh();

                    System.Threading.Thread.Sleep(500); //Pause for 500 ms = 0.1 s
                }
            }
        }

        private void mountainPictureBox_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(fractalMountainBitmap, 0, 0);
        }

        private void fieldOfViewTrackBar_ValueChanged(object sender, EventArgs e)
        {
            fieldOfViewLabel.Text = fieldOfViewTrackBar.Value.ToString();
            //Application.DoEvents();
            camera.FieldOfView = fieldOfViewTrackBar.Value;
            fractalMountainDrawingSurface.Clear(mountainPictureBox.BackColor);
            if (noShadingRadioButton.Checked == true)
            {
                drawTriangles();
                mountainPictureBox.Refresh();
            }
            else if (positiveShadingRadioButton.Checked == true)
            {
                positiveShading();
                mountainPictureBox.Refresh();
            }
            else if (negativeShadingRadioButton.Checked == true)
            {
                negativeShading();
                mountainPictureBox.Refresh();
            }
            else if (colorShadingRadioButton.Checked == true)
            {
                colorShading(colorComboBox.Text);
                mountainPictureBox.Refresh();
            }
        }

        private void pointDraggingPictureBox_Paint(object sender, PaintEventArgs e)
        {
            pointDraggingPictureBox.Image = Properties.Resources.xyz_3D_coordinate_axes;
            e.Graphics.DrawImage(pointDraggingBitmap, 0, 0);
        }

        // Update the location of the point being dragged on "pointDraggingPictureBox"
        public void movePointDotPictureBox_MouseMove(object sender, MouseEventArgs e)
        {

            if (e.Button == MouseButtons.Left)
            {
                PictureBox picBox = ((PictureBox)sender);
                Point newLocationRelativeToClientRectangle = new Point(picBox.Left + e.X, picBox.Top + e.Y);
                Point originalLocationRelativeToClientRectangle = new Point(picBox.Left, picBox.Top);

                // If the new location of the "dot" picture box is within the boundaries of "pointDraggingPictureBox,"
                // move the picture box to the new location. Otherwise, do nothing.
                if (newLocationRelativeToClientRectangle.X >= pointDraggingPictureBox.Left
                                && newLocationRelativeToClientRectangle.X <= pointDraggingPictureBox.Right - picBox.Width
                                && newLocationRelativeToClientRectangle.Y >= pointDraggingPictureBox.Top
                                && newLocationRelativeToClientRectangle.Y <= pointDraggingPictureBox.Bottom - picBox.Height)
                {
                    for (int i = 0; i < dotPic.Length; i++)
                    {
                        if (sender.Equals(dotPic[i]))
                        {
                            //Update location of dot being dragged

                            if (i == A_INDEX) //point on z-axis 
                            {
                                picBox.Location = new Point(originalLocationRelativeToClientRectangle.X, newLocationRelativeToClientRectangle.Y);
                                A_Label.Location = new Point(picBox.Location.X - A_Label.Width, picBox.Location.Y);
                            }
                            else if (i == B_INDEX) //points on x-axis have equation Y = (-71.0/69.0)*X + (21490/69) relative to "pointDraggingPictureBox"
                            {
                                picBox.Location = new Point(newLocationRelativeToClientRectangle.X,
                                                        (int)((-71.0 / 69.0) * (newLocationRelativeToClientRectangle.X - pointDraggingPictureBox.Left)
                                                                        + (21490.0 / 69.0) + pointDraggingPictureBox.Top - picBox.Height)); //
                                B_Label.Location = new Point(picBox.Location.X + B_Label.Width / 2, picBox.Location.Y + B_Label.Height / 2);
                            }
                            else //point on y-axis
                            {
                                picBox.Location = new Point(newLocationRelativeToClientRectangle.X, originalLocationRelativeToClientRectangle.Y);
                                C_Label.Location = new Point(picBox.Location.X, picBox.Location.Y + 2 * C_Label.Height / 3);
                            }//end if


                            dotsLocationRelativeToPicBox[i] = new Point(picBox.Left - pointDraggingPictureBox.Left + picBox.Width / 2, picBox.Top - pointDraggingPictureBox.Top + picBox.Height / 2);


                            /***********
                             * Redraw
                             ***********/
                            Graphics pointDraggingSurface = Graphics.FromImage(pointDraggingBitmap);

                            //erase
                            pointDraggingSurface.Clear(pointDraggingPictureBox.BackColor);

                            //draw triangle
                            pointDraggingSurface.DrawPolygon(drawInitialTrianglePen, new Point[] { dotsLocationRelativeToPicBox[0], dotsLocationRelativeToPicBox[1], dotsLocationRelativeToPicBox[2] });

                            //Fill triangle 
                            pointDraggingSurface.FillPolygon(drawInitialTriangleBrush, new Point[] { dotsLocationRelativeToPicBox[0], dotsLocationRelativeToPicBox[1], dotsLocationRelativeToPicBox[2] });

                            //Fire the "Paint" event on "pointDraggingPictureBox"
                            pointDraggingPictureBox.Refresh();

                        }//end if

                    }//end for

                    // Set co-ordinates of vertices of initial triangle in 3D space based on location of dots. 
                    // "A" lies on the z-axis, "B" lies on the x-axis, "C" lies on the y-axis

                    A = transformTo3D(dotsLocationRelativeToPicBox[A_INDEX], A_INDEX);
                    B = transformTo3D(dotsLocationRelativeToPicBox[B_INDEX], B_INDEX);
                    C = transformTo3D(dotsLocationRelativeToPicBox[C_INDEX], C_INDEX);

                    //Set edges of initial triangles.
                    AB = new Edge(A, B);
                    BC = new Edge(B, C);
                    CA = new Edge(C, A);

                } //end if
            }// end if

        }//end movePointDotPictureBox_MouseMove
        
        #endregion

        #region Other Methods
        
        // Subdivide each triangle currently in "trianglelList."
        // Return a new list containing the subdivided triangles.
        private List<Triangle> subdivideTriangles()
        {
            List<Triangle> newTriangleList = new List<Triangle>();
            System.Collections.IEnumerator TriangleListEnumerator = triangleList.GetEnumerator();

            while (TriangleListEnumerator.MoveNext())
            {
                Triangle t = (Triangle)TriangleListEnumerator.Current;
                t.subdivide(newTriangleList);
            }

            return newTriangleList;

        }//end subdivideTriangle
        
        // Used for rotating along an axis
        private List<Triangle> rotateMountain(List<Triangle> triangleList, int angle, char axis)
        {
            List<Triangle> rotatedTriangleList = new List<Triangle>();
            rotatedTriangleList.Clear();

            for (int x = 0; x < triangleList.Count; x++)
            {
                Vector3 ABVector = new Vector3(triangleList[x].AB.V1.X, triangleList[x].AB.V1.Y, triangleList[x].AB.V1.Z);
                Vector3 BCVector = new Vector3(triangleList[x].BC.V1.X, triangleList[x].BC.V1.Y, triangleList[x].BC.V1.Z);
                Vector3 ACVector = new Vector3(triangleList[x].CA.V1.X, triangleList[x].CA.V1.Y, triangleList[x].CA.V1.Z);

                if (axis == 'x')
                {
                    ABVector = ABVector.rotateDegreesX(angle);
                    BCVector = BCVector.rotateDegreesX(angle);
                    ACVector = ACVector.rotateDegreesX(angle);
                }
                else if (axis == 'y')
                {
                    ABVector = ABVector.rotateDegreesY(angle);
                    BCVector = BCVector.rotateDegreesY(angle);
                    ACVector = ACVector.rotateDegreesY(angle);
                }
                else
                {
                    ABVector = ABVector.rotateDegreesZ(angle);
                    BCVector = BCVector.rotateDegreesZ(angle);
                    ACVector = ACVector.rotateDegreesZ(angle);
                }

                Vertex first = new Vertex(ABVector.X, ABVector.Y, ABVector.Z);
                Vertex second = new Vertex(BCVector.X, BCVector.Y, BCVector.Z);
                Vertex third = new Vertex(ACVector.X, ACVector.Y, ACVector.Z);

                Triangle A = new Triangle(first, second, third);
                rotatedTriangleList.Add(A);

            }
            
            return rotatedTriangleList;
        }

        // Draw the triangles currently in "triangleList."
        // This method simply draws the outline of all triangles.
        // It is labeled as "No Shading" on the UI
        private void drawTriangles()
        {
            System.Collections.IEnumerator TriangleListEnumerator = triangleList.GetEnumerator();
            Graphics g = Graphics.FromImage(fractalMountainBitmap);

            while (TriangleListEnumerator.MoveNext())
            {
                Triangle t = (Triangle)TriangleListEnumerator.Current;

                Edge AB = new Edge(t.AB);

                Vertex A = new Vertex(AB.V1);
                Vertex B = new Vertex(AB.V2);
                Vertex C = new Vertex(t.BC.V2);

                Vector3 OA = new Vector3(A.X, A.Y, A.Z);
                Vector3 OB = new Vector3(B.X, B.Y, B.Z);
                Vector3 OC = new Vector3(C.X, C.Y, C.Z);

                Point A_Screen = camera.toScreen(OA);
                Point B_Screen = camera.toScreen(OB);
                Point C_Screen = camera.toScreen(OC);

                g.DrawPolygon(Pens.Black, new Point[] { A_Screen, B_Screen, C_Screen });
            }


        }//end drawTriangles

        // This method draws a black mountain with shading
        // It is labeled as "Negative Shading" on the UI
        private void negativeShading()
        {
            System.Collections.IEnumerator TriangleListEnumerator = triangleList.GetEnumerator();
            Graphics g = Graphics.FromImage(fractalMountainBitmap);
            int x = -1;

            while (TriangleListEnumerator.MoveNext())
            {
                Triangle t = (Triangle)TriangleListEnumerator.Current;
                x++;
                Vector3 normal = new Vector3();
                double angle = 0.0;

                Vector3 ABVector = new Vector3(triangleList[x].AB.V1.X, triangleList[x].AB.V1.Y, triangleList[x].AB.V1.Z);
                Vector3 BCVector = new Vector3(triangleList[x].BC.V1.X, triangleList[x].BC.V1.Y, triangleList[x].BC.V1.Z);
                Vector3 ACVector = new Vector3(triangleList[x].CA.V1.X, triangleList[x].CA.V1.Y, triangleList[x].CA.V1.Z);

                Vector3 ABdifference = ABVector.subtract(BCVector);
                Vector3 BCdifference = BCVector.subtract(ACVector);

                normal = ABdifference.crossProduct(BCdifference);
                angle = normal.normalize().dotProduct(lightSourceVector.normalize());

                Edge AB = new Edge(t.AB);

                Vertex A = new Vertex(AB.V1);
                Vertex B = new Vertex(AB.V2);
                Vertex C = new Vertex(t.BC.V2);

                Vector3 OA = new Vector3(A.X, A.Y, A.Z);
                Vector3 OB = new Vector3(B.X, B.Y, B.Z);
                Vector3 OC = new Vector3(C.X, C.Y, C.Z);

                Point A_Screen = camera.toScreen(OA);
                Point B_Screen = camera.toScreen(OB);
                Point C_Screen = camera.toScreen(OC);

                SolidBrush brush = new SolidBrush(Color.FromArgb(Convert.ToInt32(-127.5 * angle + 127.5), Convert.ToInt32(-127.5 * angle + 127.5), Convert.ToInt32(-127.5 * angle + 127.5)));
               
                g.FillPolygon(brush, new Point[] { A_Screen, B_Screen, C_Screen });
            }


        }//end negativeShading

        // This method draws a white mountain with shading
        // It is labeled as "Positive Shading" on the UI
        private void positiveShading()
        {
            System.Collections.IEnumerator TriangleListEnumerator = triangleList.GetEnumerator();
            Graphics g = Graphics.FromImage(fractalMountainBitmap);
            int x = -1;

            while (TriangleListEnumerator.MoveNext())
            {
                Triangle t = (Triangle)TriangleListEnumerator.Current;
                x++;
                Vector3 normal = new Vector3();
                double angle = 0.0;

                Vector3 ABVector = new Vector3(triangleList[x].AB.V1.X, triangleList[x].AB.V1.Y, triangleList[x].AB.V1.Z);
                Vector3 BCVector = new Vector3(triangleList[x].BC.V1.X, triangleList[x].BC.V1.Y, triangleList[x].BC.V1.Z);
                Vector3 ACVector = new Vector3(triangleList[x].CA.V1.X, triangleList[x].CA.V1.Y, triangleList[x].CA.V1.Z);

                Vector3 ABdifference = ABVector.subtract(BCVector);
                Vector3 BCdifference = BCVector.subtract(ACVector);

                normal = ABdifference.crossProduct(BCdifference);
                angle = normal.normalize().dotProduct(lightSourceVector.normalize());

                Edge AB = new Edge(t.AB);

                Vertex A = new Vertex(AB.V1);
                Vertex B = new Vertex(AB.V2);
                Vertex C = new Vertex(t.BC.V2);

                Vector3 OA = new Vector3(A.X, A.Y, A.Z);
                Vector3 OB = new Vector3(B.X, B.Y, B.Z);
                Vector3 OC = new Vector3(C.X, C.Y, C.Z);

                Point A_Screen = camera.toScreen(OA);
                Point B_Screen = camera.toScreen(OB);
                Point C_Screen = camera.toScreen(OC);

                SolidBrush brush = new SolidBrush(Color.FromArgb(Convert.ToInt32(127.5 * angle + 127.5), Convert.ToInt32(127.5 * angle + 127.5), Convert.ToInt32(127.5 * angle + 127.5)));

                g.FillPolygon(brush, new Point[] { A_Screen, B_Screen, C_Screen });
            }


        }//end positiveShading

        // This method draws a coloured mountain with shading with an input indicating which colour is 
        // chosen by the user.
        // It is labeled as "Colour Shading" on the UI
        private void colorShading(string colorChosen)
        {
            System.Collections.IEnumerator TriangleListEnumerator = triangleList.GetEnumerator();
            Graphics g = Graphics.FromImage(fractalMountainBitmap);
            int x = -1;

            while (TriangleListEnumerator.MoveNext())
            {
                Triangle t = (Triangle)TriangleListEnumerator.Current;
                x++;
                Vector3 normal = new Vector3();
                double angle = 0.0;

                Vector3 ABVector = new Vector3(triangleList[x].AB.V1.X, triangleList[x].AB.V1.Y, triangleList[x].AB.V1.Z);
                Vector3 BCVector = new Vector3(triangleList[x].BC.V1.X, triangleList[x].BC.V1.Y, triangleList[x].BC.V1.Z);
                Vector3 ACVector = new Vector3(triangleList[x].CA.V1.X, triangleList[x].CA.V1.Y, triangleList[x].CA.V1.Z);

                Vector3 ABdifference = ABVector.subtract(BCVector);
                Vector3 BCdifference = BCVector.subtract(ACVector);

                normal = ABdifference.crossProduct(BCdifference);
                angle = normal.normalize().dotProduct(lightSourceVector.normalize());

                Edge AB = new Edge(t.AB);

                Vertex A = new Vertex(AB.V1);
                Vertex B = new Vertex(AB.V2);
                Vertex C = new Vertex(t.BC.V2);

                Vector3 OA = new Vector3(A.X, A.Y, A.Z);
                Vector3 OB = new Vector3(B.X, B.Y, B.Z);
                Vector3 OC = new Vector3(C.X, C.Y, C.Z);

                Point A_Screen = camera.toScreen(OA);
                Point B_Screen = camera.toScreen(OB);
                Point C_Screen = camera.toScreen(OC);

                SolidBrush brush = new SolidBrush(Color.Black);

                // This conditional statement determines the colour of the brush to later
                // shade the mountain that specific colour
                if (colorChosen == "Red")
                    brush = new SolidBrush(Color.FromArgb(255, Convert.ToInt32(-127.5 * angle + 127.5), Convert.ToInt32(-127.5 * angle + 127.5)));
                else if (colorChosen == "Green")
                    brush = new SolidBrush(Color.FromArgb(Convert.ToInt32(-127.5 * angle + 127.5), 150, Convert.ToInt32(-127.5 * angle + 127.5)));
                else if (colorChosen == "Blue")
                    brush = new SolidBrush(Color.FromArgb(Convert.ToInt32(-127.5 * angle + 127.5), Convert.ToInt32(-127.5 * angle + 127.5), 255));
                else if (colorChosen == "Obsidian")
                    brush = new SolidBrush(Color.FromArgb(51, 0, Convert.ToInt32(-127.5 * angle + 127.5)));

                g.FillPolygon(brush, new Point[] { A_Screen, B_Screen, C_Screen });
            }


        }//end colorShading

        // Transform the 2D co-ordinates of the initial triangle (in "pointDraggingPictureBox") to 3D co-ordinates.
        private Vertex transformTo3D(Point p, int index)
        {
            Vertex pointIn3D = new Vertex(0, 0, 0);

            if (index == A_INDEX) //Point is on the y-axis
            {
                pointIn3D.Y = ((double)(ORIGIN_Y - p.Y)) / (ORIGIN_Y - initialA.Y);
            }

            else if (index == B_INDEX) //Point is on the z-axis
            {
                //Not yet implemented. Use default value of 1 for now.
                pointIn3D.Z = 1;
            }

            else if (index == C_INDEX) //Point is on the x-axis
            {
                pointIn3D.X = ((double)(p.X - ORIGIN_X)) / (initialC.X - ORIGIN_X);
            }

            return pointIn3D;
        }

        #endregion
        
    }// end class
}//end namespace
