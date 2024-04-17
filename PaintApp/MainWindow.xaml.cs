using Shapes;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Icon = MahApps.Metro.IconPacks.PackIconMaterial;

namespace PaintApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool _isDrawing = false;

        Point _start;
        Point _end;

        List<UIElement> _list = new List<UIElement>();
        List<IShape> _prototypes = new List<IShape>();

        IShape _painter = null;

        BitmapImage solid = new BitmapImage(new Uri("pack://application:,,,/lines/solid.png"));
        BitmapImage dash = new BitmapImage(new Uri("pack://application:,,,/lines/dash.png"));
        BitmapImage dash_dot = new BitmapImage(new Uri("pack://application:,,,/lines/dash_dot.png"));

        public ObservableCollection<double> StrokeWidths { get; set; }
        public double StrokeWidth { get; set; }

        public ObservableCollection<BitmapImage> StrokeTypes { get; set; }
        public BitmapImage StrokeType { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            KeyDown += Canvas_KeyDown;
            KeyUp += Canvas_KeyUp;

            StrokeWidths = new ObservableCollection<double> { 1, 2, 4, 8, 10, 12, 16, 20, 24, 32 };
            StrokeWidth = 2;
            
            
            StrokeTypes = new ObservableCollection<BitmapImage> {solid,dash,dash_dot};
            StrokeType = StrokeTypes[0];
        }

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
                        GroupName = "CtrlBtn",
                        Tag = item,
                    };

                    control.Click += Control_Click;

                    Grid.SetRow(control, i);
                    Grid.SetColumn(control, j);
                    ShapesBtnGrp.Children.Add(control);

                    k++;
                }
            }
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

            if (_painter != null)
            {
                _painter.SetFillColor((SolidColorBrush)FillClr.Background);
            }
        }

        private void ColorBtn_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;

            StrokeClr.Background = b.Background;

            if (_painter != null)
            {
                _painter.SetStrokeColor((SolidColorBrush)StrokeClr.Background);
            }
        }

        private void Control_Click(object sender, RoutedEventArgs e)
        {
            IShape item = (IShape)(sender as RadioButton)!.Tag;

            _painter = item;
            _painter.SetStrokeColor((SolidColorBrush)StrokeClr.Background);
            _painter.SetFillColor((SolidColorBrush)FillClr.Background);
            _painter.SetStrokeWidth(StrokeWidth);
            _painter.SetStrokeDashArray(transferStrokeDashArray(StrokeType));

            CanvasHelper.Visibility = Visibility.Visible;
        }

        private void FirstBtnGrp_Click(object sender, RoutedEventArgs e)
        {
            _painter = null;

            CanvasHelper.Visibility = Visibility.Collapsed;
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_painter != null)
            {
                _isDrawing = true;
                _start = e.GetPosition(DrawingCanvas);
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_painter != null && _isDrawing)
            {
                _end = e.GetPosition(DrawingCanvas);
                DrawingCanvas.Children.Clear();

                foreach (var item in DrawingCanvas.Objects)
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
            if (_painter != null)
            {
                _isDrawing = false;
                DrawingCanvas.Objects.Add((IShape)_painter.Clone());
            }
        }

        private void StrokeWidthCb_TextChanged(object sender, TextChangedEventArgs e)
        {
            double testInput;

            if (_painter != null && StrokeWidthCb.Text.Length > 0 && double.TryParse(StrokeWidthCb.Text, out testInput)) {
                double width = double.Parse(StrokeWidthCb.Text);

                StrokeWidth = width;
                _painter.SetStrokeWidth(StrokeWidth);
            }
        }

        private void StrokeTypeCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            if (comboBox.SelectedItem != null)
            {
                StrokeType = (BitmapImage)comboBox.SelectedItem;

                if (_painter != null)
                {
                    _painter.SetStrokeDashArray(transferStrokeDashArray(StrokeType));
                }
            }
        }

        private double[] transferStrokeDashArray(BitmapImage image)
        {
            Uri uri = image.UriSource;
            string fileName = Path.GetFileName(uri.LocalPath);

            if (fileName.Equals("solid.png"))
            {
                return null;
            }
            else if (fileName.Equals("dash.png"))
            {
                return new double[] { 5, 2 };
            }
            else
            {
                return new double[] { 5, 2, 1, 2 };
            }
        }
    }
}