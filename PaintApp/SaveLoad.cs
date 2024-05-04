using Newtonsoft.Json;
using Shapes;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using IconKind = MahApps.Metro.IconPacks.PackIconMaterialKind;

namespace PaintApp
{
    public class DataShape
    {
        public IShape Shape { get; set; }
        public string NameShape { get; set; }
        public IconKind ElementIcon { get; set; }
        public SolidColorBrush FillColor {  get; set; }
        public SolidColorBrush StrokeColor { get; set; }
        public double StrokeThickness { get; set; }
        public double[] StrokeDashArray { get; set; }
        public Point StartPoint {  get; set; }
        public Point EndPoint { get; set; }
        public string TextTB { get; set; }
        public FontFamily FontFamilyTB { get; set; }
        public double FontSizeTB { get; set; }
        public FontStyle FontStyleTB { get; set; }
        public FontWeight FontWeightTB { get; set; }
        public TextDecorationCollection TextDecorationTB { get; set; }
        public TextAlignment TextAlignmentTB { get; set; }

        [JsonConstructor]
        public DataShape(IShape shape, string nameShape, IconKind elementIcon, SolidColorBrush fillColor, SolidColorBrush strokeColor, double strokeThickness, double[] strokeDashArray, Point startPoint, Point endPoint, string textTB, FontFamily fontFamilyTB, double fontSizeTB, FontStyle fontStyleTB, FontWeight fontWeightTB, TextDecorationCollection textDecorationTB, TextAlignment textAlignmentTB)
        {
            Shape = shape;
            NameShape = nameShape;
            ElementIcon = elementIcon;
            FillColor = fillColor;
            StrokeColor = strokeColor;
            StrokeThickness = strokeThickness;
            StrokeDashArray = strokeDashArray;
            StartPoint = startPoint;
            EndPoint = endPoint;
            TextTB = textTB;
            FontFamilyTB = fontFamilyTB;
            FontSizeTB = fontSizeTB;
            FontStyleTB = fontStyleTB;
            FontWeightTB = fontWeightTB;
            TextDecorationTB = textDecorationTB;
            TextAlignmentTB = textAlignmentTB;
        }

        public DataShape(UIElement uIElement, string nameShape, IconKind icon) 
        {
            string[] name = nameShape.Split(' ');
            string style = "";

            TextBlock textBlock = new TextBlock();

            List<IShape> _prototypes = new List<IShape>();

            if (name.Length == 2)
            {
                if (name[0].Equals("Rounded"))
                {
                    style =  name[0] + ' ' + name[1];
                }
                else
                {
                    style = name[0];
                }
            }
            else if (name.Length == 1)
            {
                style = name[0];
            }
            else
            {
                style = name[0] + ' ' + name[1];
            }

            NameShape = nameShape;
            ElementIcon = icon;

            string folder = AppDomain.CurrentDomain.BaseDirectory;
            var fis = new DirectoryInfo(folder).GetFiles("*.dll");

            foreach (var fi in fis)
            {
                var assembly = Assembly.LoadFrom(fi.FullName);
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    if (type.IsClass && typeof(IShape).IsAssignableFrom(type))
                    {
                        _prototypes.Add((IShape)Activator.CreateInstance(type)!);
                    }
                }
            }

            Shape = _prototypes.FirstOrDefault(x => x.Name.Equals(style));

            if (uIElement is Shape)
            {
                FillColor = (SolidColorBrush)(uIElement as Shape).Fill;
                StrokeColor = (SolidColorBrush)(uIElement as Shape).Stroke;
                StrokeThickness = (uIElement as Shape).StrokeThickness;
                StrokeDashArray = (uIElement as Shape).StrokeDashArray.ToArray();
            }
            else if (uIElement is Grid)
            {
                foreach (UIElement child in (uIElement as Grid).Children)
                {
                    if (child is System.Windows.Shapes.Path)
                    {
                        System.Windows.Shapes.Path path = child as System.Windows.Shapes.Path;

                        FillColor = (SolidColorBrush)path.Fill;
                        StrokeColor = (SolidColorBrush)path.Stroke;
                        StrokeThickness = path.StrokeThickness;
                        StrokeDashArray = path.StrokeDashArray.ToArray();
                    }
                    else if (child is Shape)
                    {
                        FillColor = (SolidColorBrush)(child as Shape).Fill;
                        StrokeColor = (SolidColorBrush)(child as Shape).Stroke;
                        StrokeThickness = (child as Shape).StrokeThickness;
                        StrokeDashArray = (child as Shape).StrokeDashArray.ToArray();
                    }
                }
            }

            StartPoint = new Point((double)uIElement.GetValue(Canvas.LeftProperty), (double)uIElement.GetValue(Canvas.TopProperty));
            EndPoint = new Point((double)uIElement.GetValue(Canvas.LeftProperty) + (double)uIElement.GetValue(Canvas.ActualWidthProperty), (double)uIElement.GetValue(Canvas.TopProperty) + (double)uIElement.GetValue(Canvas.ActualHeightProperty));
            
            foreach (UIElement child in ((Grid)uIElement).Children)
            {
                if (child is TextBlock)
                {
                    textBlock = child as TextBlock;
                }
            }

            if (textBlock != null)
            {
                FontFamilyTB = textBlock.FontFamily;
                TextTB = textBlock.Text;
                FontSizeTB = textBlock.FontSize;
                FontStyleTB = textBlock.FontStyle;
                FontWeightTB = textBlock.FontWeight;
                TextDecorationTB = textBlock.TextDecorations;
                TextAlignmentTB = textBlock.TextAlignment;
            }
        }
        public ShapeElement Convert()
        {
            Shape.SetFillColor(FillColor);
            Shape.SetStrokeColor(StrokeColor);
            Shape.SetStrokeDashArray(StrokeDashArray);
            Shape.SetStrokeWidth(StrokeThickness);
            Shape.AddStart(StartPoint);
            Shape.AddEnd(EndPoint);

            ShapeElement element = new ShapeElement(Shape.Convert(),NameShape,ElementIcon);
            TextBlock target = null;

            foreach (UIElement child in ((Grid)element.Element).Children)
            {
                if (child is TextBlock)
                {
                    target = child as TextBlock;
                }
            }

            if (target != null)
            {
                target.Text = TextTB;
                target.TextAlignment = TextAlignmentTB;
                target.FontFamily = FontFamilyTB;
                target.FontSize = FontSizeTB;
                target.FontStyle = FontStyleTB;
                target.FontWeight = FontWeightTB;
                target.TextDecorations = TextDecorationTB;
            }

            return element;
        }
    }

    public class SaveLoad
    {
        public Dictionary<string, ObservableCollection<DataShape>> Save(ObservableCollection<Layer> layers)
        {
            Dictionary<string, ObservableCollection<DataShape>> dataLayer = new Dictionary<string, ObservableCollection<DataShape>>();

            foreach (Layer layer in layers)
            {
                ObservableCollection<DataShape> datas = new ObservableCollection<DataShape>(); 

                foreach (ShapeElement element in layer.ShapeList)
                {
                    datas.Add(new DataShape(element.Element, element.ElementName, element.ElementIcon));
                }

                dataLayer.Add(layer.LayerName, datas);
            }

            return dataLayer;
        }

        public ObservableCollection<Layer> Load(Dictionary<string, ObservableCollection<DataShape>> data, MainWindow window)
        {
            ObservableCollection<Layer> layers = new ObservableCollection<Layer>();

            foreach(var item in  data)
            {
                int index = int.Parse(item.Key.Split(' ')[1]);

                Layer layer = new Layer(window, index);
                ObservableCollection<ShapeElement> shapeElements = new ObservableCollection<ShapeElement>();

                foreach (var element in item.Value)
                {
                    ShapeElement uIElement = element.Convert();
                    shapeElements.Add(uIElement);

                    layer.DrawingCanvas.Children.Add(uIElement.Element);
                }

                layer.SetShapeList(shapeElements);
                layers.Add(layer);
            }

            return layers;
        }
    }
}
