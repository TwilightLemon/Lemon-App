using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LemonApp.Common.UIBases;
//marked form https://slimenull.com/posts/20250608020400/
public class BackgroundPresenter : FrameworkElement
{

    public double CornerRadius
    {
        get { return (double)GetValue(CornerRadiusProperty); }
        set { SetValue(CornerRadiusProperty, value); }
    }

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register("CornerRadius", typeof(double), typeof(BackgroundPresenter), new PropertyMetadata(0.0));

    private static readonly FieldInfo _drawingContentOfUIElement = typeof(UIElement)
        .GetField("_drawingContent", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly FieldInfo _contentOfDrawingVisual = typeof(DrawingVisual)
        .GetField("_content", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly FieldInfo _offsetOfVisual = typeof(Visual)
        .GetField("_offset", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly Func<UIElement, DrawingContext> _renderOpenMethod = typeof(UIElement)
        .GetMethod("RenderOpen", BindingFlags.Instance | BindingFlags.NonPublic)!
        .CreateDelegate<Func<UIElement, DrawingContext>>();

    private static readonly Action<UIElement, DrawingContext> _onRenderMethod = typeof(UIElement)
        .GetMethod("OnRender", BindingFlags.Instance | BindingFlags.NonPublic)!
        .CreateDelegate<Action<UIElement, DrawingContext>>();

    private static readonly GetContentBoundsDelegate _methodGetContentBounds = typeof(VisualBrush)
        .GetMethod("GetContentBounds", BindingFlags.Instance | BindingFlags.NonPublic)!
        .CreateDelegate<GetContentBoundsDelegate>();

    private delegate void GetContentBoundsDelegate(VisualBrush visualBrush, out Rect bounds);
    private readonly Stack<UIElement> _parentStack = new();

    public void UpdateBackground()
    {
        // cannot use 'InvalidateVisual' here, because it will cause infinite loop
        if (Visibility != Visibility.Visible) return;
        ForceRender(this);

        Debug.WriteLine("Parent layout updated, forcing render of BackgroundPresenter.");
    }

    private static void ForceRender(UIElement target)
    {
        using DrawingContext drawingContext = _renderOpenMethod(target);

        _onRenderMethod.Invoke(target, drawingContext);
    }

    private static void DrawVisual(DrawingContext drawingContext, Visual visual, Point relatedXY, Size renderSize)
    {
        var visualBrush = new VisualBrush(visual);
        var visualOffset = (Vector)_offsetOfVisual.GetValue(visual)!;

        _methodGetContentBounds.Invoke(visualBrush, out var contentBounds);
        relatedXY -= visualOffset;

        drawingContext.DrawRectangle(
            visualBrush, null,
            new Rect(relatedXY.X + contentBounds.X, relatedXY.Y + contentBounds.Y, contentBounds.Width, contentBounds.Height));
    }

    protected override Geometry GetLayoutClip(Size layoutSlotSize)
    {
        return new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight), CornerRadius, CornerRadius);
    }

    protected override void OnVisualParentChanged(DependencyObject oldParentObject)
    {
        if (oldParentObject is FrameworkElement oldParent)
        {
            oldParent.SizeChanged -= ParentLayoutUpdated;
        }

        if (Parent is FrameworkElement newParent)
        {
            newParent.SizeChanged += ParentLayoutUpdated;
        }
    }

    private void ParentLayoutUpdated(object? sender, EventArgs e)
    {
        UpdateBackground();
    }

    private static void DrawBackground(
        DrawingContext drawingContext, UIElement self,
        Stack<UIElement> parentStackStorage,
        int maxDepth,
        bool throwExceptionIfParentArranging)
    {
#if DEBUG
        bool selfInDesignMode = DesignerProperties.GetIsInDesignMode(self);
#endif

        var parent = VisualTreeHelper.GetParent(self) as UIElement;
        while (
            parent is { } &&
            parentStackStorage.Count < maxDepth)
        {
            // parent not visible, no need to render
            if (!parent.IsVisible)
            {
                parentStackStorage.Clear();
                return;
            }

#if DEBUG
            if (selfInDesignMode &&
                parent.GetType().ToString().Contains("VisualStudio"))
            {
                // 遍历到 VS 自身的设计器元素, 中断!
                break;
            }
#endif

            // is parent arranging
            // we cannot render it
            if (parent.RenderSize.Width == 0 ||
                parent.RenderSize.Height == 0)
            {
                parentStackStorage.Clear();

                if (throwExceptionIfParentArranging)
                {
                    throw new InvalidOperationException("Arranging");
                }

                // render after parent arranging finished
                self.InvalidateArrange();
                return;
            }

            parentStackStorage.Push(parent);
            parent = VisualTreeHelper.GetParent(parent) as UIElement;
        }

        var selfRect = new Rect(0, 0, self.RenderSize.Width, self.RenderSize.Height);
        while (parentStackStorage.TryPop(out var currentParent))
        {
            if (!parentStackStorage.TryPeek(out var breakElement))
            {
                breakElement = self;
            }

            var parentRelatedXY = currentParent.TranslatePoint(default, self);

            // has render data
            if (_drawingContentOfUIElement.GetValue(currentParent) is { } parentDrawingContent)
            {
                var drawingVisual = new DrawingVisual();
                _contentOfDrawingVisual.SetValue(drawingVisual, parentDrawingContent);

                DrawVisual(drawingContext, drawingVisual, parentRelatedXY, currentParent.RenderSize);
            }

            if (currentParent is Panel parentPanelToRender)
            {
                foreach (UIElement child in parentPanelToRender.Children)
                {
                    if (child == breakElement)
                    {
                        break;
                    }

                    var childRelatedXY = child.TranslatePoint(default, self);
                    var childRect = new Rect(childRelatedXY, child.RenderSize);

                    if (!selfRect.IntersectsWith(childRect))
                    {
                        continue; // skip if not intersecting
                    }

                    if (child.IsVisible)
                    {
                        DrawVisual(drawingContext, child, childRelatedXY, child.RenderSize);
                    }
                }
            }
        }
    }

    public static void DrawBackground(DrawingContext drawingContext, UIElement self)
    {
        var parentStack = new Stack<UIElement>();
        DrawBackground(drawingContext, self, parentStack, int.MaxValue, true);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (Visibility != Visibility.Visible) return;
        DrawBackground(drawingContext, this, _parentStack, MaxDepth, false);
    }

    public int MaxDepth
    {
        get { return (int)GetValue(MaxDepthProperty); }
        set { SetValue(MaxDepthProperty, value); }
    }

    public static readonly DependencyProperty MaxDepthProperty =
        DependencyProperty.Register("MaxDepth", typeof(int), typeof(BackgroundPresenter), new PropertyMetadata(64));
}
