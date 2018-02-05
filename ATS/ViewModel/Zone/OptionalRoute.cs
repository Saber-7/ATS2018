using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 线路绘图工具;

namespace ATS
{
    class OptionalRoutes:IComparable<OptionalRoutes>
    {

        public OptionalRoutes(OptionalRoutes op)
        {
            Distance = op.Distance;
            Routes = new List<ATSRoute>(op.Routes);
        }
        public OptionalRoutes()
        {
            Routes = new List<ATSRoute>();
        }

        public void  ExtendLength(OptionalRoutes route)
        {
            Routes.AddRange(route.Routes);
            Distance += route.Distance;
        }

        //进路方案
        public List<ATSRoute> Routes { get; set; }
        
        //方案距离
        public double Distance { get; set; }

        /// <summary>
        /// 更新距离结果,查找距离完毕后更新
        /// </summary>
        public void UpdateDistance()
        {
            Distance = 0;
            foreach (ATSRoute a in Routes)
            {
                Distance += a.Distance;
            }
        }
        /// <summary>
        /// 距离排序，确定进路方案选用的优先级
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>


        int IComparable<OptionalRoutes>.CompareTo(OptionalRoutes other)
        {
            return this.Distance.CompareTo(other.Distance);
        }
    }
}
