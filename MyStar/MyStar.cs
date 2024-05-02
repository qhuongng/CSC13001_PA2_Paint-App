using Shapes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using IconKind = MahApps.Metro.IconPacks.PackIconMaterialKind;

namespace MyStar
{
    public class MyStar : IShape
    {
        private Point _topLeft;
        private Point _bottomRight;
        private bool _isShiftPressed = false;

        private SolidColorBrush _stroke;
        private SolidColorBrush _fill;
        private double _strokeWidth;
        private double[]? _strokeDashArray;
        public IconKind Icon => IconKind.StarOutline;
        public string Name => "Star";

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

            // recalculate radiusX and radiusY based on radius
            double radiusX;
            double radiusY;

            if (_isShiftPressed)
            {
                radiusX = circleDiameter / 2;
                radiusY = circleDiameter / 2;
            }
            else
            {
                radiusX = width / 2;
                radiusY = height / 2;
            }

            // calculate center point
            Point center = new Point((_topLeft.X + _bottomRight.X) / 2, (_topLeft.Y + _bottomRight.Y) / 2);

            // calculate vertices of the star
            List<Point> starVertices = new List<Point>();

            double angle = -Math.PI / 2;
            double deltaAngle = Math.PI / 5;

            for (int i = 0; i < 5; i++)
            {
                double x = center.X + radiusX * Math.Cos(angle);
                double y = center.Y + radiusY * Math.Sin(angle);

                starVertices.Add(new Point(x, y));
                angle += deltaAngle;

                x = center.X + (radiusX / 2) * Math.Cos(angle);
                y = center.Y + (radiusY / 2) * Math.Sin(angle);

                starVertices.Add(new Point(x, y));
                angle += deltaAngle;
            }

            // find the minimum and maximum X and Y coordinates of the star vertices
            double minX = starVertices.Min(p => p.X) - _strokeWidth * 1.5;
            double maxX = starVertices.Max(p => p.X) + _strokeWidth * 1.5;
            double minY = starVertices.Min(p => p.Y) - _strokeWidth;
            double maxY = starVertices.Max(p => p.Y) + _strokeWidth;

            // create a PathGeometry to hold the star shape
            PathGeometry starGeometry = new PathGeometry();
            PathFigure starFigure = new PathFigure();
            starFigure.StartPoint = new Point(starVertices[0].X - minX, starVertices[0].Y - minY);

            // add line segments connecting the star vertices
            for (int i = 1; i < starVertices.Count; i++)
            {
                starFigure.Segments.Add(new LineSegment(new Point(starVertices[i].X - minX, starVertices[i].Y - minY), true));
            }

            // close the star by connecting the last point to the start point
            starFigure.IsClosed = true;
            starGeometry.Figures.Add(starFigure);

            // add the starPath to the containerGrid
            Path starPath = new Path();
            starPath.Fill = _fill;
            starPath.Stroke = _stroke;
            starPath.StrokeThickness = _strokeWidth;
            starPath.StrokeLineJoin = PenLineJoin.Round;
            starPath.StrokeDashArray = new DoubleCollection(_strokeDashArray ?? new double[] { });
            starPath.Data = starGeometry;

            // calculate the bounding box dimensions
            double boundingWidth = maxX - minX;
            double boundingHeight = maxY - minY;

            TextBlock tb = new TextBlock();

            tb.Width = width / 3;
            tb.Height = height / 3;
            tb.TextWrapping = TextWrapping.Wrap;
            tb.FontSize = 16;
            tb.Foreground = Brushes.Black;
            tb.HorizontalAlignment = HorizontalAlignment.Center;
            tb.VerticalAlignment = VerticalAlignment.Center;
            tb.Margin = new Thickness(0);

            // create a container Grid to hold the star
            Grid container = new Grid();

            container.Width = boundingWidth;
            container.Height = boundingHeight;
            container.Children.Add(starPath);
            container.Children.Add(tb);

            // Set the position of the containerGrid
            Canvas.SetLeft(container, minX);
            Canvas.SetTop(container, minY);

            return container;
        }

    }
}
