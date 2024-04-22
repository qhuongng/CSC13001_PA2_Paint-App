using System.Windows;
using System.Windows.Media;
using IconKind = MahApps.Metro.IconPacks.PackIconMaterialKind;

namespace Shapes
{
    public interface IShape : ICloneable
    {
        void AddStart(Point point);
        void AddEnd(Point point);

        void SetShiftState(bool shiftState);
        void SetStrokeColor(SolidColorBrush color);
        void SetFillColor(SolidColorBrush color);
        void SetStrokeWidth(double width);
        void SetStrokeDashArray(double[] strokeDashArray);

        UIElement Convert();

        IconKind Icon { get; }
        string Name { get; }
        
        double Top { get; }
        double Left { get; }
        double Bottom { get; }
        double Right { get; }
    }
}
