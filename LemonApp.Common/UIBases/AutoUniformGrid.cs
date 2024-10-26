using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace LemonApp.Common.UIBases;

public class AutoUniformGrid:UniformGrid
{
    public AutoUniformGrid()
    {
        SizeChanged += AutoUniformGrid_SizeChanged;
    }
    public double MaxItemWidth { get; set; }
    public double MinItemWidth { get; set; }
    public Thickness ItemMargin { get; set; }

    private void AutoUniformGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        double itemWidth = ActualWidth / Columns;
        if (itemWidth > MaxItemWidth)
        {
            int pre = Columns + 1;
            if(ActualWidth/pre >= MinItemWidth)
            {
                Columns = pre;
            }
        }
        else if (itemWidth < MinItemWidth)
        {
            int pre = Columns - 1;
            if(ActualWidth/pre <= MaxItemWidth)
            {
                Columns = pre;
            }
        }
    }
}
