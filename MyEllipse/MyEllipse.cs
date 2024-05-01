using Shapes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using IconKind = MahApps.Metro.IconPacks.PackIconMaterialKind;

namespace MyEllipse
{
    public class MyEllipse : IShape
    {
        private Point _topLeft;
        private Point _bottomRight;

        private bool _isShiftPressed = false;

        private SolidColorBrush _stroke;
        private SolidColorBrush _fill;
        private double _strokeWidth;
        private double[]? _strokeDashArray;


        public IconKind Icon => IconKind.EllipseOutline;
        public string Name => "Ellipse";

        public double Top => _topLeft.Y;
        public double Left => _topLeft.X;
        public double Bottom => _bottomRight.Y;
        public double Right => _bottomRight.X;

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
            Point start = new Point(Math.Min(_topLeft.X, _bottomRight.X), Math.Min(_topLeft.Y, _bottomRight.Y));
            Point end = new Point(Math.Max(_topLeft.X, _bottomRight.X), Math.Max(_topLeft.Y, _bottomRight.Y));

            double width = Math.Abs(end.X - start.X);
            double height = Math.Abs(end.Y - start.Y);

            Point center = new Point(start.X + width / 2, start.Y + height / 2);

            double minX = center.X - width / 2 - _strokeWidth / 2;
            double maxX = center.X + width / 2 + _strokeWidth / 2;
            double minY = center.Y - height / 2 - _strokeWidth / 2;
            double maxY = center.Y + height / 2 + _strokeWidth / 2;

            double shiftWidth = Math.Min(width, height);

            if (_isShiftPressed)
            {
                width = shiftWidth;
                height = shiftWidth;
            }

            Ellipse e = new Ellipse()
            {
                Fill = _fill,
                Stroke = _stroke,
                StrokeThickness = _strokeWidth,
                Width = width,
                Height = height,
            };

            if (_strokeDashArray != null)
            {
                e.StrokeDashArray = new DoubleCollection(_strokeDashArray);
            }

            TextBlock tb = new TextBlock();

            tb.Width = width / 1.25;
            tb.Height = height / 1.75;
            tb.TextWrapping = TextWrapping.Wrap;
            tb.FontSize = 16;
            tb.Foreground = Brushes.Black;
            tb.HorizontalAlignment = HorizontalAlignment.Center;
            tb.VerticalAlignment = VerticalAlignment.Center;

            // create a container Grid to hold the ellipse
            Grid container = new Grid();

            container.Width = maxX - minX;
            container.Height = maxY - minY;
            container.Children.Add(e);
            container.Children.Add(tb);

            Canvas.SetLeft(container, minX);
            Canvas.SetTop(container, minY);

            return container;
        }
    }
}
