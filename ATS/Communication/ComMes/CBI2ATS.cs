using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATS
{

    class SectionSate
    {
        public byte RouteLock { get; set; }
        public byte Lock { get; set; }
        public byte ProtectLock { get; set; }
        public byte Occupy { get; set; }
        public byte LockDir { get; set; }
        public byte Reserve { get; set; }
    }

    class RailSwitchState
    {
        public byte SingleLock { get; set; }
        public byte Lock { get; set; }
        public byte Pos { get; set; }
        public byte Reserve { get; set; }
    }
    class SignalState
    {
        public byte LightColor { get; set; }
        public byte Lock { get; set; }
        public byte LightOff { get; set; }
        public byte LightAlert { get; set; }
        public byte AutoPass { get; set; }
        public byte Reserve { get; set; }
    }
    class DoorState 
    {
        public byte CloseDoor { get; set; }
        public byte Side { get; set; }
    }
    class CBI2ATS
    {
        //状态域参数总数（4）
        UInt16 MAX_N_SECT { get; set; }
        UInt16 MAX_N_POINT { get; set; }
        UInt16 MAX_N_SIGNAL { get; set; }
        UInt16 MAX_N_PSD { get; set; }
        
        //有效参数
        UInt16 MesType { get; set; }
        public SectionSate[] SectionStates{get;set;}

        public RailSwitchState[] RailSwitchStates { get; set; }

        public SignalState[] SignalStates { get; set; }


        public DoorState[] DoorStates { get; set; }

        Unpack unpack = new Unpack();

        bool IsInit = false;
        public void UnpackCBI2ATS(byte[] buf)
        {
            if (!IsInit)
            {
                SectionStates = new SectionSate[MAX_N_SECT];
                RailSwitchStates = new RailSwitchState[MAX_N_POINT];
                SignalStates = new SignalState[MAX_N_PSD];
                DoorStates = new DoorState[MAX_N_PSD];
            }
            
            unpack.Reset(buf);
            MesType = unpack.GetUint16();
            for (int i = 0; i < MAX_N_SECT; i++)
            {
                SectionStates[i].RouteLock = unpack.GetLow4bit();
                SectionStates[i].Lock = unpack.GetHigh4bit();
                SectionStates[i].ProtectLock = unpack.GetLow4bit();
                SectionStates[i].Occupy = unpack.GetHigh4bit();
                SectionStates[i].LockDir = unpack.GetLow4bit();
                SectionStates[i].Reserve = unpack.GetHigh4bit();
            }
            for (int i = 0; i < MAX_N_POINT; i++)
            {
                RailSwitchStates[i].SingleLock = unpack.GetLow4bit();
                RailSwitchStates[i].Lock = unpack.GetHigh4bit();
                RailSwitchStates[i].Pos = unpack.GetLow4bit();
                RailSwitchStates[i].Reserve = unpack.GetHigh4bit();
            }
            for (int i = 0; i < MAX_N_SIGNAL; i++)
            {
                SignalStates[i].LightColor = unpack.GetLow4bit();
                SignalStates[i].Lock = unpack.GetHigh4bit();
                SignalStates[i].LightOff = unpack.GetLow4bit();
                SignalStates[i].LightAlert = unpack.GetHigh4bit();
                SignalStates[i].AutoPass = unpack.GetLow4bit();
                SignalStates[i].Reserve = unpack.GetHigh4bit();
            }
            for (int i = 0; i < MAX_N_PSD; i++)
            {
                DoorStates[i].CloseDoor = unpack.GetLow4bit();
                DoorStates[i].Side = unpack.GetHigh4bit();
            }
        }

    }
}
