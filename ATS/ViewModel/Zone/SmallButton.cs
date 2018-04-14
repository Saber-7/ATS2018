using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATS
{
    public class SmallButton : 线路绘图工具.SmallButton
    {
        按钮 _ButtonState;

        public SmallButton()
        {
            ButtonState = 按钮.弹起;
        }


        public void ClickButton()
        {
            if (ButtonState == 按钮.弹起)
                ButtonState = 按钮.按下;
            else ButtonState = 按钮.弹起;
        }


        internal 按钮 ButtonState
        {
            get
            {
                return _ButtonState;
            }

            set
            {
                _ButtonState = value;
            }
        }
    }

    enum 按钮:byte
    {
        按下=30,
        弹起=31
    }
}
