using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace PaintApp
{
    public class DragCanvas : Canvas
    {
        private UIElement _selectedElement;
        private Point _origCursorLocation;

        private double _origHorizOffset, _origVertOffset;
        private bool _modifyLeftOffset, _modifyTopOffset; // keep track of which offset should be modified for the drag element

        private bool _isDragging;
        private Rectangle _selectionBounds;

        public DragCanvas() { }

        public void BringToFront(UIElement element)
        {
            UpdateZOrder(element, true);
        }

        public void SendToBack(UIElement element)
        {
            UpdateZOrder(element, false);
        }

        public UIElement SelectedElement
        {
            get
            {
                return _selectedElement;
            }
            protected set
            {
                if (_selectedElement != null)
                    _selectedElement.ReleaseMouseCapture();

                _selectedElement = value;

                if (_selectedElement != null)
                    _selectedElement.CaptureMouse();
            }
        }

        public UIElement FindCanvasChild(DependencyObject depObj)
        {
            while (depObj != null)
            {
                // if the current object is a UIElement which is a child of the
                // Canvas, exit the loop and return it
                UIElement elem = depObj as UIElement;

                if (elem != null && Children.Contains(elem))
                    break;

                if (depObj is Visual || depObj is Visual3D)
                    depObj = VisualTreeHelper.GetParent(depObj);
            }

            return depObj as UIElement;
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            _isDragging = false;
            _origCursorLocation = e.GetPosition(this);

            Children.Remove(_selectionBounds);
            _selectionBounds = null;

            // walk up the visual tree from the element that was clicked, 
            // looking for an element that is a direct child of the Canvas.
            SelectedElement = FindCanvasChild(e.Source as DependencyObject);

            if (SelectedElement == null)
                return;

            // get the element's offsets from the four sides of the Canvas.
            double left = GetLeft(SelectedElement);
            double right = GetRight(SelectedElement);
            double top = GetTop(SelectedElement);
            double bottom = GetBottom(SelectedElement);

            // calculate the offset deltas and determine for which sides
            // of the Canvas to adjust the offsets.
            _origHorizOffset = ResolveOffset(left, right, out _modifyLeftOffset);
            _origVertOffset = ResolveOffset(top, bottom, out _modifyTopOffset);

            // set the Handled flag so that a control being dragged 
            // does not react to the mouse input.
            e.Handled = true;

            _isDragging = true;
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            if (SelectedElement == null || !_isDragging)
                return;

            Point cursorLocation = e.GetPosition(this);
            double newHorizontalOffset, newVerticalOffset;

            // determine the horizontal offset.
            if (_modifyLeftOffset)
                newHorizontalOffset = _origHorizOffset + (cursorLocation.X - _origCursorLocation.X);
            else
                newHorizontalOffset = _origHorizOffset - (cursorLocation.X - _origCursorLocation.X);

            // determine the vertical offset.
            if (_modifyTopOffset)
                newVerticalOffset = _origVertOffset + (cursorLocation.Y - _origCursorLocation.Y);
            else
                newVerticalOffset = _origVertOffset - (cursorLocation.Y - _origCursorLocation.Y);

            if (_modifyLeftOffset)
                SetLeft(SelectedElement, newHorizontalOffset);
            else
                SetRight(SelectedElement, newHorizontalOffset);

            if (_modifyTopOffset)
                SetTop(SelectedElement, newVerticalOffset);
            else
                SetBottom(SelectedElement, newVerticalOffset);
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);
            BoundSelectedElement();
            SelectedElement = null;
        }

        // Returns a rectangle bounding the selected element
        private void BoundSelectedElement()
        {
            if (SelectedElement == null)
                return;

            Size elemSize = SelectedElement.RenderSize;

            // Get the position of the element relative to its parent container
            Point elemLoc = SelectedElement.TranslatePoint(new Point(0, 0), this);

            _selectionBounds = new Rectangle
            {
                Width = elemSize.Width + 2,
                Height = elemSize.Height + 2,
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection(new double[] { 4, 2 })
            };

            // Set the position of the selection bounds based on the translated coordinates
            SetLeft(_selectionBounds, elemLoc.X - 1);
            SetTop(_selectionBounds, elemLoc.Y - 1);

            Children.Add(_selectionBounds);
        }

        /// <summary>
        /// Determines one component of a UIElement's location 
        /// within a Canvas (either the horizontal or vertical offset).
        /// </summary>
        /// <param name="side1">
        /// The value of an offset relative to a default side of the 
        /// Canvas (i.e. top or left).
        /// </param>
        /// <param name="side2">
        /// The value of the offset relative to the other side of the 
        /// Canvas (i.e. bottom or right).
        /// </param>
        /// <param name="useSide1">
        /// Will be set to true if the returned value should be used 
        /// for the offset from the side represented by the 'side1' 
        /// parameter.  Otherwise, it will be set to false.
        /// </param>
        private static double ResolveOffset(double side1, double side2, out bool useSide1)
        {
            // If the Canvas.Left and Canvas.Right attached properties 
            // are specified for an element, the 'Left' value is honored.
            // The 'Top' value is honored if both Canvas.Top and 
            // Canvas.Bottom are set on the same element.  If one 
            // of those attached properties is not set on an element, 
            // the default value is Double.NaN.
            useSide1 = true;
            double result;

            if (double.IsNaN(side1))
            {
                if (double.IsNaN(side2))
                {
                    // both sides have no value, so set the
                    // first side to a value of zero.
                    result = 0;
                }
                else
                {
                    result = side2;
                    useSide1 = false;
                }
            }
            else
            {
                result = side1;
            }

            return result;
        }

        private void UpdateZOrder(UIElement element, bool bringToFront)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            if (!base.Children.Contains(element))
                throw new ArgumentException("Must be a child element of the Canvas.", "element");

            // determine the Z-Index
            int elementNewZIndex = -1;

            if (bringToFront)
            {
                foreach (UIElement elem in base.Children)
                    if (elem.Visibility != Visibility.Collapsed)
                        ++elementNewZIndex;
            }
            else
            {
                elementNewZIndex = 0;
            }

            int offset = (elementNewZIndex == 0) ? +1 : -1;

            int elementCurrentZIndex = GetZIndex(element);

            // update the Z-Index of every UIElement in the Canvas
            foreach (UIElement childElement in base.Children)
            {
                if (childElement == element)
                    SetZIndex(element, elementNewZIndex);
                else
                {
                    int zIndex = GetZIndex(childElement);

                    // only modify the z-index of an element if it is  
                    // in between the target element's old and new z-index
                    if (bringToFront && elementCurrentZIndex < zIndex ||
                        !bringToFront && zIndex < elementCurrentZIndex)
                    {
                        SetZIndex(childElement, zIndex + offset);
                    }
                }
            }
        }
    }
}