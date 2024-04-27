using Shapes;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
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
            MemoryStream stream = new MemoryStream();
            XamlWriter.Save(element, stream);
            stream.Seek(0, SeekOrigin.Begin);
            UIElement clonedElement = (UIElement)XamlReader.Load(stream);
            clonedElement.RenderSize = element.RenderSize;

            Canvas.SetTop(clonedElement,Canvas.GetTop(element));
            Canvas.SetLeft(clonedElement,Canvas.GetLeft(element));

            Element = element;
            ElementName = name;
            ElementIcon = icon;
        }
        public ShapeElementMemento createMemento()
        {
            return new ShapeElementMemento(Element,ElementName,ElementIcon);
        }
        public void restoreFromMemento(ShapeElementMemento memento)
        {
            MemoryStream stream = new MemoryStream();
            XamlWriter.Save(memento.GetElement(), stream);
            stream.Seek(0, SeekOrigin.Begin);
            UIElement clonedElement = (UIElement)XamlReader.Load(stream);
            this.Element = clonedElement;
            this.ElementName = memento.GetElementName();
            this.ElementIcon = memento.GetIcon();

        }
    }
    public class ShapeElementMemento
    {
        private readonly UIElement element;
        private readonly string elementName;
        private readonly IconKind elementIcon;
        public ShapeElementMemento(UIElement ele,string name,IconKind icon)
        {
            // Tạo bản sao của UIElement
            MemoryStream stream = new MemoryStream();
            XamlWriter.Save(ele, stream);
            stream.Seek(0, SeekOrigin.Begin);
            UIElement clonedElement = (UIElement)XamlReader.Load(stream);

            this.element = clonedElement;
            this.elementName = name;
            this.elementIcon = icon;
        }
        public UIElement GetElement()
        {
            return element;
        }
        public string GetElementName()
        {
            return elementName;
        }
        public IconKind GetIcon()
        {
            return elementIcon;
        }
    }
    public class CareTakerShape
    {
        public List<ShapeElementMemento> historyMemento = new List<ShapeElementMemento>();
        public void addMemento(ShapeElementMemento memento) 
        { 
            historyMemento.Add(memento);
        }
        public ShapeElementMemento GetMemento(int index)
        {
            return historyMemento[index];
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
        ShapeElement _latestElement = null;

        Rectangle _selectionBounds = null;

        public CareTakerShape careTaker;
        public int currentPosition = -1;

        public Dictionary<string, int> indexShape = new Dictionary<string, int>
        {
            {"Rectangle", 0},{"Ellipse", 0},{"Line", 0},{"Arrow", 0},
            {"Heart", 0},{"Rounded Rectangle", 0},{"Star", 0},{"Triangle", 0}
        };

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            KeyDown += Canvas_KeyDown;
            KeyUp += Canvas_KeyUp;

            StrokeWidths = new ObservableCollection<double> { 1, 3, 5, 8 };
            StrokeWidth = 1;

            StrokeTypes = new ObservableCollection<BitmapImage> { solid, dash, dash_dot };
            StrokeType = StrokeTypes[0];

            ShapeList = new ObservableCollection<ShapeElement>(); 
            careTaker = new CareTakerShape();
            BtnRedo.IsEnabled = false;
            iconRedo.Foreground = Brushes.Gray;
            BtnUndo.IsEnabled = false;
            iconUndo.Foreground = Brushes.Gray;
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
            if(e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                UndoBtn_Click(null, null);
            }
            if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
            {
                RedoBtn_Click(null, null);
            }
        }

        private void Canvas_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                _painter.SetShiftState(false);
            }
        }

        private void UndoBtn_Click(object sender, RoutedEventArgs e)
        {
            BtnRedo.IsEnabled = true;
            iconRedo.Foreground = Brushes.White;
            if (currentPosition <= 0)
            {
                BtnUndo.IsEnabled = false;
                iconUndo.Foreground = Brushes.Gray;
                ShapeList.Clear();
                DrawingCanvas.Children.Clear();
                currentPosition = -1;
            }
            if (currentPosition > 0)
            {
                string nameShapeBefore = careTaker.GetMemento(currentPosition - 1).GetElementName();
                int countExists = 0;
                for (int i = currentPosition - 1; i >= 0; i--)
                {
                    if (careTaker.GetMemento(i).GetElementName().Equals(careTaker.GetMemento(currentPosition).GetElementName())) countExists++;
                }
                if (countExists == 0) // nếu không thì xóa luôn hình đó
                {
                    string nameShape = careTaker.GetMemento(currentPosition).GetElementName();
                    ShapeList.Remove(ShapeList.FirstOrDefault(x => x.ElementName.Equals(nameShape)));
                }
                else
                {
                    ShapeElement oldShape = ShapeList.FirstOrDefault(x => x.ElementName.Equals(nameShapeBefore));
                    oldShape.restoreFromMemento(careTaker.GetMemento(currentPosition - 1));
                }

                currentPosition--;
                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }

                
            }
        }

        private void RedoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currentPosition < careTaker.historyMemento.Count - 1)
            {
                
                //kiểm tra xem hình sau trong list có chưa, chưa có thì thêm vào
                string shapeNameAfter = careTaker.GetMemento(currentPosition + 1).GetElementName();
                int countExists = 0;
                for (int i = currentPosition; i >= 0; i--)
                {
                    if (careTaker.GetMemento(i).GetElementName().Equals(shapeNameAfter)) countExists++;
                }
                if (countExists == 0)
                {
                    UIElement element = new UIElement();
                    string name = "MyElement";
                    IconKind icon = IconKind.Abacus;
                    ShapeElement newShape = new ShapeElement(element,name,icon);
                    newShape.restoreFromMemento(careTaker.GetMemento(currentPosition + 1));
                    ShapeList.Add(newShape);
                   
                } else
                {
                    ShapeElement newShape  = ShapeList.FirstOrDefault(x => x.ElementName.Equals(shapeNameAfter));
                    newShape.restoreFromMemento(careTaker.GetMemento(currentPosition + 1));
                }
                currentPosition++;
                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }

                if (currentPosition == 0)
                {
                    BtnUndo.IsEnabled = true;
                    iconUndo.Foreground = Brushes.White;
                }
                if (currentPosition == careTaker.historyMemento.Count - 1)
                {
                    BtnRedo.IsEnabled = false;
                    iconRedo.Foreground = Brushes.Gray;
                }
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
                //_latestElement = new ShapeElement(_selectedElement.Element,_selectedElement.ElementName,_selectedElement.ElementIcon);
                updateMemento();
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
                //_latestElement = new ShapeElement(_selectedElement.Element, _selectedElement.ElementName, _selectedElement.ElementIcon);
                updateMemento();
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
            _painter.SetStrokeDashArray(BitmapToDashArray(StrokeType));

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
                int index = indexShape[clone.Name];
                //
                if (index == 0)
                {
                    ShapeElement newShape = new ShapeElement(_visual, clone.Name, clone.Icon);
                    //_lastestElement = new ShapeElement(newShape.Element, newShape.ElementName, newShape.ElementIcon);
                    //updateMemento();
                    indexShape[clone.Name] = 1;
                    ShapeList.Add(newShape);
                } else
                {
                    ShapeElement newShape = new ShapeElement(_visual, clone.Name + " " + index.ToString(), clone.Icon);
                   // _lastestElement = new ShapeElement(newShape.Element, newShape.ElementName, newShape.ElementIcon);
                    //updateMemento();
                    indexShape[clone.Name] += 1;
                    ShapeList.Add(newShape);
                }
                SelectionPane.SelectedItem = ShapeList.Last();
                _selectedElement = ShapeList.Last();
                updateMemento();
                //SetSelected();
            }
        }

        private void StrokeWidthCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox strokeWidthCb = (ComboBox)sender;

            if (strokeWidthCb.SelectedItem != null)
            {
                StrokeWidth = (double)strokeWidthCb.SelectedItem;

                if (_painter != null)
                {
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
                    //_latestElement = new ShapeElement(_selectedElement.Element, _selectedElement.ElementName, _selectedElement.ElementIcon);
                    updateMemento();
                    DrawingCanvas.Children.Clear();

                    foreach (var shape in ShapeList)
                    {
                        DrawingCanvas.Children.Add(shape.Element);
                    }

                    SetSelected();
                }
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
                    _painter.SetStrokeDashArray(BitmapToDashArray(StrokeType));
                }

                if (_selectedElement != null)
                {
                    DoubleCollection newParam = new DoubleCollection(0);

                    if (BitmapToDashArray(StrokeType) != null)
                    {
                        newParam = new DoubleCollection(BitmapToDashArray(StrokeType));
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
                    //_latestElement = new ShapeElement(_selectedElement.Element, _selectedElement.ElementName, _selectedElement.ElementIcon);
                    updateMemento();
                    DrawingCanvas.Children.Clear();

                    foreach (var shape in ShapeList)
                    {
                        DrawingCanvas.Children.Add(shape.Element);
                    }

                    SetSelected();
                }
            }
        }

        private double[] BitmapToDashArray(BitmapImage image)
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

        private void DashArrayToBitmap(DoubleCollection array)
        {
            double[] converted = array.Cast<double>().ToArray();

            if (Enumerable.SequenceEqual(converted, new double[] { 5, 2 }))
            {
                StrokeTypeCb.SelectedItem = StrokeTypes[1];
            }
            else if (Enumerable.SequenceEqual(converted, new double[] { 5, 2, 1, 2 }))
            {
                StrokeTypeCb.SelectedItem = StrokeTypes[2];
            }
            else
            {
                StrokeTypeCb.SelectedItem = StrokeTypes[0];
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
            //_latestElement = new ShapeElement(_selectedElement.Element, _selectedElement.ElementName, _selectedElement.ElementIcon);
            updateMemento();
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

                if (_selectedElement.Element is Shape)
                {
                    FillClr.Background = (_selectedElement.Element as Shape).Fill;
                    StrokeClr.Background = (_selectedElement.Element as Shape).Stroke;
                    StrokeWidthCb.SelectedItem = (_selectedElement.Element as Shape).StrokeThickness;

                    DashArrayToBitmap((_selectedElement.Element as Shape).StrokeDashArray);

                }
                else if (_selectedElement.Element is Grid)
                {
                    foreach (UIElement child in (_selectedElement.Element as Grid).Children)
                    {
                        if (child is System.Windows.Shapes.Path)
                        {
                            System.Windows.Shapes.Path path = child as System.Windows.Shapes.Path;

                            FillClr.Background = path.Fill;
                            StrokeClr.Background = path.Stroke;
                            StrokeWidthCb.SelectedItem = path.StrokeThickness;

                            DashArrayToBitmap(path.StrokeDashArray);
                        }
                        else if (child is Shape)
                        {
                            FillClr.Background = (child as Shape).Fill;
                            StrokeClr.Background = (child as Shape).Stroke;
                            StrokeWidthCb.SelectedItem = (child as Shape).StrokeThickness;

                            DashArrayToBitmap((child as Shape).StrokeDashArray);
                        }
                    }
                }

                // activate the move tool by default
                BtnMove.IsChecked = true;
                MoveBtn_Click(null, null);
            }
        }
        private void updateMemento()
        {
            if (currentPosition < careTaker.historyMemento.Count - 1)
            {
                for (int i = currentPosition + 1; i < careTaker.historyMemento.Count; i++)
                {
                    careTaker.historyMemento.Remove(careTaker.GetMemento(i));
                }
            }
            careTaker.addMemento(_selectedElement.createMemento());
            currentPosition++;
            if (BtnRedo.IsEnabled == true)
            {
                BtnRedo.IsEnabled = false;
                iconRedo.Foreground = Brushes.Gray;
            }
            if (currentPosition == 0)
            {
                BtnUndo.IsEnabled = true;
                iconUndo.Foreground = Brushes.White;
            }
        }
    }
}