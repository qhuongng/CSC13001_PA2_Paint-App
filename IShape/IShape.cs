using System.Windows;
using System.Windows.Media;

namespace Shapes
{
    public interface IShape : ICloneable
    {
        void AddStart(Point point);
        void AddEnd(Point point);

        void SetShiftState(bool shiftState);
        void SetStrokeColor(Color color);
        void SetStrokeWidth(double width);

        UIElement Convert();

        string Name { get; }
    }
}
