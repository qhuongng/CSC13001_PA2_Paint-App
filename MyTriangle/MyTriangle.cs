using Shapes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using IconKind = MahApps.Metro.IconPacks.PackIconMaterialKind;

namespace MyTriangle
{
    public class MyTriangle : IShape
    {
        private Point _topLeft;
        private Point _bottomRight;
        private bool _isShiftPressed = false;

        private SolidColorBrush _stroke;
        private SolidColorBrush _fill;
        private double _strokeWidth;
        private double[]? _strokeDashArray;
        public IconKind Icon => IconKind.TriangleOutline;
        public string Name => "Triangle";

        public double Top => _topLeft.Y;
        public double Left => _topLeft.X;
        public double Bottom => _bottomRight.Y;
        public double Right => _bottomRight.X;

        public void AddEnd(Point point)
        {
            _bottomRight = point;
        }

        public void AddStart(Point point)
        {
            _topLeft = point;
        }

        public void SetFillColor(SolidColorBrush color)
        {
            _fill = color;
        }

        public void SetShiftState(bool shiftState)
        {
            _isShiftPressed = shiftState;
        }

        public void SetStrokeColor(SolidColorBrush color)
        {
            _stroke = color;
        }

        public void SetStrokeWidth(double width)
        {
            _strokeWidth = width;
        }

        public void SetStrokeDashArray(double[] strokeDashArray)
        {
            _strokeDashArray = strokeDashArray;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public UIElement Convert()
        {
            // calculate radius based on diagonal distance
            double width = Math.Abs(_bottomRight.X - _topLeft.X);
            double height = Math.Abs(_bottomRight.Y - _topLeft.Y);
            double circleDiameter = Math.Min(width, height);

            if (_isShiftPressed)
            {
                width = circleDiameter;
                height = circleDiameter;
            }

            Polygon triangle = new Polygon();

            // set the fill color
            triangle.Fill = _fill;

            // set the stroke color and width
            triangle.Stroke = _stroke;
            triangle.StrokeThickness = _strokeWidth;

            if (_strokeDashArray != null)
            {
                triangle.StrokeDashArray = new DoubleCollection(_strokeDashArray);
            }

            PointCollection points = new PointCollection();

            if (_topLeft.X >= _bottomRight.X)
            {
                if (_topLeft.Y >= _bottomRight.Y)
                {
                    points.Add(new Point(_topLeft.X - width / 2, _bottomRight.Y)); // Top point
                    points.Add(new Point(_topLeft.X, _topLeft.Y)); // Bottom left point
                    points.Add(new Point(_bottomRight.X, _topLeft.Y)); // Bottom right point
                }
                else
                {
                    points.Add(new Point(_topLeft.X - width / 2, _topLeft.Y)); // Top point
                    points.Add(new Point(_topLeft.X, _bottomRight.Y)); // Bottom left point
                    points.Add(new Point(_bottomRight.X, _bottomRight.Y)); // Bottom right point
                }
            }
            else
            {
                if (_topLeft.Y >= _bottomRight.Y)
                {
                    points.Add(new Point(_topLeft.X + width / 2, _bottomRight.Y)); // Top point
                    points.Add(new Point(_topLeft.X, _topLeft.Y)); // Bottom left point
                    points.Add(new Point(_bottomRight.X, _topLeft.Y)); // Bottom right point
                }
                else
                {
                    points.Add(new Point(_topLeft.X + width / 2, _topLeft.Y)); // Top point
                    points.Add(new Point(_topLeft.X, _bottomRight.Y)); // Bottom left point
                    points.Add(new Point(_bottomRight.X, _bottomRight.Y)); // Bottom right point
                }
            }

            double minX = points.Min(p => p.X);
            double maxX = points.Max(p => p.X);
            double minY = points.Min(p => p.Y);
            double maxY = points.Max(p => p.Y);

            double boundingWidth = maxX - minX;
            double boundingHeight = maxY - minY;

            foreach (Point i in points)
            {
                triangle.Points.Add(new Point(i.X - minX, i.Y - minY));
            }

            Grid container = new Grid();

            container.Width = boundingWidth;
            container.Height = boundingHeight;
            container.Children.Add(triangle);

            // set the position of the containerGrid
            Canvas.SetLeft(container, minX);
            Canvas.SetTop(container, minY);

            return container;
        }

    }
}
