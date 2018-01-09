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
    public class ATSRoute : 线路绘图工具.Route
    {

        [XmlAttribute("Distance")]
        public double Distance { get; set; }



        [XmlAttribute("Direction")]
        public RouteDirection Dir { get; set; }



    }

    [Serializable]
    public enum RouteDirection
    {
        [XmlEnum(Name = "DIRDOWN")]
        DIRDOWN,
        [XmlEnum(Name = "DIRUP")]
        DIRUP
    }
}
