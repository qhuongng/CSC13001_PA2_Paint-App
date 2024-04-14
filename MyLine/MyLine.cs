using Shapes;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Media;
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

        public object Clone()
        {
            return MemberwiseClone();
        }

        public UIElement Convert()
        {
            return new Line()
            {
                X1 = _start.X,
                Y1 = _start.Y,
                X2 = _end.X,
                Y2 = _end.Y,
                StrokeThickness = _strokeWidth,
                Stroke = _stroke
            };
        }
    }

}

