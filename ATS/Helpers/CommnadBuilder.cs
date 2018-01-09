using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 线路绘图工具;
using System.Collections.Concurrent;
using System.Windows.Controls;

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

        public CommandBuilder(BlockingCollection<byte[]> CBIMes, List<ATSRoute> routes,List<Signal> FlashSignals)
        {
            this.CBIMes = CBIMes;
            Routes = routes;
            this.FlashSignals = FlashSignals;
        }

        BlockingCollection<byte[]> CBIMes;
        List<ATSRoute> Routes;
        List<Signal> FlashSignals=new List<Signal>();

        readonly int Max_Wait_Seconds = 20;


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
            }
            else if(cmdqueue.Count%2==0&&cmdqueue.Count>1)
            {
                while (Wait_seconds > 0)
                    Wait_seconds = 0;
                TryBuildCmd();
            }
        }

        void TryBuildCmd()
        {
            List<Object> Clicks = new List<object>();
            for (int i = 0; i < 2; i++)
            {
                Clicks.Add(cmdqueue.Dequeue());
            }
            //Package(Clicks);
            SerilizePackage(Clicks);
            //if (Clicks[0] is Signal && Clicks[1] is Signal) TryBuildRouteCmd(Clicks);
            //else if (Clicks[0] is string || Clicks[1] is string)
            //{
            //    TryBuildButtonCommand(Clicks);
            //}
        }


        //void TryBuildRouteCmd(List<Object> Clicks)
        //{
        //    Signal s1=Clicks[0] as Signal;
        //    Signal s2=Clicks[1] as Signal;
        //    //ATSRoute resRoute = Routes.Find((ATSRoute ar) =>
        //    //    {
        //    //        return ar.StartSignal == s1 && ar.EndSignal ==s2;
        //    //    });
        //    //if (resRoute != null)
        //    //{
        //        byte[] bytes = new byte[24];
        //        bytes[0] = (byte)s1.StationID;
        //        bytes[8] =(byte) Clicks.Count;
        //        bytes[12] =(byte)s1.ID;
        //        bytes[13] = 1;
        //        bytes[14] = (byte)s2.ID;
        //        bytes[15] = 1;
        //        bool IsAdd=false;
        //        while(!IsAdd)
        //        {
        //            IsAdd = CBIMes.TryAdd(bytes);
        //        }

        //    //}
        //}

        //void TryBuildButtonCommand(List<Object> Clicks)
        //{
        //    if (Clicks[1] is string)
        //    {
        //        Object obj = Clicks[0];
        //        Clicks[0] = Clicks[1];
        //        Clicks[1] = obj;
        //    }
        //    string b1 = Clicks[0] as string;
        //    if (b1 != null)
        //    {
        //        if (b1==功能按钮.总人解.ToString())
        //        {
        //            if (Clicks[1] is Signal)
        //            {
        //                Signal s1 = Clicks[1] as Signal;
        //                byte[] bytes = new byte[24];
        //                bytes[0] = (byte)s1.StationID;
        //                bytes[8] = (byte)Clicks.Count;
        //                bytes[12] = 7;
        //                bytes[13] = 5;
        //                bytes[14] = (byte)s1.ID;
        //                bytes[15] = 1;
        //                bool IsAdd = false;
        //                while (!IsAdd)
        //                {
        //                    IsAdd = CBIMes.TryAdd(bytes);
        //                }
        //            }
        //        }
        //    }

        //}

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


        //装包函数
        void Package(List<Object> Clicks)
        {
            if (Clicks[1] is string)
            {
                Object obj = Clicks[0];
                Clicks[0] = Clicks[1];
                Clicks[1] = obj;
            }
            byte[] bytes = new byte[24];
            int k=0;
            if(Clicks[1] is Device)
            {
                bytes[0]=(byte)(Clicks[1] as Device).StationID;
            }
            bytes[8]=(byte)Clicks.Count;
            for (int i = 12; i < 20; i += 2)
            {
                if(k>=Clicks.Count)
                    break;
                if (Clicks[k] is Device)
                {
                    bytes[i] = (byte)(Clicks[k] as Device).ID;
                    if(Clicks[k] is Signal)
                    bytes[i + 1] = (byte)命令类型.列车按钮;
                    else if(Clicks[k] is RailSwitch)
                        bytes[i+1]=(byte)命令类型.道岔按钮;
                    else if(Clicks[k] is Section)
                        bytes[i+1]=(byte)命令类型.区段按钮;
                }
                else if(Clicks[k] is string)
                {
                    string s = Clicks[k].ToString();
                    foreach (byte item in Enum.GetValues(typeof(功能按钮)))
                    {
                        if (Enum.GetName(typeof(功能按钮), item) == s)
                        {
                            bytes[i] = item;
                            break;
                        }
                    }
                    bytes[i + 1] = (byte)命令类型.功能按钮;
                }
                k++;
            }
            bool IsAdd = false;
            while (!IsAdd)
            {
                IsAdd = CBIMes.TryAdd(bytes);
            }
        }


        StructBytes<ATS2CBICommand> pacTool=new StructBytes<ATS2CBICommand>();
        /// <summary>
        /// 序列化装包函数
        /// </summary>
        /// <param name="Clicks"></param>
        void SerilizePackage(List<Object> Clicks)
        {
            //string在前，设备id在后
            if (Clicks[1] is string)
            {
                Object obj = Clicks[0];
                Clicks[0] = Clicks[1];
                Clicks[1] = obj;
            }
            ATS2CBICommand atsCommand=new ATS2CBICommand();
            int k = 0;
            if (Clicks[1] is Device)
            {
                atsCommand.StationID = (UInt16)(Clicks[1] as Device).StationID;
            }
            atsCommand.DeviceNum = (UInt16)Clicks.Count;
            atsCommand.DeviceQueue = new byte[8];
            foreach (var item in Clicks)
            {

                if (Clicks[k/2] is Device)
                {
                    atsCommand.DeviceQueue[k] = (byte)(Clicks[k/2] as Device).ID;
                    k++;
                    if (Clicks[k/2] is Signal)
                        atsCommand.DeviceQueue[k++] = (byte)命令类型.列车按钮;
                    else if (Clicks[k/2] is RailSwitch)
                        atsCommand.DeviceQueue[k++] = (byte)命令类型.道岔按钮;
                    else if (Clicks[k/2] is Section)
                        atsCommand.DeviceQueue[k++] = (byte)命令类型.区段按钮;
                }
                else if (Clicks[k/2] is string || Clicks[k/2] is String)
                {
                    string s = Clicks[k/2].ToString();
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
            bool IsAdd = false;
            byte[] bytes = pacTool.Serialize(atsCommand);
            while (!IsAdd)
            {
                IsAdd = CBIMes.TryAdd(bytes);
            }
        }
    }
}
