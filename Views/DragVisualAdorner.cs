using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace PocketLaunch.Views;

/// <summary>
/// Renders a floating snapshot of a dragged element that follows the cursor,
/// used while a shortcut card is being reordered by hand.
/// </summary>
public class DragVisualAdorner : Adorner
{
    private readonly VisualBrush _brush;
    private readonly Size _size;
    private Point _topLeft;

    public DragVisualAdorner(UIElement adornedElement, Visual sourceVisual, Size size)
        : base(adornedElement)
    {
        _brush = new VisualBrush(sourceVisual) { Stretch = Stretch.None };
        _size = size;
        IsHitTestVisible = false;
    }

    public void UpdatePosition(Point topLeft)
    {
        _topLeft = topLeft;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        var rect = new Rect(_topLeft, _size);

        dc.PushOpacity(0.85);
        dc.PushTransform(new ScaleTransform(1.03, 1.03, rect.Left + rect.Width / 2, rect.Top + rect.Height / 2));
        dc.DrawRectangle(_brush, null, rect);
        dc.Pop();
        dc.Pop();
    }
}
