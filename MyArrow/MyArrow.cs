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

        public void SetFillColor(SolidColorBrush color)
        {
            // Arrows typically don't have a fill color
            // You can ignore this method or throw an exception if it's called
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
            // find the arrow shaft unit vector.
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

            // create the arrow shaft
            Line line = new Line
            {
                X1 = _start.X,
                Y1 = _start.Y,
                X2 = _end.X,
                Y2 = _end.Y,
                Stroke = _stroke,
                StrokeThickness = _strokeWidth
            };

            // create the arrowhead
            PathFigure arrowHeadPath = new PathFigure();
            arrowHeadPath.StartPoint = wing1;
            arrowHeadPath.Segments.Add(new LineSegment(_end, true));
            arrowHeadPath.Segments.Add(new LineSegment(wing2, true));

            PathGeometry arrowGeometry = new PathGeometry();
            arrowGeometry.Figures.Add(arrowHeadPath);

            Path arrowHead = new Path
            {
                Data = arrowGeometry,
                Stroke = _stroke,
                StrokeThickness = _strokeWidth
            };

            // combine arrow line and arrowhead into a single container
            Canvas container = new Canvas();
            container.Children.Add(line);
            container.Children.Add(arrowHead);

            if (_strokeDashArray != null)
            {
                line.StrokeDashArray = new DoubleCollection(_strokeDashArray);
            }

            return container;
        }
    }
}
