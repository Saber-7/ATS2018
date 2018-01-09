using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 线路绘图工具;

namespace ATS
{
    class HandleCIData 
    {
        Unpack unpack;
        List<bool> Info = new List<bool>();
        int Num;
        int stationID;
        List<GraphicElement> InRailSwitchs;
        List<GraphicElement> InSections;
        List<GraphicElement> InSignals;

        public static void UpdateDevice(Device device)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(new Action(delegate
            {
                device.InvalidateVisual();
            }));
        }
        public HandleCIData(Unpack unpack, DeviceID senderID,List<GraphicElement> eles)
        {
            this.unpack = unpack;
            switch (senderID)
            {
                case DeviceID.CI1:
                    stationID = 0;
                    break;
                case DeviceID.CI2:
                    stationID = 1;
                    break;
                case DeviceID.CI3:
                    stationID = 2;
                    break;
                case DeviceID.CI4:
                    stationID = 3;
                    break;
                default:
                    break;
            }

            FindInDevices();

            UnpackSwitchStatus();
            UnPackSection();            
            int nSignal = UnpackSignal();

            UnPackProtectionSection(nSignal);


            ELements = eles;

            //站台安全门信息没有


            UnPackAccessState();
            UnPackReturnAccessState();
        }

        List<GraphicElement> ELements;
        private void FindInDevices()
        {
            InRailSwitchs = ELements.FindAll((GraphicElement railswitch) =>
            {
                if (railswitch is RailSwitch)
                {
                    return (railswitch as RailSwitch).StationID == stationID;
                }
                return false;
            });

            InSections = ELements.FindAll((GraphicElement section) =>
            {
                if (section is Section)
                {
                    return (section as Section).StationID == stationID;
                }
                return false;
            });

            InSignals = ELements.FindAll((GraphicElement signal) =>
            {
                if (signal is Signal)
                {
                    return (signal as Section).StationID == stationID;
                }
                return false;
            });
        }

        private void UnPackReturnAccessState()
        {
            int nReturnAccess = 6;
            Info.Clear();
            for (int i = 0; i < nReturnAccess; i++)
            {
                bool IsReturnAccessOpen = unpack.GetBit();
                Info.Add(IsReturnAccessOpen);
            }
            //SetReturnAccessState();
        }

        private void UnPackAccessState()
        {
            int nAccess = 34;
            Info.Clear();
            for (int i = 0; i < nAccess; i++)
            {
                bool IsAccessOpen = unpack.GetBit();
                Info.Add(IsAccessOpen);
            }
            //SetAccessState();
        }

        private void UnPackProtectionSection(int nSignal)
        {
            for (int i = 0; i < nSignal; i++)
            {
                bool InfoOfProtectSection = unpack.GetBit();
            }
            unpack.Skip();
        }

        private int UnpackSignal()
        {
            int nSignal = unpack.GetUint16();
            Info.Clear();
            for (int i = 0; i < nSignal; i++)
            {
                bool isSignalOpen = unpack.GetBit();
                Info.Add(isSignalOpen);
            }
            SetSignalState();
            unpack.Skip();
            return nSignal;
        }

        private void UnPackSection()
        {
            Info.Clear();
            int nSection = unpack.GetUint16();
            for (int i = 0; i < nSection; i++)
            {
                bool isUpward = unpack.GetBit();
                Info.Add(isUpward);
                bool isDownward = unpack.GetBit();
                Info.Add(isDownward);
            }
            SetSectionRunDirection();
            unpack.Skip();

            Info.Clear();
            for (int i = 0; i < nSection; i++)
            {
                bool isOccupied = unpack.GetBit();
                Info.Add(isOccupied);
            }
            SetSectionAxleState();
            unpack.Skip();

            Info.Clear();
            for (int i = 0; i < nSection; i++)
            {
                bool isRouteLocked = unpack.GetBit();
                Info.Add(isRouteLocked);
            }
            //SetAxleSectionAccessLock();
            unpack.Skip();
        }

        private void UnpackSwitchStatus()
        {
            int nSwitch = unpack.GetUint16();
            Info.Clear();
            for (int i = 0; i < nSwitch; i++)
            {
                bool isNormal = unpack.GetBit();
                Info.Add(isNormal);
                bool isReverse = unpack.GetBit();
                Info.Add(isReverse);
            }
            unpack.Skip();
            SetRailSwithPosition();

            Info.Clear();
            for (int i = 0; i < nSwitch; i++)
            {
                bool isLocked = unpack.GetBit();
                Info.Add(isLocked);
            }
            unpack.Skip();
            SetRailSwithLock();
        }

        private void SetRailSwithPosition()
        {
            Num = 0;
            foreach (var item in InRailSwitchs)
            {
                RailSwitch rail = item as RailSwitch;
                //rail.IsPositionNormal = Info[Num];
                //Num++;
                //rail.IsPositionReverse = Info[Num];
                //Num++;
                rail.State = Info[Num]?RailSwitch.SwitchPosition.PosNormal:RailSwitch.SwitchPosition.PosNeither;
                Num++;
                rail.State = Info[Num] ? RailSwitch.SwitchPosition.PosReverse : RailSwitch.SwitchPosition.PosNeither;
                Num++;
            }
        }

        public void SetRailSwithLock()
        {
            Num = 0;
            foreach (var item in InRailSwitchs)
            {
                (item as RailSwitch).Islock = Info[Num];
                Num++;
            }
        }

        public void SetSectionRunDirection()
        {
            Num = 0;
            foreach (var item in InSections)
            {
                (item as Section).Direction = Info[Num] == true ? 0 : 1;
                Num = Num + 2;
            }
            Dictionary<string, int> RailSwitchDir = new Dictionary<string, int>();
            foreach (var item in InRailSwitchs)
            {
                if (RailSwitchDir.Keys.Contains((item as RailSwitch).SectionName))
                {
                    (item as RailSwitch).Direction = RailSwitchDir[(item as RailSwitch).SectionName];
                }
                else
                {
                    (item as RailSwitch).Direction = Info[Num] == true ? 0 : 1;
                    RailSwitchDir.Add((item as RailSwitch).SectionName, (item as RailSwitch).Direction);
                    Num = Num + 2;
                }
            }
        }

        public void SetSectionAxleState()
        {
            Num = 0;
            foreach (var item in InSections)
            {
                Section section = item as Section;
                if (section.AxleOccupy != Info[Num])
                {
                    section.AxleOccupy = Info[Num];
                    UpdateDevice(section);
                }
                Num++;
            }
            Dictionary<string, bool> RailSwitchAxleState = new Dictionary<string, bool>();
            foreach (var item in InRailSwitchs)
            {
                RailSwitch railswitch = item as RailSwitch;
                if (RailSwitchAxleState.Keys.Contains(railswitch.SectionName))
                {
                    railswitch.AxleOccupy = RailSwitchAxleState[railswitch.SectionName];
                    UpdateDevice(railswitch);
                }
                else
                {
                    if (railswitch.AxleOccupy != Info[Num])
                    {
                        railswitch.AxleOccupy = Info[Num];
                        UpdateDevice(railswitch);
                    }
                    RailSwitchAxleState.Add(railswitch.SectionName, railswitch.AxleOccupy);
                    Num++;
                }                  
                
            }
        }

        public void SetAxleSectionAccessLock()
        {
            Num = 0;
            foreach (var item in InSections)
            {
                //(item as Section).IsAccessLock = Info[Num];
                Num++;
            }
            Dictionary<string, bool> RailSwitchAccessLock = new Dictionary<string, bool>();
            foreach (var item in InRailSwitchs)
            {
                RailSwitch railswitch = item as RailSwitch;
                if (RailSwitchAccessLock.Keys.Contains(railswitch.SectionName))
                {
                    railswitch.IsAccessLock = RailSwitchAccessLock[railswitch.SectionName];
                }
                else
                {
                    railswitch.IsAccessLock = Info[Num];
                    RailSwitchAccessLock.Add(railswitch.SectionName, railswitch.IsAccessLock);
                    Num++;
                }                
            }
        }

        public void SetSignalState()
        {
            Num = 0;
            foreach (var item in InSignals)
            {
                Signal signal = item as Signal;
                if (signal.IsSignalOpen != Info[Num])
                {
                    signal.IsSignalOpen = Info[Num];
                    UpdateDevice(signal);
                }
            }
        }
    }
}
