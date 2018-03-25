using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATS;
using System.Windows.Media;
using System.Windows;

namespace ATS.ViewModel
{
    class TrainViewModel:ViewModelBase
    {
        readonly PointCollection Rectangle = new PointCollection
        {
            new Point(-20,-7),new Point(-20,7),new Point(20,7),new Point(20,-7)
        };

        readonly PointCollection LeftArrow = new PointCollection
        {
            new Point(-27,0),new Point(-20,-7),new Point(-20,7)
        };

        readonly PointCollection RightArrow = new PointCollection
        {
            new Point(27,0),new Point(20,-7),new Point(20,7)
        };

        readonly List<Brush> RectangleColors = new List<Brush>()
        {
            new SolidColorBrush(Colors.Black),
            new SolidColorBrush(Colors.Green),
            new SolidColorBrush(Colors.Orange),
            new SolidColorBrush(Colors.Gray),
            new SolidColorBrush(Colors.White),
            new SolidColorBrush(Colors.Yellow),
            new SolidColorBrush(Colors.Red) 
        };

        bool _IsEmergent;

        public bool IsEmergent
        {
            get
            {
                return _IsEmergent;
            }

            set
            {
                _IsEmergent = value;
            }
        }
    }
}
