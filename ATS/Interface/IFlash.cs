using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATS
{
    /// <summary>
    /// 设定定时器定时触发
    /// </summary>
     internal interface IFlash
    {

         //是否闪烁的控制位
        bool IsFlashing
        {
            get;
        }


        //控制当前周期是否闪烁
        bool FlashFlag
        {
            get;
            set;
        }

        //控制名字是否闪烁
        bool FlashNameFlag
        {
            get;
            set;
        }
    }
}
