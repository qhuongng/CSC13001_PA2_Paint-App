using MahApps.Metro.IconPacks;
using Shapes;
using System.Windows;
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

        public void AddEnd(Point point)
        {
            _bottomRight = point;
        }

        public void AddStart(Point point)
        {
            _topLeft = point;
        }

        public object Clone()
        {
            return MemberwiseClone();
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

        public void SetStrokeDashArray(double[] strokeDashArray)
        {
            _strokeDashArray = strokeDashArray;
        }

        public void SetStrokeWidth(double width)
        {
            _strokeWidth = width;
        }
        public UIElement Convert()
        {
            // Calculate radius based on diagonal distance
            double width = Math.Abs(_bottomRight.X - _topLeft.X);
            double height = Math.Abs(_bottomRight.Y - _topLeft.Y);
            double circleDiameter = Math.Min(width, height);

            // Recalculate radiusX and radiusY based on radius
            double radiusX;
            double radiusY;

            if(_isShiftPressed)
            {
                radiusX = circleDiameter / 2;
                radiusY = circleDiameter / 2;
            } else
            {
                radiusX = width / 2;
                radiusY = height / 2;
            }


            // Calculate center point
            Point center;
            if(_topLeft.X >= _bottomRight.X)
            {
                if(_topLeft.Y >= _bottomRight.Y)
                {
                    center = new Point(_topLeft.X - radiusX, _topLeft.Y - radiusY);
                }
                else
                {
                    center = new Point(_topLeft.X - radiusX, _topLeft.Y + radiusY);
                }
            } else
            {
                if(_topLeft.Y >= _bottomRight.Y)
                {
                    center = new Point(_topLeft.X + radiusX, _topLeft.Y - radiusY);
                } else
                {
                    center = new Point(_topLeft.X + radiusX, _topLeft.Y + radiusY);
                }
            }
            

            // Vẽ hình ngôi sao
            PathGeometry starGeometry = new PathGeometry();
            PathFigure starFigure = new PathFigure();
            starFigure.StartPoint = new Point(center.X, center.Y - radiusY); // Start from the top point
            double angle = -Math.PI / 2;
            double deltaAngle = Math.PI / 5;
            for (int i = 0; i < 5; i++)
            {
                double x = center.X + radiusX * Math.Cos(angle);
                double y = center.Y + radiusY * Math.Sin(angle);
                starFigure.Segments.Add(new LineSegment(new Point(x, y), true));
                angle += deltaAngle;

                x = center.X + (radiusX / 2) * Math.Cos(angle);
                y = center.Y + (radiusY / 2) * Math.Sin(angle);

                starFigure.Segments.Add(new LineSegment(new Point(x, y), true));
                angle += deltaAngle;
            }
            // Close the star by connecting the last point to the start point
            starFigure.Segments.Add(new LineSegment(starFigure.StartPoint, true));
            starGeometry.Figures.Add(starFigure);

            // Tạo đối tượng Path để vẽ hình ngôi sao
            Path starPath = new Path();
            starPath.Fill = _fill;
            starPath.Stroke = _stroke;
            starPath.StrokeThickness = _strokeWidth;
            starPath.StrokeDashArray = new DoubleCollection(_strokeDashArray ?? new double[] { });
            starPath.Data = starGeometry;

            return starPath;
        }

    }
}
