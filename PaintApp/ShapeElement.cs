using System.IO;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Markup;
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

        public ShapeElement Clone()
        {
            MemoryStream stream = new MemoryStream();

            XamlWriter.Save(Element, stream);
            stream.Seek(0, SeekOrigin.Begin);

            UIElement clonedElement = (UIElement)XamlReader.Load(stream);
            clonedElement.RenderSize = Element.RenderSize;

            Canvas.SetTop(clonedElement, Canvas.GetTop(Element));
            Canvas.SetLeft(clonedElement, Canvas.GetLeft(Element));

            ShapeElement shapeElement = new ShapeElement(clonedElement, ElementName, ElementIcon);

            return shapeElement;
        }

        public ShapeElementMemento CreateMemento()
        {
            return new ShapeElementMemento(Element, ElementName, ElementIcon);
        }

        public void RestoreFromMemento(ShapeElementMemento memento)
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
}
