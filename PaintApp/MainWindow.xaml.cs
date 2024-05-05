using Microsoft.Win32;
using Newtonsoft.Json;
using Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Icon = MahApps.Metro.IconPacks.PackIconMaterial;
using IconKind = MahApps.Metro.IconPacks.PackIconMaterialKind;

namespace PaintApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        SaveLoad _saveLoadProcess = new SaveLoad();

        bool _isDrawing = false;
        bool _isDragging = false;
        bool _justEditedText = false;
        bool _isSelectArea = false;
        bool _isSaved = true;

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

        private ObservableCollection<Layer> _layers;
        public ObservableCollection<Layer> Layers
        {
            get { return _layers; }
            set
            {
                _layers = value;
                OnPropertyChanged(nameof(Layers));
            }
        }

        private Layer _currentLayer;
        public Layer CurrentLayer
        {
            get { return _currentLayer; }
            set
            {
                if (_currentLayer != value)
                {
                    _currentLayer = value;
                    OnPropertyChanged(nameof(CurrentLayer));

                    if (_currentLayer != null)
                    {
                        DrawingCanvas = _currentLayer.DrawingCanvas;
                        ShapeList = _currentLayer.ShapeList;
                        CareTaker = _currentLayer.CareTaker;
                        CurrentPosition = _currentLayer.CurrentPosition;
                    }
                }
            }
        }

        public ShapeElement SelectedElement { get; set; }

        ShapeElement _prevSelectedElement = null;
        ShapeElement _copyElement = null;

        double _deleteIndex = 0;

        public ResizeAdorner Adorner { get; set; }

        public int RotateCorner = 0;
        public int FlipHorizontal = 1;
        public int FlipVertical = 1;
        public string SaveFilePath;

        private Canvas _drawingCanvas;
        public Canvas DrawingCanvas
        {
            get { return _drawingCanvas; }
            set
            {
                _drawingCanvas = value;
                OnPropertyChanged(nameof(DrawingCanvas));
            }
        }

        private ObservableCollection<ShapeElement> _shapeList;
        public ObservableCollection<ShapeElement> ShapeList
        {
            get { return _shapeList; }
            set
            {
                _shapeList = value;
                OnPropertyChanged(nameof(ShapeList));
            }
        }

        private CareTakerShape _careTaker;
        public CareTakerShape CareTaker
        {
            get { return _careTaker; }
            set
            {
                _careTaker = value;
                OnPropertyChanged(nameof(CareTaker));
            }
        }

        private int _currentPosition;
        public int CurrentPosition
        {
            get { return _currentPosition; }
            set
            {
                _currentPosition = value;
                OnPropertyChanged(nameof(CurrentPosition));
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Dictionary<string, int> indexShape = new Dictionary<string, int>
        {
            {"Rectangle", 0},{"Ellipse", 0},{"Line", 0},{"Arrow", 0},
            {"Heart", 0},{"Rounded Rectangle", 0},{"Star", 0},{"Triangle", 0}
        };

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            Layers = [new Layer(this, 1)];
            CurrentLayer = Layers[0];

            KeyDown += Canvas_KeyDown;
            KeyUp += Canvas_KeyUp;

            StrokeWidths = new ObservableCollection<double> { 1, 3, 5, 8 };
            StrokeWidth = 1;

            FontSizes = new ObservableCollection<double> { 6, 8, 10, 12, 16, 20, 24, 28, 32, 36 };

            StrokeTypes = new ObservableCollection<BitmapImage> { solid, dash, dash_dot };
            StrokeType = StrokeTypes[0];

            BtnRedo.IsEnabled = false;
            iconRedo.Foreground = Brushes.Gray;

            BtnUndo.IsEnabled = false;
            iconUndo.Foreground = Brushes.Gray;

            FontCb.ItemsSource = Fonts.SystemFontFamilies;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow = this;

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
                        Tag = item
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

            if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                NewFile_Click(null, null);
            }

            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SaveFile_Click(null, null);
            }

            if (e.Key == Key.S && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                SaveAsFile_Click(null, null);
            }

            if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Export_Click(null, null);
            }

            if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
            {
                OpenFile_Click(null, null);
            }

            if (e.Key == Key.Delete && SelectedElement != null)
            {
                if (SelectedElement != null)
                {
                    ShapeElement delElement = ShapeList.FirstOrDefault(x => x.ElementName.Equals(SelectedElement.ElementName));

                    SetPrevSelected();
                    SelectedElement = new ShapeElement(new UIElement(), "del", IconKind.None);
                    UpdateMemento();

                    CareTaker.RemoveElement(CurrentPosition, delElement.ElementName);

                    ShapeList.Remove(delElement);

                    DrawingCanvas.Children.Clear();

                    foreach (var shape in ShapeList)
                    {
                        DrawingCanvas.Children.Add(shape.Element);
                    }

                    SelectionPane.UnselectAll();
                }
            }

            if (e.Key == Key.System && e.SystemKey == Key.F4)
            {
                e.Handled = true;
                Quit_Click(null, null);
            }
        }

        private void Canvas_KeyUp(object sender, KeyEventArgs e)
        {
            if (_painter != null && (e.Key == Key.LeftShift || e.Key == Key.RightShift))
            {
                _painter.SetShiftState(false);
            }
        }

        private void NewFile_Click(object sender, RoutedEventArgs e)
        {
            if (_isSaved == false)
            {
                MessageBoxResult result = MessageBox.Show("Do you want to save the changes you made?", "Save Changes", MessageBoxButton.YesNoCancel);

                if (result == MessageBoxResult.Yes)
                {
                    SaveFile_Click(null, null);

                    if (_isSaved == true)
                    {
                        ResetWorkspace();
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
                else
                {
                    ResetWorkspace();
                }
            }
            else
            {
                ResetWorkspace();
            }
        }

        private void ResetWorkspace()
        {
            foreach (Layer layer in Layers)
            {
                layer.DrawingCanvas.Children.Clear();
                CanvasGrid.Children.Remove(layer.DrawingCanvas);
            }

            Layers.Clear();
            Layers = [new Layer(this, 1)];
            CurrentLayer = Layers[0];

            StrokeWidth = 1;
            StrokeType = StrokeTypes[0];

            BtnRedo.IsEnabled = false;
            iconRedo.Foreground = Brushes.Gray;

            BtnUndo.IsEnabled = false;
            iconUndo.Foreground = Brushes.Gray;

            FontCb.ItemsSource = Fonts.SystemFontFamilies;
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (!_isSaved)
            {
                MessageBoxResult result = MessageBox.Show("Do you want to save the changes you made?", "Save Changes", MessageBoxButton.YesNoCancel);
                
                if (result == MessageBoxResult.Yes)
                {
                    SaveFile_Click(null, null);

                    if (_isSaved)
                    {
                        OpenFile();
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
                else
                {
                    OpenFile();
                }
            }
            else
            {
                OpenFile();
            }
        }

        private void OpenFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Open Paint";
            openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(openFileDialog.FileName);
                    SaveFilePath = openFileDialog.FileName;

                    JsonSerializerSettings settings = new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects
                    };

                    Dictionary<string, ObservableCollection<DataShape>> rawData = JsonConvert.DeserializeObject<Dictionary<string, ObservableCollection<DataShape>>>(json, settings);

                    foreach (Layer layer in Layers)
                    {
                        layer.DrawingCanvas.Children.Clear();
                        CanvasGrid.Children.Remove(layer.DrawingCanvas);
                    }

                    Layers.Clear();
                    Layers = _saveLoadProcess.Load(rawData, this);
                    CurrentLayer = Layers[0];

                    _isSaved = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            if (SaveFilePath == null || SaveFilePath.Equals(""))
            {
                if (ShapeList.Count == 0)
                {
                    MessageBox.Show("There are no unsaved changes.");
                }
                else
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Title = "Save As";
                    saveFileDialog.Filter = "JSON file (*.json)|*.json";
                    saveFileDialog.FilterIndex = 1;
                    saveFileDialog.RestoreDirectory = true;

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        string filePath = saveFileDialog.FileName;

                        SaveFilePath = filePath;

                        Dictionary<string, ObservableCollection<DataShape>> data = _saveLoadProcess.Save(Layers);

                        string json = JsonConvert.SerializeObject(data,
                            new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.Objects
                            });

                        File.WriteAllText(filePath, json);

                        _isSaved = true;
                    }
                }
            }
            else
            {
                if (_isSaved == false)
                {
                    Dictionary<string, ObservableCollection<DataShape>> data = _saveLoadProcess.Save(Layers);

                    string json = JsonConvert.SerializeObject(data,
                        new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.Objects
                        });

                    File.WriteAllText(SaveFilePath, json);

                    _isSaved = true;
                }
            }
        }

        private void SaveAsFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save As";
            saveFileDialog.Filter = "JSON file (*.json)|*.json";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                SaveFilePath = filePath;

                Dictionary<string, ObservableCollection<DataShape>> data = _saveLoadProcess.Save(Layers);

                string json = JsonConvert.SerializeObject(data,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects
                    });

                File.WriteAllText(filePath, json);

                _isSaved = true;
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            RenderTargetBitmap rtb = new RenderTargetBitmap((int)CanvasGrid.RenderSize.Width, (int)CanvasGrid.RenderSize.Height, 96, 96, PixelFormats.Default);

            VisualBrush sourceBrush = new VisualBrush(CanvasGrid);
            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();

            using (drawingContext)
            {
                drawingContext.DrawRectangle(sourceBrush, null, new Rect(new Point(0, 0),
                      new Point(CanvasGrid.RenderSize.Width, CanvasGrid.RenderSize.Height)));
            }

            rtb.Render(drawingVisual);

            SaveFileDialog exportDialog = new SaveFileDialog();
            exportDialog.Title = "Export Image";
            exportDialog.Filter = "PNG (*.png)|*.png|JPEG (*.jpg)|*.jpg|Bitmap (*.bmp)|*.bmp|GIF (*.gif)|*.gif|TIFF (*.tiff)|*.tiff";

            if (exportDialog.ShowDialog() == true)
            {
                string filePath = exportDialog.FileName;

                // Tạo một BitmapEncoder tương ứng với định dạng được chọn
                BitmapEncoder encoder = null;
                string selectedExtension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();

                if (selectedExtension == ".png")
                    encoder = new PngBitmapEncoder();
                else if (selectedExtension == ".jpg" || selectedExtension == ".jpeg")
                    encoder = new JpegBitmapEncoder();
                else if (selectedExtension == ".bmp")
                    encoder = new BmpBitmapEncoder();
                else if (selectedExtension == ".gif")
                    encoder = new GifBitmapEncoder();
                else if (selectedExtension == ".tiff" || selectedExtension == ".tif")
                    encoder = new TiffBitmapEncoder();

                // Nếu encoder được khởi tạo thành công, thêm khung hình ảnh vào và lưu tệp tin
                if (encoder != null)
                {
                    encoder.Frames.Add(BitmapFrame.Create(rtb));

                    using (FileStream fs = File.Open(filePath, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }

                    // Xác nhận hình ảnh đã được lưu
                    MessageBox.Show($"Image exported successfully.");
                }
                else
                {
                    // Hiển thị thông báo lỗi nếu không thể tạo encoder cho định dạng tệp tin
                    MessageBox.Show("The specified format is not supported");
                }
            }
        }

        private void UndoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Adorner != null)
            {
                AdornerLayer.GetAdornerLayer(DrawingCanvas).Remove(Adorner);
            }

            SelectionPane.UnselectAll();
            SetSelected(false);

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

                // TH nếu vị trí hiện tại là một hình bị cắt
                if (nameShapeCurrent.Equals("cut"))
                {
                    if (nameShapeBefore.Equals("cut"))
                    {
                        CurrentPosition--;
                    }

                    string elementNameBefore = CareTaker.GetRemoveElement(CurrentPosition);
                    ShapeElement oldShape = new ShapeElement(new UIElement(), "cutElement", IconKind.None);

                    oldShape.RestoreFromMemento(CareTaker.HistoryMemento.LastOrDefault(x => x.GetElementName().Equals(elementNameBefore)));
                    ShapeList.Add(oldShape);

                    CurrentPosition--;

                    DrawingCanvas.Children.Clear();

                    foreach (var shape in ShapeList)
                    {
                        DrawingCanvas.Children.Add(shape.Element);
                    }
                }
                /// TH nếu vị trí hiện tại là một hình bị cắt
                else if (nameShapeCurrent.Equals("del"))
                {
                    if (nameShapeBefore.Equals("del"))
                    {
                        CurrentPosition--;
                    }

                    string elementNameBefore = CareTaker.GetRemoveElement(CurrentPosition);
                    ShapeElement oldShape = new ShapeElement(new UIElement(), "delElement", IconKind.None);

                    oldShape.RestoreFromMemento(CareTaker.HistoryMemento.LastOrDefault(x => x.GetElementName().Equals(elementNameBefore)));
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
                        oldShape.RestoreFromMemento(CareTaker.GetMemento(CurrentPosition - 1));
                    }

                    CurrentPosition--;
                    DrawingCanvas.Children.Clear();

                    foreach (var shape in ShapeList)
                    {
                        DrawingCanvas.Children.Add(shape.Element);
                    }
                }
            }

            _isSaved = false;

            Dispatcher.BeginInvoke(new Action(CurrentLayer.RenderThumbnail), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void RedoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Adorner != null)
            {
                AdornerLayer.GetAdornerLayer(DrawingCanvas).Remove(Adorner);
            }

            SelectionPane.UnselectAll();
            SetSelected(false);

            if (CurrentPosition < CareTaker.HistoryMemento.Count - 1)
            {
                // kiểm tra xem hình sau trong list có chưa, chưa có thì thêm vào
                string shapeNameAfter = CareTaker.GetMemento(CurrentPosition + 1).GetElementName();

                if (shapeNameAfter.Equals("cut") || shapeNameAfter.Equals("del"))
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
                        newShape.RestoreFromMemento(CareTaker.GetMemento(CurrentPosition + 1));
                        ShapeList.Add(newShape);

                    }
                    else
                    {
                        ShapeElement newShape = ShapeList.FirstOrDefault(x => x.ElementName.Equals(shapeNameAfter));
                        newShape.RestoreFromMemento(CareTaker.GetMemento(CurrentPosition + 1));
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

            _isSaved = false;

            Dispatcher.BeginInvoke(new Action(CurrentLayer.RenderThumbnail), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void RotateRightBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedElement != null)
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

                double width = SelectedElement.Element.RenderSize.Width;
                double height = SelectedElement.Element.RenderSize.Height;

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

                SelectedElement.Element.RenderTransform = transformGroup;

                // update positions of top-left and bottom-right thumbs
                Point topLeft = new Point(Canvas.GetLeft(SelectedElement.Element), Canvas.GetTop(SelectedElement.Element));
                Point bottomRight = new Point(topLeft.X + width, topLeft.Y + height);

                // apply the rotation to the corner points
                topLeft = rightRotate.Transform(topLeft);
                bottomRight = rightRotate.Transform(bottomRight);

                // update positions of the adorner thumb
                Canvas.SetLeft(Adorner.Thumb, bottomRight.X);
                Canvas.SetTop(Adorner.Thumb, bottomRight.Y);

                // update the adorner visuals
                Adorner.Border.Arrange(new Rect(-2.5, -2.5, width + 5, height + 5));
                Adorner.Thumb.Arrange(new Rect(width - 5, height - 5, 10, 10));

                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }

                UpdateMemento();
                SetSelected(false);
            }
        }

        private void RotateLeftBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedElement != null)
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

                double width = SelectedElement.Element.RenderSize.Width;
                double height = SelectedElement.Element.RenderSize.Height;

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

                SelectedElement.Element.RenderTransform = transformGroup;

                // update positions of top-left and bottom-right thumbs
                Point topLeft = new Point(Canvas.GetLeft(SelectedElement.Element), Canvas.GetTop(SelectedElement.Element));
                Point bottomRight = new Point(topLeft.X + width, topLeft.Y + height);

                // apply the rotation to the corner points
                topLeft = leftRotate.Transform(topLeft);
                bottomRight = leftRotate.Transform(bottomRight);

                // update positions of the adorner thumb
                Canvas.SetLeft(Adorner.Thumb, bottomRight.X);
                Canvas.SetTop(Adorner.Thumb, bottomRight.Y);

                // update the adorner visuals
                Adorner.Border.Arrange(new Rect(-2.5, -2.5, width + 5, height + 5));
                Adorner.Thumb.Arrange(new Rect(width - 5, height - 5, 10, 10));

                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }

                UpdateMemento();
                SetSelected(false);
            }
        }

        private void FlipHorizontalBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedElement != null)
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

                double width = SelectedElement.Element.RenderSize.Width;
                double height = SelectedElement.Element.RenderSize.Height;

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

                SelectedElement.Element.RenderTransform = transformGroup;

                // update positions of top-left and bottom-right thumbs
                Point topLeft = new Point(Canvas.GetLeft(SelectedElement.Element), Canvas.GetTop(SelectedElement.Element));
                Point bottomRight = new Point(topLeft.X + width, topLeft.Y + height);

                // apply the transformation to the corner points
                topLeft = transformGroup.Transform(topLeft);
                bottomRight = transformGroup.Transform(bottomRight);

                // update positions of the adorner thumb
                Canvas.SetLeft(Adorner.Thumb, bottomRight.X);
                Canvas.SetTop(Adorner.Thumb, bottomRight.Y);

                // update the adorner visuals
                Adorner.Border.Arrange(new Rect(-2.5, -2.5, width + 5, height + 5));
                Adorner.Thumb.Arrange(new Rect(width - 5, height - 5, 10, 10));

                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }

                UpdateMemento();
                SetSelected(false);
            }
        }

        private void FlipVerticalBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedElement != null)
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

                double width = SelectedElement.Element.RenderSize.Width;
                double height = SelectedElement.Element.RenderSize.Height;

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

                SelectedElement.Element.RenderTransform = transformGroup;

                // update positions of top-left and bottom-right thumbs
                Point topLeft = new Point(Canvas.GetLeft(SelectedElement.Element), Canvas.GetTop(SelectedElement.Element));
                Point bottomRight = new Point(topLeft.X + width, topLeft.Y + height);

                // apply the transformation to the corner points
                topLeft = transformGroup.Transform(topLeft);
                bottomRight = transformGroup.Transform(bottomRight);

                // update positions of the adorner thumb
                Canvas.SetLeft(Adorner.Thumb, bottomRight.X);
                Canvas.SetTop(Adorner.Thumb, bottomRight.Y);

                // update the adorner visuals
                Adorner.Border.Arrange(new Rect(-2.5, -2.5, width + 5, height + 5));
                Adorner.Thumb.Arrange(new Rect(width - 5, height - 5, 10, 10));

                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }

                UpdateMemento();
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
            if (SelectedElement != null)
            {
                // if the user is editing the shape's text, change the textblock's background color
                if (TextPanel.Visibility == Visibility.Visible)
                {
                    TextBlock target = null;

                    foreach (UIElement child in ((Grid)SelectedElement.Element).Children)
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
                    foreach (UIElement child in (SelectedElement.Element as Grid).Children)
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

                UpdateMemento();
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

            if (SelectedElement != null)
            {
                if (TextPanel.Visibility == Visibility.Visible)
                {
                    TextBlock target = null;

                    foreach (UIElement child in ((Grid)SelectedElement.Element).Children)
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
                    foreach (UIElement child in (SelectedElement.Element as Grid).Children)
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

                UpdateMemento();
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
            _painter.SetShiftState(false);

            if (Adorner != null)
            {
                AdornerLayer.GetAdornerLayer(DrawingCanvas).Remove(Adorner);
            }

            SelectionPane.UnselectAll();
            SetSelected(false);

            CanvasHelper.Visibility = Visibility.Visible;
        }

        private void SelectAreaBtn_Click(object sender, RoutedEventArgs e)
        {
            _isSelectArea = true;
            _painter = _prototypes.FirstOrDefault(x => x.Name.Equals("Rectangle"));
            _painter.SetStrokeColor(new SolidColorBrush(Colors.Red));
            _painter.SetFillColor(new SolidColorBrush(Colors.Transparent));
            _painter.SetStrokeWidth(1);
            _painter.SetStrokeDashArray([5, 2]);
            _painter.SetShiftState(false);

            if (Adorner != null)
            {
                AdornerLayer.GetAdornerLayer(DrawingCanvas).Remove(Adorner);
            }

            SelectionPane.UnselectAll();
            SetSelected(false);

            CanvasHelper.Visibility = Visibility.Visible;
        }

        private void SelectAreaBtn_Unchecked(object sender, RoutedEventArgs e)
        {
            _isSelectArea = false;
        }

        private void MoveBtn_Click(object sender, RoutedEventArgs e)
        {
            _painter = null;
            CanvasHelper.Visibility = Visibility.Collapsed;
        }

        private void TextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedElement == null || SelectedElement.ElementName.Contains("Line"))
            {
                return;
            }

            _painter = null;
            CanvasHelper.Visibility = Visibility.Collapsed;
            TextPanel.Visibility = Visibility.Visible;

            TextBlock target = null;

            foreach (UIElement child in ((Grid)SelectedElement.Element).Children)
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

                ElementTb.CaretIndex = ElementTb.Text.Length;
                ElementTb.Focus();
            }
        }

        private void TextBtn_Unchecked(object sender, RoutedEventArgs e)
        {
            TextPanel.Visibility = Visibility.Collapsed;
        }

        private void ElementTb_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SelectedElement == null)
            {
                return;
            }

            TextBlock target = null;

            foreach (UIElement child in ((Grid)SelectedElement.Element).Children)
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

            if (SelectedElement == null)
            {
                return;
            }

            TextBlock target = null;

            foreach (UIElement child in ((Grid)SelectedElement.Element).Children)
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

            if (SelectedElement == null)
            {
                return;
            }

            TextBlock target = null;

            foreach (UIElement child in ((Grid)SelectedElement.Element).Children)
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

            if (SelectedElement == null)
            {
                return;
            }

            TextBlock target = null;

            foreach (UIElement child in ((Grid)SelectedElement.Element).Children)
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

        void AlignmentBtn_Checked(object sender, RoutedEventArgs e)
        {
            var rb = sender as RadioButton;

            if (SelectedElement == null)
            {
                return;
            }

            TextBlock target = null;

            foreach (UIElement child in ((Grid)SelectedElement.Element).Children)
            {
                if (child is TextBlock)
                {
                    target = child as TextBlock;
                }
            }

            if (target != null)
            {
                switch (rb.Name)
                {
                    case "BtnLeft":
                        target.TextAlignment = TextAlignment.Left;
                        break;
                    case "BtnRight":
                        target.TextAlignment = TextAlignment.Right;
                        break;
                    case "BtnCenter":
                        target.TextAlignment = TextAlignment.Center;
                        break;
                    default:
                        target.TextAlignment = TextAlignment.Left;
                        break;
                }
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_painter != null)
            {
                _isDrawing = true;
                _start = e.GetPosition(DrawingCanvas);

                if (Adorner != null)
                {
                    AdornerLayer.GetAdornerLayer(DrawingCanvas).Remove(Adorner);
                }

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

                if (_isSelectArea)
                {
                    double width = (double)(_visual.RenderSize.Width);
                    double height = (double)(_visual.RenderSize.Height);

                    _visual.Visibility = Visibility.Hidden;

                    RenderTargetBitmap rtb = new RenderTargetBitmap((int)CanvasGrid.RenderSize.Width, (int)CanvasGrid.RenderSize.Height, 96, 96, PixelFormats.Default);

                    VisualBrush sourceBrush = new VisualBrush(CanvasGrid);
                    DrawingVisual drawingVisual = new DrawingVisual();
                    DrawingContext drawingContext = drawingVisual.RenderOpen();

                    using (drawingContext)
                    {
                        drawingContext.DrawRectangle(sourceBrush, null, new Rect(new Point(0, 0),
                              new Point(CanvasGrid.RenderSize.Width, CanvasGrid.RenderSize.Height)));
                    }

                    rtb.Render(drawingVisual);

                    // Cắt phần cần thiết từ hình ảnh
                    CroppedBitmap croppedBitmap = new CroppedBitmap(rtb, new Int32Rect((int)Canvas.GetLeft(_visual), (int)Canvas.GetTop(_visual), (int)width, (int)height));

                    // Gắn hình ảnh vào clipboard
                    Clipboard.SetImage(croppedBitmap);

                    DrawingCanvas.Children.Remove(_visual);

                    e.Handled = true;

                    MessageBox.Show("Selected area copied to clipboard.");
                }
                else
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
            if (SelectedElement == null)
            {
                return;
            }

            TextBlock target = null;

            foreach (UIElement child in ((Grid)SelectedElement.Element).Children)
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
            if (SelectedElement == null)
            {
                return;
            }

            TextBlock target = null;

            foreach (UIElement child in ((Grid)SelectedElement.Element).Children)
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

                if (SelectedElement != null)
                {
                    if (SelectedElement.Element is Shape)
                    {
                        (SelectedElement.Element as Shape).StrokeThickness = StrokeWidth;
                    }
                    else if (SelectedElement.Element is Grid)
                    {
                        Grid element = SelectedElement.Element as Grid;

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

                    DrawingCanvas.Children.Clear();

                    foreach (var shape in ShapeList)
                    {
                        DrawingCanvas.Children.Add(shape.Element);
                    }

                    UpdateMemento();
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

                if (SelectedElement != null)
                {
                    DoubleCollection newParam = new DoubleCollection(0);

                    if (BitmapToDashArray(StrokeType) != null)
                    {
                        newParam = new DoubleCollection(BitmapToDashArray(StrokeType));
                    }

                    if (SelectedElement.Element is Shape)
                    {
                        (SelectedElement.Element as Shape).StrokeDashArray = newParam;
                    }
                    else if (SelectedElement.Element is Grid)
                    {
                        // custom shapes consists of Path(s) wrapped inside a Grid
                        // find all Paths that are child of the grid and modify their colors
                        foreach (UIElement child in (SelectedElement.Element as Grid).Children)
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

                    UpdateMemento();
                    SetSelected(false);
                }
            }
        }

        public double[] BitmapToDashArray(BitmapImage image)
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

            if (Adorner != null)
            {
                AdornerLayer.GetAdornerLayer(DrawingCanvas).Remove(Adorner);
            }

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

                Canvas.SetLeft(SelectedElement.Element, left + (newPoint.X - _dragStart.X));
                Canvas.SetTop(SelectedElement.Element, top + (newPoint.Y - _dragStart.Y));

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
            if (SelectedElement == null)
            {
                return;
            }

            if (Adorner != null)
            {
                AdornerLayer.GetAdornerLayer(DrawingCanvas).Remove(Adorner);
            }

            Adorner = new ResizeAdorner(SelectedElement.Element);
            AdornerLayer.GetAdornerLayer(DrawingCanvas).Add(Adorner);
        }

        private void LayerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            for (int i = 0; i < Layers.Count; i++)
            {
                Layers[i].DrawingCanvas.IsHitTestVisible = false;
            }

            if (Adorner != null && AdornerLayer.GetAdornerLayer(DrawingCanvas) != null)
            {
                AdornerLayer.GetAdornerLayer(DrawingCanvas).Remove(Adorner);
            }

            if (CurrentLayer != null)
            {
                DrawingCanvas = CurrentLayer.DrawingCanvas;
                ShapeList = CurrentLayer.ShapeList;
                CareTaker = CurrentLayer.CareTaker;
                CurrentPosition = CurrentLayer.CurrentPosition;

                DrawingCanvas.IsHitTestVisible = true;

                int index = Layers.IndexOf(CurrentLayer);

                BtnDelLayer.IsEnabled = true;
                iconDelLayer.Foreground = Brushes.White;

                if (index == 0)
                {
                    BtnLayerUp.IsEnabled = false;
                    iconLayerUp.Foreground = Brushes.Gray;

                    BtnLayerDown.IsEnabled = true;
                    iconLayerDown.Foreground = Brushes.White;
                }
                else if (index == Layers.Count - 1)
                {
                    BtnLayerDown.IsEnabled = false;
                    iconLayerDown.Foreground = Brushes.Gray;

                    BtnLayerUp.IsEnabled = true;
                    iconLayerUp.Foreground = Brushes.White;
                }
                else
                {
                    BtnLayerDown.IsEnabled = true;
                    iconLayerDown.Foreground = Brushes.White;

                    BtnLayerUp.IsEnabled = true;
                    iconLayerUp.Foreground = Brushes.White;
                }

                SetControlsToLayerVisibility(CurrentLayer.DrawingCanvas.Visibility == Visibility.Visible);
            }
            else
            {
                BtnDelLayer.IsEnabled = false;
                iconDelLayer.Foreground = Brushes.Gray;

                BtnLayerDown.IsEnabled = false;
                iconLayerDown.Foreground = Brushes.Gray;

                BtnLayerUp.IsEnabled = false;
                iconLayerUp.Foreground = Brushes.Gray;

                SetControlsToLayerVisibility(false);
            }
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

            _prevSelectedElement = SelectedElement;

            if (_prevSelectedElement != null)
            {
                _prevSelectedElement.Element.IsHitTestVisible = false;

                _prevSelectedElement.Element.PreviewMouseDown -= Element_MouseDown;
                _prevSelectedElement.Element.PreviewMouseMove -= Element_MouseMove;
                _prevSelectedElement.Element.PreviewMouseUp -= Element_MouseUp;
            }

            SelectedElement = (ShapeElement)SelectionPane.SelectedItem;

            if (SelectedElement != null)
            {
                SelectedElement.Element.IsHitTestVisible = true;

                SelectedElement.Element.PreviewMouseDown += Element_MouseDown;
                SelectedElement.Element.PreviewMouseMove += Element_MouseMove;
                SelectedElement.Element.PreviewMouseUp += Element_MouseUp;

                BoundSelectedElement();

                if (SelectedElement.Element is Shape)
                {
                    FillClr.Background = (SelectedElement.Element as Shape).Fill;
                    StrokeClr.Background = (SelectedElement.Element as Shape).Stroke;
                    StrokeWidthCb.SelectedItem = (SelectedElement.Element as Shape).StrokeThickness;

                    DashArrayToBitmap((SelectedElement.Element as Shape).StrokeDashArray);

                }
                else if (SelectedElement.Element is Grid)
                {
                    foreach (UIElement child in (SelectedElement.Element as Grid).Children)
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
                    BtnMove.IsEnabled = true;
                    iconMove.Foreground = Brushes.White;

                    BtnMove.IsChecked = true;
                    MoveBtn_Click(null, null);
                }

                if (!SelectedElement.ElementName.Contains("Line"))
                {
                    BtnText.IsEnabled = true;
                    iconText.Foreground = Brushes.White;
                }
                else
                {
                    BtnText.IsEnabled = false;
                    iconText.Foreground = Brushes.Gray;
                }
            }
            else
            {
                BtnText.IsChecked = false;
                TextPanel.Visibility = Visibility.Collapsed;
                BtnText.IsEnabled = false;
                iconText.Foreground = Brushes.Gray;

                BtnMove.IsChecked = false;
                BtnMove.IsEnabled = false;
                iconMove.Foreground = Brushes.Gray;
            }
        }

        private void SetPrevSelected()
        {
            _prevSelectedElement = SelectedElement;

            if (_prevSelectedElement != null)
            {
                _prevSelectedElement.Element.IsHitTestVisible = false;

                _prevSelectedElement.Element.PreviewMouseDown -= Element_MouseDown;
                _prevSelectedElement.Element.PreviewMouseMove -= Element_MouseMove;
                _prevSelectedElement.Element.PreviewMouseUp -= Element_MouseUp;
            }
        }

        public void UpdateMemento()
        {
            if (CurrentPosition < CareTaker.HistoryMemento.Count - 1)
            {
                for (int i = CurrentPosition + 1; i < CareTaker.HistoryMemento.Count; i++)
                {
                    CareTaker.HistoryMemento.Remove(CareTaker.GetMemento(i));
                }
            }

            CareTaker.AddMemento(SelectedElement.CreateMemento());
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

            _isSaved = false;

            Dispatcher.BeginInvoke(new Action(CurrentLayer.RenderThumbnail), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            if (_isSaved == false)
            {
                MessageBoxResult result = MessageBox.Show("Do you want to save the changes you made?", "Save Changes", MessageBoxButton.YesNoCancel);

                if (result == MessageBoxResult.Yes)
                {
                    SaveFile_Click(null, null);

                    if (_isSaved == true)
                    {
                        Application.Current.Shutdown();
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedElement != null)
            {
                MemoryStream stream = new MemoryStream();
                XamlWriter.Save(SelectedElement.Element, stream);

                stream.Seek(0, SeekOrigin.Begin);

                UIElement clonedElement = (UIElement)XamlReader.Load(stream);
                string[] name = SelectedElement.ElementName.Split(' ');

                if (name.Length == 2)
                {
                    if (name[0].Equals("Rounded"))
                    {
                        _copyElement = new ShapeElement(clonedElement, name[0] + ' ' + name[1], SelectedElement.ElementIcon);
                    }
                    else
                    {
                        _copyElement = new ShapeElement(clonedElement, name[0], SelectedElement.ElementIcon);
                    }
                }
                else if (name.Length == 1)
                {
                    _copyElement = new ShapeElement(clonedElement, name[0], SelectedElement.ElementIcon);
                }
                else
                {
                    _copyElement = new ShapeElement(clonedElement, name[0] + ' ' + name[1], SelectedElement.ElementIcon);
                }
            }
        }

        private void Cut_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedElement != null)
            {
                _copyElement = ShapeList.FirstOrDefault(x => x.ElementName.Equals(SelectedElement.ElementName));

                SetPrevSelected();
                SelectedElement = new ShapeElement(new UIElement(), "cut", IconKind.None);
                UpdateMemento();

                CareTaker.RemoveElement(CurrentPosition, _copyElement.ElementName);

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

                ShapeElement newShape = new ShapeElement(clonedElement, _copyElement.ElementName + " " + index, _copyElement.ElementIcon);

                indexShape[_copyElement.ElementName] += 1;

                ShapeList.Add(newShape);

                SetPrevSelected();
                SelectedElement = newShape;

                DrawingCanvas.Children.Clear();

                foreach (var shape in ShapeList)
                {
                    DrawingCanvas.Children.Add(shape.Element);
                }

                UpdateMemento();
                SelectionPane.UnselectAll();
            }
        }

        private void BtnNewLayer_Click(object sender, RoutedEventArgs e)
        {
            Layer newLayer = new Layer(this, Layers.Count + 1);
            Layers.Insert(0, newLayer);
            CurrentLayer = Layers.First();

            if (Layers.Count > 1)
            {
                BtnDelLayer.IsEnabled = true;
                iconDelLayer.Foreground = Brushes.White;
            }
        }

        private void BtnLayerUp_Click(object sender, RoutedEventArgs e)
        {
            SelectionPane.UnselectAll();
            SetSelected(false);

            int layerIndex = Layers.IndexOf(CurrentLayer);
            int canvasIndex = CanvasGrid.Children.IndexOf(CurrentLayer.DrawingCanvas);

            CanvasGrid.Children.Remove(CurrentLayer.DrawingCanvas);
            CanvasGrid.Children.Insert(canvasIndex + 1, CurrentLayer.DrawingCanvas);

            Layer l = CurrentLayer;
            Layers.Remove(CurrentLayer);
            Layers.Insert(layerIndex - 1, l);
        }

        private void BtnLayerDown_Click(object sender, RoutedEventArgs e)
        {
            SelectionPane.UnselectAll();
            SetSelected(false);

            int layerIndex = Layers.IndexOf(CurrentLayer);
            int canvasIndex = CanvasGrid.Children.IndexOf(CurrentLayer.DrawingCanvas);

            CanvasGrid.Children.Remove(CurrentLayer.DrawingCanvas);
            CanvasGrid.Children.Insert(canvasIndex - 1, CurrentLayer.DrawingCanvas);

            Layer l = CurrentLayer;
            Layers.Remove(CurrentLayer);
            Layers.Insert(layerIndex + 1, l);
        }

        private void BtnDelLayer_Click(object sender, RoutedEventArgs e)
        {
            if (Adorner != null)
            {
                AdornerLayer.GetAdornerLayer(DrawingCanvas).Remove(Adorner);
            }

            CanvasGrid.Children.Remove(CurrentLayer.DrawingCanvas);
            Layers.Remove(CurrentLayer);
            CurrentLayer = Layers[0];

            if (Layers.Count == 1)
            {
                BtnDelLayer.IsEnabled = false;
                iconDelLayer.Foreground = Brushes.Gray;

                BtnLayerDown.IsEnabled = false;
                iconLayerDown.Foreground = Brushes.Gray;

                BtnLayerUp.IsEnabled = true;
                iconLayerUp.Foreground = Brushes.Gray;
            }
        }

        private void BtnHideLayer_Click(object sender, RoutedEventArgs e)
        {
            if (Adorner != null)
            {
                AdornerLayer.GetAdornerLayer(DrawingCanvas).Remove(Adorner);
            }

            ToggleButton tb = sender as ToggleButton;
            Layer l = tb.DataContext as Layer;

            if (tb.IsChecked == true)
            {
                l.DrawingCanvas.Visibility = Visibility.Collapsed;

                if (l == CurrentLayer)
                {
                    SetControlsToLayerVisibility(false);
                }
            }
            else
            {
                l.DrawingCanvas.Visibility = Visibility.Visible;

                if (l == CurrentLayer)
                {
                    SetControlsToLayerVisibility(true);
                }
            }
        }

        public void SetControlsToLayerVisibility(bool isLayerVisible)
        {
            SelectionPane.UnselectAll();
            SetSelected(false);
            BtnMove.IsChecked = false;
            
            if (!isLayerVisible)
            {
                SelectionPane.IsEnabled = false;

                BtnMove.IsEnabled = false;
                iconMove.Foreground = Brushes.Gray;

                BtnText.IsEnabled = false;
                iconText.Foreground = Brushes.Gray;

                ShapesBtnGrp.Children.Clear();

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
                            Content = new Icon { Kind = item.Icon, Foreground = new SolidColorBrush(Colors.Gray), Width = 24, Height = 24 },
                            Style = Application.Current.Resources["IconRadioButtonStyle"] as Style,
                            GroupName = "CtrlBtn",
                            Tag = item
                        };

                        control.Click += ShapeBtn_Click;

                        Grid.SetRow(control, i);
                        Grid.SetColumn(control, j);

                        ShapesBtnGrp.Children.Add(control);

                        k++;
                    }
                }

                ShapesBtnGrp.IsEnabled = false;
            }
            else
            {
                SelectionPane.IsEnabled = true;

                if (SelectedElement != null)
                {
                    BtnMove.IsEnabled = true;
                    iconMove.Foreground = Brushes.White;
                }

                ShapesBtnGrp.Children.Clear();

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
                            Tag = item
                        };

                        control.Click += ShapeBtn_Click;

                        Grid.SetRow(control, i);
                        Grid.SetColumn(control, j);

                        ShapesBtnGrp.Children.Add(control);

                        k++;
                    }
                }

                ShapesBtnGrp.IsEnabled = true;
            }
        }
    }
}