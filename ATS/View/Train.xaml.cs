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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ATS.ViewModel;
using 线路绘图工具;
using System.ComponentModel;
using System.Net;
using System.Collections.Concurrent;
using log4net;

namespace ATS
{
    /// <summary>
    /// Train.xaml 的交互逻辑
    /// </summary>
    public partial class Train : UserControl, INotifyPropertyChanged
    {
        private static readonly ILog Log4ATS = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public Train()
        {
            InitializeComponent();
            DataContext = this;
            Points = PointList[0];
            UPdateTrainPos();



        }

        public Train(List<线路绘图工具.GraphicElement> eles,EndPoint rep)
        {
            InitializeComponent();
            DataContext = this;
            //com.ListenControlData();
            Points = PointList[1];
            Elements = eles;
        }

        //寻路、位置更新
        public Train(List<线路绘图工具.GraphicElement> eles,List<ATSRoute> routes,int num)
        {
            InitializeComponent();
            DataContext = this;
            //com.ListenControlData();
            Points = PointList[1];
            Elements = eles;
            Routes = routes;
            TrainGroupNum = num;
            //PathFind("ZHG2", "T0112");
            //FindPath("ZHG2", "T0310", RouteDirection.DIRDOWN);
            //FindSinglePath("ZHG2", "T0310", RouteDirection.DIRDOWN);

            //var t = Res[0];
            //ks = new List<int>();

        }

        public Train(List<线路绘图工具.GraphicElement> eles, List<ATSRoute> routes, int num, ConcurrentDictionary<string, string> section2StationName)
        {
            InitializeComponent();
            DataContext = this;
            //com.ListenControlData();
            Points = PointList[1];
            RecPoints = Rectangle;
            Elements = eles;
            Routes = routes;
            TrainGroupNum = num;
            Section2StationName = section2StationName;
            _TraintState = 列车模式.非车队模式;
            //FindPathSD("ZHG1", "T0407", "T0409", "ZHG2", RouteDirection.DIRDOWN, true);
            //FindPathSD("ZHG2", "T0411", "T0411", "ZHG1", RouteDirection.DIRDOWN, true);
            this.Lab.Tag = this;
        }

        object TrainStateLocker = new object();
        列车模式 _TraintState;

        public 列车模式 TrainState
        {
            get {
                lock(TrainStateLocker)
                return _TraintState;
            }
            set
            {
                if(value!=_TraintState)
                {
                    lock (TrainStateLocker)
                    _TraintState = value;
                }
            }
        }



        bool _IsLive = false;

        //车站和区段的映射关系
        ConcurrentDictionary<string, string> Section2StationName;
        //车辆是否注册
        public bool IsRegistered
        {
            get { return _IsLive; }
            set { _IsLive = value; }
        }

        public GraphicElement NowSection { get; set; }
        public int NowLineNum { get; set; }
        public int PlanLineNum { get; set; }

        //现在到站的序号，相对于起始站
        public int PointOrderNum{ get; set; }

        //存储之前的状态
        //bool _isTop = false;

        //public bool IsStop
        //{
        //    get { return _isTop; }
        //    set { _isTop = value; }
        //}

        #region 外观




        List<PointCollection> _PointList = new List<PointCollection> {
            new PointCollection
            {
            new Point(-20,-7),
            new Point(20,-7),
            new Point(27,0),
            new Point(20,7),
            new Point(-20,7),
            },
            new PointCollection
            {
            new Point(-27,0),
            new Point(-20,7),
            new Point(20,7),
            new Point(20,-7),
            new Point(-20,-7)
            }
        
        };

        public List<PointCollection> PointList
        {
            get { return _PointList; }
            set { _PointList = value; }
        }

        PointCollection _Points ;


        public PointCollection Points
        {
            get { return _Points; }
            set {
                if (value != _Points)
                {
                    _Points = value;
                    RaisePropertyChanged("Points");
                }
                }
        }

       Brush _RecColor = Brushes.Green;

        public Brush RecColor
        {
            get { return _RecColor; }
            set {
                if(_RecColor!=value)
                {
                    _RecColor = value;
                    RaisePropertyChanged("RecColor");
                }

            }
        }

        private Brush _arrowColor = Brushes.Green;

        public Brush ArrowColor
        {
            get
            {
                return _arrowColor;
            }

            set
            {
                if(_arrowColor!=value)
                {
                    _arrowColor = value;
                    RaisePropertyChanged("ArrowColor");
                }

            }
        }
        #endregion

        #region 运行参数

        ActualRunMode _runMode;

        public ActualRunMode RunMode
        {
            get { return _runMode; }
            set {
                if (value != _runMode)
                {
                    _runMode = value;
                    int order = (byte)(_runMode);
                    if (order >= 0 && order <= RectangleColors.Count)
                    RecColor = RectangleColors[order];
                }
            }
        }

        int _TrainGroupNum;

        public int TrainGroupNum
        {
            get { return _TrainGroupNum; }
            set 
            {
                if (_TrainGroupNum != value)
                {
                    _TrainGroupNum = value;
                    RaisePropertyChanged("TrainNum");
                }
            }
        }

        string _TrainOrderNum;
        public string TrainOrderNum
        {
            get { return _TrainOrderNum; }
            set{
                if (_TrainOrderNum != value)
                {
                    _TrainOrderNum = String.Copy(value);
                    RaisePropertyChanged("TrainOrderNum");
                }
            }
        }

        int _DevType;
        public int DevType
        {
            get { return _DevType; }
            set { _DevType = value; }
        }
        //所在位置id
        int _DevId;

        public int DevId
        {
            get { return _DevId; }
            set { _DevId = value; }
        }
        //当前区段偏移量
        double _offSet = 0;

        public double 偏移量
        {
            get { return _offSet; }
            set { _offSet = value; }
        }

        //List<Route> _routes;

        //public List<Route> Routes
        //{
        //    get { return _routes; }
        //    set { _routes = value; }
        //}
        
        List<线路绘图工具.GraphicElement> _elements;

        public List<线路绘图工具.GraphicElement> Elements
        {
          get { return _elements; }
          set { _elements = value; }
        }

        object obj = new object();
        Point _pos = new Point(252,290);
        public Point Pos
        {
            get {
                lock (obj)
                {
                    return _pos;
                }
                }
            set {
                lock (obj)
                {
                    _pos = value; 
                }
            }
        }


        byte _dir = 170;

        public byte Dir
        {
            get { return _dir; }
            set {
                if (value != _dir)
                {
                    _dir = value;
                    if (_dir == 85) ArrowPoints = RightArrow;
                    else if (_dir == 170) ArrowPoints = LeftArrow;
                    else ArrowPoints = null;

                } 
            }
        }
        #endregion

        #region 确定列车位置
        public void UPdateTrainPos()
        {
            Canvas.SetLeft(this, Pos.X);
            Canvas.SetTop(this,Pos.Y);
        }

        public void UpdateTrainPosByXY(Point p)
        {
            this.Dispatcher.Invoke(() =>
                {
                    Canvas.SetLeft(this, p.X);
                    Canvas.SetTop(this, p.Y);
                    Pos = new Point(p.X,p.Y);
                });
        }


        public GraphicElement UPdateTrainPosByOffset(int type, int id, byte dir, double offset)
        {
            Dir = dir;
            this.Dispatcher.Invoke(() =>
            {
                nowid = id;
                nowtype = type;
                GraphicElement nowSection = Elements.Find((线路绘图工具.GraphicElement item) =>
                {
                    if (type == (byte)DeviceType.区段 && item is Section)
                    {
                        Section sc = item as Section;
                        return sc.ID == id;
                    }
                    else if (type == (byte)DeviceType.道岔 && item is RailSwitch)
                    {
                        RailSwitch rs = item as RailSwitch;
                        return rs.ID == id;
                    }
                    else
                    {
                        return false;
                    }
                });

                UpdatePos(nowSection, dir, offset);
                if (Res != null && Res.Count != 0)
                {
                    ATSRoute OpenRoute = null;
                    List<ATSRoute> routes = Res[0].Routes;
                    OpenRoute = routes.Find((ATSRoute r) =>
                    {
                        foreach (Device item in r.IncomingSections)
                        {
                            if (item == nowSection)
                                return true;
                        }
                        return false;
                    });
                    this.OpenRoute = OpenRoute;
                }
                NowSection = nowSection;
                return NowSection;
            });
            return null;

        }

        public Vector[] DirVector = new Vector[2];
        public Point StartPoint { get; set; }
        public 线路绘图工具.GraphicElement NowG, PreG;
        public double k=1;//比例尺

        public int pretype, nowtype, preid, nowid;


        //直道、定位、反位
        public double seclen, norlen, revlen;
        /// <summary>
        /// 更新方向向量，当前的的Element
        /// </summary>
        /// <param name="item"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        void UpdatePos(线路绘图工具.GraphicElement item,byte dir,double offset)
        {
            Dir = dir;
            if (item != null)
            {
                Point origin = new Point(Canvas.GetLeft(item), Canvas.GetTop(item));
                Log4ATS.Info(origin.X + "   " + origin.Y+" "+offset);
                Vector LineDirVector=new Vector();
                Canvas.SetZIndex(this, -1);
                #region 直道
                if (item is Section)
                {
                    Section s = (Section)item;
                    //左走
                    if (dir == (byte)(TrainDir.左行))
                    {

                        if (true)
                        //if (preid!=nowid||pretype!=nowtype)
                        {
                            //D 0斜 1直
                            线路绘图工具.Line l = s.Graphics.Last() as 线路绘图工具.Line;
                            StartPoint = l.Pt1;
                            DirVector[0] = CalDirUnitVector(l.Pt1, l.Pt0);
                            l = s.Graphics.First() as 线路绘图工具.Line;
                            DirVector[1] = CalDirUnitVector(l.Pt1, l.Pt0);
                            k = (s.Lens[0] + s.Lens[1]) / s.Distance;
                            preid = nowid;
                            pretype = nowtype;
                        }
                        offset *= k;
                        

                        //l 0直道长度 1斜线长度
                        if (offset > s.Lens[1])
                        {
                            LineDirVector = s.DirVectorList.First();
                            double len = offset - s.Lens[1];
                            double x = len * DirVector[1].X + s.Lens[1] * DirVector[0].X + StartPoint.X + origin.X;
                            double y = len * DirVector[1].Y + s.Lens[1] * DirVector[0].Y + StartPoint.Y + origin.Y;
                            UpdateTrainPosByXY(new Point(x, y));
                        }
                        else
                        {
                            LineDirVector = s.DirVectorList.Last();
                            double x = offset * DirVector[0].X + StartPoint.X + origin.X;
                            double y = offset * DirVector[0].Y + StartPoint.Y + origin.Y;
                            UpdateTrainPosByXY(new Point(x, y));
                        }
                    }
                    else//右行
                    {
                        if (true)
                        //if (preid != nowid || pretype != nowtype)
                        {
                            //D 0斜 1直
                            线路绘图工具.Line l = s.Graphics.First() as 线路绘图工具.Line;
                            StartPoint = l.Pt0;
                            DirVector[0] = CalDirUnitVector(l.Pt0, l.Pt1);
                            l = s.Graphics.Last() as 线路绘图工具.Line;
                            DirVector[1] = CalDirUnitVector(l.Pt0, l.Pt1);
                            k = (s.Lens[0] + s.Lens[1]) / s.Distance;
                            preid = nowid;
                            pretype = nowtype;
                        }
                        offset *= k;
                        //l 0直道长度 1斜线长度
                        if (offset > s.Lens[0])
                        {
                            LineDirVector = s.DirVectorList.Last();
                            double len = offset - s.Lens[0];
                            double x = len * DirVector[1].X + s.Lens[0] * DirVector[0].X + StartPoint.X + origin.X;
                            double y = len * DirVector[1].Y + s.Lens[0] * DirVector[0].Y + StartPoint.Y + origin.Y;
                            UpdateTrainPosByXY(new Point(x, y));
                        }
                        else
                        {
                            LineDirVector = s.DirVectorList.First();
                            double x = offset * DirVector[0].X + StartPoint.X + origin.X;
                            double y = offset * DirVector[0].Y + StartPoint.Y + origin.Y;
                            UpdateTrainPosByXY(new Point(x, y));
                        }

                    }
                }
                #endregion
                #region 弯道
                else if (item is RailSwitch)
                {
                    RailSwitch rs = item as RailSwitch;
                    if (rs.Position == RailSwitch.SwitchPosition.PosNormal)
                    {
                        LineDirVector = rs.DirVectorList.First();
                        if (dir == (byte)(TrainDir.左行))
                        {
                            //左行驶
                            //if (preid != nowid || pretype != nowtype)
                            if (true)
                            {
                                //定 左
                                k = (rs.Lens[0] + rs.Lens[1]) / rs.NormalDistance;
                                if (!rs.IsLeft)
                                {
                                    
                                    线路绘图工具.Line l = rs.Graphics.First() as 线路绘图工具.Line;
                                    DirVector[0] = CalDirUnitVector(l.Pt1, l.Pt0);
                                    StartPoint = (rs.Graphics[rs.SectionIndexList[1].Last()] as 线路绘图工具.Line).Pt1;
                                    DirVector[1] = DirVector[0];//因为直线所以简化处理
                                }
                                else
                                {
                                    线路绘图工具.Line l = rs.Graphics.First() as 线路绘图工具.Line;
                                    DirVector[0] = CalDirUnitVector(l.Pt0, l.Pt1);
                                    StartPoint = (rs.Graphics[rs.SectionIndexList[0].First()] as 线路绘图工具.Line).Pt0;
                                    DirVector[1] = DirVector[0];
                                }

                                preid = nowid;
                                pretype = nowtype;
                            }
                            offset *= k;
                            double x = DirVector[0].X * offset + StartPoint.X + origin.X;
                            double y = DirVector[0].Y * offset + StartPoint.Y + origin.Y;
                            UpdateTrainPosByXY(new Point(x, y));
                        }
                        else
                        {
                            //右行驶
                            if (true)
                            //if (preid != nowid || pretype != nowtype)
                            {

                                //定右
                                k = (rs.Lens[0] + rs.Lens[1]) / rs.NormalDistance;
                                if (!rs.IsLeft)
                                {
                                    StartPoint = (rs.Graphics[rs.SectionIndexList[0].First()] as 线路绘图工具.Line).Pt0;
                                    线路绘图工具.Line l = rs.Graphics.First() as 线路绘图工具.Line;
                                    DirVector[0] = CalDirUnitVector(l.Pt0, l.Pt1);
                                    DirVector[1] = DirVector[0];//因为直线所以简化处理
                                }
                                else
                                {
                                    StartPoint = (rs.Graphics[rs.SectionIndexList[1].Last()] as 线路绘图工具.Line).Pt1;
                                    线路绘图工具.Line l = rs.Graphics.First() as 线路绘图工具.Line;
                                    DirVector[0] = CalDirUnitVector(l.Pt1, l.Pt0);
                                    DirVector[1] = DirVector[0];//因为直线所以简化处理
                                }

                                preid = nowid;
                                pretype = nowtype;
                            }
                            offset *= k;
                            double x = DirVector[0].X * offset + StartPoint.X + origin.X;
                            double y = DirVector[0].Y * offset + StartPoint.Y + origin.Y;
                            UpdateTrainPosByXY(new Point(x, y));
                        }

                    }
                    else
                    {
                        //反位左行
                        if (dir == (byte)(TrainDir.左行))
                        {
                            //if (preid != nowid || pretype != nowtype)
                            if (true)
                            {
                                if (!rs.IsLeft)
                                {
                                    线路绘图工具.Line l = rs.Graphics[rs.SectionIndexList[2].Last()] as 线路绘图工具.Line;
                                    DirVector[0] = CalDirUnitVector(l.Pt1, l.Pt0);
                                    StartPoint = l.Pt1;
                                    l = rs.Graphics[rs.SectionIndexList[0].First()] as 线路绘图工具.Line;
                                    DirVector[1] = CalDirUnitVector(l.Pt1, l.Pt0);
                                }
                                else
                                {
                                    线路绘图工具.Line l = rs.Graphics[rs.SectionIndexList[0].First()] as 线路绘图工具.Line;
                                    DirVector[0] = CalDirUnitVector(l.Pt0, l.Pt1);
                                    StartPoint = l.Pt0;
                                    l = rs.Graphics[rs.SectionIndexList[2].Last()] as 线路绘图工具.Line;
                                    DirVector[1] = CalDirUnitVector(l.Pt0, l.Pt1);
                                }

                                //直 定 反
                                k = (rs.Lens[0] + rs.Lens[2]) / rs.ReverseDistance;
                                preid = nowid;
                                pretype = nowtype;
                            }
                            offset *= k;
                            if (!rs.IsLeft)
                            {

                                if (offset > rs.Lens[2])
                                {
                                    LineDirVector = rs.DirVectorList[0];
                                    double len = offset - rs.Lens[2];
                                    double x = DirVector[0].X * rs.Lens[2] + DirVector[1].X * len + StartPoint.X + origin.X;
                                    double y = DirVector[0].Y * rs.Lens[2] + DirVector[1].Y * len + StartPoint.Y + origin.Y;
                                    UpdateTrainPosByXY(new Point(x, y));
                                }
                                else
                                {
                                    LineDirVector = rs.DirVectorList[2];
                                    double x = DirVector[0].X * offset + StartPoint.X + origin.X;
                                    double y = DirVector[0].Y * offset + StartPoint.Y + origin.Y;
                                    UpdateTrainPosByXY(new Point(x, y));
                                }
                            }
                            else
                            {
                                if (offset > rs.Lens[0])
                                {
                                    LineDirVector=rs.DirVectorList[2];
                                    double len = offset - rs.Lens[0];
                                    double x = DirVector[0].X * rs.Lens[0] + DirVector[1].X * len + StartPoint.X + origin.X;
                                    double y = DirVector[0].Y * rs.Lens[0] + DirVector[1].Y * len + StartPoint.Y + origin.Y;
                                    UpdateTrainPosByXY(new Point(x, y));
                                }
                                else
                                {
                                    LineDirVector = rs.DirVectorList[0];
                                    double x = DirVector[0].X * offset + StartPoint.X + origin.X;
                                    double y = DirVector[0].Y * offset + StartPoint.Y + origin.Y;
                                    UpdateTrainPosByXY(new Point(x, y));
                                }
                            }

                        }
                        else
                        {
                            //反位右行
                            if (true)
                            //if (preid != nowid || pretype != nowtype)
                            {
                                if (!rs.IsLeft)
                                {
                                    线路绘图工具.Line l = rs.Graphics[rs.SectionIndexList[0].First()] as 线路绘图工具.Line;
                                    DirVector[0] = CalDirUnitVector(l.Pt0, l.Pt1);
                                    StartPoint = l.Pt0;
                                    l = rs.Graphics[rs.SectionIndexList[2].Last()] as 线路绘图工具.Line;
                                    DirVector[1] = CalDirUnitVector(l.Pt0, l.Pt1);
                                }
                                else
                                {
                                    线路绘图工具.Line l = rs.Graphics[rs.SectionIndexList[2].Last()] as 线路绘图工具.Line;
                                    DirVector[0] = CalDirUnitVector(l.Pt1, l.Pt0);
                                    StartPoint = l.Pt1;
                                    l = rs.Graphics[rs.SectionIndexList[0].Last()] as 线路绘图工具.Line;
                                    DirVector[1] = CalDirUnitVector(l.Pt1, l.Pt0);
                                }

                                //直 定 反
                                k = (rs.Lens[0] + rs.Lens[2]) / rs.ReverseDistance;
                                preid = nowid;
                                pretype = nowtype;
                            }
                            offset *= k;
                            if (!rs.IsLeft)
                            {
                                if (offset > rs.Lens[0])
                                {
                                    LineDirVector = rs.DirVectorList[2];
                                    double len = offset - rs.Lens[0];
                                    double x = DirVector[0].X * rs.Lens[0] + len * DirVector[1].X + StartPoint.X + origin.X;
                                    double y = DirVector[0].Y * rs.Lens[0] + len * DirVector[1].Y + StartPoint.Y + origin.Y;
                                    UpdateTrainPosByXY(new Point(x, y));
                                }
                                else
                                {
                                    LineDirVector = rs.DirVectorList[0];
                                    double x = DirVector[0].X * offset + StartPoint.X + origin.X;
                                    double y = DirVector[0].Y * offset + StartPoint.Y + origin.Y;
                                    UpdateTrainPosByXY(new Point(x, y));
                                }
                            }
                            else
                            {
                                if (offset > rs.Lens[2])
                                {
                                    LineDirVector = rs.DirVectorList[0];
                                    double len = offset - rs.Lens[2];
                                    double x = DirVector[0].X * rs.Lens[2] + len * DirVector[1].X + StartPoint.X + origin.X;
                                    double y = DirVector[0].Y * rs.Lens[2] + len * DirVector[1].Y + StartPoint.Y + origin.Y;
                                    UpdateTrainPosByXY(new Point(x, y));
                                }
                                else
                                {
                                    LineDirVector = rs.DirVectorList[2];
                                    double x = DirVector[0].X * offset + StartPoint.X + origin.X;
                                    double y = DirVector[0].Y * offset + StartPoint.Y + origin.Y;
                                    UpdateTrainPosByXY(new Point(x, y));
                                }
                            }

                        }
                    }

                }
                #endregion
          
                if (item is RailSwitch)
                {
                    RailSwitch rs = item as RailSwitch;
                    if (!rs.IsLeft)
                    {

                    }
                    else
                    {
                        LineDirVector.Negate();
                    }
                }
                this.TrainRotate(LineDirVector, dir);
            }

        }


        //小车偏移轨道的距离
        int offset = 10;
        //向上偏移还是向下偏移-1上1下
        int offdir = -1;
        void TrainRotate(Vector LineDirVector,int trainDir)
        { 
            Vector TrainDirVector=new Vector(trainDir==(byte)TrainDir.左行?-1:1,0);
            Vector NorVector = new Vector(TrainDirVector.Y, TrainDirVector.X * offset*offdir);
            double angle =( -Math.Atan2(TrainDirVector.Y, TrainDirVector.X) + Math.Atan2(LineDirVector.Y, LineDirVector.X) )* 180 / Math.PI;
            if (trainDir == (byte)(TrainDir.左行))
                angle -= 180;
            Matrix matrix=new Matrix();
            matrix.Translate(NorVector.X, NorVector.Y);
            matrix.Rotate(angle);
            this.RenderTransform = new MatrixTransform(matrix);
        }


        /// <summary>
        /// 求解单位方向向量
        /// </summary>
        /// <param name="former"></param>
        /// <param name="latter"></param>
        /// <returns></returns>
        Vector CalDirUnitVector(Point former, Point latter)
        {
            Vector v1 = new Vector(former.X, former.Y);
            Vector v2 = new Vector(latter.X, latter.Y);
            Vector resv = v2 - v1;
            resv.Normalize();
            return resv;
        }
        #endregion

        #region 进路寻路算法dfs


        List<ATS.ATSRoute> _routes;

        List<ATS.ATSRoute> Routes
        {
            get { return _routes; }
            set { _routes = value; }
        }


        public void FindPath(string src, string tar,RouteDirection dir)
        {
            Device now = new Device() { Name = src };
            List<OptionalRoutes> res = new List<OptionalRoutes>();
            SearchPathBy(now, res, new OptionalRoutes(), src, tar, 0, new HashSet<int>(),dir);
            res.Sort();
            this.Res = res;
        }

        ///// <summary>
        ///// 假设折返时折返时返回的对面的站台
        ///// </summary>
        ///// <param name="src">起点</param>
        ///// <param name="tar">终点</param>
        ///// <param name="?"></param>
        ///// <param name="?"></param>
        //public void FindPathSD(string src,string tar,RouteDirection dir,bool isReturn)
        //{
        //    List<OptionalRoutes> res = new List<OptionalRoutes>();
        //    List<ATSRoute> nowRoutes = Routes.FindAll((ATSRoute ar) =>
        //    {
        //        foreach (var de in ar.InSections)
        //        {
        //            if (de.Name == src) return true;
        //        }
        //        return false;
        //    });
        //    for (int i = 0; null != nowRoutes && i < nowRoutes.Count; i++)
        //    {
        //        SearchSinglePath(nowRoutes[i], res, new OptionalRoutes(), tar, 0, new HashSet<ATSRoute>(), dir);
        //    }
        //    res.Sort();
        //    if (isReturn)
        //    {

        //        foreach (RouteDirection dirItem in Enum.GetValues(typeof(RouteDirection)))
        //        {
        //            if (dirItem != dir)
        //            {
        //                dir = dirItem;
        //                break;
        //            }
        //        }
        //        string srcname =null;
        //        try
        //        {
        //            srcname = Section2StationName[src];
        //        }
        //        catch
        //        {
        //            MessageBox.Show("区段名称有误，请检查后重新输入");
        //            return;
        //        }
                
        //        foreach (var item in Section2StationName)
        //        {
        //            if (srcname == item.Value && src != item.Key)
        //            {
        //                src = item.Key;
        //                break;
        //            }

        //        }
        //        tar = src;

        //        nowRoutes = Routes.FindAll((ATSRoute ar) =>
        //        {
        //            foreach (var de in ar.InSections)
        //            {
        //                if (de.Name == tar) return true;
        //            }
        //            return false;
        //        });

        //        List<OptionalRoutes> nextRes = new List<OptionalRoutes>();
        //        for (int i = 0; null != nowRoutes && i < nowRoutes.Count; i++)
        //        {
        //            SearchSinglePath(nowRoutes[i], res, new OptionalRoutes(), tar, 0, new HashSet<ATSRoute>(), dir);
        //        }
        //        nextRes.Sort();
        //        List<OptionalRoutes> CombineRes = new List<OptionalRoutes>();
        //        int j = 0;
        //        for (; (res != null && nextRes != null && j < res.Count && j < nextRes.Count); j++)
        //        {
        //            res[j].ExtendLength(nextRes[j]);
        //        }
        //        try
        //        {
        //            res.RemoveRange(j, res.Count - j);
        //        }
        //        catch
        //        {

        //        }

        //        this.Res = res;

        //    }
        //    else
        //    {

        //        this.Res = res;
        //    }

        //}
        int Yoffset=20;

        public void FindPathSD(string src1, string tar1, string src2, string tar2, RouteDirection dir, bool isReturn)
        {
            List<OptionalRoutes> res1 = new List<OptionalRoutes>();
            List<OptionalRoutes> res2 = new List<OptionalRoutes>();
            int minY, maxY;
            List<ATSRoute> nowRoutes = Routes.FindAll((ATSRoute ar) =>
            {
                foreach (var de in ar.InSections)
                {
                    if (de.Name == src1) return true;
                }
                return false;
            });

            List<ATSRoute> tarRoutes = Routes.FindAll((ATSRoute ar) =>
            {
                foreach (var de in ar.InSections)
                {
                    if (de.Name == tar1) return true;
                }
                //foreach (var de in ar.InCommingSections)
                //{
                //    if (de.Name == src1) return true;
                //}
                return false;
            });

            double y1 = Canvas.GetTop(nowRoutes.First().StartSignal);
            double y2 = Canvas.GetTop(tarRoutes.First().StartSignal);
            maxY = (int)Math.Max(y1, y2)+Yoffset;
            minY = (int)Math.Min(y1, y2)-Yoffset;
            for (int i = 0; null != nowRoutes && i < nowRoutes.Count; i++)
            {
                SearchSinglePath(nowRoutes[i], res1, new OptionalRoutes(), tar1, 0, new HashSet<ATSRoute>(), dir,maxY,minY);
            }
            if (isReturn == false)
            {
                res1.Sort();
                this.Res = res1;
            }
            else
            {
                foreach (RouteDirection Dir in Enum.GetValues(typeof(RouteDirection)))
                {
                    if (dir != Dir)
                    {
                        dir = Dir;
                        break;
                    }
                }

                nowRoutes = Routes.FindAll((ATSRoute ar) =>
                {
                    foreach (var de in ar.InSections)
                    {
                        if (de.Name == src2) return true;
                    }
                    foreach (var de in ar.IncomingSections)
                    {
                        if (de.Name == src2) return true;
                    }
                    return false;
                });
                tarRoutes = Routes.FindAll((ATSRoute ar) =>
                {
                    foreach (var de in ar.InSections)
                    {
                        if (de.Name == tar2) return true;
                    }
                    //foreach (var de in ar.InCommingSections)
                    //{
                    //    if (de.Name == src1) return true;
                    //}
                    return false;
                });
                y1 = Canvas.GetTop(nowRoutes.First().StartSignal);
                y2 = Canvas.GetTop(tarRoutes.First().StartSignal);
                maxY = (int)Math.Max(y1, y2)+Yoffset;
                minY = (int)Math.Min(y1, y2)-Yoffset;
                for (int i = 0; null != nowRoutes && i < nowRoutes.Count; i++)
                {
                    OptionalRoutes tempRoutes = new OptionalRoutes();
                    tempRoutes.Routes.Add(nowRoutes[i]);
                    SearchSinglePath(nowRoutes[i], res2, tempRoutes, tar2, 0, new HashSet<ATSRoute>(), dir,maxY,minY);
                }
                int k1 = 0, k2 = 0, k = 0;
                res1.Sort();
                res2.Sort();
                while (res1 != null && res2 != null && k1 < res1.Count && k2 < res2.Count)
                {
                    res1[k1].Routes.AddRange(res2[k2].Routes);
                    res1[k1].Distance += res2[k2].Distance;
                    k1++;
                    k2++;
                }
                k = Math.Max(k1, k2);
                this.Res = new List<OptionalRoutes>();
                for (int i = 0; i < k; i++)
                {
                    this.Res.Add(res1[i]);
                }
            }


        }

        public void FindSinglePath(string src, string tar, RouteDirection dir)
        {
            List<OptionalRoutes> res = new List<OptionalRoutes>();
            List<ATSRoute> nowRoutes = Routes.FindAll((ATSRoute ar) =>
                {
                    foreach (var de in ar.InSections)
                    {
                        if (de.Name == src) return true;
                    }
                    return false;
                });
            List<ATSRoute>  tarRoutes = Routes.FindAll((ATSRoute ar) =>
            {
                foreach (var de in ar.InSections)
                {
                    if (de.Name == tar) return true;
                }
                //foreach (var de in ar.InCommingSections)
                //{
                //    if (de.Name == src1) return true;
                //}
                return false;
            });
            double y1 = Canvas.GetTop(nowRoutes.First().StartSignal);
            double y2 = Canvas.GetTop(tarRoutes.First().StartSignal);
            int maxY = (int)Math.Max(y1, y2)+Yoffset;
            int minY = (int)Math.Min(y1, y2)-Yoffset;
            for (int i = 0; null != nowRoutes && i < nowRoutes.Count; i++)
            { 
                SearchSinglePath(nowRoutes[i],res,new OptionalRoutes(),tar,0,new HashSet<ATSRoute>(),dir,maxY,minY);
            }

        }

        /// <summary>
        /// 存放查找结果
        /// </summary>
        List<OptionalRoutes> _res;
        object reslock = new object(); 

        internal List<OptionalRoutes> Res
        {
            get { lock (reslock) return _res; }
            set { lock (reslock) _res = value; }
        }

        //递归深度和最大结果数
        readonly int deepth = 20;
        readonly int MaxResNum = 12;

        /// <summary>
        /// bt爆搜寻路
        /// </summary>
        /// <param name="Now"></param>
        /// <param name="res"></param>
        /// <param name="tempRoutes"></param>
        /// <param name="src"></param>
        /// <param name="tar"></param>
        /// <param name="k"></param>
        /// <param name="used"></param>
        void SearchPathBy(Device Now, List<OptionalRoutes> res, OptionalRoutes tempRoutes, string src, string tar, int k, HashSet<int> used,RouteDirection routedir)
        {
            if (k > deepth || res.Count > MaxResNum)
                return;
            if (Now.Name == tar)
            {
                tempRoutes.UpdateDistance();
                res.Add(new OptionalRoutes(tempRoutes));
            }
            else
            {
                List<ATSRoute> PossibleRoutes = Routes.FindAll((ATSRoute route) =>
                {
                    foreach (Device d in route.IncomingSections)
                    {
                        if (d.Name == Now.Name && !used.Contains(route.ID)) return true;
                    }
                    return false;
                });
                if (PossibleRoutes != null)
                {
                    foreach (ATSRoute route in PossibleRoutes)
                    {
                        if (route.Dir==routedir)
                        {
                            tempRoutes.Routes.Add(route);
                            used.Add(route.ID);
                            SearchPathBy(route.InSections.Last(), res, tempRoutes, src, tar, k + 1, used,routedir);
                            tempRoutes.Routes.Remove(tempRoutes.Routes.Last());
                            used.Remove(route.ID);
                        }

                    }
                }
            }
        }



        /// <summary>
        /// bt爆搜寻路（可搜折返）,单向寻路,以进路为单位搜索
        /// </summary>
        /// <param name="Now"></param>
        /// <param name="res"></param>
        /// <param name="tempRoutes"></param>
        /// <param name="src"></param>
        /// <param name="tar"></param>
        /// <param name="k"></param>
        /// <param name="used"></param>
        void SearchSinglePath(ATSRoute Now, List<OptionalRoutes> res, OptionalRoutes tempRoutes, string tar, int k, HashSet<ATSRoute> routeUsed, RouteDirection routedir,int maxY,int minY)
        {
            if (k > deepth || res.Count >MaxResNum)
                return;
            foreach (var de in Now.InSections)
            {
                if (de.Name == tar)
                {
                    tempRoutes.UpdateDistance();
                    res.Add(new OptionalRoutes(tempRoutes));
                    return;
                }
            }

            foreach (var possibleRoute in Now.OptionalRoutes)
            {
                ATS.Signal signal = possibleRoute.StartSignal as ATS.Signal;
                double y = Canvas.GetTop(signal);
                if (possibleRoute.Dir == routedir && y >= minY && y <= maxY&&!routeUsed.Contains(possibleRoute))
                //if (possibleRoute.Dir == routedir)
                {
                    tempRoutes.Routes.Add(possibleRoute);
                    routeUsed.Add(possibleRoute);
                    SearchSinglePath(tempRoutes.Routes.Last(), res, tempRoutes, tar, k + 1, routeUsed, routedir,maxY,minY);
                    tempRoutes.Routes.Remove(tempRoutes.Routes.Last());
                    routeUsed.Remove(possibleRoute);
                }
            }


        }
        #endregion


        #region 进路处置

        public  ATSRoute OpenRoute{get;set;}

        #endregion

        internal void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;




        PointCollection _RecPoints, _ArrowPoints;

        public PointCollection RecPoints
        {
            get
            {
                return _RecPoints;
            }

            set
            {
                if (_RecPoints != value)
                {
                    _RecPoints = value;
                    RaisePropertyChanged("RecPoints");
                }
            }
        }

        public PointCollection ArrowPoints
        {
            get
            {
                return _ArrowPoints;
            }

            set
            {
                if (_ArrowPoints != value)
                {
                    _ArrowPoints = value;
                    RaisePropertyChanged("ArrowPoints");
                }

            }
        }


        #region 列车外观相关静态内容

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

        readonly List<Brush> ArrowColors = new List<Brush>()
        {
            new SolidColorBrush(Colors.Black),
            new SolidColorBrush(Colors.Blue),
            new SolidColorBrush(Colors.Yellow),
            new SolidColorBrush(Colors.Red),
            new SolidColorBrush(Colors.White),
        };
        bool _IsEmergent=false;

        readonly private Object lockobj = new object();
        public bool IsEmergent
        {
            get
            {
                lock(lockobj)
                return _IsEmergent;
            }

            set
            {
                lock(lockobj)
                _IsEmergent = value;
            }
        }







        #endregion
    }

    /// <summary>
    /// 左==上行，右=下行
    /// </summary>
    enum TrainDir:byte
    {
        左行=0xaa,
        右行=0x55
    }

    enum DeviceType : byte
    { 
        区段=1,
        道岔=2
    }

    public enum 列车模式
    {
        车队模式,
        非车队模式
    }
}
