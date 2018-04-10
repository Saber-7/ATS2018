using System.Windows;
using System.Windows.Media;
using 线路绘图工具;

namespace ATS
{
    public class Axle : 线路绘图工具.Axle
    {
        static Pen OccupyPen_ = new Pen(Brushes.Red, 3);

        protected override void OnRender(DrawingContext dc)
        {
            for (int i = 0; i < 2; i++)
            {
                bool isOccupied = (sections_[i] as IRailway).IsOccupied;
                (graphics_[i] as Line).OnRender(dc, isOccupied ? OccupyPen_: DefaultPen_);

                Circle circle = graphics_[2 + i] as Circle;
                dc.DrawEllipse((sections_[i] as IRailway).IsOccupied ? Brushes.Red : Brushes.Gray, null,
                    circle.Center, circle.Radius, circle.Radius);
            }
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            (sections_[0] as IRailway).RelateAxle = this;
            (sections_[1] as IRailway).RelateAxle = this;
        }
    }
}
