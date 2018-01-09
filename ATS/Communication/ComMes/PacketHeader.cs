using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATS
{
    public enum DataType : byte
    {
        Default = 0,
        CIToZC = 1,
        ZCToCI = 2,
        ATPToZC = 8,
        ZCToATP = 9,
        ATSToZC = 10,
        ZCToATS = 11
    }

    public enum DeviceID : byte
    {
        CI1 = 1,
        CI2 = 2,
        CI3 = 17,
        CI4 = 18,
        ZC = 3,
        ATP1 = 4,
        ATP2 = 5,
        ATP3 = 6,
        ATP4 = 7,
        ATS = 19
    }

    class PacketHeader
    {
        private UInt32 _cycleNumber;

        public UInt32 CycleNumber
        {
            get { return _cycleNumber; }
            set { _cycleNumber = value; }
        }

        private DataType _type;

        public DataType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        private DeviceID _senderID;

        public DeviceID SenderID
        {
            get { return _senderID; }
            set { _senderID = value; }
        }

        private DeviceID _receiveID;

        public DeviceID ReceiveID
        {
            get { return _receiveID; }
            set { _receiveID = value; }
        }

        private UInt16 _dataLength;

        public UInt16 DataLength
        {
            get { return _dataLength; }
            set { _dataLength = value; }
        }

    }
}
