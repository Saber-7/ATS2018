using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ATS.Model;
using ATS.ViewModel;
using System.ComponentModel;

namespace ATS.View
{
    /// <summary>
    /// Plan.xaml 的交互逻辑
    /// </summary>
    public partial class PlanWindow : Window
    {
        public PlanWindow()
        {
            InitializeComponent();
            DataContext = new PlanViewModel();
        }
    }
}
