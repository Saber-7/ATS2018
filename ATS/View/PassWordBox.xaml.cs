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

namespace ATS.View
{
    /// <summary>
    /// PassWordBox.xaml 的交互逻辑
    /// </summary>
    public partial class PassWordWindow : Window
    {
        public PassWordWindow()
        {
            InitializeComponent();
            DataContext = this;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(grid); i++)
            { 
                var child=VisualTreeHelper.GetChild(grid,i);
                if (child is Button)
                {
                    Button b = child as Button;
                    b.Click += b_Click;
                }
            }
        }

        public PassWordWindow(string password)
        {
            InitializeComponent();
            DataContext = this;
            RightPassWord = String.Copy(password);
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(grid); i++)
            {
                var child = VisualTreeHelper.GetChild(grid, i);
                if (child is Button)
                {
                    Button b = child as Button;
                    b.Click += b_Click;
                }
            }
        }

        String RightPassWord;

        public delegate void DecideResHandler(bool res);

        public event DecideResHandler DecideRes;
        void b_Click(object sender, RoutedEventArgs e)
        {
            Button b=sender as Button;
            string s = b.Content.ToString();
            if (s =="确认")
            {
                IntPtr p = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(pb.SecurePassword);
                string tp = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(p);
                DecideRes(tp == RightPassWord);
                this.Close();
            }
            else if (s == "取消")
            {
                this.Close();
            }
            else
            {
                int Input = Int16.Parse(s);
                pb.Password += Input;
            }
        }

        private void pb_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key==Key.Enter)
            {
                IntPtr p = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(pb.SecurePassword);
                string tp = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(p);
                DecideRes(tp == RightPassWord);
                this.Close();
            }
        }

    }
}
