using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace PaintApp
{
    public class Layer : INotifyPropertyChanged
    {
        public string LayerName { get; set; }

        private ImageSource _thumbnail;
        public ImageSource Thumbnail
        {
            get { return _thumbnail; }
            set
            {
                _thumbnail = value;
                OnPropertyChanged(nameof(Thumbnail));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<ShapeElement> ShapeList { get; set; }

        public CareTakerShape CareTaker;
        public int CurrentPosition { get; set; }

        public Canvas DrawingCanvas { get; set; }

        public Layer(MainWindow window, int layerIndex)
        {
            LayerName = "Layer " + layerIndex;

            ShapeList = new ObservableCollection<ShapeElement>();

            CareTaker = new CareTakerShape();
            CurrentPosition = -1;

            DrawingCanvas = new Canvas();
            DrawingCanvas.Background = Brushes.Transparent;
            DrawingCanvas.IsHitTestVisible = false;
            DrawingCanvas.Loaded += DrawingCanvas_Loaded;

            int index = window.CanvasGrid.Children.IndexOf(window.CanvasHelper);

            window.CanvasGrid.Children.Insert(index, DrawingCanvas);
        }

        private void DrawingCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            RenderThumbnail();
        }

        public void RenderThumbnail()
        {
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                (int)DrawingCanvas.ActualWidth,
                (int)DrawingCanvas.ActualHeight,
                96d,
                96d,
                PixelFormats.Pbgra32);

            renderBitmap.Render(DrawingCanvas);

            Thumbnail = renderBitmap;
        }
    }
}
