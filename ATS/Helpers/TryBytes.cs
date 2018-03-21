using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATS
{
    /// <summary>
    /// 尝试2次发送的数据
    /// </summary>
    public class TryBytes
    {
        int Maxtrys = 2;
        int TryTimes;
        public byte[] bytes;
        public TryBytes(byte[] bytes)
        {
            this.bytes = bytes;
            TryTimes = 0;
        }

        public TryBytes(byte[] bytes,int MaxConfirmTimes)
        {
            this.bytes = bytes;
            TryTimes = 0;
            Maxtrys = MaxConfirmTimes;
        }
        /// <summary>
        /// 尝试次数+1
        /// </summary>
        public void TryTimeAddOnce() { TryTimes++; }

        /// <summary>
        /// 判断尝试次数是否超限
        /// </summary>
        /// <returns></returns>
        public bool IsTryFinished() { return TryTimes >= Maxtrys;}
    }
}
