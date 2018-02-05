using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using 线路绘图工具;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using ATS.Model;
using ATS.View;
using ATS.ViewModel;
using System.ComponentModel;
using System.Configuration;


namespace ATS
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {         
            InitializeComponent();
            DataContext = new MainViewModel(MainCanvas);
        }

        //code behind 命令模式可能不适用，需要重写双绑定
        double preSv;
        private void MainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.DirectlyOver is Canvas)
            {
                preSv = e.GetPosition(MainCanvas).X;
                this.Cursor = Cursors.SizeAll;
            }
        }

        private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                double nowSv = e.GetPosition(MainCanvas).X;
                MainScroll.ScrollToHorizontalOffset(MainScroll.HorizontalOffset-(nowSv - preSv));
            }
        }

        private void MainCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }
    }
}
