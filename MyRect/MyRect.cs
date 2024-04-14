using Shapes;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Shapes;

namespace MyRect
{
    public class MyRect : IShape
    {
        private Point _topLeft;
        private Point _bottomRight;

        private bool _isShiftPressed = false;
        private SolidColorBrush _stroke;
        private double _strokeWidth;

        public string Name => "Rectangle";

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

        public void SetStrokeColor(Color color)
        {
            _stroke = new SolidColorBrush(color);
        }

        public void SetStrokeWidth(double width)
        {
            _strokeWidth = width;
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

            Rectangle e = new Rectangle()
            {
                Stroke = _stroke,
                StrokeThickness = _strokeWidth
            };

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
