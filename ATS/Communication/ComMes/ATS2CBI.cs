using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace ATS
{

    [StructLayout(LayoutKind.Sequential)]
    public struct ATS2CBICommand
    {
        public UInt16 StationID;
        public UInt16 PackageID;
        public UInt32 Reserve;
        public UInt16 DeviceNum;
        public UInt16 RouteNum;
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=8)]
        public  byte[] DeviceQueue;
        public UInt32 Crc;
        //public ATS2CBICommand()
        //{
        //    StationID = 0;
        //    PackageID = 0;
        //    Reserve = 0;
        //    DeviceNum = 0;
        //    RouteNum = 0;
        //    DeviceQueue = new byte[8];
        //    Crc = 0;
        //}
    }


    class ATSCommand
    {
        byte _devType;

        public byte DevType
        {
            get { return _devType; }
            set { _devType = value; }
        }
        byte _devID;

        public byte DevID
        {
            get { return _devID; }
            set { _devID = value; }
        }

        public ATSCommand(byte devType,byte devID)
        {
            this.DevType = devType;
            this.DevID = devID;
        }
    }

    enum DevType : byte
    { 
        TrainButton=0x01,
        ShuntButton=0x02,
        SwitchButton=0x03,
        SectionButton=0x04,
        FunctionButton=0x05
    }

    class ATS2CBI
    {

        UInt16 _head = 0xFAFA;

        public UInt16 Head
        {
            get { return _head; }
            set { _head = value; }
        }

        UInt16 _packageID;

        public UInt16 PackageID
        {
            get { return _packageID; }
            set { _packageID = value; }
        }

        private UInt32 _reserve;
        public UInt32 Reserve
        {
            get { return _reserve; }
            set { _reserve = value; }
        }

        public UInt16 CommandButtonNum { get; set; }

        private UInt16 _routeNum = 0;

        public UInt16 RouteNum
        {
            get { return _routeNum; }
            set { _routeNum = value; }
        }





        private ATSCommand[] _CommandQueue = new ATSCommand[4];

        public ATSCommand[] CommandQueue
        {
            get { return _CommandQueue; }
            set { _CommandQueue = value; }
        }


        public UInt32 CRC32 { get; set; }

        Pack _Package ;

        public  Pack Package
        {
          get { return _Package; }
          set { _Package = value; }
        }
        public void CreateRouteCommand(List<byte> Commands,byte commandtype)
        {
            CommandButtonNum =(UInt16) Commands.Count;
            int i = 0;
            for (; i < CommandButtonNum; i++)
            {
                CommandQueue[i] = new ATSCommand(commandtype, Commands[i]);
            }
            for (; i < CommandQueue.Length; i++)
            {
                CommandQueue[i] = new ATSCommand(0, 0);
            }
        }
        public void PackCBI2ATS()
        {
            Package = new Pack();
            //Package.PackUint16(MesType);
            Package.PackUint16(Head);
            Package.PackUint16(PackageID);
            Package.PackUint32(Reserve);
            Package.PackUint16(CommandButtonNum);
            Package.PackUint16(RouteNum);
            foreach (ATSCommand com in CommandQueue)
            {
                Package.PackByte(com.DevID);
                Package.PackByte(com.DevType);

                // BinaryReader
                // BinaryWriter
                // NetStream
            }
            Package.PackUint32(CRC32);

        }




    }
}
