using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATS
{
    class State
    {
        /// <summary>
        /// 封锁解锁、亮灯灭灯、报警不报警、通过不通过、关门不关
        /// </summary>
        enum 锁闭or解锁 : byte
        { 
            锁闭=5,
            解锁=10
        }
        enum 锁闭方向 : byte
        { 
            上行=5,
            下行=10,
            无方向=15
        }
        enum 道岔位置 : byte
        {
            定位=5,
            反位=10,
            四开=12,
            挤岔=3,
        }

        enum 灯色 : byte
        { 
            红=1,
            绿=2,
            黄=3,
            引导=4,
            灯丝1断丝=5,
            灯丝2断丝=6
        }

    }

    enum 功能按钮 : byte
    {
        上电解封 = 0,
        封锁 = 1,
        解封 = 2,
        总定位 = 3,
        总反位 = 4,
        单锁 = 5,
        单解 = 6,
        总人解 = 7,
        总取消 = 8,
        区故解 = 9,
        信号重开 = 0x0C,
        信号关闭 = 0x0E,
        设置车队模式 = 0x1D,
        取消车队模式 = 0x1F
    }
    enum 命令类型 : byte
    { 
        列车按钮=1,
        调车按钮=2,
        道岔按钮=3,
        区段按钮=4,
        功能按钮=5
    }

    public enum ActualRunMode : byte
    {
        AM = 1,
        CM = 2,
        RM = 3,
        EUM = 4,
        注销 = 5

    }
}
