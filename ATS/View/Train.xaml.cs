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

namespace ATS
{
    /// <summary>
    /// Train.xaml 的交互逻辑
    /// </summary>
    public partial class Train : UserControl, INotifyPropertyChanged
    {
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
            TrainNum = num;
            //PathFind("ZHG2", "T0112");
            //FindPath("ZHG2", "T0310", RouteDirection.DIRDOWN);
            //var t = Res[0];
            //ks = new List<int>();

        }
        //public List<int> ks = new List<int>();

        bool _IsLive = false;

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
        bool _isTop = false;

        public bool IsStop
        {
            get { return _isTop; }
            set { _isTop = value; }
        }

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

       Brush _TrainColor=Brushes.YellowGreen;

        public Brush TrainColor
        {
            get { return _TrainColor; }
            set { _TrainColor = value; }
        }

        #endregion 

        #region 运行参数

        RunMode _runMode=RunMode.CBTC模式;

        public RunMode RunMode
        {
            get { return _runMode; }
            set {
                if (value != _runMode)
                {
                    _runMode = value;
                    if (_runMode == RunMode.CBTC模式) TrainColor = Brushes.YellowGreen;
                    else TrainColor = Brushes.Purple;
                }
            }
        }

        int _TrainNum;

        public int TrainNum
        {
            get { return _TrainNum; }
            set 
            {
                if (_TrainNum != value)
                {
                    _TrainNum = value;
                    RaisePropertyChanged("TrainNum");
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
                    if (_dir == 85) Points = PointList[0];
                    else Points =PointList[1];

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

        public void UPdateTrainPosByOffset(int type,int id,byte dir,double offset)
        {
            Dir = dir;
            this.Dispatcher.Invoke(() =>
                {
                    nowid = id;
                    nowtype = type;
                    GraphicElement nowSection = Elements.Find((线路绘图工具.GraphicElement item) =>
                    {
                        if (type == 1&&item is Section)
                        {
                            Section sc = item as Section;
                            return sc.ID == id;
                        }
                        else if (type == 2&&item is RailSwitch)
                        {
                            RailSwitch rs = item as RailSwitch;
                            return rs.ID == id;
                        }
                        else
                        {
                            return false;
                        }
                    });
                    if (Res != null&&Res.Count!=0)
                    {
                        OpenRoute = null;
                        UpdatePos(nowSection, dir, offset);
                        List<ATSRoute> routes = Res[0].Routes;
                        OpenRoute = routes.Find((ATSRoute r) =>
                            {
                                foreach (var item in r.InSections)
                                {
                                    if (item == nowSection)
                                        return true;
                                }
                                return false;
                            });
                        if (OpenRoute != null)
                        {
                            OpenRoute = routes[routes.IndexOf(OpenRoute) + 1];
                        }
                        else
                        {
                            OpenRoute = Res[0].Routes.Find((ATSRoute route) =>
                            {
                                return nowSection == route.InCommingSections.First();
                            });
                        }

                    }
                    NowSection = nowSection;
                });

        }

        public Point[] DirPos = new Point[2];
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
            if (item != null)
            {
                Point origin = new Point(Canvas.GetLeft(item), Canvas.GetTop(item));
                #region 直道
                if (item is Section)
                {
                    Section s = (Section)item;
                    //左走
                    if (dir == 0xaa)
                    {
                        if (true)
                        //if (preid!=nowid||pretype!=nowtype)
                        {
                            //D 0斜 1直
                            线路绘图工具.Line l = s.Graphics.Last() as 线路绘图工具.Line;
                            StartPoint = l.Pt1;
                            DirPos[0] = CalDirVector(l.Pt1, l.Pt0);
                            l = s.Graphics.First() as 线路绘图工具.Line;
                            DirPos[1] = CalDirVector(l.Pt1, l.Pt0);
                            k = (s.Lens[0] + s.Lens[1]) / s.Distance;
                            preid = nowid;
                            pretype = nowtype;
                        }
                        offset *= k;

                        //l 0直道长度 1斜线长度
                        if (offset > s.Lens[1])
                        {
                            double len = offset - s.Lens[1];
                            double x = len * DirPos[1].X + s.Lens[1] * DirPos[0].X + StartPoint.X + origin.X;
                            double y = len * DirPos[1].Y + s.Lens[1] * DirPos[0].Y + StartPoint.Y + origin.Y;
                            UpdateTrainPosByXY(new Point(x, y));
                        }
                        else
                        {
                            double x = offset * DirPos[0].X + StartPoint.X + origin.X;
                            double y = offset * DirPos[0].Y + StartPoint.Y + origin.Y;
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
                            DirPos[0] = CalDirVector(l.Pt0, l.Pt1);
                            l = s.Graphics.Last() as 线路绘图工具.Line;
                            DirPos[1] = CalDirVector(l.Pt0, l.Pt1);
                            k = (s.Lens[0] + s.Lens[1]) / s.Distance;
                            preid = nowid;
                            pretype = nowtype;
                        }
                        offset *= k;
                        //l 0直道长度 1斜线长度
                        if (offset > s.Lens[0])
                        {
                            double len = offset - s.Lens[0];
                            double x = len * DirPos[1].X + s.Lens[0] * DirPos[0].X + StartPoint.X + origin.X;
                            double y = len * DirPos[1].Y + s.Lens[0] * DirPos[0].Y + StartPoint.Y + origin.Y;
                            UpdateTrainPosByXY(new Point(x, y));
                        }
                        else
                        {
                            double x = offset * DirPos[0].X + StartPoint.X + origin.X;
                            double y = offset * DirPos[0].Y + StartPoint.Y + origin.Y;
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
                        if (dir == 0xaa)
                        {
                            //左行驶
                            //if (preid != nowid || pretype != nowtype)
                            if (true)
                            {
                                //直 定 反
                                k = (rs.Lens[0] + rs.Lens[1]) / rs.NormalDistance;
                                if (!rs.IsLeft)
                                {
                                    //ok
                                    线路绘图工具.Line l = rs.Graphics.First() as 线路绘图工具.Line;
                                    DirPos[0] = CalDirVector(l.Pt1, l.Pt0);
                                    StartPoint = (rs.Graphics[rs.SectionIndexList[1].Last()] as 线路绘图工具.Line).Pt1;
                                    DirPos[1] = DirPos[0];//因为直线所以简化处理
                                }
                                else
                                {
                                    线路绘图工具.Line l = rs.Graphics.First() as 线路绘图工具.Line;
                                    DirPos[0] = CalDirVector(l.Pt0, l.Pt1);
                                    StartPoint = (rs.Graphics[rs.SectionIndexList[0].First()] as 线路绘图工具.Line).Pt0;
                                    DirPos[1] = DirPos[0];
                                }

                                preid = nowid;
                                pretype = nowtype;
                            }
                            offset *= k;
                            double x = DirPos[0].X * offset + StartPoint.X + origin.X;
                            double y = DirPos[0].Y * offset + StartPoint.Y + origin.Y;
                            UpdateTrainPosByXY(new Point(x, y));
                        }
                        else
                        {
                            //右行驶
                            if (true)
                            //if (preid != nowid || pretype != nowtype)
                            {

                                //直 定 反
                                k = (rs.Lens[0] + rs.Lens[1]) / rs.NormalDistance;
                                if (!rs.IsLeft)
                                {
                                    StartPoint = (rs.Graphics[rs.SectionIndexList[0].First()] as 线路绘图工具.Line).Pt0;
                                    线路绘图工具.Line l = rs.Graphics.First() as 线路绘图工具.Line;
                                    DirPos[0] = CalDirVector(l.Pt0, l.Pt1);
                                    DirPos[1] = DirPos[0];//因为直线所以简化处理
                                }
                                else
                                {
                                    StartPoint = (rs.Graphics[rs.SectionIndexList[1].Last()] as 线路绘图工具.Line).Pt1;
                                    线路绘图工具.Line l = rs.Graphics.First() as 线路绘图工具.Line;
                                    DirPos[0] = CalDirVector(l.Pt1, l.Pt0);
                                    DirPos[1] = DirPos[0];//因为直线所以简化处理
                                }

                                preid = nowid;
                                pretype = nowtype;
                            }
                            offset *= k;
                            double x = DirPos[0].X * offset + StartPoint.X + origin.X;
                            double y = DirPos[0].Y * offset + StartPoint.Y + origin.Y;
                            UpdateTrainPosByXY(new Point(x, y));
                        }

                    }
                    else
                    {
                        //反位左行
                        if (dir == 0xaa)
                        {
                            //if (preid != nowid || pretype != nowtype)
                            if (true)
                            {
                                if (!rs.IsLeft)
                                {
                                    线路绘图工具.Line l = rs.Graphics[rs.SectionIndexList[2].Last()] as 线路绘图工具.Line;
                                    DirPos[0] = CalDirVector(l.Pt1, l.Pt0);
                                    StartPoint = l.Pt1;
                                    l = rs.Graphics[rs.SectionIndexList[0].First()] as 线路绘图工具.Line;
                                    DirPos[1] = CalDirVector(l.Pt1, l.Pt0);
                                }
                                else
                                {
                                    线路绘图工具.Line l = rs.Graphics[rs.SectionIndexList[0].First()] as 线路绘图工具.Line;
                                    DirPos[0] = CalDirVector(l.Pt0, l.Pt1);
                                    StartPoint = l.Pt0;
                                    l = rs.Graphics[rs.SectionIndexList[2].Last()] as 线路绘图工具.Line;
                                    DirPos[1] = CalDirVector(l.Pt0, l.Pt1);
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
                                    double len = offset - rs.Lens[2];
                                    double x = DirPos[0].X * rs.Lens[2] + DirPos[1].X * len + StartPoint.X + origin.X;
                                    double y = DirPos[0].Y * rs.Lens[2] + DirPos[1].Y * len + StartPoint.Y + origin.Y;
                                    UpdateTrainPosByXY(new Point(x, y));
                                }
                                else
                                {
                                    double x = DirPos[0].X * offset + StartPoint.X + origin.X;
                                    double y = DirPos[0].Y * offset + StartPoint.Y + origin.Y;
                                    UpdateTrainPosByXY(new Point(x, y));
                                }
                            }
                            else
                            {
                                if (offset > rs.Lens[0])
                                {
                                    double len = offset - rs.Lens[0];
                                    double x = DirPos[0].X * rs.Lens[0] + DirPos[1].X * len + StartPoint.X + origin.X;
                                    double y = DirPos[0].Y * rs.Lens[0] + DirPos[1].Y * len + StartPoint.Y + origin.Y;
                                    UpdateTrainPosByXY(new Point(x, y));
                                }
                                else
                                {
                                    double x = DirPos[0].X * offset + StartPoint.X + origin.X;
                                    double y = DirPos[0].Y * offset + StartPoint.Y + origin.Y;
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
                                    DirPos[0] = CalDirVector(l.Pt0, l.Pt1);
                                    StartPoint = l.Pt0;
                                    l = rs.Graphics[rs.SectionIndexList[2].Last()] as 线路绘图工具.Line;
                                    DirPos[1] = CalDirVector(l.Pt0, l.Pt1);
                                }
                                else
                                {
                                    线路绘图工具.Line l = rs.Graphics[rs.SectionIndexList[2].Last()] as 线路绘图工具.Line;
                                    DirPos[0] = CalDirVector(l.Pt1, l.Pt0);
                                    StartPoint = l.Pt1;
                                    l = rs.Graphics[rs.SectionIndexList[0].Last()] as 线路绘图工具.Line;
                                    DirPos[1] = CalDirVector(l.Pt1, l.Pt0);
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
                                    double len = offset - rs.Lens[1];
                                    double x = DirPos[0].X * rs.Lens[0] + len * DirPos[1].X + StartPoint.X + origin.X;
                                    double y = DirPos[0].Y * rs.Lens[0] + len * DirPos[1].Y + StartPoint.Y + origin.Y;
                                    UpdateTrainPosByXY(new Point(x, y));
                                }
                                else
                                {
                                    double x = DirPos[0].X * offset + StartPoint.X + origin.X;
                                    double y = DirPos[0].Y * offset + StartPoint.Y + origin.Y;
                                    UpdateTrainPosByXY(new Point(x, y));
                                }
                            }
                            else
                            {
                                if (offset > rs.Lens[2])
                                {
                                    double len = offset - rs.Lens[2];
                                    double x = DirPos[0].X * rs.Lens[2] + len * DirPos[1].X + StartPoint.X + origin.X;
                                    double y = DirPos[0].Y * rs.Lens[2] + len * DirPos[1].Y + StartPoint.Y + origin.Y;
                                    UpdateTrainPosByXY(new Point(x, y));
                                }
                                else
                                {
                                    double x = DirPos[0].X * offset + StartPoint.X + origin.X;
                                    double y = DirPos[0].Y * offset + StartPoint.Y + origin.Y;
                                    UpdateTrainPosByXY(new Point(x, y));
                                }
                            }

                        }
                    }
                    //}
                    //else
                    //{
                    //if (rs.State == SwitchState.PosNormal)
                    //{
                    //    if (dir == 0xaa)
                    //    {
                    //        if (preid != nowid || pretype != nowtype)
                    //        {
                    //            线路绘图工具.Line l = rs.Graphics.First() as 线路绘图工具.Line;
                    //            DirPos[0] = CalDirVector(l.Pt1, l.Pt0);
                    //            DirPos[1] = DirPos[0];//因为直线所以简化处理
                    //            //直 定 反
                    //            k = (rs.Lens[0] + rs.Lens[1]) / rs.NormalDistance;
                    //            StartPoint = (rs.Graphics[rs.SectionIndexList[1].Last()] as 线路绘图工具.Line).Pt1;
                    //        }
                    //        offset *= k;
                    //        double x = DirPos[0].X * offset + StartPoint.X;
                    //        double y = DirPos[0].Y * offset + StartPoint.Y;
                    //        UpdateTrainPosByXY(new Point(x, y));
                    //    }
                    //    else
                    //    {
                    //        if (preid != nowid || pretype != nowtype)
                    //        {
                    //            线路绘图工具.Line l = rs.Graphics.First() as 线路绘图工具.Line;
                    //            DirPos[0] = CalDirVector(l.Pt0, l.Pt1);
                    //            DirPos[1] = DirPos[0];//因为直线所以简化处理
                    //            //直 定 反
                    //            k = (rs.Lens[0] + rs.Lens[1]) / rs.NormalDistance;
                    //            StartPoint = (rs.Graphics[rs.SectionIndexList[0].First()] as 线路绘图工具.Line).Pt0;
                    //        }
                    //        offset *= k;
                    //        double x = DirPos[0].X * offset + StartPoint.X;
                    //        double y = DirPos[0].Y * offset + StartPoint.Y;
                    //        UpdateTrainPosByXY(new Point(x, y));
                    //    }
                    //}
                    //else
                    //{
                    //    if (dir == 0xaa)
                    //    {
                    //        if (preid != nowid || pretype != nowtype)
                    //        {
                    //            线路绘图工具.Line l = rs.Graphics[rs.SectionIndexList[2].Last()] as 线路绘图工具.Line;
                    //            DirPos[0] = CalDirVector(l.Pt1, l.Pt0);
                    //            StartPoint = l.Pt1;
                    //            l = rs.Graphics[rs.SectionIndexList[0].First()] as 线路绘图工具.Line;
                    //            DirPos[1] = CalDirVector(l.Pt1, l.Pt0);
                    //            //直 定 反
                    //            k = (rs.Lens[0] + rs.Lens[2]) / rs.ReverseDistance;
                    //        }
                    //        offset *= k;
                    //        if (offset > rs.Lens[2])
                    //        {
                    //            double len = offset - rs.Lens[2];
                    //            double x = DirPos[0].X * rs.Lens[2] + DirPos[1].X * len + StartPoint.X;
                    //            double y = DirPos[0].Y * rs.Lens[2] + DirPos[1].Y * len + StartPoint.Y;
                    //            UpdateTrainPosByXY(new Point(x, y));
                    //        }
                    //        else
                    //        {
                    //            double x = DirPos[0].X * offset + StartPoint.X;
                    //            double y = DirPos[0].Y * offset + StartPoint.Y;
                    //            UpdateTrainPosByXY(new Point(x, y));
                    //        }
                    //    }
                    //    else
                    //    {
                    //        if (preid != nowid || pretype != nowtype)
                    //        {
                    //            线路绘图工具.Line l = rs.Graphics[rs.SectionIndexList[0].First()] as 线路绘图工具.Line;
                    //            DirPos[0] = CalDirVector(l.Pt0, l.Pt1);
                    //            StartPoint = l.Pt0;
                    //            l = rs.Graphics[rs.SectionIndexList[0].First()] as 线路绘图工具.Line;
                    //            DirPos[1] = CalDirVector(l.Pt0, l.Pt1);
                    //            //直 定 反
                    //            k = (rs.Lens[0] + rs.Lens[2]) / rs.ReverseDistance;
                    //        }
                    //        if (offset > rs.Lens[0])
                    //        {
                    //            double len = offset - rs.Lens[1];
                    //            double x = DirPos[0].X * rs.Lens[0] + len * DirPos[1].X + StartPoint.X;
                    //            double y = DirPos[0].Y * rs.Lens[0] + len * DirPos[1].X + StartPoint.Y;
                    //            UpdateTrainPosByXY(new Point(x, y));
                    //        }
                    //        else
                    //        {
                    //            double x = DirPos[0].X * offset + StartPoint.X;
                    //            double y = DirPos[0].Y * offset + StartPoint.Y;
                    //            UpdateTrainPosByXY(new Point(x, y));
                    //        }
                    //    }


                }
                #endregion
            }

        }

        Point CalDirVector(Point former, Point latter)
        {
            Point tp = new Point(latter.X - former.X, latter.Y - former.Y);
            double len = Math.Pow(Math.Pow(tp.Y, 2) + Math.Pow(tp.X, 2), 0.5);
            if (len != 0)
            {
                tp.X /= len;
                tp.Y /= len;
            }
            return tp;
        }
        #endregion

        #region 进路寻路算法dfs


        List<ATS.ATSRoute> _routes;

        public List<ATS.ATSRoute> Routes
        {
            get { return _routes; }
            set { _routes = value; }
        }


        public void FindPath(string src, string tar,RouteDirection dir)
        {
            Device now = new Device() { Name = src };
            Res = new List<OptionalRoutes>();
            SearchPathBy(now, Res, new OptionalRoutes(), src, tar, 0, new HashSet<int>(),dir);
            Res.Sort();
        }


        /// <summary>
        /// 存放查找结果
        /// </summary>
        List<OptionalRoutes> _res;

        internal List<OptionalRoutes> Res
        {
            get { return _res; }
            set { _res = value; }
        }

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
            if (k > 20 || res.Count > 10)
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
                    foreach (Device d in route.InCommingSections)
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

        #endregion


        #region 进路处置

        public ATSRoute OpenRoute{get;set;}

        #endregion

        internal void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
