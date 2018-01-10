using System.Windows.Media;
using 线路绘图工具;

namespace CBI_LCP
{
    public class RelayButton : 线路绘图工具.RelayButton, ITrackSide
    {
        public bool IsStatusChanged { get; set; }

        bool ITrackSide.IsFailed { get; }

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

        public void ResetDefaultStatus()
        {
            isOccupied_ = true;
        }

        public void SetCommandMenu()
        {
        }

        public void SetStartByte(int ID)
        {
            startByte_ = Section.StartByte + ID * 3;
        }

        public void SetLocalStartByte(CiStartId startID)
        {
            SetStartByte(ID - startID.SectionStart);
        }

        public void UpdateStatus(byte[] recvBuf, int nRecv)
        {
            const byte TRUE_VALUE = 0X05;
            IsOccupied = (recvBuf[startByte_ + 1] >> 4) == TRUE_VALUE;
        }

        protected override void OnRender(DrawingContext dc)
        {
            Rectangle rect = graphics_[0] as Rectangle;
            rect.OnRender(dc, isOccupied_ ? Brushes.Red : Brushes.DarkGreen);
            dc.DrawText(formattedName_, namePoint_);
        }
    }
}
