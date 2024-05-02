﻿using Shapes;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Xml.Linq;
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

            Canvas.SetTop(clonedElement, Canvas.GetTop(element));
            Canvas.SetLeft(clonedElement, Canvas.GetLeft(element));

            Element = element;
            ElementName = name;
            ElementIcon = icon;
        }

        public ShapeElementMemento createMemento()
        {
            return new ShapeElementMemento(Element, ElementName, ElementIcon);
        }

        public void restoreFromMemento(ShapeElementMemento memento)
        {
            MemoryStream stream = new MemoryStream();

            XamlWriter.Save(memento.GetElement(), stream);
            stream.Seek(0, SeekOrigin.Begin);

            UIElement clonedElement = (UIElement)XamlReader.Load(stream);
            Canvas.SetTop(clonedElement, Canvas.GetTop(memento.GetElement()));
            Canvas.SetLeft(clonedElement, Canvas.GetLeft(memento.GetElement()));
          
            Element = clonedElement;
            ElementName = memento.GetElementName();
            ElementIcon = memento.GetIcon();
        }
    }

    public class ShapeElementMemento
    {
        private readonly UIElement _element;
        private readonly string _elementName;
        private readonly IconKind _elementIcon;

        public ShapeElementMemento(UIElement ele, string name, IconKind icon)
        {
            // Tạo bản sao của UIElement
            MemoryStream stream = new MemoryStream();

            XamlWriter.Save(ele, stream);
            stream.Seek(0, SeekOrigin.Begin);

            UIElement clonedElement = (UIElement)XamlReader.Load(stream);
          
            Canvas.SetTop(clonedElement,Canvas.GetTop(ele));
            Canvas.SetLeft(clonedElement,Canvas.GetLeft(ele));
          
            _element = clonedElement;
            _elementName = name;
            _elementIcon = icon;
        }

        public UIElement GetElement()
        {
            return _element;
        }

        public string GetElementName()
        {
            return _elementName;
        }

        public IconKind GetIcon()
        {
            return _elementIcon;
        }
    }

    public class CareTakerShape
    {
        public List<ShapeElementMemento> HistoryMemento = new List<ShapeElementMemento>();
        public Dictionary<int, string> RemoveMemento = new Dictionary<int, string>();
      
        public void AddMemento(ShapeElementMemento memento) 
        { 
            HistoryMemento.Add(memento);
        }

        public ShapeElementMemento GetMemento(int index)
        {
            return HistoryMemento[index];
        }
        public void RemoveElement(int index, string ElementName)
        {
            // Lưu trữ memento
            RemoveMemento.Add(index,ElementName);
        }
        public string GetRemoveElement(int index)
        {
            return RemoveMemento[index];
        }
    }

    public partial class MainWindow : Window
    {
        bool _isDrawing = false;
        bool _isDragging = false;
        bool _justEditedText = false;
        bool _isSelectArea = false;

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

        public ObservableCollection<double> FontSizes { get; set; }

        public ObservableCollection<ShapeElement> ShapeList { get; set; }

        ShapeElement _selectedElement = null;
        ShapeElement _prevSelectedElement = null;
        ShapeElement _copyElement = null;

        Rectangle _selectionBounds = null;

        public CareTakerShape CareTaker;
        public int CurrentPosition = -1;

        public int RotateCorner = 0;
        public int FlipHorizontal = 1;
        public int FlipVertical = 1;

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

            FontSizes = new ObservableCollection<double> { 6, 8, 10, 12, 16, 20, 24, 28, 32, 36 };

            StrokeTypes = new ObservableCollection<BitmapImage> { solid, dash, dash_dot };
            StrokeType = StrokeTypes[0];

            ShapeList = new ObservableCollection<ShapeElement>();
            CareTaker = new CareTakerShape();

            BtnRedo.IsEnabled = false;
            iconRedo.Foreground = Brushes.Gray;

            BtnUndo.IsEnabled = false;
            iconUndo.Foreground = Brushes.Gray;

            FontCb.ItemsSource = Fonts.SystemFontFamilies;
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
                        Style = System.Windows.Application.Current.Resources["IconRadioButtonStyle"] as Style,
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
            if (_painter != null && (e.Key == Key.LeftShift || e.Key == Key.RightShift))
            {
                _painter.SetShiftState(true);
            }

            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                UndoBtn_Click(null, null);
            }

            if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
            {
                RedoBtn_Click(null, null);
            }

            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Copy_Click(null, null);
            }

            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Paste_Click(null, null);
            }

            if (e.Key == Key.X && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Cut_Click(null, null);
            }
        }

        private void Canvas_KeyUp(object sender, KeyEventArgs e)
        {
            if (_painter != null && (e.Key == Key.LeftShift || e.Key == Key.RightShift))
            {
                _painter.SetShiftState(false);
            }
        }

        private void UndoBtn_Click(object sender, RoutedEventArgs e)
        {
            // xử lí logic về hiện hoặc tắt button undo redo
            BtnRedo.IsEnabled = true;
            iconRedo.Foreground = Brushes.White;

            if (CurrentPosition <= 0)
            {
                BtnUndo.IsEnabled = false;
                iconUndo.Foreground = Brushes.Gray;

                ShapeList.Clear();
                DrawingCanvas.Children.Clear();

                CurrentPosition = -1;
            }

            if (CurrentPosition > 0)
            {
                string nameShapeBefore = CareTaker.GetMemento(CurrentPosition - 1).GetElementName();
                string nameShapeCurrent = CareTaker.GetMemento(CurrentPosition).GetElementName();
                
                /// TH nếu vị trí hiện tại là một hình bị xóa
                if (nameShapeCurrent.Equals("cut"))
                {
                    if (nameShapeBefore.Equals("cut"))
                    {
                        CurrentPosition--;
                    }

                    string elementNameBefore = CareTaker.GetRemoveElement(CurrentPosition);
                    ShapeElement oldShape = new ShapeElement(new UIElement(),"cutElement",IconKind.None);
                    
                    oldShape.restoreFromMemento(CareTaker.HistoryMemento.LastOrDefault(x => x.GetElementName().Equals(elementNameBefore)));
                    ShapeList.Add(oldShape);

                    CurrentPosition--;

                    DrawingCanvas.Children.Clear();

                    foreach (var shape in ShapeList)
                    {
                        DrawingCanvas.Children.Add(shape.Element);
                    }
                }
                //TH bình thường
                else
                {
                    int countExists = 0;

                    for (int i = CurrentPosition - 1; i >= 0; i--)
                    {
                        if (CareTaker.GetMemento(i).GetElementName().Equals(CareTaker.GetMemento(CurrentPosition).GetElementName()))
                        {
                            countExists++;
                        }
                    }

                    if (countExists == 0) // nếu không thì xóa luôn hình đó
                    {
                        string nameShape = CareTaker.GetMemento(CurrentPosition).GetElementName();
                        ShapeList.Remove(ShapeList.FirstOrDefault(x => x.ElementName.Equals(nameShape)));
                    }
                    else
                    {
                        ShapeElement oldShape = ShapeList.FirstOrDefault(x => x.ElementName.Equals(nameShapeCurrent));
                        oldShape.restoreFromMemento(CareTaker.GetMemento(CurrentPosition - 1));
                    }

                    CurrentPosition--;
                    DrawingCanvas.Children.Clear();

                    foreach (var shape in ShapeList)
                    {
                        DrawingCanvas.Children.Add(shape.Element);
                    }
                }
            }
        }

        private void RedoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPosition < CareTaker.HistoryMemento.Count - 1)
            {
                // kiểm tra xem hình sau trong list có chưa, chưa có thì thêm vào
                string shapeNameAfter = CareTaker.GetMemento(CurrentPosition + 1).GetElementName();
                
                if (shapeNameAfter.Equals("cut"))
                {
                    string elementNameAfter = CareTaker.GetRemoveElement(CurrentPosition + 1);
                    ShapeList.Remove(ShapeList.FirstOrDefault(x => x.ElementName.Equals(elementNameAfter)));
                }
                else
                {
                    int countExists = 0;

                    for (int i = CurrentPosition; i >= 0; i--)
                    {
                        if (CareTaker.GetMemento(i).GetElementName().Equals(shapeNameAfter)) countExists++;
                    }
                    if (countExists == 0)
                    {
                        ShapeElement newShape = new ShapeElement(new UIElement(), "MyElement", IconKind.None);
                        newShape.restoreFromMemento(CareTaker.GetMemento(CurrentPosition + 1));
                        ShapeList.Add(newShape);

                    }
                    else
                    {
                        ShapeElement newShape = ShapeList.FirstOrDefault(x => x.ElementName.Equals(shapeNameAfter));
                        newShape.restoreFromMemento(CareTaker.GetMemento(CurrentPosition + 1));
                    }
                }

                CurrentPosition++;
                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }

                if (CurrentPosition == 0)
                {
                    BtnUndo.IsEnabled = true;
                    iconUndo.Foreground = Brushes.White;
                }

                if (CurrentPosition == CareTaker.HistoryMemento.Count - 1)
                {
                    BtnRedo.IsEnabled = false;
                    iconRedo.Foreground = Brushes.Gray;
                }
            }
        }
        
        private void RotateRightBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedElement != null)
            {
                if (RotateCorner == 360)
                {
                    RotateCorner = 0;
                }

                RotateCorner += 90;

                RotateTransform rightRotate = new RotateTransform();
                rightRotate.Angle = RotateCorner;

                ScaleTransform flip = new ScaleTransform();
                flip.ScaleY = FlipVertical;
                flip.ScaleX = FlipHorizontal;

                double width = _selectedElement.Element.RenderSize.Width;
                double height = _selectedElement.Element.RenderSize.Height;

                // Tính toán tâm
                double centerX = width / 2;
                double centerY = height / 2;

                // Thiết lập tâm quay
                rightRotate.CenterX = centerX;
                rightRotate.CenterY = centerY;

                flip.CenterX = centerX;
                flip.CenterY = centerY;

                TransformGroup transformGroup = new TransformGroup();
                transformGroup.Children.Add(rightRotate);
                transformGroup.Children.Add(flip);

                _selectedElement.Element.RenderTransform = transformGroup;

                UpdateMemento();

                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }

                SetSelected(false);
            }
        }

        private void RotateLeftBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedElement != null)
            {
                if (RotateCorner == -360)
                {
                    RotateCorner = 0;
                }

                RotateCorner -= 90;

                RotateTransform leftRotate = new RotateTransform();
                leftRotate.Angle = RotateCorner;

                ScaleTransform flip = new ScaleTransform();
                flip.ScaleY = FlipVertical;
                flip.ScaleX = FlipHorizontal;

                double width = _selectedElement.Element.RenderSize.Width;
                double height = _selectedElement.Element.RenderSize.Height;

                // Tính toán tâm
                double centerX = width / 2;
                double centerY = height / 2;

                // Thiết lập tâm quay
                leftRotate.CenterX = centerX;
                leftRotate.CenterY = centerY;

                flip.CenterX = centerX;
                flip.CenterY = centerY;

                TransformGroup transformGroup = new TransformGroup();
                transformGroup.Children.Add(leftRotate);
                transformGroup.Children.Add(flip);

                _selectedElement.Element.RenderTransform = transformGroup;

                UpdateMemento();

                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }

                SetSelected(false);
            }
        }

        private void FlipHorizontalBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedElement != null)
            {
                if (FlipHorizontal == 1)
                {
                    FlipHorizontal = -1;
                }
                else
                {
                    FlipHorizontal = 1;
                }

                ScaleTransform flip = new ScaleTransform();
                flip.ScaleY = FlipVertical;
                flip.ScaleX = FlipHorizontal;

                RotateTransform rotate = new RotateTransform();
                rotate.Angle = RotateCorner;

                double width = _selectedElement.Element.RenderSize.Width;
                double height = _selectedElement.Element.RenderSize.Height;

                // Tính toán tâm
                double centerX = width / 2;
                double centerY = height / 2;

                flip.CenterX = centerX;
                flip.CenterY = centerY;

                rotate.CenterX  = centerX;
                rotate.CenterY = centerY;

                TransformGroup transformGroup = new TransformGroup();
                transformGroup.Children.Add(rotate);
                transformGroup.Children.Add(flip);

                _selectedElement.Element.RenderTransform = transformGroup;

                UpdateMemento();

                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }

                SetSelected(false);
            }
        }

        private void FlipVerticalBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedElement != null)
            {
                if (FlipVertical == 1)
                {
                    FlipVertical = -1;
                }
                else
                {
                    FlipVertical = 1;
                }

                ScaleTransform flip = new ScaleTransform();
                flip.ScaleX = FlipHorizontal;
                flip.ScaleY = FlipVertical;

                RotateTransform rotate = new RotateTransform();
                rotate.Angle = RotateCorner;

                double width = _selectedElement.Element.RenderSize.Width;
                double height = _selectedElement.Element.RenderSize.Height;

                // Tính toán tâm
                double centerX = width / 2;
                double centerY = height / 2;

                flip.CenterX = centerX;
                flip.CenterY = centerY;

                rotate.CenterX = centerX;
                rotate.CenterY = centerY;

                TransformGroup transformGroup = new TransformGroup();
                transformGroup.Children.Add(rotate);
                transformGroup.Children.Add(flip);

                _selectedElement.Element.RenderTransform = transformGroup;

                UpdateMemento();

                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }

                SetSelected(false);
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

            // shapes consist of Path(s) and a TextBlock wrapped inside a Grid
            if (_selectedElement != null)
            {
                // if the user is editing the shape's text, change the textblock's background color
                if (TextPanel.Visibility == Visibility.Visible)
                {
                    TextBlock target = null;

                    foreach (UIElement child in ((Grid)_selectedElement.Element).Children)
                    {
                        if (child is TextBlock)
                        {
                            target = child as TextBlock;
                        }
                    }

                    if (target != null)
                    {
                        target.Background = FillClr.Background;
                    }
                }
                else
                {
                    // find all Paths that are children of the grid and modify their colors
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

                UpdateMemento();
                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }

                SetSelected(true);
            }
        }

        // change stroke color/text color on left-click
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
                if (TextPanel.Visibility == Visibility.Visible)
                {
                    TextBlock target = null;

                    foreach (UIElement child in ((Grid)_selectedElement.Element).Children)
                    {
                        if (child is TextBlock)
                        {
                            target = child as TextBlock;
                        }
                    }

                    if (target != null)
                    {
                        target.Foreground = StrokeClr.Background;
                    }
                }
                else
                {
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

                UpdateMemento();
                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }

                SetSelected(true);
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
            SetSelected(false);

            CanvasHelper.Visibility = Visibility.Visible;
        }

        private void SelectAreaBtn_Click(object sender, RoutedEventArgs e)
        {
            _isSelectArea = true;
            _painter = _prototypes.FirstOrDefault(x => x.Name.Equals("Rectangle"));
            _painter.SetStrokeColor(new SolidColorBrush(Colors.Red));
            _painter.SetStrokeWidth(1);
            _painter.SetStrokeDashArray(new Double[] {5,2});

            DrawingCanvas.Children.Remove(_selectionBounds);
            SelectionPane.UnselectAll();
            SetSelected(false);

            CanvasHelper.Visibility = Visibility.Visible;
        }

        private void MoveBtn_Click(object sender, RoutedEventArgs e)
        {
            _painter = null;
            CanvasHelper.Visibility = Visibility.Collapsed;
        }

        private void TextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedElement == null)
            {
                return;
            }

            _painter = null;
            CanvasHelper.Visibility = Visibility.Collapsed;
            TextPanel.Visibility = Visibility.Visible;

            TextBlock target = null;

            foreach (UIElement child in ((Grid)_selectedElement.Element).Children)
            {
                if (child is TextBlock)
                {
                    target = child as TextBlock;
                }
            }

            if (target != null)
            {
                FontCb.SelectedItem = target.FontFamily;
                ElementTb.Text = target.Text;
                FontSizeCb.SelectedItem = target.FontSize;

                if (target.FontStyle == FontStyles.Italic)
                {
                    BtnItalic.IsChecked = true;
                }
                else
                {
                    BtnItalic.IsChecked = false;
                }

                if (target.FontWeight == FontWeights.Bold)
                {
                    BtnBold.IsChecked = true;
                }
                else
                {
                    BtnBold.IsChecked = false;
                }

                if (target.TextDecorations == TextDecorations.Underline)
                {
                    BtnUnderline.IsChecked = true;
                }
                else
                {
                    BtnUnderline.IsChecked = false;
                }

                switch (target.TextAlignment)
                {
                    case TextAlignment.Center:
                        BtnCenter.IsChecked = true;
                        break;
                    case TextAlignment.Left:
                        BtnLeft.IsChecked = true;
                        break;
                    case TextAlignment.Right:
                        BtnRight.IsChecked = true;
                        break;
                }

                ElementTb.Focus();
            }
        }

        private void TextBtn_Unchecked(object sender, RoutedEventArgs e)
        {
            TextPanel.Visibility = Visibility.Collapsed;
        }

        private void ElementTb_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedElement == null)
            {
                return;
            }

            TextBlock target = null;

            foreach (UIElement child in ((Grid)_selectedElement.Element).Children)
            {
                if (child is TextBlock)
                {
                    target = child as TextBlock;
                }
            }

            if (target != null)
            {
                target.Text = ElementTb.Text;
                _justEditedText = true;
            }
        }

        private void ElementTb_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_justEditedText)
            {
                UpdateMemento();
                _justEditedText = false;
            }
        }

        private void BoldBtn_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton tb = sender as ToggleButton;

            if (_selectedElement == null)
            {
                return;
            }

            TextBlock target = null;

            foreach (UIElement child in ((Grid)_selectedElement.Element).Children)
            {
                if (child is TextBlock)
                {
                    target = child as TextBlock;
                }
            }

            if (target != null)
            {
                if (tb.IsChecked == true)
                {
                    target.FontWeight = FontWeights.Bold;
                }
                else
                {
                    target.FontWeight = FontWeights.Regular;
                }
            }
        }

        private void ItalicBtn_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton tb = sender as ToggleButton;

            if (_selectedElement == null)
            {
                return;
            }

            TextBlock target = null;

            foreach (UIElement child in ((Grid)_selectedElement.Element).Children)
            {
                if (child is TextBlock)
                {
                    target = child as TextBlock;
                }
            }

            if (target != null)
            {
                if (tb.IsChecked == true)
                {
                    target.FontStyle = FontStyles.Italic;
                }
                else
                {
                    target.FontStyle = FontStyles.Normal;
                }
            }
        }

        private void UnderlineBtn_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton tb = sender as ToggleButton;

            if (_selectedElement == null)
            {
                return;
            }

            TextBlock target = null;

            foreach (UIElement child in ((Grid)_selectedElement.Element).Children)
            {
                if (child is TextBlock)
                {
                    target = child as TextBlock;
                }
            }

            if (target != null)
            {
                if (tb.IsChecked == true)
                {
                    target.TextDecorations = TextDecorations.Underline;
                }
                else
                {
                    target.TextDecorations = null;
                }
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_painter != null)
            {

                _isDrawing = true;
                _start = e.GetPosition(DrawingCanvas);

                DrawingCanvas.Children.Remove(_selectionBounds);
                SelectionPane.UnselectAll();
                SetSelected(false);
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
                if(_isSelectArea)
                {

                    double width = (double)(_visual.RenderSize.Width);
                    double height = (double)(_visual.RenderSize.Height);

                    RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                        (int)Math.Round(width),
                        (int)Math.Round(height),
                        96.0,
                        96.0,
                        PixelFormats.Default
                    );
                    DrawingVisual dv = new DrawingVisual();
                    
                    using (DrawingContext dc = dv.RenderOpen())
                    {
                        VisualBrush vb = new VisualBrush(DrawingCanvas);
                        Rect rect = new Rect
                        {
                            X = (double)_visual.GetValue(Canvas.LeftProperty) - 120 - 141,
                            Y = (double)_visual.GetValue(Canvas.TopProperty) - 60,
                            Width = width,
                            Height = height,
                        };
                        dc.DrawRectangle(vb, null, rect);
                    }
                    renderBitmap.Render(dv);

                    Clipboard.SetImage(renderBitmap);
                    DrawingCanvas.Children.Remove(_visual);
                } else
                {
                    IShape clone = (IShape)_painter.Clone();
                    int index = indexShape[clone.Name];

                    if (index == 0)
                    {
                        ShapeElement newShape = new ShapeElement(_visual, clone.Name, clone.Icon);
                        indexShape[clone.Name] = 1;
                        ShapeList.Add(newShape);
                    }
                    else
                    {
                        ShapeElement newShape = new ShapeElement(_visual, clone.Name + " " + index.ToString(), clone.Icon);
                        indexShape[clone.Name] += 1;
                        ShapeList.Add(newShape);
                    }

                    SelectionPane.SelectedItem = ShapeList.Last();

                    SetSelected(false);
                    UpdateMemento();
                }
            }
        }

        private void FontCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectedElement == null)
            {
                return;
            }

            TextBlock target = null;

            foreach (UIElement child in ((Grid)_selectedElement.Element).Children)
            {
                if (child is TextBlock)
                {
                    target = child as TextBlock;
                }
            }

            if (target != null)
            {
                target.FontFamily = (FontFamily)FontCb.SelectedItem;
            }
        }

        private void FontSizeCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectedElement == null)
            {
                return;
            }

            TextBlock target = null;

            foreach (UIElement child in ((Grid)_selectedElement.Element).Children)
            {
                if (child is TextBlock)
                {
                    target = child as TextBlock;
                }
            }

            if (target != null)
            {
                target.FontSize = (double)FontSizeCb.SelectedItem;
            }
        }

        private void StrokeWidthCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox strokeWidthCb = (ComboBox)sender;

            if (strokeWidthCb.SelectedItem != null)
            {
                double oldStrokeWidth = StrokeWidth;
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
                        Grid element = _selectedElement.Element as Grid;

                        // custom shapes consists of Path(s) wrapped inside a Grid
                        // find all Paths that are child of the grid and modify their colors
                        foreach (UIElement child in element.Children)
                        {
                            if (child is System.Windows.Shapes.Path)
                            {
                                System.Windows.Shapes.Path path = child as System.Windows.Shapes.Path;

                                path.StrokeThickness = StrokeWidth;

                                double oldWidth = element.ActualWidth;
                                double oldHeight = element.ActualHeight;

                                double minX = Canvas.GetLeft(element) + (StrokeWidth - oldStrokeWidth);
                                double minY = Canvas.GetTop(element) + (StrokeWidth - oldStrokeWidth);

                                double boundingWidth = oldWidth + (StrokeWidth - oldStrokeWidth);
                                double boundingHeight = oldHeight + (StrokeWidth - oldStrokeWidth);

                                // Update container size and position
                                element.Width = boundingWidth;
                                element.Height = boundingHeight;

                                Canvas.SetLeft(element, minX);
                                Canvas.SetTop(element, minY);
                            }
                            else if (child is Shape)
                            {
                                (child as Shape).StrokeThickness = StrokeWidth;
                            }
                        }
                    }

                    UpdateMemento();
                    DrawingCanvas.Children.Clear();

                    foreach (var shape in ShapeList)
                    {
                        DrawingCanvas.Children.Add(shape.Element);
                    }

                    SetSelected(false);
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

                    UpdateMemento();
                    DrawingCanvas.Children.Clear();

                    foreach (var shape in ShapeList)
                    {
                        DrawingCanvas.Children.Add(shape.Element);
                    }

                    SetSelected(false);
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
            MoveBtn_Click(null, null);

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

            UpdateMemento();
            BoundSelectedElement();

            (sender as UIElement).ReleaseMouseCapture();
        }

        private void BoundSelectedElement()
        {
            DrawingCanvas.Children.Remove(_selectionBounds);

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
            SetSelected(false);
        }

        private void SetSelected(bool isColorChange)
        {
            if (_justEditedText)
            {
                UpdateMemento();
                _justEditedText = false;
            }

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

                if (!isColorChange)
                {
                    // activate the move tool by default
                    BtnMove.IsChecked = true;
                    MoveBtn_Click(null, null);
                }

                BtnText.IsEnabled = true;
                iconText.Foreground = Brushes.White;
            }
            else
            {
                BtnText.IsEnabled = false;
                iconText.Foreground = Brushes.Gray;
            }
        }

        private void SetPrevSelected()
        {
            _prevSelectedElement = _selectedElement;

            if (_prevSelectedElement != null)
            {
                _prevSelectedElement.Element.MouseDown -= Element_MouseDown;
                _prevSelectedElement.Element.MouseMove -= Element_MouseMove;
                _prevSelectedElement.Element.MouseUp -= Element_MouseUp;
            }
        }

        private void UpdateMemento()
        {
            if (CurrentPosition < CareTaker.HistoryMemento.Count - 1)
            {
                for (int i = CurrentPosition + 1; i < CareTaker.HistoryMemento.Count; i++)
                {
                    CareTaker.HistoryMemento.Remove(CareTaker.GetMemento(i));
                }
            }

            CareTaker.AddMemento(_selectedElement.createMemento());
            CurrentPosition++;

            if (BtnRedo.IsEnabled == true)
            {
                BtnRedo.IsEnabled = false;
                iconRedo.Foreground = Brushes.Gray;
            }
            if (CurrentPosition == 0)
            {
                BtnUndo.IsEnabled = true;
                iconUndo.Foreground = Brushes.White;
            }
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedElement != null)
            {
                MemoryStream stream = new MemoryStream();
                XamlWriter.Save(_selectedElement.Element, stream);

                stream.Seek(0, SeekOrigin.Begin);

                UIElement clonedElement = (UIElement)XamlReader.Load(stream);
                string[] name = _selectedElement.ElementName.Split(' ');

                if (name.Length == 2)
                {
                    if (name[0].Equals("Rounded"))
                    {
                        _copyElement = new ShapeElement(clonedElement, name[0] + ' ' + name[1], _selectedElement.ElementIcon);
                    } else
                    {
                        _copyElement = new ShapeElement(clonedElement, name[0], _selectedElement.ElementIcon);
                    }
                } 
                else if (name.Length == 1)
                {
                    _copyElement = new ShapeElement(clonedElement, name[0], _selectedElement.ElementIcon);
                }
                else
                {
                    _copyElement = new ShapeElement(clonedElement, name[0] + ' ' + name[1], _selectedElement.ElementIcon);
                }
            }
        }

        private void Cut_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedElement != null)
            {
                _copyElement = ShapeList.FirstOrDefault(x => x.ElementName.Equals(_selectedElement.ElementName));

                SetPrevSelected();
                _selectedElement = new ShapeElement(new UIElement(),"cut",IconKind.None);
                UpdateMemento();
                
                CareTaker.RemoveElement(CurrentPosition,_copyElement.ElementName);

                ShapeList.Remove(_copyElement);

                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }
                SelectionPane.UnselectAll();
            }
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            if (_copyElement != null)
            {
                int index = indexShape[_copyElement.ElementName];

                MemoryStream stream = new MemoryStream();
                XamlWriter.Save(_copyElement.Element, stream);
                stream.Seek(0, SeekOrigin.Begin);
                UIElement clonedElement = (UIElement)XamlReader.Load(stream);
                Canvas.SetTop(clonedElement, index);
                Canvas.SetLeft(clonedElement, index);

                ShapeElement newShape = new ShapeElement(clonedElement,_copyElement.ElementName +  " " + index, _copyElement.ElementIcon);

                indexShape[_copyElement.ElementName] += 1;

                ShapeList.Add(newShape);

                SetPrevSelected();
                _selectedElement = newShape;

                UpdateMemento();

                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }

                SelectionPane.UnselectAll();
            }
        }
    }
}