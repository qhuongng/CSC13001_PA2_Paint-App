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

        public void SetPosition(double top, double left)
        {
            double xDelta = _bottomRight.X - _topLeft.X;
            double yDelta = _bottomRight.Y - _topLeft.Y;

            _topLeft.X = left;
            _topLeft.Y = top;
            _bottomRight.X = left + xDelta;
            _bottomRight.Y = top + yDelta;
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

            Ellipse e = new Ellipse()
            {
                Fill = _fill,
                Stroke = _stroke,
                StrokeThickness = _strokeWidth,
            };
            if (_strokeDashArray != null)
            {
                e.StrokeDashArray = new DoubleCollection(_strokeDashArray);
            }

            if (_isShiftPressed)
            {
                // draw a circle
                e.Width = circleDiameter;
                e.Height = circleDiameter;
            }
            else
            {
                // draw an ellipse
                e.Width = width;
                e.Height = height;
            }

            if (_bottomRight.X >= _topLeft.X)
            {
                e.SetValue(Canvas.LeftProperty, _topLeft.X);
            }
            else
            {
                if (_isShiftPressed)
                    e.SetValue(Canvas.LeftProperty, _topLeft.X - circleDiameter);
                else
                    e.SetValue(Canvas.LeftProperty, _topLeft.X - width);
            }

            if (_bottomRight.Y >= _topLeft.Y)
            {
                e.SetValue(Canvas.TopProperty, _topLeft.Y);
            }
            else
            {
                if (_isShiftPressed)
                    e.SetValue(Canvas.TopProperty, _topLeft.Y - circleDiameter);
                else
                    e.SetValue(Canvas.TopProperty, _topLeft.Y - height);
            }

            return e;
        }
    }
}
