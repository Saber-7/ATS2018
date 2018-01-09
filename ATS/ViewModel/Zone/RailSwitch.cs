using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using 线路绘图工具;

namespace ATS
{
    public class RailSwitch : 线路绘图工具.RailSwitch, IRailway, IFlash
    {
        public RailSwitch()
        {
            ResetDefaultStatus();
        }
        public static int StartByte { get; set; }
        static Pen SingleLockPen_ = new Pen(Brushes.White, 7);

        #region 彭亚枫添加部分
        /*彭亚枫添加部分************/
        //直、正、反1
        double[] lens = new double[3];

        public double[] Lens
        {
            get { return lens; }
            set { lens = value; }
        }

        //交路2
        //List<Route> _routes = new List<Route>();

        //public List<Route> Routes
        //{
        //    get { return _routes; }
        //    set { _routes = value; }
        //}
        //3
        public List<Graphic> Graphics
        {
            get { return graphics_; }
            set { value = graphics_; }
        }

        //4
        private SwitchPosition _State;

        public SwitchPosition State
        {
            get { return _State; }
            set { _State = value; }
        }


        int _Offset;
        public int Offset
        {
            get { return _Offset; }
            set
            {
                if (_Offset != value)
                {
                    _Offset = value;
                }
            }
        }

        private bool axleOccupy_;
        public bool AxleOccupy
        {
            get { return axleOccupy_; }
            set
            {
                if (axleOccupy_ != value)
                {
                    axleOccupy_ = value;
                }
            }
        }

        private bool RouteOccupy_;

        public bool RouteOccupy
        {
            get { return RouteOccupy_; }
            set { RouteOccupy_ = value; }
        }



        private List<UInt16> trainOccupy_ = new List<UInt16>();
        public List<UInt16> TrainOccupy
        {
            get { return trainOccupy_; }
            set { trainOccupy_ = value; }
        }

        List<UInt16> hasNonComTrain_ = new List<UInt16>();
        public List<UInt16> HasNonComTrain
        {
            get { return hasNonComTrain_; }
            set
            {
                if (hasNonComTrain_ != value)
                {
                    hasNonComTrain_ = value;
                }
            }
        }


        //Todolist
        //定位+方向=>方向量集合、长度集合

        ////跑路集合（同一时间只有两条线连通）-> ->
        //Line[] _line = new Line[2];

        //public Line[] Line
        //{
        //    get { return _line; }
        //    set { _line = value; }
        //}

        //void AdjustPar()
        //{ 
        //    if(this.IsLeft)
        //}

        /*彭亚枫添加部分***********************/
        #endregion

        Line normalNail_ = new Line();
        Line reverseNail_ = new Line();
        int startByte_;
        int sectionStartByte_;
        //RightButtonMenuHandler handler_;

        public RailSwitch DoubleSwitch { get; set; }
        public RailSwitch NNSwitch { get; set; } // Normal 2 Normal
        public RailSwitch RSSwitch { get; set; } // Section 2 Reverse
        public bool IsStatusChanged { get; set; }

        SwitchPosition position_ = SwitchPosition.PosNeither;
        internal SwitchPosition Position
        {
            get { return position_; }
            set
            {
                if (position_ != value)
                {
                    position_ = value;
                    IsStatusChanged = true;

                    formattedName_.SetForegroundBrush(value == SwitchPosition.PosError ? Brushes.Red : Brushes.Silver);
                    if (value == SwitchPosition.PosError)
                    {
                        //MainWindow.Instance.SetDeviceFail(Name + "四开");
                        isFailed_ = true;
                    }
                    else
                    {
                        //MainWindow.Instance.UnSetDeviceFail(Name + "四开");
                        isFailed_ = false;
                    }
                }
            }
        }

        //占用标志位
        bool isOccupied_ = false;
        bool IsOccupied
        {
            get { return isOccupied_; }
            set
            {
                if (isOccupied_ != value)
                {
                    isOccupied_ = value;
                    IsStatusChanged = true;
                }
            }
        }

        //进路锁闭
        bool isRouteLock_ = false;
        bool IsRouteLock
        {
            get { return isRouteLock_; }
            set
            {
                if (isRouteLock_ != value)
                {
                    isRouteLock_ = value;
                    IsStatusChanged = true;
                }
            }
        }

        //阻塞？？？
        bool isBlocked_ = false;
        bool IsBlocked
        {
            get { return isBlocked_; }
            set
            {
                if (isBlocked_ != value)
                {
                    isBlocked_ = value;
                    IsStatusChanged = true;
                }
            }
        }


        //默认
        bool isProtected_ = false;
        bool IsProtected
        {
            get { return isProtected_; }
            set
            {
                if (isProtected_ != value)
                {
                    isProtected_ = value;
                    IsStatusChanged = true;
                }
            }
        }

        //单锁
        bool isSingleLock_ = false;
        bool IsSingleLock
        {
            get { return isSingleLock_; }
            set
            {
                if (isSingleLock_ != value)
                {
                    isSingleLock_ = value;
                    IsStatusChanged = true;
                }
            }
        }

        bool isFailed_;
        public bool IsFailed { get { return isFailed_; } }

        public Graphic InsuLine { get; set; }

        public bool IsFlashing { get { return Position == SwitchPosition.PosNeither || position_ == SwitchPosition.PosError; } }

        public bool FlashFlag { get; set; }

        public bool FlashNameFlag { get; set; }
        
        protected override void OnRender(DrawingContext dc)
        {
            UpdateFrontSwitch();
            if (IsBlocked)
            {
                foreach (Line line in graphics_)
                {
                    line.OnRender(dc, Section.BlockPen);
                }
            }
            else if (IsSingleLock)
            {
                foreach (Line line in graphics_)
                {
                    line.OnRender(dc, SingleLockPen_);
                }
            }

            DrawSection(dc, SectionIndexList[0], IsOccupied, IsRouteLock, IsProtected);

            switch (Position)
            {
                case SwitchPosition.PosNormal:
                    Pen normalNailPen = DrawSection(dc, SectionIndexList[1], IsOccupied, IsRouteLock, IsProtected);
                    DrawSection(dc, SectionIndexList[2], false, false, false);
                    normalNail_.OnRender(dc, normalNailPen);
                    break;
                case SwitchPosition.PosReverse:
                    DrawSection(dc, SectionIndexList[1], false, false, false);
                    Pen reverseNailPen = DrawSection(dc, SectionIndexList[2], IsOccupied, IsRouteLock, IsProtected);
                    reverseNail_.OnRender(dc, reverseNailPen);
                    break;
                case SwitchPosition.PosNeither:
                case SwitchPosition.PosError:
                    DrawSection(dc, SectionIndexList[1], IsOccupied, IsRouteLock, IsProtected);
                    DrawSection(dc, SectionIndexList[2], IsOccupied, IsRouteLock, IsProtected);
                    if (IsFlashing && FlashFlag)
                    {
                        normalNail_.OnRender(dc, Section.OccupyPen);
                        reverseNail_.OnRender(dc, Section.OccupyPen);
                    }
                    break;
                default:
                    break;
            }

            if (!FlashNameFlag)
            {
                dc.DrawText(formattedName_, namePoint_);
            }

            if (InsuLine is Line)
            {
                (InsuLine as Line).OnRender(dc, Section.DefaultPen);
            }
        }

        private void UpdateFrontSwitch()
        {
            if (NNSwitch != null)
            {
                if (Position == SwitchPosition.PosNormal && NNSwitch.Position == SwitchPosition.PosReverse)
                {
                    isOccupied_ = false;
                    isRouteLock_ = false;
                }
            }
            else if (RSSwitch != null)
            {
                if (position_ == SwitchPosition.PosNormal && RSSwitch.position_ == SwitchPosition.PosNormal)
                {
                    isOccupied_ = false;
                    isRouteLock_ = false;
                }
            }
        }

        private Pen DrawSection(DrawingContext dc, List<int> sectionIndexs, bool isOccupied, bool isRouteLock, bool isProtected)
        {
            Pen linePen = isOccupied ? Section.OccupyPen :
                    (isRouteLock ? Section.RouteLockPen : 
                    (isProtected ? Section.ProtectPen : Section.DefaultPen));

            foreach (int i in sectionIndexs)
            {
                Line line = (graphics_[i] as Line);
                line.OnRender(dc, linePen);
            }

            return linePen;
        }

        public override void ReadXml(XmlReader reader)
        {
            base.ReadXml(reader);

            normalNail_.Points[0] = reverseNail_.Points[0] = new Point((graphics_[SectionIndexList[0][0]] as Line).Points[1].X, (graphics_[SectionIndexList[0][0]] as Line).Points[1].Y);
            normalNail_.Points[1] = new Point((graphics_[SectionIndexList[1][0]] as Line).Points[0].X, (graphics_[SectionIndexList[1][0]] as Line).Points[0].Y);
            reverseNail_.Points[1] = new Point((graphics_[SectionIndexList[2][0]] as Line).Points[0].X, (graphics_[SectionIndexList[2][0]] as Line).Points[0].Y);
        }

        public void UpdateStatus(byte[] recvBuf, int nRecv)
        {
            const byte TRUE_VALUE = 0X05;
            IsOccupied = (recvBuf[sectionStartByte_ + 1] >> 4) == TRUE_VALUE;
            IsRouteLock = (recvBuf[sectionStartByte_] & 0x0f) == TRUE_VALUE;
            IsBlocked = (recvBuf[sectionStartByte_] >> 4) == TRUE_VALUE;
            IsProtected = (recvBuf[sectionStartByte_ + 1] & 0x0f) == TRUE_VALUE;

            int position = recvBuf[startByte_ + 1] & 0x0f;
            SetPosition(position);
            IsSingleLock = (recvBuf[startByte_] & 0x0f) == TRUE_VALUE;
        }

        public override string ToString()
        {
            return string.Format("{0, -16} [位置: {1}, 占用: {2}, 进路锁: {3}, 封锁: {4}, 单锁: {5}, 保护: {6}]", name_,
                position_.ToString(),
                isOccupied_ ? "是" : "否", 
                isRouteLock_ ? "是" : "否", 
                isBlocked_ ? "是" : "否", 
                isSingleLock_ ? "是" : "否", 
                isProtected_ ? "是" : "否");
        }

        private void SetPosition(int position)
        {
            switch (position)
            {
                case 0x05:
                    Position = SwitchPosition.PosNormal;
                    break;
                case 0x0a:
                    Position = SwitchPosition.PosReverse;
                    break;
                case 0x0c:
                    Position = SwitchPosition.PosNeither;
                    break;
                case 0x03:
                    Position = SwitchPosition.PosError;
                    break;
                default:
                    break;
            }
        }

        public void SetStartByte(int ID,int SectionStart)
        {
            startByte_ = StartByte + ID * 2;
            sectionStartByte_ = Section.StartByte + (SectionID-SectionStart) * 3;
            AddInsulation();
        }

        public void SetStartByte(int ID)
        {
            startByte_ = StartByte + ID * 2;
            sectionStartByte_ = Section.StartByte + SectionID * 3;
            AddInsulation();
        }
        //public void SetLocalStartByte(CiStartId startID)
        //{
        //    SetStartByte(ID - startID.SwitchStart);
        //    sectionStartByte_ = Section.StartByte + (SectionID - startID.SectionStart) * 3;
        //}

        public void ResetDefaultStatus()
        {
            position_ = SwitchPosition.PosError;
            isOccupied_ = true;
            isRouteLock_ = true;
            isBlocked_ = true;
            isSingleLock_ = true;
        }

        #region 右键面板

        //public void SetCommandMenu()
        //{
        //    AddMenuItem("总定位", OnMoveToNormal);
        //    AddMenuItem("总反位", OnMoveToReverse);
        //    handler_ = new RightButtonMenuHandler(this);
        //    AddMenuItem("单锁", OnSingleLock);
        //    AddMenuItem("单解", OnUnSingleLock);
        //}

        //private void OnUnSingleLock(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.Instance.SetCommandFromMenu(MainWindow.Instance.UnSingleLockButton, this);
        //}

        //private void OnSingleLock(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.Instance.SetCommandFromMenu(MainWindow.Instance.SingleLockButton, this);
        //}

        //private void OnMoveToNormal(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.Instance.SetCommandFromMenu(MainWindow.Instance.MoveToNormalButton, this);
        //}

        //private void OnMoveToReverse(object sender, RoutedEventArgs e)
        //{
        //    MainWindow.Instance.SetCommandFromMenu(MainWindow.Instance.MoveToReverseButton, this);
        //}

        #endregion

        public void AddInsulation()
        {
            List<Point> leftPts = new List<Point>();
            //GetLeftPoints(leftPts);
            Line l = this.Graphics[0] as Line;
            leftPts.Add(l.Pt0);

            InsuLine = new Line()
            {
                Pt0 = new Point(leftPts[0].X, leftPts[0].Y - Line.LineThickness),
                Pt1 = new Point(leftPts[0].X, leftPts[0].Y + Line.LineThickness)
            };
        }
    }
}
