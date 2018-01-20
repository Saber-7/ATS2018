using System.Windows.Media;
using 线路绘图工具;

namespace ATS
{
    public class PSDoor : 线路绘图工具.PSDoor
    {
        static Pen DisconnectPen = new Pen(Brushes.Yellow, 3);

        public static int StartByte { get; set; }

        public bool IsStatusChanged { get; set; }

        //public bool IsFailed { get; }

        int startByte_;
        bool isOccupied_ = true;
        bool IsOccupied
        {
            get { return isOccupied_; }
            set
            {
                if (value != isOccupied_)
                {
                    isOccupied_ = value;
                    IsStatusChanged = true;
                }
            }
        }
        bool isDisconnected_ = false;
        bool IsDisconnected
        {
            get { return isDisconnected_; }
            set
            {
                if (value != isDisconnected_)
                {
                    isDisconnected_ = value;
                    IsStatusChanged = true;
                }
            }
        }

        public void ResetDefaultStatus()
        {
            IsOccupied = true;
        }

        public void SetCommandMenu()
        {
        }

        public void SetStartByte(int ID)
        {
            startByte_ = StartByte + ID;
        }

        //public void SetLocalStartByte(CiStartId startID)
        //{
        //    SetStartByte(ID - startID.PSDoorStart);
        //}

        public void UpdateStatus(byte[] recvBuf, int nRecv)
        {
            const byte TRUE_VALUE = 0X05;
            IsDisconnected = (recvBuf[startByte_] >> 4) == TRUE_VALUE;
            IsOccupied = (recvBuf[startByte_] & 0x0f) == TRUE_VALUE;
        }

        protected override void OnRender(DrawingContext dc)
        {
            Pen pen = IsDisconnected ? DisconnectPen : DefaultPen_;
            
            (graphics_[0] as Line).OnRender(dc, pen);
            (graphics_[2] as Line).OnRender(dc, pen);

            if (!IsOccupied && !IsDisconnected)
            {
                (graphics_[1] as Line).OnRender(dc, DefaultPen_);
            }
        }
    }
}
