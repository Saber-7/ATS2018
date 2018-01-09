using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using 线路绘图工具;

namespace ATS
{
    // 必须是 public class，名称必须是 RouteCreator
    // 因为 Xml 中的根节点是这个名称。
    // 如果想更改类名，可以把 XML 文件中的根节点名称改掉。
    public class RouteCreator
    {
        [XmlElement("Route", typeof(ATSRoute))]
        public List<ATSRoute> Routes { get; set; }
        
        static internal RouteCreator Open()
        {
            using (StreamReader sr = new StreamReader("RouteList.xml"))
            {
                return new XmlSerializer(typeof(RouteCreator)).Deserialize(sr) as RouteCreator;
            }
        }


        public void LoadDevices(List<GraphicElement> elements)
        {
            foreach (var item in Routes)
            {
                item.LoadDevices(elements);
            }
        }
    }
}
