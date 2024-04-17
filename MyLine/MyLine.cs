using Shapes;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using IconKind = MahApps.Metro.IconPacks.PackIconMaterialKind;

namespace MyLine
{
    public class MyLine : IShape
    {
        private Point _start;
        private Point _end;

        private bool _isShiftPressed = false;

        private SolidColorBrush _stroke;
        private SolidColorBrush _fill;
        private double _strokeWidth;
        private double[]? _strokeDashArray;

        public IconKind Icon => IconKind.VectorLine;

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
            Line l = new Line()
            {
                X1 = _start.X,
                Y1 = _start.Y,
                X2 = _end.X,
                Y2 = _end.Y,
                StrokeThickness = _strokeWidth,
                Stroke = _stroke,
            };

            if (_strokeDashArray != null)
            {
                l.StrokeDashArray = new DoubleCollection(_strokeDashArray);
            }

            return l;
        }
    }

}

