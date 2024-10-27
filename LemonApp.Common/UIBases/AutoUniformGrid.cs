using EleCho.WpfSuite;
using System.Diagnostics;
using System.Windows;

namespace LemonApp.Common.UIBases;
//TODO: 优化纵向布局 空间大时被拉伸
public class AutoUniformGrid:UniformGrid
{
    public AutoUniformGrid()
    {
        SizeChanged += AutoUniformGrid_SizeChanged;
    }
    public double MaxItemWidth { get; set; }
    public double MinItemWidth { get; set; }

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
        Rows = Children.Count / Columns + 1;
    }
}
