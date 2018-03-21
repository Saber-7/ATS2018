using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 线路绘图工具;
using System.Xml.Serialization;

namespace ATS
{
    [XmlRoot(ElementName = "Route",Namespace="ATS.ATSRoute")]
    [Serializable]
    public class ATSRoute : 线路绘图工具.Route, IComparable<ATSRoute>
    {

        public ATSRoute()
        {
            OptionalRoutes = new List<ATSRoute>();
        }

        [XmlAttribute("Distance")]
        public double Distance { get; set; }



        [XmlAttribute("Direction")]
        public RouteDirection Dir { get; set; }

        public List<ATSRoute> OptionalRoutes { get; set; }


        #region IComparable<ATSRoute> 成员

        int IComparable<ATSRoute>.CompareTo(ATSRoute other)
        {
            return this.Distance.CompareTo(other.Distance);
        }

        #endregion
    }

    [Serializable]
    public enum RouteDirection:byte
    {
        [XmlEnum(Name = "DIRDOWN")]
        DIRDOWN=0x55,
        [XmlEnum(Name = "DIRUP")]
        DIRUP=0xaa
    }
}
