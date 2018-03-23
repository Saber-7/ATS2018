using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 线路绘图工具;
using System.Collections.Concurrent;
using System.Windows.Controls;
using ATS;

namespace ATS
{
    /// <summary>
    /// 手动命令创建与检测，假设单人操作,调用addDevice方法即可
    /// </summary>
    class CommandBuilder
    {
        public CommandBuilder(BlockingCollection<byte[]> CBIMes,List<ATSRoute> routes)
        {
            this.CBIMes=CBIMes;
            Routes = routes;
        }

        //public CommandBuilder(BlockingCollection<byte[]> CBIMes, List<ATSRoute> routes, FixedConQueue<HandleMes> handleMess,List<HandleMes> handleMesDisplay)
        //{
        //    this.CBIMes = CBIMes;
        //    Routes = routes;
        //    this.handleMess=handleMess;
        //    this.handleMesDisplay = handleMesDisplay;
        //}
        public CommandBuilder(BlockingCollection<byte[]> CBIMes, List<ATSRoute> routes,UpdateMesHandle UpdateMesEvent)
        {
            this.CBIMes = CBIMes;
            Routes = routes;
            handleMess = new FixedConQueue<HandleMes>(50);
            this.UpdateMesEvent = UpdateMesEvent;
        }

        BlockingCollection<byte[]> CBIMes;
        List<ATSRoute> Routes;
        List<Signal> FlashSignals=new List<Signal>();
        FixedConQueue<HandleMes> handleMess;
        readonly int Max_Wait_Seconds = 20;
        event UpdateMesHandle UpdateMesEvent;
        readonly object lockobj = new object();

        int wait_seconds = 0;


        object locker=new object();
        public int Wait_seconds
        {
            get { 
               lock(locker)  return wait_seconds; }
            set { 
               lock(locker) wait_seconds = value; }
        }
        Queue<object> cmdqueue = new Queue<object>(); 
        public void AddDevice(object obj)
        {
            cmdqueue.Enqueue(obj);
            if (cmdqueue.Count%2 ==1)
            {
                Wait_seconds = Max_Wait_Seconds;
                if (obj is Signal)
                {
                    FlashSignals.Add((Signal)obj);
                }
                if (obj is RelayButton)
                {
                    TryBuildCmd(1);
                }
            }
            else if(cmdqueue.Count%2==0&&cmdqueue.Count>1)
            {
                while (Wait_seconds > 0)
                    Wait_seconds = 0;
                TryBuildCmd(2);
            }
        }

        public void AddTwoDevice(List<object> objs)
        {
            foreach (var obj in objs)
            {
                cmdqueue.Enqueue(obj);
            }
            if (cmdqueue.Count > 1)
            {
                while (Wait_seconds > 0)
                    Wait_seconds = 0;
                TryBuildCmd(2);
            }
        }


        void TryBuildCmd(int CommandLen)
        {
            List<Object> Clicks = new List<object>();
            for (int i = 0; i < CommandLen; i++)
            {
                Clicks.Add(cmdqueue.Dequeue());
            }
            if (CommandCheck(Clicks))
            {
                SerilizePackage(Clicks);
            }
            else
            {
                BuildHandleMes(Clicks, true);
            }

        }


        /// <summary>
        /// 命令名字闪烁操作
        /// </summary>
        public void FlashName()
        {
            if (Wait_seconds > 0)
            {
                Wait_seconds--;
                foreach (var item in FlashSignals)
                {
                    if (item is IFlash)
                    {
                        IFlash f = item as IFlash;
                        f.FlashNameFlag = !f.FlashNameFlag;
                        item.InvalidateVisual();
                    }
                }
            }
            else
            {
                foreach (var item in FlashSignals)
                {
                    item.FlashNameFlag = false;
                }
                FlashSignals.Clear();
            }
        }


        //序列化工具
        StructBytes<ATS2CBICommand> pacTool=new StructBytes<ATS2CBICommand>();
        /// <summary>
        /// 序列化装包函数
        /// </summary>
        /// <param name="Clicks"></param>
        void SerilizePackage(List<Object> Clicks)
        {
            
            switch(Clicks.Count)
            {
                case 1:
                    { 
                    //to do 扣车代码
                    }
                    break;
                case 2:
                    {
                        ATS2CBICommand atsCommand = new ATS2CBICommand();
                        //string在前，设备id在后
                        if (Clicks[1] is string)
                        {
                            Object obj = Clicks[0];
                            Clicks[0] = Clicks[1];
                            Clicks[1] = obj;
                        }
                        BuildHandleMes(Clicks);
                        foreach (var item in Clicks)
                        {
                            if (item is Device)
                            {
                                Device de = item as Device;
                                atsCommand.StationID = (UInt16)de.StationID;
                                break;
                            }
                        }
                        atsCommand.DeviceNum = (UInt16)Clicks.Count;
                        atsCommand.DeviceQueue = new byte[8];

                        int k = 0;


                        foreach (var item in Clicks)
                        {

                            if (Clicks[k / 2] is Device)
                            {
                                atsCommand.DeviceQueue[k] = (byte)(Clicks[k / 2] as Device).ID;
                                k++;
                                    if (Clicks[k / 2] is Signal)
                                    atsCommand.DeviceQueue[k] = (byte)命令类型.列车按钮;
                                else if (Clicks[k / 2] is RailSwitch)
                                    atsCommand.DeviceQueue[k] = (byte)命令类型.道岔按钮;
                                else if (Clicks[k / 2] is Section)
                                    atsCommand.DeviceQueue[k] = (byte)命令类型.区段按钮;
                                k++;
                            }
                            else if (Clicks[k / 2] is string)
                            {
                                string s = Clicks[k / 2].ToString();
                                foreach (byte bt in Enum.GetValues(typeof(功能按钮)))
                                {
                                    if (Enum.GetName(typeof(功能按钮), bt) == s)
                                    {
                                        atsCommand.DeviceQueue[k++] = bt;
                                        break;
                                    }
                                }
                                atsCommand.DeviceQueue[k++] = (byte)命令类型.功能按钮;
                            }
                        }
                        if (Clicks.Count>1&&(Clicks[0] as string =="解封"||Clicks[0] as string=="封锁")&&Clicks[1] is RailSwitch)
                        {
                            RailSwitch rs = Clicks[1] as RailSwitch;
                            atsCommand.DeviceQueue[2] = (byte)rs.SectionID;
                            atsCommand.DeviceQueue[3] = (byte)命令类型.区段按钮;
                        }
                        byte[] bytes = pacTool.Serialize(atsCommand);
                        bool IsAdd = false;
                        while (!IsAdd)
                        {
                            IsAdd = CBIMes.TryAdd(bytes);
                        }
                    }
                    break;
                default: break;
            }
        }

        /// <summary>
        /// 检查是否有错误命令 1.只点击一次对象的时候必然是扣车2.两次点击对象不能相同3.两个信号机的方向不能相反
        /// </summary>
        /// <param name="TestObjs"></param>
        /// <returns></returns>
        bool CommandCheck(List<Object> TestObjs)
        {
            if (TestObjs.Count<2)
            {
                return TestObjs.FirstOrDefault() is RelayButton; 
            }
            if(TestObjs.Count==2)
            {
                if (TestObjs[0] != TestObjs[1])
                {
                    if (TestObjs[0] is Signal && TestObjs[1] is Signal)
                    {
                        Signal s1 = TestObjs[0] as Signal;
                        Signal s2 = TestObjs[0] as Signal;
                        return s1.IsUp == s2.IsUp;
                    }
                    else return true;
                }
                else return false;
            }
            return false;
        }


        HandleMes HandleFactory(object obj)
        {
            HandleMes mes = new HandleMes();
            if (obj is string)
            {
                mes.CBIName = "";
                mes.HandleButton = obj as string;
            }
            else
            {
                if (obj is Signal)
                {
                    Signal s = obj as Signal;
                    mes.CBIName = s==null?null:s.StationID.ToString();
                    mes.HandleButton = s == null ? null:s.Name;
                }
                else
                {
                    RailSwitch rs = obj as RailSwitch;
                    mes.CBIName = rs==null?null:rs.StationID.ToString();
                    mes.HandleButton = rs == null ? null:rs.Name;
                }
                mes.ErrorMes1 = "";
                mes.ErrorMes2 = "";
            }
            return mes;
        }
        HandleMes HandleFactory(object obj,bool IsError)
        {
            HandleMes mes = new HandleMes();
            if (obj is string)
            {
                mes.CBIName = "";
                mes.HandleButton = obj as string;
            }
            else
            {
                if (obj is Signal)
                {
                    Signal s = obj as Signal;
                    mes.CBIName = s == null ? null : s.StationID.ToString();
                    mes.HandleButton = s== null ? null : s.Name;
                }
                else
                {
                    RailSwitch rs = obj as RailSwitch;
                    mes.CBIName = rs == null ? null : rs.StationID.ToString();
                    mes.HandleButton = rs == null ? null : rs.Name;
                }
                if(IsError)
                {
                    mes.ErrorMes1 = "Error";
                    mes.ErrorMes2 = "Error";
                }

            }
            return mes;
        }

        void  BuildHandleMes(List<object> MesList)
        {
            lock(lockobj){
                foreach (var mes in MesList)
                {
                    handleMess.Enqueue(HandleFactory(mes));
                    if (handleMess.IsChanged) UpdateMesEvent(handleMess);
                }
            }

        }
        void BuildHandleMes(List<object> MesList,bool IsError)
        {
            lock(locker)
            {
                if (IsError)
                {
                    foreach (var mes in MesList)
                    {
                        handleMess.Enqueue(HandleFactory(mes, IsError));
                        if (handleMess.IsChanged) UpdateMesEvent(handleMess);
                    }
                }
            }


        }
    }


}
