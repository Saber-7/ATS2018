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
using System.Windows.Threading;
using ZCATSMes;
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
using System.Windows.Documents;
using System.Text.RegularExpressions;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace ATS
{
    public enum RunMode
    { 
        CBTC模式,
        点式ATP模式
    }

    delegate void UpdateMesHandle(FixedConQueue<HandleMes> fixqueue);
    public class MainViewModel:ViewModelBase
    {
        private  static readonly ILog Log4ATS=log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MainViewModel(Canvas canvas)
        {
            ReadAppConfig();
            InitStation();
            InitCommand();
            MCanvas = canvas;
            LoadGraphicElements();
            LoadStationTopo();
            InitRoute();//初始化进路
            InitCommunication();//初始化通信模块
            InitSection2StationName();
            InitMes();

            


            ClassifyElements();
            UpdateMesHandle UpdateHandleMesEvent = new UpdateMesHandle(UpdateHandleMes);
            manualCB = new CommandBuilder(CBIMesSend, RC.Routes, HandleMesQueue, UpdateHandleMesEvent);
            autoCB = new CommandBuilder(CBIMesSend, RC.Routes, HandleMesQueue, UpdateHandleMesEvent);


                //EF暖机,解决第一次打开慢的问题
                Task.Run(() =>
                {
                    try
                    {
                        using (var pm = new PlanModel())
                        {
                            var objectContext = ((IObjectContextAdapter)pm).ObjectContext;
                            var mappingCollection = (StorageMappingItemCollection)objectContext.MetadataWorkspace.GetItemCollection(DataSpace.CSSpace);
                            mappingCollection.GenerateViews(new List<EdmSchemaError>());
                        }
                    }
                    catch
                    {
                        MessageBox.Show("数据库连接故障,请检查网络连接与配置！");
                    }

                });




            InitTrain();
            InitThreads();
            //WriteAppConfig();

        }

        List<Station> StationList;
        void InitStation(String path= @"ConfigFiles\Station.xml")
        {
            StationList = new List<Station>();
            XDocument xdoc = XDocument.Load(path);
            foreach (var xxe in xdoc.Elements())
            {
                foreach (var xe in xxe.Elements())
                {
                    Station s = new Station();
                    s.Name = xe.Element("name").Value;
                    s.Distance = Convert.ToDouble(xe.Element("distance").Value);
                    StationList.Add(s);
                }
            }
            InitStaNameDic(null);
        }

        #region 配置信息相关，部分内容初始化自xml

        //联锁个数
        int CBINum;

        //列车最小等待时间
        UInt16 MinWaitTimePerStation;

        //最大停车时间
        Int16 MaxWaitTime = 180;

        //ATS-->联锁的发送间隔
        int CBISendInteval ;

        //验证密码
        string password ;

        //需要验证的项目
        string[] NeedAu = { "总人解", "引导进路", "强制扳道岔定位", "区故解" };



        //右键内容
        //信号机、区段、道岔
        //string[][] RightButtonItems ={
        //              new string[]{"总取消", "总人解", "信号关闭", "信号重开", "封锁", "解封", "引导进路"},
        //              new string[]{"总定位", "总反位", "单锁", "单解", "封锁", "解封", "强制扳道岔定位", "强制扳道岔反位"},
        //              new string[]{"封锁", "解封", "区故解"}};
        string[][] RightButtonItems ={
                      new string[]{"总取消", "总人解", "信号关闭", "信号重开"},
                      new string[]{"总定位", "总反位", "单锁", "单解", "封锁", "解封"},
                      new string[]{"封锁", "解封", "区故解"},
                      new string[] {"进入车队模式","退出车队模式"}
        };


        /// <summary>
        /// 确认间隔 只对开进路确认
        /// </summary>
        private  int ConfirmInterval=6000;

        //反复确认次数只对开进路确认
        private int ConfirmTimes = 2;

        #endregion

        #region 初始化“静态”内容

        /// <summary>
        /// 从App.config初始化静态配置内容
        /// </summary>
        void ReadAppConfig()
        {
            NameValueCollection appSettings = System.Configuration.ConfigurationManager.AppSettings;
            try
            {
                MinWaitTimePerStation = UInt16.Parse(appSettings["MinWaitTimePerStation"]);
                CBISendInteval = int.Parse(appSettings["CBISendInteval"]);
                CBINum = int.Parse(appSettings["CBINum"]);
                password = appSettings["Password"];
                ConfirmInterval = int.Parse(appSettings["ConfirmInterval"]);
                ConfirmTimes = int.Parse(appSettings["ConfirmTimes"]);
            }
            catch
            {
                MinWaitTimePerStation = 3;
                CBISendInteval = 3000;
                CBINum = 4;
                password = "888";
                ConfirmInterval = 7000;
                ConfirmTimes = 2;
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
            config.AppSettings.Settings.Add("ConfirmInterval", "5000");
            config.AppSettings.Settings.Add("ConfirmTimes", "3");

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

            //确认线程
            Task.Run(new Action(ConfirmRouteOpen));
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



        UInt16 NowJiao;

        string preName=null,curName=null;
        bool isInSta=false;
        List<double> dis = new List<double>();


        //简单剔除重复数据，降低UI负担
        Dictionary<ushort, ZC2ATS> ZCMesDic = new Dictionary<ushort, ZC2ATS>();
        /// <summary>
        /// 要求车的排号必须要从4开
        /// </summary>
        void UpdateTrainsPosition()
        {
            foreach (byte[] tempbytes in ZCMes.GetConsumingEnumerable())
            {
                ZC2ATS ZC2ATSinfo = null;
                try
                {
                    //info = (InfoToATS)(MySerialize.deSerializeObject(tempbytes));
                    MySerialize serializer = new MySerialize();
                    ZC2ATSinfo = (ZC2ATS)(serializer.deSerializeObject(tempbytes));
                }
                catch
                {

                }
                //去重
                if (ZC2ATSinfo != null)
                {
                    if (!ZCMesDic.ContainsKey(ZC2ATSinfo.TrainId))
                    {
                        ZCMesDic.Add(ZC2ATSinfo.TrainId,ZC2ATSinfo);
                    }
                    else
                    {
                        if (ZCMesDic[ZC2ATSinfo.TrainId].Equals(ZC2ATSinfo))
                        {
                            ZC2ATSinfo = null;
                        }
                        else
                        {
                            ZCMesDic[ZC2ATSinfo.TrainId] = ZC2ATSinfo;
                        }
                    }
                }
                if (ZC2ATSinfo != null)
                {
                    App.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Train t = Trains.Find((Train tt) =>
                        {
                            return tt.TrainGroupNum == ZC2ATSinfo.TrainId;
                        });
                        if (t != null)
                        {
                            t.RunMode = (ActualRunMode)ZC2ATSinfo.Runmode;
                            t.IsEmergent = ZC2ATSinfo.IsEmergent;
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
                                    if (SelectCollection != null&&SList!=null)
                                    {
                                        SelectCollection.Add(new LineSeries()
                                        {
                                            Title = t.TrainGroupNum.ToString(),
                                            LineSmoothness = 0,
                                            Values = new ChartValues<DateTimePoint>(),
                                            Fill = new SolidColorBrush(),
                                            LabelPoint = x => t.TrainGroupNum.ToString(),
                                        });
                                        t.IsRegistered = true;
                                        t.NowLineNum = SelectCollection.Count - 1;
                                        int k = t.PlanLineNum = NowJiao++;
                                        t.TrainOrderNum = String.Copy(SList[t.PlanLineNum].TrainGroupNum);
                                        selectjiao jiao = SList[t.PlanLineNum];
                                        List<double> dis = (from p in StaNamesDict
                                                            where p.Value == "转换轨"
                                                            select p.Key).ToList();
                                        SelectCollection[t.NowLineNum].Values.Add(new DateTimePoint(DateTime.Now, dis.FirstOrDefault()));
                                        if (jiao.ReturnMode == "否")
                                            t.FindPathSD(jiao.StartSection, jiao.EndSection, null, null, jiao.Dir == "上行" ? RouteDirection.DIRDOWN : RouteDirection.DIRUP, jiao.ReturnMode == "否" ? false : true);
                                        else
                                            t.FindPathSD(jiao.StartSection, jiao.EndSection, jiao.StartSection2, jiao.EndSection2, jiao.Dir == "上行" ? RouteDirection.DIRDOWN : RouteDirection.DIRUP, jiao.ReturnMode == "否" ? false : true);
                                        if(t.Res==null||t.Res.Count==0)
                                        {
                                            MessageBox.Show("进路排列失败，请检查计划！");
                                        }
                                    }
                                }
                            }
                            //摆车位
                            GraphicElement nowElement = t.UPdateTrainPosByOffset(ZC2ATSinfo.PositionType, ZC2ATSinfo.PositionId, ZC2ATSinfo.Direction, ZC2ATSinfo.Offset);
                            TrainMes mes = TrainMesFactory(t.NowSection, ZC2ATSinfo);
                            TrainMesQueue.Enqueue(mes);
                            if (trainMesQueue.IsChanged)
                                TrainMesDsipaly = new List<TrainMes>(trainMesQueue);

                            if (t.Res != null && t.Res.Count != 0)
                            {
                                //开进路
                                if (t.OpenRoute != null)
                                {
                                    ATSRoute route = t.OpenRoute;
                                    ATS.Signal signal = route.StartSignal as ATS.Signal;
                                    SmallButton sb = SmallButtons.Find((SmallButton ssb) =>
                                    {
                                        return ssb.ID == signal.ID;
                                    });
                                    if (signal.SColor != ATS.Signal.SignalColor.Green && signal.SColor != ATS.Signal.SignalColor.Yellow)
                                    {
                                        if(t.TrainState== 列车模式.车队模式)
                                        {
                                            if (sb == null)
                                            {
                                                List<object> objs = new List<object>();
                                                objs.Add(route.StartSignal);
                                                objs.Add(route.EndSignal);
                                                autoCB.AddTwoDevice(objs);
                                            }
                                            else
                                            {
                                                if (!signal.IsCBTCRoute)
                                                {
                                                    sb.PressDown();
                                                    autoCB.AddDevice(sb);
                                                }
                                                else
                                                {

                                                }

                                            }
                                        }
                                        else
                                        {
                                            List<object> objs = new List<object>();
                                            objs.Add(route.StartSignal);
                                            objs.Add(route.EndSignal);
                                            autoCB.AddTwoDevice(objs);
                                        }
                                    }
                                }


                            }
                            ////车注销
                            if (ZC2ATSinfo.Runmode == (byte)ActualRunMode.注销)
                            {
                                MCanvas.Children.Remove(t);
                                t.IsRegistered = false;
                            }

                        }



                    }));
                }

            }
            
        }


        /// <summary>
        /// 更新光带和信号机,以及联锁信息显示
        /// </summary>
        void UpdateBandSignal()
        {
            foreach (byte[] recvBuf in CBIMesRec.GetConsumingEnumerable())
            {
                try
                {
                    App.Current.Dispatcher.BeginInvoke(new Action(
                    () =>
                    {
                        int CBINum = recvBuf[0];
                        //设备起始号码
                        int sRs = 0, sSec = 0, sSig = 0,sPd = 0,sRB = 0;
                        for (int i = 0; i < CBINum; i++)
                        {
                            sRs += RsNum[i];
                            sSec += SecNum[i];
                            sSig += SignalNum[i];
                            sPd += PSDoorNum[i];
                            //sRB += RBNum[i];
                        }
                        for (int i = 0; i < SecNum[CBINum]; i++)
                        {
                            int Id = sSec + i;
                            Sections[Id].SetStartByte(i);
                            Sections[Id].UpdateStatus(recvBuf, 0);
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
                            RailSwitches[Id].UpdateStatus(recvBuf, 0);
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
                            Signals[Id].UpdateStatus(recvBuf, 0);
                            if (Signals[Id].IsStatusChanged)
                                Signals[Id].InvalidateVisual();
                        }

                        for (int i = 0; i < PSDoorNum[CBINum]; i++)
                        {
                            int Id = sPd + i;
                            PSdoors[Id].SetStartByte(i);
                            PSdoors[Id].UpdateStatus(recvBuf, 0);
                            if (PSdoors[Id].IsStatusChanged)
                                PSdoors[Id].InvalidateVisual();
                        }
                        
                        const int helpTipFlag = 1134 + 128; // 默认
                    if (recvBuf[helpTipFlag] != 0 && recvBuf[helpTipFlag] != 0xff)
                    {
                        int strLen = 0;
                        while (recvBuf[helpTipFlag + strLen] != 0)
                        {
                            strLen++;
                        }
                       string CIReply = System.Text.Encoding.Default.GetString(recvBuf, helpTipFlag, strLen);
                       HandleMes handleMes = CBIerrorFactory(CIReply);
                       HandleMesQueue.Enqueue(handleMes);
                        //wpf不能绑定queue 只能绑定list等少数集合故强行转一下
                        handleMesdipaly = new List<HandleMes>(HandleMesQueue);
                    }
                        //if (ShouldOpenRouteOrNot())
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
                      if (ar.IncomingSections.First().Name == "ZHG1") return true;
                      else return false;
                  });

                autoCB.AddDevice(route.StartSignal);
                autoCB.AddDevice(route.EndSignal);
            }
            if (ZHG2 != null && ZHG2.IsOccupied)
            {
                Route route = RC.Routes.Find((ATSRoute ar) =>
                {
                    if (ar.IncomingSections.First().Name == "ZHG2") return true;
                    else return false;
                });
                autoCB.AddDevice(route.StartSignal);
                autoCB.AddDevice(route.EndSignal);
            }
        }


        int permitTime = 1;
        /// <summary>
        /// 判定是否是合适的开车时间
        /// </summary>
        /// <returns></returns>
        /// 
        bool ShouldOpenRouteOrNot()
        {
            TimeSpan TimeToLeave = new TimeSpan(0, permitTime, 0);
            for (int i = 0; i < PlanNum; i++)
            {
              TimeSpan? t =SList[i].plan.First().LeaveTime;
              if (t != null)
              {
                  TimeSpan ts = (TimeSpan)t;
                  TimeSpan curts= DateTime.Now - DateTime.Today.Add(ts);
                    if (Math.Abs(curts.TotalSeconds) <= TimeToLeave.TotalSeconds)
                    {
                        return true;
                    }

              }
            }
            return false;

        }

        DateTime dt = DateTime.Now;
        int preOffset, curOffset;
        void UpdateCountDown()
        {
            foreach (var item in ATPsMes.GetConsumingEnumerable())
            {
                ATP2ATS ps = new ATP2ATS();

                string mes = Encoding.UTF8.GetString(item.Mes);
                String[] temps = Regex.Split(mes, "\\s+");
                ps.TrainNum = Convert.ToUInt16(temps[0]);
                curName = temps[1];
                curOffset = Convert.ToInt32(temps[2]);
                Train t = Trains.Find((Train tt) =>
                  {
                      return tt.TrainGroupNum == ps.TrainNum;
                  });
                if (curName != preName && t != null)
                {
                    //Log4ATS.Info(preOffset + "***********&" + curOffset);
                    //如果到下一区段进站，则用该方法更新站场图
                    //if (isInSta&&dis!=null)
                    //{
                    //    int nln = t.NowLineNum;
                    //    SelectCollection[nln].Values.Add(new DateTimePoint(DateTime.Now, dis.FirstOrDefault()));
                    //    Log4ATS.Info(DateTime.Now + "    " + dis.FirstOrDefault());
                    //    isInSta = false;
                    //}
                    if (Section2StationName.ContainsKey(curName)&&SelectCollection!=null)
                    {
                        int nln = t.NowLineNum;
                        int pln = t.PlanLineNum;
                        string s = Section2StationName[curName];
                        dis = (from p in StaNamesDict
                               where p.Value == s
                               select p.Key).ToList();
                        SelectCollection[nln].Values.Add(new DateTimePoint(DateTime.Now, dis.FirstOrDefault()));
                        isInSta = true;
                        if (t != null && SList != null)
                        {
                            if (!isInSta)
                            {

                            }
                            List<plan> plans = SList[t.PlanLineNum].plan.ToList();
                            TimeSpan? pts = null;
                            try
                            {
                                pts = plans.Find((plan pl) =>
                                {
                                    //return pl.StaName == t.NowSection.Name;
                                    return pl.StaName == s;
                                }).ReachTime;
                            }
                            catch
                            {

                            }

                            //Log4ATS.Info("PTS："+pts);

                            if (pts != null)
                            {
                                DateTime pdt = DateTime.Today.Add((TimeSpan)pts);
                                //TimeSpan ctt = DateTime.Now - pdt;
                                //cts caculate timespan
                                TimeSpan cts = DateTime.Now.TimeOfDay-pdt.TimeOfDay;
                                int waitTime,caculateTime=((int)cts.TotalSeconds ) % MaxWaitTime;

                                if (caculateTime < MinWaitTimePerStation)
                                {
                                    waitTime = MinWaitTimePerStation;
                                }
                                else
                                {
                                    waitTime = caculateTime;
                                }
                                byte[] bytes= new byte[4];
                                bytes[0] = 0;
                                bytes[1] = 0;
                                bytes[2] = 10;
                                //bytes[3] = (byte)(waitTime);
                                bytes[3] = 7;
                                ATPsCom.SendData(bytes, item.EP);
                                //Log4ATS.Info(waitTime+"   7");
                                //Log4ATS.Info(bytes[3]);
                            }
                        }
                    }
                }
                //该站第二个应答器算出站
                else if (isInSta && dis != null && preOffset != curOffset)
                {
                   int  nln = t.NowLineNum;
                    SelectCollection[nln].Values.Add(new DateTimePoint(DateTime.Now, dis.FirstOrDefault()));
                    //Log4ATS.Info(DateTime.Now + "    " + dis.FirstOrDefault());
                    isInSta = false;
                }
                //Log4ATS.Info(preOffset + "&&&&&&&&&&" + curOffset);
                preName = string.Copy(curName);
                preOffset = curOffset;
            }


        }


        
        //之前内容缓存，剔除重复数据使用
        byte[] preBytes=new byte[24];
        void SendMesToCBI()
        {
            foreach (var curbytes in CBIMesSend.GetConsumingEnumerable())
            {
                if (!curbytes.SequenceEqual(preBytes))
                {
                    if (curbytes[13]==(byte)(命令类型.功能按钮)&&(curbytes[12] == (byte)功能按钮.总人解 || curbytes[12] == (byte)功能按钮.总取消))
                    {
                        lock (lockobj)
                        {
                            List<TryBytes> rmBytes = Confirmbytes.FindAll((TryBytes trybytes) =>
                            {
                                byte[] bytes = trybytes.bytes;
                                //信号机相同
                                if (bytes[12] == curbytes[14] && bytes[13] == bytes[15] && bytes[13] == (byte)命令类型.列车按钮)
                                    return true;
                                return false;
                            });
                            for(int i = 0; i < rmBytes.Count; i++)
                            {
                                Confirmbytes.Remove(rmBytes[i]);
                            }
                        }
                        
                    }

                    foreach (var ep in CBIsCom.REPs)
                    {
                        CBIsCom.SendData(curbytes, ep, curbytes.Length);
                    }
                    if(curbytes[13]==1&&curbytes[15]==1)
                        lock (lockobj)
                        {
                            Confirmbytes.Add(new TryBytes(curbytes,ConfirmTimes));
                            //Log4ATS.Info(curbytes[12] +"     "+ curbytes[14]);
                        }
                    Array.Copy(curbytes, preBytes, curbytes.Length);
                }
            }
        }

        //ConcurrentBag<TryBytes> Tryqueue = new ConcurrentBag<TryBytes>();
        //BlockingCollection<TryBytes> Tryqueue = new BlockingCollection<TryBytes>(50);
        //ConcurrentQueue<TryBytes> Tryqueues = new ConcurrentQueue<TryBytes>();
        List<TryBytes> Confirmbytes = new List<TryBytes>();
        private readonly Object lockobj = new object(); 


        /// <summary>
        /// 确保进路开放正常
        /// </summary>

        void ConfirmRouteOpen()
        {
            while(true)
            {

                List<TryBytes> liveTrybytes = new List<TryBytes>();
                Thread.Sleep(ConfirmInterval);
                lock (lockobj)
                {
                    for(int i= Confirmbytes.Count-1;i>-1;i--)
                    {
                        if (!IsRouteOpen(Confirmbytes[i].bytes))
                        {
                            foreach (var ep in CBIsCom.REPs)
                            {
                                byte[] bytes = Confirmbytes[i].bytes;
                                CBIsCom.SendData(bytes, ep, bytes.Length);
                                //Log4ATS.Info(bytes[12] + "++++" + bytes[14] + "---" + ep);
                            }
                            Confirmbytes[i].TryTimeAddOnce();
                            if (Confirmbytes[i].IsTryFinished())
                            {
                                Confirmbytes.RemoveAt(i);
                            }
                        }
                        else
                        {
                            Confirmbytes.RemoveAt(i);
                        }
                    }
                }
            }

        }

        /// <summary>
        /// 检查开进路命令是否执行完毕
        /// </summary>
        /// <param name="bytes">待检测开进路命令</param>
        /// <returns></returns>
        bool IsRouteOpen(byte[] bytes)
        {
            if (bytes[13] == 1 && bytes[15] == 1)
            {
                Signal signal = Signals.Find((Signal sg) => {
                    return sg.ID == bytes[12];
                });
                Route route =RC.Routes.Find((ATSRoute ar) =>{
                    return ar.StartSignal.ID == bytes[12] && ar.EndSignal.ID == bytes[14];
                });
                if (signal.SColor != Signal.SignalColor.Green 
                    && signal.SColor != Signal.SignalColor.GreenYellow) return false;
                if(route!=null)
                {
                    Device de = route.InSections.First();
                    if (de is Section)
                    {
                        Section sc = de as Section;
                        return sc.IsOccupied;
                    }
                    else
                    {
                        RailSwitch rs = de as RailSwitch;
                        return rs.IsOccupied;
                    }
                }
                else return true;
            }
            else
            {
                return true;
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
            if (sList != null)
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
            else
            {
                StaNamesDict = new Dictionary<double, string>();
                foreach (var item in StationList)
                {
                    if (!StaNamesDict.ContainsKey(item.Distance)) StaNamesDict.Add(item.Distance, item.Name);
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
            //DateTime min = DateTime.MinValue;
            List<plan> pl = sj.plan.ToList();
            for (int i = 0; i < pl.Count; i++)
            {
                if (i > 0)
                    dpl.Add(new DateTimePoint(DateTime.Today.Add(pl[i].ReachTime.Value), Convert.ToDouble(pl[i].Distance)));
                if (i < pl.Count - 1)
                    dpl.Add(new DateTimePoint(DateTime.Today.Add(pl[i].LeaveTime.Value), Convert.ToDouble(pl[i].Distance)));
            }
            return dpl;
        }

        object temp = new object();
        //显示集合
        private SeriesCollection _selectCollection;
        private SeriesCollection _AdjustCollection=new SeriesCollection();
        public SeriesCollection SelectCollection
        {
            get {
                lock (temp)
                {
                    return _AdjustCollection;
                    //return null;
                }
            }
            set
            {
                lock (temp)
                {
                    if (value != _selectCollection)
                    {
                        _selectCollection = value;
                        _AdjustCollection = AdjustTime(_selectCollection);
                        RaisePropertyChanged("SelectCollection");
                    }
                }
            }
        }

        //时间放大比例尺
        double timek = 10;
        ///产生一个特定时间跨度的集合
        LiveCharts.SeriesCollection AdjustTime(SeriesCollection sc)
        {
            SeriesCollection res=new SeriesCollection();
            if (sc == null)
                return null;
            else
            {
                for(int i = 0; i < sc.Count; i++)
                {
                
                    res.Add(new LineSeries()
                    {
                        Title = sc[i].Title,
                        LineSmoothness = 0,
                        Values = new ChartValues<DateTimePoint>(),
                        Fill = new SolidColorBrush()
                    });
                    for(int j=0;j<sc[i].Values.Count;j++)
                    {
                        if (j == 0)
                        {
                            DateTimePoint point = sc[i].Values[j] as DateTimePoint;
                            if(point!=null)
                            res[i].Values.Add(new DateTimePoint((point.DateTime),point.Value));
                        }
                        else
                        {
                            DateTimePoint point1 = sc[i].Values[j] as DateTimePoint,
                            point2 = sc[i].Values[j - 1] as DateTimePoint;
                            TimeSpan ts = point2.DateTime - point1.DateTime;
                            ts = new TimeSpan(Convert.ToInt64(ts.Milliseconds * timek));
                            res[i].Values.Add(new DateTimePoint((point2.DateTime.Add(ts)), point2.Value));
                        }

                    }
                }
                return res;
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
            PlanNum = sl.Count;
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

        int PlanNum = 0;

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
            //try
            //{
                var pw = new PlanWindow();
                PlanViewModel pvm = pw.DataContext as PlanViewModel;
                pvm.UpdateRunChartAction += InitPointSeries;
                pw.ShowDialog();
            //}
            //catch
            //{
                //MessageBox.Show("数据库连接故障，请检查网络连接与配置!");
            //}

        }

        void cmdMouseLeftButtonDown(object obj)
        {
            if (obj is Canvas)
            {

            }
            else
            {
                var De = (object)Mouse.DirectlyOver;
                if (De is ATS.SmallButton)
                {
                    ATS.SmallButton sm = De as SmallButton;
                    Signal s=Signals.Find((Signal ss)=>{
                    return ss.ID==sm.ID;
                    });
                    if(s.IsCBTCRoute)
                    {

                    }else
                    {
                        sm.PressDown();
                        manualCB.AddDevice(De);
                    }
                }

                else
                manualCB.AddDevice(De);
            }
        }

        //信号机右键，道岔右键，区段右键
        ContextMenu[] cms=new ContextMenu[4];


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
                    if(tempObject is Train)
                    {
                        Train t = tempObject as Train;
                        //"进入车队模式","退出车队模式"
                        if (name == "进入车队模式")
                        {
                            t.TrainState = 列车模式.车队模式;
                            t.TrainOrderNum += "队";
                        }
                        else if (name == "退出车队模式") t.TrainState = 列车模式.非车队模式;
                    }
                    else
                    {
                        manualCB.AddDevice(tempObject);
                        manualCB.AddDevice(name);
                    }

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


        public static T FindVisualParent<T>(UIElement element) where T : UIElement
        {
            UIElement parent = element;
            while (parent != null)
            {
                var correctlyTyped = parent as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }

                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }

            return null;
        }

        public static T GetElementUnderMouse<T>() where T : UIElement
        {
            return FindVisualParent<T>(Mouse.DirectlyOver as UIElement);
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
                tempObject = De;
            }
            else if (De is RailSwitch)
            {
                RailSwitch t = (RailSwitch)De;
                cms[1].PlacementTarget = t;
                cms[1].IsOpen = true;
                tempObject = De;
            }
            else if (De is Section)
            {
                Section t = (Section)De;
                cms[2].PlacementTarget = t;
                cms[2].IsOpen = true;
                tempObject = De;
            }
            else if (De is Polygon)
            {
                Train train = ((De as Polygon).Parent as Canvas).Parent as Train;
                tempObject = train;
                cms[3].PlacementTarget = train;
                cms[3].IsOpen = true;
            }
            else if(De is System.Windows.Controls.TextBlock)
            {

                Train train = GetElementUnderMouse<Train>();
                tempObject = train;
                cms[3].PlacementTarget = train;
                cms[3].IsOpen = true;
            }
            //else if(De is Train)
            //{
            //    Train t = De as Train;
            //    cms[3].PlacementTarget = t;
            //    cms[3].IsOpen = true;
            //}
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
            foreach (var item in stationElements_.Elements)
            {
                item.RightButtonMenu.Items.Clear();
            }
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
                    FlashTrain();
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

        void FlashTrain()
        {
            foreach (var t in Trains)
            {
                if (t.IsEmergent)
                {
                    if (t.Visibility == Visibility.Hidden)
                    {
                        t.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        t.Visibility = Visibility.Hidden;
                    }
                }
                else
                {
                    t.Visibility = Visibility.Visible;
                }
            }
        }

        #endregion

        #region 设备分类、初始化各联锁段设备数量
        List<RailSwitch> RailSwitches = new List<RailSwitch>();
        List<Section> Sections = new List<Section>();
        List<Signal> Signals = new List<Signal>();
        List<PSDoor> PSdoors = new List<PSDoor>();
        List<SmallButton> SmallButtons = new List<SmallButton>();
        List<RelayButton> RelayButtons = new List<RelayButton>();

        //保存不同联锁的设备数量

        List<int> RsNum = new List<int>();
        List<int> SecNum = new List<int>();
        List<int> SignalNum = new List<int>();
        List<int> PSDoorNum = new List<int>();
        List<int> RBNum = new List<int>();


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
                else if (item is PSDoor)
                {
                    PSdoors.Add(item as PSDoor);
                }
                else if (item is RelayButton)
                {
                    RelayButtons.Add(item as RelayButton);
                }
                else if (item is SmallButton)
                {
                    SmallButtons.Add(item as SmallButton);
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
            for (int i = 0; i < CBINum; i++)
            {
                int k = PSdoors.FindAll((PSDoor ps) =>
                {
                    return ps.StationID == i;
                }).Count;
                PSDoorNum.Add(k);
                k = RelayButtons.FindAll((RelayButton rb) =>
                {
                    return rb.StationID == i;
                }).Count;
            }

            Section.StartByte = 8;
            RailSwitch.StartByte = Section.StartByte + 384;
            Signal.StartByte = RailSwitch.StartByte + 128;
            PSDoor.StartByte = Signal.StartByte + 128 * 3;

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

        void InitMes(int TrainMesNum=6,int HandleMesNUm=6)
        {
            trainMesQueue = new FixedConQueue<TrainMes>(TrainMesNum);
            HandleMesQueue = new FixedConQueue<HandleMes>(TrainMesNum);

        }

        public FixedConQueue<TrainMes> trainMesQueue;
        public FixedConQueue<TrainMes> TrainMesQueue
        {
            get{return trainMesQueue;}
            set{
                trainMesQueue = value;
                RaisePropertyChanged("TrainMess");
                //TrainMesDsipaly = new List<TrainMes>(trainMess);
            }
        
        }

        private List<TrainMes> trainMesDispaly;
        public List<TrainMes> TrainMesDsipaly
        {
            get { return trainMesDispaly; }
            set{
                trainMesDispaly = value;
                RaisePropertyChanged("TrainMesDsipaly");

            }
        }

        public void UpdateHandleMes(FixedConQueue<HandleMes> fixqueue)
        {
            HandleMesDispaly = new List<HandleMes>(fixqueue);
        }
        public FixedConQueue<HandleMes> HandleMesQueue { get; set; }

        private List<HandleMes> handleMesdipaly;
        public List<HandleMes> HandleMesDispaly
        {
            get { return handleMesdipaly;}
            set{
                handleMesdipaly = value;
                RaisePropertyChanged("HandleMesDispaly");
            }
        }



        /// <summary>
        /// zc信息产生工厂
        /// </summary>
        /// <param name="ge"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        TrainMes TrainMesFactory(GraphicElement ge,ZC2ATS info)
        {
            TrainMes mes = new TrainMes();
            if (ge is Section)
            {
                Section sc = ge as Section;
                mes.CBIPos = Convert.ToString(sc.StationID);
            }
            else if(ge is RailSwitch)
            {
                RailSwitch rs = ge as RailSwitch;
                mes.CBIPos =Convert.ToString(rs.StationID);
            }
            mes.RunMode = Convert.ToString((ActualRunMode)(info.Runmode));
            mes.Speed = info.Speed.ToString()+"km/h";
            GraphicElement MASection=null;
            App.Current.Dispatcher.Invoke(() =>
            {
                MASection = stationElements_.Elements.Find((线路绘图工具.GraphicElement item) =>
                {
                    if (info.MAType == (byte)DeviceType.区段 && item is Section)
                    {
                        Section sc = item as Section;
                        return sc.ID == info.MAId;
                    }
                    else if (info.MAType == (byte)DeviceType.道岔 && item is RailSwitch)
                    {
                        RailSwitch rs = item as RailSwitch;
                        return rs.ID == info.MAId;
                    }
                    else
                    {
                        return false;
                    }
                }); 
            });
            if (MASection != null)
            {
                if (MASection is RailSwitch)
                {
                    RailSwitch rs = MASection as RailSwitch;
                    mes.Destnation = rs.SectionName;
                }
                else
                {
                    mes.Destnation = MASection.Name;
                }
            }
            else mes.Destnation = null;
            mes.IsPlanTrain = "是";
            mes.TrainPos = ge==null?"无":ge.Name + " " + info.Offset.ToString()+"m";
            return mes;
        }


        HandleMes CBIerrorFactory(string CBIErrorStr)
        {
            HandleMes handleMes = new HandleMes();
            handleMes.ErrorMes2 = String.Copy(CBIErrorStr);
            return handleMes;
        }
    }




}
