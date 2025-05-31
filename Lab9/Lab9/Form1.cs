using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using ScottPlot;

namespace Lab9
{
    public partial class Form1 : Form
    {
        string projectDirectory = Directory.GetParent(Application.StartupPath).Parent.Parent.Parent.Parent.Parent.FullName;

        public Form1()
        {
            InitializeComponent();
            CalculatePrecisionThresholds();
            PlotDependency();
        }

        private void CalculatePrecisionThresholds()
        {
            float x1 = 100, y1 = 100;
            float x2 = 1000, y2 = 800;
            float step = 1.0f;

            using (var writer = new StreamWriter(Path.Combine(projectDirectory, "result\\data.txt")))
            {
                for(double radius = 0.5; radius <= 10;  radius += 0.5) 
                {
                    var result = FindMinPrecision(x1, y1, x2, y2, step, radius);
                    writer.WriteLine($"{radius:F2}\t{result.minN}");
                }
            }
        }

        private (int minN, double finalDistance, int iterations) FindMinPrecision(
            float x1, float y1, float x2, float y2, float step, double targetRadius)
        {
            int minN = 1;
            int maxN = 30;
            int foundN = maxN;
            double finalDistance = 0;
            int totalIterations = 0;

            while (minN <= maxN)
            {
                int currentN = (minN + maxN) / 2;
                var result = SimulateWithPrecision(x1, y1, x2, y2, step, targetRadius, currentN);
                totalIterations += result.iterations;

                if (result.hitTarget)
                {
                    foundN = currentN;
                    finalDistance = result.finalDistance;
                    maxN = currentN - 1;
                }
                else
                {
                    minN = currentN + 1;
                }
            }

            return (foundN, finalDistance, totalIterations);
        }

        private (bool hitTarget, int iterations, double finalDistance) SimulateWithPrecision(
            float x1, float y1, float x2, float y2, float step, double targetRadius, int n)
        {
            double angle = CustomAtan2(y2 - y1, x2 - x1, n);
            double cos = CustomCos(angle, n);
            double sin = CustomSin(angle, n);

            double x = x1;
            double y = y1;
            int iterations = 0;
            double distance = CalculateDistance(x, y, x2, y2);

            while (distance > targetRadius && iterations < 10000)
            {
                x += step * cos;
                y += step * sin;
                distance = CalculateDistance(x, y, x2, y2);
                iterations++;
            }

            return (distance <= targetRadius, iterations, distance);
        }

        private double CustomSin(double angle, int n)
        {
            double result = 0;
            for (int i = 0; i < n; i++)
            {
                int power = 2 * i + 1;
                double term = Math.Pow(angle, power) / Factorial(power);
                result += (i % 2 == 0) ? term : -term;
            }
            return result;
        }

        private double CustomCos(double angle, int n)
        {
            double result = 0;
            for (int i = 0; i < n; i++)
            {
                int power = 2 * i;
                double term = Math.Pow(angle, power) / Factorial(power);
                result += (i % 2 == 0) ? term : -term;
            }
            return result;
        }

        private double CustomAtan2(double y, double x, int n)
        {
            if (x == 0) return y > 0 ? Math.PI / 2 : -Math.PI / 2;

            double atan = CustomAtan(y / x, n);
            if (x > 0) return atan;
            return y >= 0 ? atan + Math.PI : atan - Math.PI;
        }

        private double CustomAtan(double x, int n)
        {
            x = Math.Min(1, Math.Max(-1, x)); // Ограничиваем x для сходимости ряда
            double result = 0;
            for (int i = 0; i < n; i++)
            {
                int power = 2 * i + 1;
                double term = Math.Pow(x, power) / power;
                result += (i % 2 == 0) ? term : -term;
            }
            return result;
        }

        private long Factorial(int n)
        {
            long result = 1;
            for (int i = 2; i <= n; i++)
                result *= i;
            return result;
        }

        private double CalculateDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        private void PlotDependency()
        {
            var dataLines = File.ReadAllLines(Path.Combine(projectDirectory, "result\\data.txt"))
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split('\t'))
                .Where(parts => parts.Length >= 2)
                .Select(parts => new {
                    N = int.Parse(parts[1]),
                    Radius = double.Parse(parts[0]),
                })
                .ToList();

            double[] radii = dataLines.Select(d => d.Radius).ToArray();
            double[] nValues = dataLines.Select(d => (double)d.N).ToArray();

            var plt = new Plot();
            plt.Title("Зависимость количества членов ряда от точности");

            var scatter = plt.Add.Scatter(radii, nValues);
            scatter.LineWidth = 2;
            scatter.MarkerSize = 5;
            scatter.Color = ScottPlot.Colors.Blue;

            plt.XLabel("Радиус зоны попадания");
            plt.YLabel("Количество членов ряда (n)");

            string savePath = Path.Combine(projectDirectory, "result\\plot.png");
            plt.SavePng(savePath, 800, 600);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.ScaleTransform(0.5f, 0.5f);

            e.Graphics.FillEllipse(Brushes.Red, 100, 100, 10, 10);
            e.Graphics.FillEllipse(Brushes.Green, 1000, 800, 10, 10);

            DrawTrajectory(e.Graphics, 3, System.Drawing.Color.Blue);
            DrawTrajectory(e.Graphics, 5, System.Drawing.Color.Purple);
            DrawTrajectory(e.Graphics, 10, System.Drawing.Color.Orange);
        }

        private void DrawTrajectory(Graphics g, int n, System.Drawing.Color color)
        {
            float x1 = 100, y1 = 100;
            float x2 = 1000, y2 = 800;
            float step = 1.0f;
            float targetRadius = 4.0f;

            double angle = CustomAtan2(y2 - y1, x2 - x1, n);
            double cos = CustomCos(angle, n);
            double sin = CustomSin(angle, n);

            double x = x1;
            double y = y1;
            double distance = CalculateDistance(x, y, x2, y2);

            var pen = new Pen(color, 2);
            PointF prevPoint = new PointF((float)x, (float)y);

            while (distance > targetRadius)
            {
                x += step * cos;
                y += step * sin;
                distance = CalculateDistance(x, y, x2, y2);

                PointF currentPoint = new PointF((float)x, (float)y);
                g.DrawLine(pen, prevPoint, currentPoint);
                prevPoint = currentPoint;

                if (CalculateDistance(x, y, x1, y1) > (CalculateDistance(x1, y1, x2, y2) + 1000))
                    break;
            }
        }
    }
}
