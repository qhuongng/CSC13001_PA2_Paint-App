﻿using Shapes;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Icon = MahApps.Metro.IconPacks.PackIconMaterial;
using IconKind = MahApps.Metro.IconPacks.PackIconMaterialKind;

namespace PaintApp
{
    public class ShapeElement
    {
        public UIElement Element { get; set; }
        public string ElementName { get; set; }
        public IconKind ElementIcon { get; set; }

        public ShapeElement(UIElement element, string name, IconKind icon)
        {
            Element = element;
            ElementName = name;
            ElementIcon = icon;
        }
    }

    public partial class MainWindow : Window
    {
        bool _isDrawing = false;
        bool _isDragging = false;

        Point _start;
        Point _end;
        Point _dragStart;

        List<IShape> _prototypes = new List<IShape>();

        IShape _painter = null;
        UIElement _visual = null;

        BitmapImage solid = new BitmapImage(new Uri("pack://application:,,,/lines/solid.png"));
        BitmapImage dash = new BitmapImage(new Uri("pack://application:,,,/lines/dash.png"));
        BitmapImage dash_dot = new BitmapImage(new Uri("pack://application:,,,/lines/dash_dot.png"));

        public ObservableCollection<double> StrokeWidths { get; set; }
        public double StrokeWidth { get; set; }

        public ObservableCollection<BitmapImage> StrokeTypes { get; set; }
        public BitmapImage StrokeType { get; set; }

        public ObservableCollection<ShapeElement> ShapeList { get; set; }

        ShapeElement _selectedElement = null;
        ShapeElement _prevSelectedElement = null;

        Rectangle _selectionBounds = null;
        

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            KeyDown += Canvas_KeyDown;
            KeyUp += Canvas_KeyUp;

            StrokeWidths = new ObservableCollection<double> { 1, 2, 4, 8, 10, 12, 16, 20, 24, 32 };
            StrokeWidth = 2;

            StrokeTypes = new ObservableCollection<BitmapImage> { solid, dash, dash_dot };
            StrokeType = StrokeTypes[0];

            ShapeList = new ObservableCollection<ShapeElement>();
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

                    control.Click += ShapeBtn_Click;

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

        // change fill color on right-click
        private void ColorBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Button b = (Button)sender;

            FillClr.Background = b.Background;

            if (_painter != null)
            {
                _painter.SetFillColor((SolidColorBrush)FillClr.Background);
            }

            if (_selectedElement != null)
            {
                if (_selectedElement.Element is Shape)
                {
                    (_selectedElement.Element as Shape).Fill = FillClr.Background;
                }
                else if (_selectedElement.Element is Grid)
                {
                    // custom shapes consists of Path(s) wrapped inside a Grid
                    // find all Paths that are child of the grid and modify their colors
                    foreach (UIElement child in (_selectedElement.Element as Grid).Children)
                    {
                        if (child is System.Windows.Shapes.Path)
                        {
                            System.Windows.Shapes.Path path = child as System.Windows.Shapes.Path;
                            path.Fill = FillClr.Background;
                        }
                        else if (child is Shape)
                        {
                            (child as Shape).Fill = FillClr.Background;
                        }
                    }
                }

                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }

                SetSelected();
            }
        }

        // change stroke color on left-click
        private void ColorBtn_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;

            StrokeClr.Background = b.Background;

            if (_painter != null)
            {
                _painter.SetStrokeColor((SolidColorBrush)StrokeClr.Background);
            }

            if (_selectedElement != null)
            {
                if (_selectedElement.Element is Shape)
                {
                    (_selectedElement.Element as Shape).Stroke = StrokeClr.Background;
                }
                else if (_selectedElement.Element is Grid) {
                    foreach (UIElement child in (_selectedElement.Element as Grid).Children)
                    {
                        if (child is System.Windows.Shapes.Path)
                        {
                            System.Windows.Shapes.Path path = child as System.Windows.Shapes.Path;
                            path.Stroke = StrokeClr.Background;
                        }
                        else if (child is Shape)
                        {
                            (child as Shape).Stroke = StrokeClr.Background;
                        }
                    }
                }

                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }

                SetSelected();
            }
        }

        private void ShapeBtn_Click(object sender, RoutedEventArgs e)
        {
            IShape item = (IShape)(sender as RadioButton)!.Tag;

            _painter = item;
            _painter.SetStrokeColor((SolidColorBrush)StrokeClr.Background);
            _painter.SetFillColor((SolidColorBrush)FillClr.Background);
            _painter.SetStrokeWidth(StrokeWidth);
            _painter.SetStrokeDashArray(TransferStrokeDashArray(StrokeType));

            DrawingCanvas.Children.Remove(_selectionBounds);
            SelectionPane.UnselectAll();
            SetSelected();

            CanvasHelper.Visibility = Visibility.Visible;
        }

        private void MoveBtn_Click(object sender, RoutedEventArgs e)
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

                DrawingCanvas.Children.Remove(_selectionBounds);
                SelectionPane.UnselectAll();
                SetSelected();
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_painter != null && _isDrawing)
            {
                _end = e.GetPosition(DrawingCanvas);
                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }

                _painter.AddStart(_start);
                _painter.AddEnd(_end);

                _visual = _painter.Convert();
                DrawingCanvas.Children.Add(_visual);
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_painter != null)
            {
                _isDrawing = false;

                IShape clone = (IShape)_painter.Clone();
                ShapeElement newShape = new ShapeElement(_visual, clone.Name, clone.Icon );

                ShapeList.Add(newShape);
            }
        }

        private void StrokeWidthCb_TextChanged(object sender, TextChangedEventArgs e)
        {
            double testInput;

            if (_painter != null && StrokeWidthCb.Text.Length > 0 && double.TryParse(StrokeWidthCb.Text, out testInput))
            {
                double width = double.Parse(StrokeWidthCb.Text);

                StrokeWidth = width;
                _painter.SetStrokeWidth(StrokeWidth);
            }

            if (_selectedElement != null)
            {
                if (_selectedElement.Element is Shape)
                {
                    (_selectedElement.Element as Shape).StrokeThickness = StrokeWidth;
                }
                else if (_selectedElement.Element is Grid)
                {
                    // custom shapes consists of Path(s) wrapped inside a Grid
                    // find all Paths that are child of the grid and modify their colors
                    foreach (UIElement child in (_selectedElement.Element as Grid).Children)
                    {
                        if (child is System.Windows.Shapes.Path)
                        {
                            System.Windows.Shapes.Path path = child as System.Windows.Shapes.Path;
                            path.StrokeThickness = StrokeWidth;
                        }
                        else if (child is Shape)
                        {
                            (child as Shape).StrokeThickness = StrokeWidth;
                        }
                    }
                }

                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }

                SetSelected();
            }
        }

        private void StrokeTypeCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox strokeTypeCb = (ComboBox)sender;

            if (strokeTypeCb.SelectedItem != null)
            {
                StrokeType = (BitmapImage)strokeTypeCb.SelectedItem;

                if (_painter != null)
                {
                    _painter.SetStrokeDashArray(TransferStrokeDashArray(StrokeType));
                }

                if (_selectedElement != null)
                {
                    DoubleCollection newParam = new DoubleCollection(0);

                    if (TransferStrokeDashArray(StrokeType) != null)
                    {
                        newParam = new DoubleCollection(TransferStrokeDashArray(StrokeType));
                    }

                    if (_selectedElement.Element is Shape)
                    {
                        (_selectedElement.Element as Shape).StrokeDashArray = newParam;
                    }
                    else if (_selectedElement.Element is Grid)
                    {
                        // custom shapes consists of Path(s) wrapped inside a Grid
                        // find all Paths that are child of the grid and modify their colors
                        foreach (UIElement child in (_selectedElement.Element as Grid).Children)
                        {
                            if (child is System.Windows.Shapes.Path)
                            {
                                System.Windows.Shapes.Path path = child as System.Windows.Shapes.Path;
                                path.StrokeDashArray = newParam;
                            }
                            else if (child is Shape)
                            {
                                (child as Shape).StrokeDashArray = newParam;
                            }
                        }
                    }

                    DrawingCanvas.Children.Clear();

                    foreach (var shape in ShapeList)
                    {
                        DrawingCanvas.Children.Add(shape.Element);
                    }

                    SetSelected();
                }
            }
        }

        private double[] TransferStrokeDashArray(BitmapImage image)
        {
            Uri uri = image.UriSource;
            string fileName = System.IO.Path.GetFileName(uri.LocalPath);

            if (fileName.Equals("solid.png"))
            {
                return null;
            }
            else if (fileName.Equals("dash.png"))
            {
                return [5, 2];
            }
            else
            {
                return [5, 2, 1, 2];
            }
        }

        private void Element_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragStart = Mouse.GetPosition(DrawingCanvas);

            DrawingCanvas.Children.Remove(_selectionBounds);

            (sender as UIElement).CaptureMouse();
        }

        private void Element_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                UIElement draggedElement = sender as UIElement;

                Point newPoint = Mouse.GetPosition(DrawingCanvas);

                double left = Canvas.GetLeft(draggedElement);
                double top = Canvas.GetTop(draggedElement);

                Canvas.SetLeft(_selectedElement.Element, left + (newPoint.X - _dragStart.X));
                Canvas.SetTop(_selectedElement.Element, top + (newPoint.Y - _dragStart.Y));

                _dragStart = newPoint;
            }
        }

        private void Element_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            _dragStart = new Point();

            BoundSelectedElement();

            (sender as UIElement).ReleaseMouseCapture();
        }

        private void BoundSelectedElement()
        {
            if (_selectedElement == null)
            {
                return;
            }

            Size elemSize = _selectedElement.Element.RenderSize;

            // get the position of the element relative to its parent container
            Point elemLoc = _selectedElement.Element.TranslatePoint(new Point(0, 0), DrawingCanvas);

            _selectionBounds = new Rectangle
            {
                Width = elemSize.Width + 2,
                Height = elemSize.Height + 2,
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection([4, 2])
            };

            // set the position of the selection bounds based on the translated coordinates
            Canvas.SetLeft(_selectionBounds, elemLoc.X - 1);
            Canvas.SetTop(_selectionBounds, elemLoc.Y - 1);

            DrawingCanvas.Children.Add(_selectionBounds);
        }

        private void SelectionPane_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetSelected();
        }

        private void SetSelected()
        {
            _prevSelectedElement = _selectedElement;

            if (_prevSelectedElement != null)
            {
                _prevSelectedElement.Element.MouseDown -= Element_MouseDown;
                _prevSelectedElement.Element.MouseMove -= Element_MouseMove;
                _prevSelectedElement.Element.MouseUp -= Element_MouseUp;
            }

            _selectedElement = (ShapeElement)SelectionPane.SelectedItem;

            if (_selectedElement != null)
            {
                _selectedElement.Element.MouseDown += Element_MouseDown;
                _selectedElement.Element.MouseMove += Element_MouseMove;
                _selectedElement.Element.MouseUp += Element_MouseUp;

                DrawingCanvas.Children.Remove(_selectionBounds);
                BoundSelectedElement();

                // activate the move tool by default
                BtnMove.IsChecked = true;
                MoveBtn_Click(null, null);
            }
        }
    }
}