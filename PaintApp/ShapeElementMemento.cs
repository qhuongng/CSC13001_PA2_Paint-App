using System.IO;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Markup;
using IconKind = MahApps.Metro.IconPacks.PackIconMaterialKind;

namespace PaintApp
{
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

            Canvas.SetTop(clonedElement, Canvas.GetTop(ele));
            Canvas.SetLeft(clonedElement, Canvas.GetLeft(ele));

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
            // save memento
            RemoveMemento.Add(index, ElementName);
        }

        public string GetRemoveElement(int index)
        {
            return RemoveMemento[index];
        }
    }
}
