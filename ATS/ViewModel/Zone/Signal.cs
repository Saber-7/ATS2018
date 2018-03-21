using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using 线路绘图工具;

namespace ATS
{
    public class Signal : 线路绘图工具.Signal, IFlash
    {
        public static int StartByte { get; set; }
        static Pen linePen = new Pen(Brushes.White, 3);
        static Pen blockPen = new Pen(Brushes.White, 1);

        public Signal()
        {
            ResetDefaultStatus();
        }
        public enum SignalColor
        {
            Red, Green, Yellow, RedYellow,
            DSFail, DS2Fail,
            White, RedWhite, DoubleYellow, Blue, DoubleGreen, GreenYellow
        }

        List<Circle> lights_ = new List<Circle>();
        List<Line> lines_ = new List<Line>();
        int startByte_;
        Rect blockRect_;
        public List<Graphic> Graphics
        {
            get { return graphics_; }
            set { value = graphics_; }
        }

        SignalColor sColor_ = SignalColor.Yellow;
        internal SignalColor SColor
        {
            get { return sColor_; }
            set
            {
                if (sColor_ != value)
                {
                    sColor_ = value;
                    IsStatusChanged = true;
                    //if (value == SignalColor.DSFail)
                    //{
                    //    MainWindow.Instance.SetDeviceFail(Name + "灯丝断丝");
                    //    isFailed_ = true;
                    //}
                    //else
                    //{
                    //    MainWindow.Instance.UnSetDeviceFail(Name + "灯丝断丝");
                    //    isFailed_ = false;
                    //}
                }
            }
        }

        bool bBlocked_ = false;
        internal bool IsBlocked { get { return bBlocked_; }
            set
            {
                if (bBlocked_ != value)
                {
                    bBlocked_ = value;
                    IsStatusChanged = true;
                }
            }
        }

        bool bFlameWarn_ = true;
        internal bool IsFlameWarn { get { return bFlameWarn_; }
            set
            {
                if (bFlameWarn_ != value)
                {
                    bFlameWarn_ = value;
                    IsStatusChanged = true;
                }
            }
        }

        bool bCBTCRoute_ = true;
        internal bool IsCBTCRoute
        {
            get { return bCBTCRoute_; }
            set
            {
                if (bCBTCRoute_ != value)
                {
                    bCBTCRoute_ = value;
                    IsStatusChanged = true;

                    if (CBTCButton != null)
                    {
                        CBTCButton.InvalidateVisual();
                    }
                }
            }
        }

        public SmallButton CBTCButton { get; set; }

        public bool IsStatusChanged { get; set; }

        public bool IsFlashing { get { return SColor == SignalColor.DSFail; } }

        public bool FlashFlag { get; set; }

        public bool FlashNameFlag { get; set; }


        protected override void OnRender(DrawingContext dc)
        {
            foreach (var item in lines_)
            {
                item.OnRender(dc, linePen);
            }

            if (bCBTCRoute_)
            {
                lights_[0].OnRender(dc, Brushes.Black);
                DrawEmptyLight_1(dc);
            }
            else
            {
                switch (SColor)
                {
                    case SignalColor.Red:
                        lights_[0].OnRender(dc, Brushes.Red);
                        DrawEmptyLight_1(dc);
                        break;
                    case SignalColor.Green:
                        lights_[0].OnRender(dc, Brushes.Green);
                        DrawEmptyLight_1(dc);
                        break;
                    case SignalColor.Yellow:
                        lights_[0].OnRender(dc, Brushes.Yellow);
                        DrawEmptyLight_1(dc);
                        break;
                        //调车
                    case SignalColor.Blue:
                        lights_[0].OnRender(dc, Brushes.Blue);
                        DrawEmptyLight_1(dc);
                        break;
                    case SignalColor.White:
                        lights_[0].OnRender(dc, Brushes.White);
                        DrawEmptyLight_1(dc);
                        break;
                    case SignalColor.RedYellow:
                        lights_[0].OnRender(dc, Brushes.Red);
                        lights_[1].OnRender(dc, Brushes.Yellow);
                        break;
                    case SignalColor.RedWhite:
                        lights_[0].OnRender(dc, Brushes.Red);
                        lights_[1].OnRender(dc, Brushes.White);
                        break;
                    case SignalColor.GreenYellow:
                        lights_[0].OnRender(dc, Brushes.Green);
                        lights_[1].OnRender(dc, Brushes.Yellow);
                        break;
                    case SignalColor.DoubleYellow:
                        lights_[0].OnRender(dc, Brushes.Yellow);
                        lights_[1].OnRender(dc, Brushes.Yellow);
                        break;
                    case SignalColor.DoubleGreen:
                        lights_[0].OnRender(dc, Brushes.Green);
                        lights_[1].OnRender(dc, Brushes.Green);
                        break;
                    case SignalColor.DSFail:
                        lights_[0].OnRender(dc, FlashFlag ? Brushes.Red : Brushes.Black);
                        DrawEmptyLight_1(dc);
                        break;
                    case SignalColor.DS2Fail:
                        lights_[0].OnRender(dc, defaultBrush_);
                        DrawEmptyLight_1(dc);
                        break;
                    default:
                        break;
                }
            }
            
            if (IsBlocked)
            {
                dc.DrawRectangle(null, blockPen, blockRect_);
            }

            if (!FlashNameFlag)
            {
                dc.DrawText(formattedName_, namePoint_);
            }
        }

        private void DrawEmptyLight_1(DrawingContext dc)
        {
            if (lights_.Count > 1)
            {
                lights_[1].OnRender(dc, Brushes.Black);
            }
        }

        public override void ReadXml(XmlReader reader)
        {
            base.ReadXml(reader);

            foreach (var item in graphics_)
            {
                if (item is Circle)
                {
                    lights_.Add(item as Circle);
                }
                else
                {
                    lines_.Add(item as Line);
                }
            }

            double x = lights_[lights_.Count - 1].Center.X;
            double y = lights_[0].Center.Y > 0 ? Line.LineThickness : -Line.LineThickness;
            blockRect_ = new Rect(new Point(0, y), 
                new Point(x + (x > 0 ? lights_[0].Radius : -lights_[0].Radius), lines_[0].Pt1.Y));
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
        }

        public void UpdateStatus(byte[] recvBuf, int nRecv)
        {
            int color = recvBuf[startByte_] & 0x0f;
            if (color > 0)
            {
                SColor = (SignalColor)(color - 1);
            }

            IsBlocked = (recvBuf[startByte_] >> 4) == 0x05;
            IsFlameWarn = (recvBuf[startByte_ + 1] >> 4) == 0x05;
            IsCBTCRoute = recvBuf[startByte_ + 2] != 0xFE;
        }

        public void SetStartByte(int ID)
        {
            startByte_ = StartByte + ID * 3;
        }

        //public void SetLocalStartByte(CiStartId startID)
        //{
        //    SetStartByte(ID - startID.SignalStart);
        //}

        public void ResetDefaultStatus()
        {
            sColor_ = SignalColor.DSFail;
           
        }

        public void SetCommandMenu()
        {
            AddMenuItem("总取消", OnCancelRoute);
            AddMenuItem("总人解", OnUnlockRoute);
            AddMenuItem("信号关闭", OnCloseSignal);
            AddMenuItem("信号重开", OnReOpenSignal);
        }

        private void OnReOpenSignal(object sender, RoutedEventArgs e)
        {
            //MainWindow.Instance.SetCommandFromMenu(MainWindow.Instance.ReOpenSignalButton, this);
        }

        private void OnCloseSignal(object sender, RoutedEventArgs e)
        {
            //MainWindow.Instance.SetCommandFromMenu(MainWindow.Instance.CloseSignalButton, this);
        }

        private void OnUnlockRoute(object sender, RoutedEventArgs e)
        {
            //MainWindow.Instance.SetCommandFromMenu(MainWindow.Instance.UnlockRouteButton, this);
        }

        private void OnCancelRoute(object sender, RoutedEventArgs e)
        {
            //MainWindow.Instance.SetCommandFromMenu(MainWindow.Instance.CancelRouteButton, this);
        }

        public override string ToString()
        {
            return string.Format("{0, -16} [ 灯色: {1}, 封锁: {2}, CBTC进路: {2} ]", name_, sColor_.ToString(),
                bBlocked_ ? "是" : "否", bCBTCRoute_ ? "是" : "否");
        }
    }
}
