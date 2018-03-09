using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATS;
using LiveCharts;
using LiveCharts.Wpf;
using ATS.Model;
using ATS.View;
using ATS.ViewModel;
using System.Windows.Media;
using System.Windows.Data;
using System.Threading;
using LiveCharts.Defaults;
using ATS.Helpers;
using System.Windows.Controls;
using 线路绘图工具;
using System.Windows;
using System.Collections.Concurrent;
using System.Net;
using ZCToATS;
using System.Timers;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Windows.Input;
using System.Xml.Linq;
using System.IO;
using log4net;
using log4net.Config;
using System.Configuration;
using System.Collections.Specialized;


namespace ATS
{
    public enum RunMode
    { 
        CBTC模式,
        点式ATP模式
    }

    public class MainViewModel:ViewModelBase
    {
        private  static readonly ILog Log4ATS=log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public MainViewModel(Canvas canvas)
        {
            InitCommand();
            MCanvas = canvas;
            LoadGraphicElements();
            LoadStationTopo();
            InitRoute();//初始化进路
            InitCommunication();//初始化通信模块
            InitSection2StationName();
            //WriteAppConfig();
            ReadAppConfig();



            ClassifyElements();
            manualCB = new CommandBuilder(CBIMesSend, RC.Routes);
            autoCB = new CommandBuilder(CBIMesSend, RC.Routes);
            InitThreads();

            //EF暖机,解决第一次打开慢的问题
            Task.Run(() =>
                {
                    using (var pm=new PlanModel())
                    {
                        var objectContext = ((IObjectContextAdapter)pm).ObjectContext;
                        var mappingCollection = (StorageMappingItemCollection)objectContext.MetadataWorkspace.GetItemCollection(DataSpace.CSSpace);
                        mappingCollection.GenerateViews(new List<EdmSchemaError>());
                    }
                }
                );
            InitTrain();
            Log4ATS.Info("打开程序");
        }

        #region 配置信息相关，部分内容初始化自xml

        //联锁个数
        int CBINum;

        //列车最小等待时间
        UInt16 MinWaitTimePerStation;

        //ATS-->联锁的发送间隔
        int CBISendInteval ;

        //验证密码
        string password ;

        //需要验证的项目
        string[] NeedAu = { "总人解", "引导进路", "强制扳道岔定位", "区故解" };



        //右键内容
        //信号机、区段、道岔
        string[][] RightButtonItems ={
                      new string[]{"总取消", "总人解", "信号关闭", "信号重开", "封锁", "解封", "引导进路"},
                      new string[]{"总定位", "总反位", "单锁", "单解", "封锁", "解封", "强制扳道岔定位", "强制扳道岔反位"},
                      new string[]{"封锁", "解封", "区故解"}};

        #endregion

        #region 初始化“静态”内容

        /// <summary>
        /// 从App.config初始化静态配置内容
        /// </summary>
        void ReadAppConfig()
        {
            NameValueCollection appSettings = System.Configuration.ConfigurationManager.AppSettings;
            password = appSettings["password"];
            try
            {
                MinWaitTimePerStation = UInt16.Parse(appSettings["MinWaitTimePerStation"]);
                CBISendInteval = int.Parse(appSettings["CBISendInteval"]);
                CBINum = int.Parse(appSettings["CBINum"]);
            }
            catch
            {
                MinWaitTimePerStation = 7;
                CBISendInteval = 5000;
                CBINum = 4;
            }
        }

        /// <summary>
        /// 写App.config配置
        /// </summary>
        void WriteAppConfig()
        {
            Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings.Add("password", "888");
            config.AppSettings.Settings.Add("MinWaitTimePerStation", "7");
            config.AppSettings.Settings.Add("CBINum","4");
            config.AppSettings.Settings.Add("CBISendInteval", "5000");

            config.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }


        #endregion

        private void InitThreads()
        {
            //更新位置和实际运行图的线程
            Thread t1 = new Thread(UpdateTrainsPosition);
            t1.SetApartmentState(ApartmentState.STA);
            t1.IsBackground = true;
            t1.Start();


            //更新站场图线程
            Thread t2 = new Thread(UpdateBandSignal);
            t2.SetApartmentState(ApartmentState.STA);
            t2.IsBackground = true;
            t2.Start();


            Thread t4;
            t4= new Thread(SendMesToCBI);
            t4.SetApartmentState(ApartmentState.STA);
            t4.IsBackground = true;
            t4.Start();

            //闪烁设定
            SetFlashTimer();
            timer.Start();

            //更新倒记计时
            Thread t3 = new Thread(UpdateCountDown);
            t3.SetApartmentState(ApartmentState.STA);
            t3.IsBackground = true;
            t3.Start();
        }

        #region 初始化通信模块



        #region 初始化IP配置等配置

        IPConfigure ipconf;

        /// <summary>
        /// 初始化IP、端口配置
        /// </summary>
        void InitIPconf()
        {
            ipconf = new IPConfigure();
        }
        #endregion


        Communication ZcCom;
        Communication CBIsCom;
        ATPsCommunication ATPsCom;
        void InitCommunication()
        { 
           InitIPconf();
           IPList ATSIPPort = IPConfigure.IPList.Find((IPList ipl) => { return ipl.DeviceName == "ATS4ZC"; });
           ZcCom = new Communication(new IPEndPoint(ATSIPPort.IP, ATSIPPort.Port),ZCMes);
           ATSIPPort = IPConfigure.IPList.Find((IPList ipl) => { return ipl.DeviceName == "ATS4CBIs"; });
           CBIsCom = new Communication(new IPEndPoint(ATSIPPort.IP, ATSIPPort.Port),CBIMesRec);
           ATSIPPort = IPConfigure.IPList.Find((IPList ipl) => { return ipl.DeviceName == "ATS4ATPs"; });
           ATPsCom = new ATPsCommunication(new IPEndPoint(ATSIPPort.IP, ATSIPPort.Port), ATPsMes);
           ZcCom.ListenData();
           CBIsCom.ListenData();
           ATPsCom.ListenData();
        }

        #region 消息队列等
        BlockingCollection<byte[]> ZCMes = new BlockingCollection<byte[]>();

        BlockingCollection<byte[]> CBIMesRec = new BlockingCollection<byte[]>();

        BlockingCollection<byte[]> CBIMesSend = new BlockingCollection<byte[]>();

        BlockingCollection<EPAndBytes> ATPsMes = new BlockingCollection<EPAndBytes>();

        #endregion

        #endregion



        #region 破车和破车跑出的破线


        //车辆配置要重写
        List<Train> Trains{get;set;}
        void InitTrain()
        {
            Trains = new List<Train>();
            List<IPList> ATPs=IPConfigure.IPList.FindAll((IPList ipl)=>{return ipl.DeviceName.Length>2&&ipl.DeviceName.Substring(0,3)=="ATP";});
            foreach (var item in ATPs)
            {
                Trains.Add(new Train(stationElements_.Elements, RC.Routes,item.DeviceID,Section2StationName));
            }
        }

        DateTime dt = DateTime.MinValue.Add(DateTime.Now-DateTime.Today);

        UInt16 id=0;

        UInt16 NowJiao;

        string preName=null,curName=null;
        bool isInSta=false;
        List<double> dis = new List<double>();
        /// <summary>
        /// 要求车的排号必须要从4开
        /// </summary>
        void UpdateTrainsPosition()
        {
            foreach (byte[] tempbytes in ZCMes.GetConsumingEnumerable())
            {
                App.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        InfoToATS info = null;
                        try
                        {
                            info = (InfoToATS)(MySerialize.deSerializeObject(tempbytes));
                        }
                        catch
                        { 
                        
                        }
                        if (info != null)
                        {
                            Train t = Trains.Find((Train tt) =>
                            {
                                return tt.TrainNum == info.TrainID;
                            });
                            object locker = new object();
                            //激活车
                            if (!t.IsRegistered)
                            {
                                    lock (locker)
                                    {
                                        try
                                        {
                                            MCanvas.Children.Add(t);
                                        }
                                        catch
                                        {

                                        }
                                        if (SelectCollection != null)
                                        {
                                            SelectCollection.Add(new LineSeries()
                                            {
                                                Title = t.TrainNum.ToString(),
                                                LineSmoothness = 0,
                                                Values = new ChartValues<DateTimePoint>(),
                                                Fill = new SolidColorBrush(),
                                                LabelPoint = x => t.TrainNum.ToString(),
                                            });
                                            t.IsRegistered = true;
                                            t.NowLineNum = SelectCollection.Count - 1;
                                            int k = t.PlanLineNum = NowJiao++;
                                            selectjiao jiao = SList[t.PlanLineNum];
                                        t.FindPathSD(jiao.StartSection, jiao.EndSection, jiao.Dir == "上行" ? RouteDirection.DIRDOWN : RouteDirection.DIRUP, jiao.ReturnMode == "否" ? false : true);
                                        //t.FindSinglePath(jiao.StartSection, jiao.EndSection, jiao.Dir == "上行" ? RouteDirection.DIRDOWN : RouteDirection.DIRUP);
                                        //        //t.FindPath(jiao.StartSection, jiao.EndSection, jiao.Dir == "上行" ? RouteDirection.DIRDOWN : RouteDirection.DIRUP);
                                        //Thread temp = new Thread(() =>
                                        //    {
                                        //t.FindPath(jiao.StartSection, jiao.EndSection, jiao.Dir == "上行" ? RouteDirection.DIRDOWN : RouteDirection.DIRUP);
                                        //        //t.FindSinglePath(jiao.StartSection, jiao.EndSection, jiao.Dir == "上行" ? RouteDirection.DIRDOWN : RouteDirection.DIRUP);

                                        //    });
                                        //    temp.SetApartmentState(ApartmentState.STA);
                                        //    temp.IsBackground = true;
                                        //    temp.Start();
                                    }
                                }
                            }
                            //摆车位
                            GraphicElement nowElement=t.UPdateTrainPosByOffset(info.Type, info.PositionID, info.Direction, info.Offset);

                            if (t.Res != null&&t.Res.Count!=0)
                            {

                                ////动静改变画点
                                //if (SList != null && info.IsStop != t.IsStop && Section2StationName.ContainsKey(t.NowSection.Name))
                                //{
                                //    dt = DateTime.MinValue.Add(DateTime.Now - DateTime.Today);
                                //    int nln = t.NowLineNum;
                                //    int pln = t.PlanLineNum;
                                //    t.IsStop = info.IsStop;
                                //    string s = Section2StationName[t.NowSection.Name];
                                //    List<double> dis = (from p in StaNamesDict
                                //                        where p.Value == s
                                //                        select p.Key).ToList();
                                //    SelectCollection[nln].Values.Add(new DateTimePoint(dt, dis.FirstOrDefault()));
                                //}
                                //开进路
                                if (t.OpenRoute != null)
                                {
                                    ATSRoute route = t.OpenRoute;
                                    ATS.Signal signal = route.StartSignal as ATS.Signal;
                                    if (signal.SColor != ATS.Signal.SignalColor.Green &&signal.SColor != ATS.Signal.SignalColor.Yellow)
                                    {
                                        List<object> objs=new List<object>();
                                        objs.Add(route.StartSignal);
                                        objs.Add(route.EndSignal);
                                        autoCB.AddTwoDevice(objs);
                                    }
                                }
                                //车注销
                                if (info.IsQuit)
                                {
                                    MCanvas.Children.Remove(t);
                                    t.IsRegistered = false;
                                }
                            }
                        }
                       
                    }));

            }
            
        }


        /// <summary>
        /// 更新光带和信号机
        /// </summary>
        void UpdateBandSignal()
        {
            foreach (byte[] tempbytes in CBIMesRec.GetConsumingEnumerable())
            {
                try
                {
                    App.Current.Dispatcher.BeginInvoke(new Action(
                    () =>
                    {
                        int CBINum = tempbytes[0];
                        //设备起始号码
                        int sRs = 0, sSec = 0, sSig = 0;
                        for (int i = 0; i < CBINum; i++)
                        {
                            sRs += RsNum[i];
                            sSec += SecNum[i];
                            sSig += SignalNum[i];
                        }
                        for (int i = 0; i < SecNum[CBINum]; i++)
                        {
                            int Id = sSec + i;
                            Sections[Id].SetStartByte(i);
                            Sections[Id].UpdateStatus(tempbytes, 0);
                            if (Sections[Id].IsStatusChanged)
                            {
                                Sections[Id].InvalidateVisual();
                                Sections[Id].IsStatusChanged = false;
                            }
                        }
                        for (int i = 0; i < RsNum[CBINum]; i++)
                        {
                            int Id = sRs + i;
                            RailSwitches[Id].SetStartByte(i, RSStartSections[CBINum]);
                            RailSwitches[Id].UpdateStatus(tempbytes, 0);
                            if (RailSwitches[Id].IsStatusChanged)
                            {
                                RailSwitches[Id].InvalidateVisual();
                                RailSwitches[Id].IsStatusChanged = false;
                            }

                        }
                        for (int i = 0; i < SignalNum[CBINum]; i++)
                        {
                            int Id = sSig + i;
                            Signals[Id].SetStartByte(i);
                            Signals[Id].UpdateStatus(tempbytes, 0);
                            if (Signals[Id].IsStatusChanged)
                                Signals[Id].InvalidateVisual();
                        }
                        OpenFirstRoute();
                    }));
                }
                catch
                { 
                
                }


            }
        }



        Section ZHG1, ZHG2;
        /// <summary>
        /// 上转换轨直接开启下一段进路
        /// </summary>
        void OpenFirstRoute()
        {
            if (ZHG1 == null) ZHG1 = stationElements_.Elements.Find((GraphicElement ge) =>
            {
                if (ge is Section && ge.Name == "ZHG1") return true;
                else return false;
            }) as Section;
            if (ZHG2 == null) ZHG2 = stationElements_.Elements.Find((GraphicElement ge) =>
            {
                if (ge is Section && ge.Name == "ZHG2") return true;
                else return false;
            }) as Section;
            if (ZHG1 != null && ZHG1.IsOccupied)
            {
                Route route = RC.Routes.Find((ATSRoute ar) =>
                  {
                      if (ar.InCommingSections.First().Name == "ZHG1") return true;
                      else return false;
                  });
                autoCB.AddDevice(route.StartSignal);
                autoCB.AddDevice(route.EndSignal);
            }
            if (ZHG2 != null && ZHG2.IsOccupied)
            {
                Route route = RC.Routes.Find((ATSRoute ar) =>
                {
                    if (ar.InCommingSections.First().Name == "ZHG2") return true;
                    else return false;
                });
                autoCB.AddDevice(route.StartSignal);
                autoCB.AddDevice(route.EndSignal);
            }
        }

        void UpdateCountDown()
        {
            foreach (var item in ATPsMes.GetConsumingEnumerable())
            {
                ATP2ATS ps = new ATP2ATS();

                string mes = Encoding.UTF8.GetString(item.Mes);
                curName = mes.Substring(0,5);
                ps.TrainNum = Convert.ToUInt16(mes.Substring(5, 1));
                Train t = Trains.Find((Train tt) =>
                  {
                      return tt.TrainNum == ps.TrainNum;
                  });
                if (curName != preName)
                {
                    if (Section2StationName.ContainsKey(curName))
                    {
                        dt = DateTime.MinValue.Add(DateTime.Now - DateTime.Today);
                        int nln = t.NowLineNum;
                        int pln = t.PlanLineNum;
                        string s = Section2StationName[curName];
                        dis = (from p in StaNamesDict
                               where p.Value == s
                               select p.Key).ToList();
                        SelectCollection[nln].Values.Add(new DateTimePoint(dt, dis.FirstOrDefault()));
                        isInSta = true;
                        if (t != null && SList != null)
                        {
                            List<plan> plans = SList[t.PlanLineNum].plan.ToList();
                            TimeSpan? pts = null;
                            try
                            {
                                pts = plans.Find((plan pl) =>
                                {
                                    //return pl.StaName == t.NowSection.Name;
                                    return pl.StaName == Section2StationName[t.NowSection.Name];
                                }).ReachTime;
                            }
                            catch
                            {

                            }

                            if (pts != null)
                            {
                                DateTime pdt = DateTime.MinValue.Add((TimeSpan)pts);
                                TimeSpan ctt = DateTime.Now - pdt;
                                UInt16 ui;
                                if (ctt.Seconds < MinWaitTimePerStation)
                                {
                                    ui = MinWaitTimePerStation;
                                }
                                else
                                {
                                    ui = (UInt16)ctt.Seconds;
                                }
                                ATPsCom.SendData(MySerialize.serializeObject((UInt16)ctt.Seconds), item.EP);
                            }
                        }

                    }
                    else if (isInSta)
                    {
                        dt = DateTime.MinValue.Add(DateTime.Now - DateTime.Today);
                        int nln = t.NowLineNum;
                        SelectCollection[nln].Values.Add(new DateTimePoint(dt, dis.FirstOrDefault()));
                        isInSta = false;
                    }
                }
                preName = curName;

                //if (ps != null)
                //{
                //    if (t != null&&SList!=null)
                //    {
                //        List<plan> plans = SList[t.PlanLineNum].plan.ToList();
                //        TimeSpan? pts = null;
                //        try
                //        {
                //            pts = plans.Find((plan pl) =>
                //            {
                //                return pl.StaName == Section2StationName[t.NowSection.Name];
                //            }).ReachTime;
                //        }
                //        catch
                //        {

                //        }

                //        if (pts != null)
                //        {
                //            DateTime pdt = DateTime.MinValue.Add((TimeSpan)pts);
                //            TimeSpan ctt = DateTime.Now - pdt;
                //            UInt16 ui;
                //            if (ctt.Seconds < MinWaitTimePerStation)
                //            {
                //                ui = MinWaitTimePerStation;
                //            }
                //            else
                //            {
                //                ui = (UInt16)ctt.Seconds;
                //            }
                //            ATPsCom.SendData(MySerialize.serializeObject((UInt16)ctt.Seconds), item.EP);
                //        }
                //    }
                //}
            }
        }


        
        //之前内容缓存，剔除重复数据使用
        byte[] preBytes=new byte[24];
        void SendMesToCBI()
        {
            foreach (var item in CBIMesSend.GetConsumingEnumerable())
            {
                if (!item.SequenceEqual(preBytes))
                {
                    foreach (var ep in CBIsCom.REPs)
                    {
                        CBIsCom.SendData(item, ep, item.Length);
                    }
                    Array.Copy(item, preBytes, item.Length);
                }
                Thread.Sleep(CBISendInteval);
            }
        }

        #endregion



        #region 显示格式设置
        private Func<double, string> _yFormatter;

        public Func<double, string>YFormatter
        {
            get { return _yFormatter; }
            set
            {
                if (value != null)
                {
                    _yFormatter = value;
                    RaisePropertyChanged("YFormatter");
                }
            }
        }


        private Func<double, string> _xFormatter;

        public Func<double, string> XFormatter
        {
            get { return _xFormatter; }
            set
            {
                if (value != _xFormatter)
                {
                    _xFormatter = value;
                    RaisePropertyChanged("XFormatter");
                }
            }
        }

        int? _Step = 1;

        public int? Step
        {
            get { return _Step; }
            set { _Step = value; }
        }
        //距离--站名映射
        public Dictionary<double, String> StaNamesDict { get; set; }
        void InitStaNameDic(List<selectjiao> sList)
        {
            StaNamesDict = new Dictionary<double, string>();
            for (int i = 0; i < sList.Count; i++)
            {
                List<plan> pl = sList[i].plan.ToList();
                for (int j = 0; j < pl.Count; j++)
                {
                    double dis = (double)(pl[j].Distance);
                    if (!StaNamesDict.ContainsKey(dis)) StaNamesDict.Add(dis, pl[j].StaName);
                }
            }
        }


        void InitDisFormatter()
        {
              XFormatter = val => new DateTime((long)val).ToString("HH:mm:ss");
              YFormatter = (double x) => StaNamesDict.ContainsKey(x) ? StaNamesDict[x] :null ;
        }

        #endregion

        #region 构建计划图点集

        List<selectjiao> _sList;

        object o = new object();
        public List<selectjiao> SList
        {
            get { 
                
              lock(o)  return _sList; }
            set
            {
                lock (o)
                {
                    if (value != _sList)
                    {
                        _sList = value;
                        RaisePropertyChanged("SList");
                    }
                }

            }
        }


        /// <summary>
        /// 构建一条线的点
        /// </summary>
        /// <param name="sl"></param>
        /// <returns></returns>
        List<DateTimePoint> Plan2Point(selectjiao sj)
        {
            List<DateTimePoint> dpl = new List<DateTimePoint>();
            DateTime min = DateTime.MinValue;
            List<plan> pl = sj.plan.ToList();
            for (int i = 0; i < pl.Count; i++)
            {
                if (i > 0)
                    dpl.Add(new DateTimePoint(min.Add(pl[i].ReachTime.Value), Convert.ToDouble(pl[i].Distance)));
                if (i < pl.Count - 1)
                    dpl.Add(new DateTimePoint(min.Add(pl[i].LeaveTime.Value), Convert.ToDouble(pl[i].Distance)));
            }
            return dpl;
        }

        object temp = new object();
        //显示集合
        private SeriesCollection _selectCollection;
        public SeriesCollection SelectCollection
        {
            get { lock(temp) return _selectCollection; }
            set
            {
                lock (temp)
                {
                    if (value != _selectCollection)
                    {
                        _selectCollection = value;
                        RaisePropertyChanged("SelectCollection");
                    }
                }
            }
        }


        /// <summary>
        /// 显示集合构建
        /// </summary>
        void InitPointSeries(List<selectjiao> sl)
        {
            SList = sl; 
            InitStaNameDic(sl);
            InitDisFormatter();
            NowJiao = 0;
            SelectCollection = new SeriesCollection();
            for (int i = 0; i < sl.Count; i++)
            {
                SelectCollection.Add(
                    new LineSeries() { 
                    Title=sl[i].TrainNum,
                    LineSmoothness=0,
                    Values=new ChartValues<DateTimePoint>(Plan2Point(sl[i])),
                    Fill=new SolidColorBrush(),
                    PointGeometry=null,
                    //DataLabels=true
                    //LabelPoint = x => sl[i].TrainNum,
                    });
            }

        }

        #endregion

        #region 命令

        void InitCommand()
        {
            OpenPWCommand = new RelayCommand(OpenPW);
            CmdMouseLeftButtonDown = new RelayCommand(cmdMouseLeftButtonDown);
            ButtonCommand = new RelayCommand(buttonCommand);
            //OnMoveCommand = new RelayCommand(onMoveCommand);
            //OffCommand = new RelayCommand(offMoveCommand);
            CmdMouseRightButtonDown = new RelayCommand(cmdMouseRightButtonDown);
            InitContextMenu();
        }
        public RelayCommand OpenPWCommand { get;set;}
        public RelayCommand CmdMouseLeftButtonDown{get;set;}

        public RelayCommand ButtonCommand { get; set; }
        public RelayCommand OnMoveCommand { get; set; }
        public RelayCommand OffCommand { get; set; }

        public RelayCommand CmdMouseRightButtonDown { get; set; }

        void OpenPW(object obj)
        {
            SelectCollection = new SeriesCollection();
            InitTrain();
            var pw = new PlanWindow();
            PlanViewModel pvm = pw.DataContext as PlanViewModel;
            pvm.UpdateRunChartAction += InitPointSeries;
            pw.ShowDialog();
        }

        void cmdMouseLeftButtonDown(object obj)
        {
            if (obj is Canvas)
            {

            }
            else
            {
                var De = (object)Mouse.DirectlyOver;
                manualCB.AddDevice(De);
            }
        }

        //信号机右键，道岔右键，区段右键
        ContextMenu[] cms=new ContextMenu[3];


        /// <summary>
        /// 初始化右键菜单
        /// </summary>
        void InitContextMenu()
        {
            //信号机、区段、道岔
            //ss[0] = new string[] { "总取消", "总人解", "信号关闭", "信号重开", "封锁", "解封", "引导进路" };
            //ss[1] = new string[] { "总定位", "总反位", "单锁", "单解", "封锁", "解封", "强制扳道岔定位", "强制扳道岔反位" };
            //ss[2] = new string[] { "封锁", "解封", "区故解" };
            for (int i = 0; i < cms.Length; i++)
            {
                cms[i] = new ContextMenu();
                foreach (var item in RightButtonItems[i])
                {
                    MenuItem mi=new MenuItem();
                    mi.Header = String.Copy(item);
                    mi.Click += RightButtonClickHandler;
                    cms[i].Items.Add(mi);
                }
            }
        }



        /// <summary>
        /// 右键点击事件，生成相应命令包
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void RightButtonClickHandler(object sender, RoutedEventArgs e)
        {
            string name = (sender as MenuItem).Header.ToString();
            bool isNeedAu=false;
            foreach (var item in NeedAu)
            {
                if (item == name)
                {
                    isNeedAu = true;
                }
            }
            if (isNeedAu)
            {
                PassWordWindow pww = new PassWordWindow(password);
                pww.DecideRes += pww_DecideRes;
                pww.ShowDialog();
                if (IsPswordRight && tempObject != null)
                {
                    manualCB.AddDevice(tempObject);
                    manualCB.AddDevice(name);
                }
            }
            else
            {
                if (tempObject != null)
                {
                    manualCB.AddDevice(tempObject);
                    manualCB.AddDevice(name);
                }
            }
            tempObject = null;
            IsPswordRight = false;
        }

        /// <summary>
        /// 修改密码判定结果
        /// </summary>
        /// <param name="res"></param>


        /// <summary>
        /// 判定密码结果
        /// </summary>
        /// <param name="pwtest"></param>
        void pww_DecideRes(string pwtest)
        {
            IsPswordRight =pwtest==password ;
        }


        object tempObject;
        void cmdMouseRightButtonDown(object obj)
        {

            var De = (object)Mouse.DirectlyOver;
            if (De is Signal)
            {
                Signal t = (Signal)De;
                cms[0].PlacementTarget = t;
                cms[0].IsOpen = true;
            }
            else if (De is RailSwitch)
            {
                RailSwitch t = (RailSwitch)De;
                cms[1].PlacementTarget = t;
                cms[1].IsOpen=true;
            }
            else if(De is Section)
            {
                Section t = (Section)De;
                cms[2].PlacementTarget = t;
                cms[2].IsOpen = true;
            }
            tempObject = De;
        }

        void buttonCommand(object obj)
        {
            manualCB.AddDevice(obj);

        }

        //double preSc = 0;
        //void onMoveCommand(object obj)
        //{
        //    if (Mouse.LeftButton == MouseButtonState.Pressed)
        //    {
        //        ScrollViewer sv = obj as ScrollViewer;
        //        double nowSc = Mouse.GetPosition(MCanvas).X;
        //        sv.ScrollToHorizontalOffset(sv.HorizontalOffset+preSc - nowSc);
        //    }
        //}

        //void offMoveCommand(object obj)
        //{
        //    ScrollViewer sv = obj as ScrollViewer;
        //    preSc = Mouse.GetPosition(MCanvas).X;
        //}

        #endregion


        #region 站场图


        Canvas _mCanvas;

        

        public Canvas MCanvas
        {
            get { return _mCanvas; }
            set
            {
                if (value != _mCanvas)
                {
                    _mCanvas = value;
                    RaisePropertyChanged("MCanvas");
                }
            }
        }

        public static StationElements stationElements_;
        public static StationTopoloty stationTopoloty_;

        //读取站场图
        private void LoadGraphicElements(string path = @"ConfigFiles\StationElements.xml")
        {
            stationElements_ = StationElements.Open(path);
            stationElements_.CheckSectionSwitches();
            stationElements_.AddElementsToCanvas(MCanvas);
        }

        private void LoadStationTopo(string path = @"ConfigFiles\StationTopoloty.xml")
        {
            stationTopoloty_ = new StationTopoloty();
            stationTopoloty_.Open(path, stationElements_.Elements);
        }

        ConcurrentDictionary<string, string> Section2StationName;

        void InitSection2StationName()
        { 
            Section2StationName=new ConcurrentDictionary<string,string>();
            Section2StationName.TryAdd("ZHG1", "转换轨");
            Section2StationName.TryAdd("ZHG2", "转换轨");
            Section2StationName.TryAdd("T0107","十三号街站");
            Section2StationName.TryAdd("T0108", "十三号街站");
            Section2StationName.TryAdd("T0113", "中央大街站");
            Section2StationName.TryAdd("T0112", "中央大街站");
            Section2StationName.TryAdd("T0201", "七号街站");
            Section2StationName.TryAdd("T0202", "七号街站");
            Section2StationName.TryAdd("T0211", "四号街站");
            Section2StationName.TryAdd("T0208", "四号街站");
            Section2StationName.TryAdd("T0301", "张士站");
            Section2StationName.TryAdd("T0302", "张士站");
            Section2StationName.TryAdd("T0303", "沈新路站");
            Section2StationName.TryAdd("T0304", "沈新路站");
            Section2StationName.TryAdd("T0307", "黄海路站");
            Section2StationName.TryAdd("T0310", "黄海路站");
            Section2StationName.TryAdd("T0401", "洪湖北街站");
            Section2StationName.TryAdd("T0402", "洪湖北街站");
            Section2StationName.TryAdd("T0405", "重工街站");
            Section2StationName.TryAdd("T0408", "重工街站");
            Section2StationName.TryAdd("T0407", "启工街站");
            Section2StationName.TryAdd("T0410", "启工街站");
        }

        #endregion


        #region 进路初始化


        RouteCreator _RC;

        public RouteCreator RC
        {
            get { return _RC; }
            set { _RC = value; }
        }

        /// <summary>
        /// 初始化进路、距离求解、建立进路图
        /// </summary>
        /// <param name="path"></param>
        void InitRoute(string path = @"ConfigFiles\RouteList.xml")
        {
            RC = new XmlTool<RouteCreator>().DeSerialize(path);
            RC.LoadDevices(stationElements_.Elements);
        }











        #endregion

       

        #region 定时器设定以及闪烁设置
        System.Timers.Timer timer;

        //需要闪烁的信号灯集合
        ConcurrentBag<Signal> FlashSignals = new ConcurrentBag<Signal>();
        void SetFlashTimer()
        {
            //黑 其他色交替重绘，造成一秒闪一次的效果
            timer = new System.Timers.Timer(500);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
        }
        void FlashSignalName()
        {

        }

        void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (App.Current != null)
            {
                App.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    foreach (GraphicElement ge in stationElements_.Elements)
                    {
                        this.FlashDevice(ge);
                    }
                    manualCB.FlashName();
                }
                ));
            }

        }

        private void FlashDevice(GraphicElement item)
        {

            if (item is IFlash)
            {
                IFlash flash = item as IFlash;
                bool isFlashing = flash.IsFlashing;
                if (isFlashing)
                {
                    flash.FlashFlag = !flash.FlashFlag;
                    //flash.FlashNameFlag = !flash.FlashNameFlag;
                    if (item is Signal)
                    { 
                    
                    }
                    item.InvalidateVisual();
                }
            }
        }

        #endregion

        #region 设备分类、初始化各联锁段设备数量
        List<RailSwitch> RailSwitches = new List<RailSwitch>();
        List<Section> Sections = new List<Section>();
        List<Signal> Signals = new List<Signal>();

        //保存不同联锁的设备数量

        List<int> RsNum = new List<int>();
        List<int> SecNum = new List<int>();
        List<int> SignalNum = new List<int>();


        //记录不同联锁区段的最小SectionNum
        List<int> RSStartSections = new List<int>();




        public void ClassifyElements()
        {
            foreach (var item in stationElements_.Elements)
            {
                if (item is RailSwitch)
                {
                    RailSwitches.Add(item as RailSwitch);
                }
                else if (item is Section)
                {
                    Sections.Add(item as Section);
                }
                else if (item is Signal)
                {
                    Signals.Add(item as Signal);
                }
            }
            
            for(int i=0;i<CBINum;i++)
            {
                int k = RailSwitches.FindAll((RailSwitch rs) =>
                    {
                        return rs.StationID ==i;
                    }).Count;
                RsNum.Add(k);
            }

            for (int i = 0; i < CBINum; i++)
            {
                int k = Sections.FindAll((Section sc) =>
                {
                    return sc.StationID == i;
                }).Count;
                SecNum.Add(k);
            }

            for (int i = 0; i < CBINum; i++)
            {
                int k = Signals.FindAll((Signal s) =>
                    {
                        return s.StationID == i;
                    }).Count;
                SignalNum.Add(k);
            }

            Section.StartByte = 8;
            RailSwitch.StartByte = Section.StartByte + 384;
            Signal.StartByte = RailSwitch.StartByte + 128;

            List<int> temp = new List<int>();
            foreach (var item in Sections)
            {
                temp.Add(item.ID);
            }

            List<int> list=new List<int>();
            for (int i = 0; i < CBINum; i++)
            {
                int min = int.MaxValue;
                foreach (Section sc in Sections)
                {
                    if (sc.StationID==i)
                    {
                        min = Math.Min(min, sc.ID);
                    }
                }

                RSStartSections.Add(min);
            }


        }

        #endregion



        //手动命令输入
        CommandBuilder manualCB;
        CommandBuilder autoCB;



        Boolean IsPswordRight = false;
        



    }
}
