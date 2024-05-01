using Shapes;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
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
        public string Name => "Line";

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
            double width = Math.Abs(_end.X - _start.X);
            double height = Math.Abs(_end.Y - _start.Y);

            Point center = new Point(_start.X + width / 2, _start.Y + height / 2);

            double minX = center.X - width / 2 - _strokeWidth / 2;
            double maxX = center.X + width / 2 + _strokeWidth / 2;
            double minY = center.Y - height / 2 - _strokeWidth / 2;
            double maxY = center.Y + height / 2 + _strokeWidth / 2;

            Line l = new Line()
            {
                X1 = _start.X - minX,
                Y1 = _start.Y - minY,
                X2 = _end.X - minX,
                Y2 = _end.Y - minY,
                StrokeThickness = _strokeWidth,
                Stroke = _stroke,
                StrokeLineJoin = PenLineJoin.Round
            };

            if (_strokeDashArray != null)
            {
                l.StrokeDashArray = new DoubleCollection(_strokeDashArray);
            }

            Grid container = new Grid();

            container.Width = maxX - minX;
            container.Height = maxY - minY;
            container.Children.Add(l);

            Canvas.SetLeft(container, minX);
            Canvas.SetTop(container, minY);

            return container;
        }
    }

}

