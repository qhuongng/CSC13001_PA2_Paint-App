using Shapes;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Icon = MahApps.Metro.IconPacks.PackIconMaterial;

namespace PaintApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            KeyDown += Canvas_KeyDown;
            KeyUp += Canvas_KeyUp;
        }

        bool _isDrawing = false;
        bool _isShiftPressed = false;

        Point _start;
        Point _end;

        List<UIElement> _list = new List<UIElement>();
        List<IShape> _painters = new List<IShape>();
        UIElement _lastElement;
        List<IShape> _prototypes = new List<IShape>();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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

            int k = 0;

            for (int i = 0; i < ShapesBtnGrp.RowDefinitions.Count; i++)
            {
                for (int j = 0; j < ShapesBtnGrp.ColumnDefinitions.Count; j++)
                {
                    if (k == _prototypes.Count)
                    {
                        break;
                    }

                    var item = _prototypes[k];

                    var control = new RadioButton()
                    {
                        Width = 36,
                        Height = 36,
                        Content = new Icon { Kind = item.Icon, Foreground = new SolidColorBrush(Colors.White), Width = 24, Height = 24 },
                        Style = Application.Current.Resources["IconRadioButtonStyle"] as Style,
                        Tag = item,
                    };

                    control.Click += Control_Click;

                    Grid.SetRow(control, i);
                    Grid.SetColumn(control, j);
                    ShapesBtnGrp.Children.Add(control);

                    k++;
                }
            }

            _painter = _prototypes[0];
        }

        private void Canvas_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                _painter.SetShiftState(true);
            }
        }

        private void Canvas_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                _painter.SetShiftState(false);
            }
        }

        private void ColorBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Button b = (Button)sender;

            FillClr.Background = b.Background;
            _painter.SetFillColor((SolidColorBrush)FillClr.Background);
        }

        private void ColorBtn_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;

            StrokeClr.Background = b.Background;
            _painter.SetStrokeColor((SolidColorBrush)StrokeClr.Background);
        }

        private void Control_Click(object sender, RoutedEventArgs e)
        {
            IShape item = (IShape)(sender as RadioButton)!.Tag;
            _painter = item;
            _painter.SetStrokeColor((SolidColorBrush)StrokeClr.Background);
            _painter.SetFillColor((SolidColorBrush)FillClr.Background);
            _painter.SetStrokeWidth(5);
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDrawing = true;
            _start = e.GetPosition(DrawingCanvas);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawing)
            {
                _end = e.GetPosition(DrawingCanvas);
                DrawingCanvas.Children.Clear();

                foreach (var item in _painters)
                {
                    DrawingCanvas.Children.Add(item.Convert());
                }

                _painter.AddStart(_start);
                _painter.AddEnd(_end);

                DrawingCanvas.Children.Add(_painter.Convert());
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDrawing = false;
            _painters.Add((IShape)_painter.Clone());
        }

        IShape _painter = null;
    }
}