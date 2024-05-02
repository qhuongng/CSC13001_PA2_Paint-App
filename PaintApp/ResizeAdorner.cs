using Shapes;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;

namespace PaintApp
{
    public class ResizeAdorner : Adorner
    {
        VisualCollection _adornerVisuals;

        Thumb _topLeftThumb;
        Thumb _bottomRightThumb;

        Rectangle _border;

        Thickness _textBoxMargins;

        Dictionary<string, IShape> painters = new Dictionary<string, IShape>
        {
            { "Line", new MyLine.MyLine() },
            { "Star", new MyStar.MyStar() },
            { "Heart", new MyHeart.MyHeart() },
            { "Arrow", new MyArrow.MyArrow() },
            { "Triangle", new MyTriangle.MyTriangle() }
        };

        // IShape object to reconvert the shape for custom shapes, since changing width and height does nothing to them
        IShape _painter;

        public ResizeAdorner(UIElement adornedElement) : base(adornedElement)
        {
            _adornerVisuals = new VisualCollection(this);

            _topLeftThumb = new Thumb()
            {
                Background = Brushes.BlanchedAlmond,
                BorderBrush = Brushes.Red,
                BorderThickness = new Thickness(1),
                Width = 10,
                Height = 10
            };

            _bottomRightThumb = new Thumb()
            {
                Background = Brushes.BlanchedAlmond,
                BorderBrush = Brushes.Red,
                BorderThickness = new Thickness(1),
                Width = 10,
                Height = 10
            };

            _topLeftThumb.DragStarted += Thumb_DragStarted;
            _bottomRightThumb.DragStarted += Thumb_DragStarted;

            _topLeftThumb.DragDelta += TopLeftThumb_DragDelta;
            _bottomRightThumb.DragDelta += BottomRightThumb_DragDelta;

            _topLeftThumb.DragCompleted += Thumb_DragCompleted;
            _bottomRightThumb.DragCompleted += Thumb_DragCompleted;

            _border = new Rectangle()
            {
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection([3, 2])
            };

            _adornerVisuals.Add(_border);
            _adornerVisuals.Add(_topLeftThumb);
            _adornerVisuals.Add(_bottomRightThumb);
        }

        private string GetKeyFromElementName(string elementName)
        {
            int spaceIndex = elementName.IndexOf(' ');

            if (spaceIndex != -1)
            {
                return elementName.Substring(0, spaceIndex);
            }
            else
            {
                return elementName;
            }
        }

        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            MainWindow mw = (MainWindow)Application.Current.MainWindow;
            string key = GetKeyFromElementName(mw.SelectedElement.ElementName);

            if (painters.ContainsKey(key))
            {
                _painter = painters[key];
            }

            if (_painter != null)
            {
                _painter.SetStrokeWidth(mw.StrokeWidth);
                _painter.SetStrokeColor((SolidColorBrush)mw.StrokeClr.Background);
                _painter.SetFillColor((SolidColorBrush)mw.FillClr.Background);
                _painter.SetStrokeDashArray(mw.BitmapToDashArray(mw.StrokeType));
            }
        }

        private void TopLeftThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            MainWindow mw = (MainWindow)Application.Current.MainWindow;

            var element = (FrameworkElement)AdornedElement;

            // store the original bottom-right corner to use as anchor point
            Point oldBottomRight = new Point(Canvas.GetLeft(element) + element.Width, Canvas.GetTop(element) + element.Height);

            double newWidth = element.Width - e.HorizontalChange < 0 ? 0 : element.Width - e.HorizontalChange;
            double newHeight = element.Height - e.VerticalChange < 0 ? 0 : element.Height - e.VerticalChange;

            // calculate the new top-left corner
            double newLeft = oldBottomRight.X - newWidth;
            double newTop = oldBottomRight.Y - newHeight;

            // scale the container grid
            Canvas.SetLeft(element, newLeft);
            Canvas.SetTop(element, newTop);

            element.Width = newWidth;
            element.Height = newHeight;

            // update the position of the adorner visuals
            _border.Arrange(new Rect(-2.5, -2.5, element.DesiredSize.Width + 5, element.DesiredSize.Height + 5));
            _topLeftThumb.Arrange(new Rect(-5, -5, 10, 10));
            _bottomRightThumb.Arrange(new Rect(element.DesiredSize.Width - 5, element.DesiredSize.Height - 5, 10, 10));

            UIElement shape = null;
            TextBlock textBlock = null;

            foreach (UIElement child in ((Grid)AdornedElement).Children)
            {
                if (child is TextBlock)
                {
                    textBlock = child as TextBlock;
                }
                else
                {
                    shape = child;
                }
            }

            if (textBlock != null)
            {
                // scale the text block
                textBlock.Width = textBlock.Width - e.HorizontalChange < 0 ? 0 : textBlock.Width - e.HorizontalChange;
                textBlock.Height = textBlock.Height - e.VerticalChange < 0 ? 0 : textBlock.Height - e.VerticalChange;
            }

            if (shape != null)
            {
                // scale the shape
                if (mw.SelectedElement.ElementName != "Ellipse" && mw.SelectedElement.ElementName != "Rectangle" && mw.SelectedElement.ElementName != "Rounded Rectangle")
                {
                    ((Grid)element).Children.Remove(shape);

                    _painter.AddStart(new Point(newLeft, newTop));

                    if (mw.SelectedElement.ElementName.Contains("Heart"))
                    {
                        _painter.AddEnd(new Point(oldBottomRight.X -= newWidth / 14, oldBottomRight.Y -= newHeight / 6.75));
                    }
                    else
                    {
                        _painter.AddEnd(oldBottomRight);
                    }

                    UIElement modified = _painter.Convert();

                    ((Grid)element).Children.Insert(0, modified);
                }
                else
                {
                    var s = (FrameworkElement)shape;

                    s.Height = newHeight;
                    s.Width = newWidth;
                }
            }
        }

        private void BottomRightThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            MainWindow mw = (MainWindow)Application.Current.MainWindow;

            var element = (FrameworkElement)AdornedElement;

            element.Height = element.Height + e.VerticalChange < 0 ? 0 : element.Height + e.VerticalChange;
            element.Width = element.Width + e.HorizontalChange < 0 ? 0 : element.Width + e.HorizontalChange;

            UIElement shape = null;
            TextBlock textBlock = null;

            foreach (UIElement child in ((Grid)AdornedElement).Children)
            {
                if (child is TextBlock)
                {
                    textBlock = child as TextBlock;
                }
                else
                {
                    shape = child;
                }
            }

            if (textBlock != null)
            {
                // scale the text block
                textBlock.Height = textBlock.Height + e.VerticalChange < 0 ? 0 : textBlock.Height + e.VerticalChange;
                textBlock.Width = textBlock.Width + e.HorizontalChange < 0 ? 0 : textBlock.Width + e.HorizontalChange;
            }

            if (shape != null)
            {
                // scale the shape
                if (mw.SelectedElement.ElementName != "Ellipse" && mw.SelectedElement.ElementName != "Rectangle" && mw.SelectedElement.ElementName != "Rounded Rectangle")
                {
                    Point start = new Point(Canvas.GetLeft(element), Canvas.GetTop(element));

                    double newHeight = element.Height + e.VerticalChange < 0 ? 0 : element.Height + e.VerticalChange;
                    double newWidth = element.Width + e.HorizontalChange < 0 ? 0 : element.Width + e.HorizontalChange;

                    if (mw.SelectedElement.ElementName.Contains("Heart"))
                    {
                        newHeight -= newHeight / 6.75;
                        newWidth -= newWidth / 14;
                    }

                    ((Grid)element).Children.Remove(shape);

                    _painter.AddStart(start);
                    _painter.AddEnd(new Point(start.X + newWidth, start.Y + newHeight));

                    UIElement modified = _painter.Convert();

                    ((Grid)element).Children.Insert(0, modified);
                }
                else
                {
                    var s = (FrameworkElement)shape;

                    s.Height = s.Height + e.VerticalChange < 0 ? 0 : s.Height + e.VerticalChange;
                    s.Width = s.Width + e.HorizontalChange < 0 ? 0 : s.Width + e.HorizontalChange;
                }
            }
        }

        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            MainWindow mw = (MainWindow)Application.Current.MainWindow;
            mw.UpdateMemento();
        }

        protected override Visual GetVisualChild(int index)
        {
            return _adornerVisuals[index];
        }

        protected override int VisualChildrenCount => _adornerVisuals.Count;

        protected override Size ArrangeOverride(Size finalSize)
        {
            _border.Arrange(new Rect(-2.5, -2.5, AdornedElement.DesiredSize.Width + 5, AdornedElement.DesiredSize.Height + 5));

            _topLeftThumb.Arrange(new Rect(-5, -5, 10, 10));
            _bottomRightThumb.Arrange(new Rect(AdornedElement.DesiredSize.Width - 5, AdornedElement.DesiredSize.Height - 5, 10, 10));

            return base.ArrangeOverride(finalSize);
        }
    }
}
