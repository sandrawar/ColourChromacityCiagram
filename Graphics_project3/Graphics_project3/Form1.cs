using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Graphics_project3
{
    public partial class Form1 : Form
    {
        private List<PointF> controlPoints;
        private List<PointF> bezierPoints;
        private Bitmap backgroundBitmap1;
        private Bitmap backgroundBitmap2;
        private bool showBackground1 = true;
        private bool showPoint = false;

        private int bezierDegree = 3;
        private bool movingPoint = false;
        private int movingIndex = -1;

        private Dictionary<double, List<double>> wavelengthDictionary;

        public Form1()
        {
            InitializeComponent();
            controlPoints = new List<PointF>();
            bezierPoints = new List<PointF>();

            LoadBitmaps();
            InitializeEvents();
            wavelengthDictionary = LoadDataToDictionary("WL.txt");
        }

        private void LoadBitmaps()
        {
            try
            {
                backgroundBitmap2 = new Bitmap("map.png");
                backgroundBitmap1 = new Bitmap("uklad.png");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nie udało się załadować bitmap: " + ex.Message);
            }
        }

        private void InitializeEvents()
        {
            pictureBox1.Paint += PictureBox1_Paint;
            pictureBox2.Paint += PictureBox2_Paint;
            pictureBox1.MouseDown += PictureBox1_MouseDown;
            pictureBox1.MouseMove += PictureBox1_MouseMove;
            pictureBox1.MouseUp += PictureBox1_MouseUp;

            textBox1.TextChanged += TextBox1_TextChanged;

            checkBoxBlackDots.CheckedChanged += (s, e) =>
            {
                showPoint = checkBoxBlackDots.Checked;
                pictureBox2.Invalidate();
            };
        }

        private void PictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (showBackground1 && backgroundBitmap1 != null)
            {
                g.DrawImage(backgroundBitmap1, new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height));
            }
            else
            {
                g.Clear(Color.White);
            }

            foreach (var point in controlPoints)
            {
                g.FillEllipse(Brushes.Blue, point.X - 5, point.Y - 5, 10, 10);
            }

            if (controlPoints.Count > 1)
            {
                DrawBezierCurve(g, controlPoints.ToArray(), bezierPoints, 100);
            }
        }

        private void PictureBox2_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (backgroundBitmap2 != null)
            {
                g.DrawImage(backgroundBitmap2, new Rectangle(0, 0, pictureBox2.Width, pictureBox2.Height));
            }
            else
            {
                g.Clear(Color.White);
            }

            if (controlPoints.Count > 1 && showPoint)
            {
                PointF chromaticity = CalculateChromaticity(controlPoints.ToArray());
                float scaledX = Math.Clamp(chromaticity.X, 0, 1) * pictureBox2.Width;
                float scaledY = (1 - Math.Clamp(chromaticity.Y, 0, 1)) * pictureBox2.Height;

                g.FillEllipse(Brushes.Black, scaledX + 75, scaledY - 75, 10, 10);
                string coordinates = $"({chromaticity.X:F5}, {chromaticity.Y:F5})";
                g.DrawString(coordinates, new Font("Arial", 8), Brushes.Black, scaledX + 50, scaledY - 50);

            }
        }


        static Dictionary<double, List<double>> LoadDataToDictionary(string filePath)
        {
            var dictionary = new Dictionary<double, List<double>>();

            foreach (var line in File.ReadLines(filePath))
            {
                var parts = line.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                    continue;

                if (double.TryParse(parts[0], out double wavelength))
                {
                    var coefficients = new List<double>();

                    for (int i = 1; i < parts.Length; i++)
                    {
                        coefficients.Add(Double.Parse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture));

                    }

                    dictionary[wavelength] = coefficients;
                }
            }

            return dictionary;
        }
        private PointF CalculateChromaticity(PointF[] controlPoints)
        {
            double X = 0, Y = 0, Z = 0;
            double wavelengthMin = 380.0;
            double wavelengthMax = 780.0;

            foreach (PointF bezierPoint in bezierPoints)
            {
                double wavelength = Math.Round(wavelengthMin + ((bezierPoint.X - 50) / pictureBox1.Width) * (wavelengthMax - wavelengthMin));

                double intensity = 1.8f * (1 - (bezierPoint.Y - 50) / pictureBox1.Height);

                if (intensity < 0) intensity = 0;
                if (wavelengthDictionary.ContainsKey(wavelength))
                {
                    var coefficients = wavelengthDictionary[wavelength];
                    X += coefficients[0] * intensity;
                    Y += coefficients[1] * intensity;
                    Z += coefficients[2] * intensity;
                }
            }


            double sum = X + Y + Z;
            if (sum == 0) return PointF.Empty;

            float x = (float)(X / sum);
            float y = (float)(Y / sum);


            return new PointF(x, y);
        }

        private void DrawBezierCurve(Graphics g, PointF[] points, List<PointF> bezierPoints, int steps)
        {
            bezierPoints.Clear();
            int n = points.Length - 1; 
            if (n < 1) return;

            PointF prevPoint = points[0];

            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                PointF currPoint = GetBezierPoint(points, t);
                bezierPoints.Add(currPoint);

                g.DrawLine(Pens.Black, prevPoint, currPoint);
                prevPoint = currPoint;
            }
        }

        private PointF GetBezierPoint(PointF[] points, float t)
        {
            int n = points.Length;
            PointF[] temp = new PointF[n];
            Array.Copy(points, temp, n);

            for (int r = 1; r < n; r++)
            {
                for (int i = 0; i < n - r; i++)
                {
                    temp[i].X = (1 - t) * temp[i].X + t * temp[i + 1].X;
                    temp[i].Y = (1 - t) * temp[i].Y + t * temp[i + 1].Y;
                }
            }
            return temp[0];
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(textBox1.Text, out int newDegree) && newDegree >= 1)
            {
                bezierDegree = newDegree;
                pictureBox1.Invalidate();
            }
        }

        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < controlPoints.Count; i++)
            {
                if (Math.Abs(e.X - controlPoints[i].X) < 5 && Math.Abs(e.Y - controlPoints[i].Y) < 5)
                {
                    movingPoint = true;
                    movingIndex = i;
                    return;
                }
            }

            if (controlPoints.Count < bezierDegree + 1)
            {
                controlPoints.Add(new PointF(e.X, e.Y));
                pictureBox1.Invalidate();
            }
        }

        private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (movingPoint && movingIndex >= 0)
            {
                controlPoints[movingIndex] = new PointF(e.X, e.Y);
                pictureBox1.Invalidate();
            }
        }

        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            movingPoint = false;
            movingIndex = -1;
            pictureBox2.Invalidate();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            controlPoints = new List<PointF>();
            bezierPoints = new List<PointF>();
            pictureBox2.Invalidate();
            pictureBox1.Invalidate();
            checkBoxBlackDots.Checked = false;
        }
    }
}
