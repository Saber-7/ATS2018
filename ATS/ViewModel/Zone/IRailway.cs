﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 线路绘图工具;

namespace ATS
{
    interface IRailway
    {
        void AddInsulation();
        Graphic InsuLine { get; set; }

        bool IsOccupied { get; set; }

        Axle RelateAxle { get; set; }

    }
}
