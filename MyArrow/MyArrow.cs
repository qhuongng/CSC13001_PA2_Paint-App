using Shapes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using IconKind = MahApps.Metro.IconPacks.PackIconMaterialKind;

namespace MyArrow
{
    public class MyArrow : IShape
    {
        private Point _start;
        private Point _end;

        private bool _isShiftPressed = false;

        private SolidColorBrush _stroke;
        private double _strokeWidth;
        private double[]? _strokeDashArray;

        public IconKind Icon => IconKind.CallMade;

        public double Top => _start.Y;
        public double Left => _start.X;
        public double Bottom => _end.Y;
        public double Right => _end.X;

        public void AddStart(Point point)
        {
            _start = point;
        }

        public void AddEnd(Point point)
        {
            _end = point;
        }

        public void SetShiftState(bool shiftState)
        {
            _isShiftPressed = shiftState;
        }

        public void SetStrokeColor(SolidColorBrush color)
        {
            _stroke = color;
        }

        public void SetFillColor(SolidColorBrush color) { }

        public void SetStrokeWidth(double width)
        {
            _strokeWidth = width;
        }

        public void SetStrokeDashArray(double[] strokeDashArray)
        {
            _strokeDashArray = strokeDashArray;
        }

        public void SetPosition(double top, double left)
        {
            double xDelta = _end.X - _start.X;
            double yDelta = _end.Y - _start.Y;

            _start.X = left;
            _start.Y = top;
            _end.X = left + xDelta;
            _end.Y = top + yDelta;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public UIElement Convert()
        {
            // find the arrow shaft unit vector
            double vx = _end.X - _start.X;
            double vy = _end.Y - _start.Y;
            double dist = (double)Math.Sqrt(vx * vx + vy * vy);
            vx /= dist;
            vy /= dist;

            double wingLength = Math.Max(7, _strokeWidth * 2);

            double ax = wingLength * (-vy - vx);
            double ay = wingLength * (vx - vy);

            Point wing1 = new Point(_end.X + ax, _end.Y + ay);
            Point wing2 = new Point(_end.X - ay, _end.Y + ax);

            // calculate the bounding box of the arrow
            double minX = Math.Min(_start.X, Math.Min(_end.X, Math.Min(wing1.X, wing2.X)));
            double minY = Math.Min(_start.Y, Math.Min(_end.Y, Math.Min(wing1.Y, wing2.Y)));
            double maxX = Math.Max(_start.X, Math.Max(_end.X, Math.Max(wing1.X, wing2.X)));
            double maxY = Math.Max(_start.Y, Math.Max(_end.Y, Math.Max(wing1.Y, wing2.Y)));

            double width = maxX - minX;
            double height = maxY - minY;

            // create the arrow shaft
            Line line = new Line
            {
                X1 = _start.X - minX,
                Y1 = _start.Y - minY,
                X2 = _end.X - minX,
                Y2 = _end.Y - minY,
                Stroke = _stroke,
                StrokeThickness = _strokeWidth
            };

            if (_strokeDashArray != null)
            {
                line.StrokeDashArray = new DoubleCollection(_strokeDashArray);
            }

            // create the arrowhead
            PathFigure arrowHeadPath = new PathFigure();
            arrowHeadPath.StartPoint = new Point(wing1.X - minX, wing1.Y - minY);
            arrowHeadPath.Segments.Add(new LineSegment(new Point(_end.X - minX, _end.Y - minY), true));
            arrowHeadPath.Segments.Add(new LineSegment(new Point(wing2.X - minX, wing2.Y - minY), true));

            PathGeometry arrowGeometry = new PathGeometry();
            arrowGeometry.Figures.Add(arrowHeadPath);

            Path arrowHead = new Path
            {
                Data = arrowGeometry,
                Stroke = _stroke,
                StrokeThickness = _strokeWidth
            };

            // combine arrow line and arrowhead into a single container
            Canvas container = new Canvas
            {
                Width = width,
                Height = height
            };

            container.Children.Add(line);
            container.Children.Add(arrowHead);

            if (_end.X >= _start.X)
            {
                container.SetValue(Canvas.LeftProperty, _start.X);
            }
            else
            {
                container.SetValue(Canvas.LeftProperty, _start.X - width);
            }

            if (_end.Y >= _start.Y)
            {
                container.SetValue(Canvas.TopProperty, _start.Y);
            }
            else
            {
                container.SetValue(Canvas.TopProperty, _start.Y - height);
            }

            return container;
        }
    }
}
