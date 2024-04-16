using Shapes;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Media;
using IconKind = MahApps.Metro.IconPacks.PackIconMaterialKind;

namespace MyHeart
{
    public class MyHeart : IShape
    {
        private Point _topLeft;
        private Point _bottomRight;

        private bool _isShiftPressed = false;

        private SolidColorBrush _stroke;
        private SolidColorBrush _fill;
        private double _strokeWidth;
        private double[]? _strokeDashArray;


        public IconKind Icon => IconKind.HeartOutline;

        public void AddStart(Point point)
        {
            _topLeft = point;
        }

        public void AddEnd(Point point)
        {
            _bottomRight = point;
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
            _fill = color;
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
            // Calculate width and height based on the diagonal distance
            double width = Math.Abs(_bottomRight.X - _topLeft.X);
            double height = Math.Abs(_bottomRight.Y - _topLeft.Y);
            double circleDiameter = Math.Min(width, height);


            if (_isShiftPressed)
            {
                width = circleDiameter;
                height = circleDiameter;
            }

            // Calculate the center point of the heart shape
            Point center = new Point((_topLeft.X + _bottomRight.X) / 2, (_topLeft.Y + _bottomRight.Y) / 2);

            // Create the PathGeometry to define the heart shape
            PathGeometry heartGeometry = new PathGeometry();

            PathFigure heartFigure = new PathFigure();
            heartFigure.StartPoint = new Point(center.X, center.Y + height / 4); // Starting point at the bottom center

            heartFigure.Segments.Add(new LineSegment(new Point(center.X - width/2, center.Y - height / 4), true)); // Draw the left line segment
                                                                                                           // Draw the left arc of the heart
            ArcSegment leftArc = new ArcSegment(
                new Point(center.X, center.Y - height / 2),
                new Size(width / 4, height / 4),
                0,
                false,
                SweepDirection.Clockwise,
                true);

            heartFigure.Segments.Add(leftArc);

            // Draw the right arc of the heart
            ArcSegment rightArc = new ArcSegment(
                new Point(center.X + width/2, center.Y - height / 4),
                new Size(width / 4, height / 4),
                0,
                false,
                SweepDirection.Clockwise,
                true);

            heartFigure.Segments.Add(rightArc);
            heartFigure.Segments.Add(new LineSegment(new Point(center.X, center.Y + height / 4), true)); // Draw the right line segment

            // Add the heart figure to the PathGeometry
            heartGeometry.Figures.Add(heartFigure);

            // Create the Path object to draw the heart shape
            Path heartPath = new Path();
            heartPath.Fill = _fill;
            heartPath.Stroke = _stroke;
            heartPath.StrokeThickness = _strokeWidth;
            heartPath.StrokeDashArray = new DoubleCollection(_strokeDashArray ?? new double[] { });
            heartPath.Data = heartGeometry;

            return heartPath;
        }

    }
}
