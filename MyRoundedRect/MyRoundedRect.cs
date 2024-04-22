using Shapes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using IconKind = MahApps.Metro.IconPacks.PackIconMaterialKind;

namespace MyRoundedRect
{
    public class MyRoundedRect : IShape
    {
        private Point _topLeft;
        private Point _bottomRight;

        private bool _isShiftPressed = false;

        private SolidColorBrush _stroke;
        private SolidColorBrush _fill;
        private double _strokeWidth;
        private double[]? _strokeDashArray;

        public IconKind Icon => IconKind.SquareRoundedOutline;
        public string Name => "Rounded Rectangle";

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
            double width = Math.Abs(_bottomRight.X - _topLeft.X);
            double height = Math.Abs(_bottomRight.Y - _topLeft.Y);
            double circleDiameter = Math.Min(width, height);

            Rectangle r = new Rectangle()
            {
                Fill = _fill,
                Stroke = _stroke,
                StrokeThickness = _strokeWidth,
                RadiusX = 12,
                RadiusY = 12
            };

            if (_strokeDashArray != null)
            {
                r.StrokeDashArray = new DoubleCollection(_strokeDashArray);
            }

            if (_isShiftPressed)
            {
                // draw a square
                r.Width = circleDiameter;
                r.Height = circleDiameter;
            }
            else
            {
                // draw a rectangle
                r.Width = width;
                r.Height = height;
            }

            if (_bottomRight.X >= _topLeft.X)
            {
                r.SetValue(Canvas.LeftProperty, _topLeft.X);
            }
            else
            {
                if (_isShiftPressed)
                    r.SetValue(Canvas.LeftProperty, _topLeft.X - circleDiameter);
                else
                    r.SetValue(Canvas.LeftProperty, _topLeft.X - width);
            }

            if (_bottomRight.Y >= _topLeft.Y)
            {
                r.SetValue(Canvas.TopProperty, _topLeft.Y);
            }
            else
            {
                if (_isShiftPressed)
                    r.SetValue(Canvas.TopProperty, _topLeft.Y - circleDiameter);
                else
                    r.SetValue(Canvas.TopProperty, _topLeft.Y - height);
            }

            return r;
        }
    }
}
