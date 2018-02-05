using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using 线路绘图工具;
using System.Xml.Linq;
using System;
using System.Linq;
namespace ATS
{
    // 必须是 public class，名称必须是 RouteCreator
    // 因为 Xml 中的根节点是这个名称。
    // 如果想更改类名，可以把 XML 文件中的根节点名称改掉。
    
    /// <summary>
    /// ATSRoute工厂类
    /// </summary>
    public class RouteCreator
    {

        static String path = @"ConfigFiles\RouteList.xml";
        string DirAttri = "Direction";
        //static RouteCreator _Instance = new RouteCreator();
        //static RouteCreator Instance { get { return _Instance; } }

        //private RouteCreator()
        //{
        //    _Instance=new XmlTool<RouteCreator>().DeSerialize(path);
        //}



        [XmlElement("Route", typeof(ATSRoute))]
        public List<ATSRoute> Routes { get; set; }
        

        static internal RouteCreator Open()
        {
            using (StreamReader sr = new StreamReader(path))
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
            InitPos();
            InitDistance();
            BuildRoutesGraph();
        }

        //权宜之计
        /// <summary>
        /// 关联方向
        /// </summary>
        void InitPos()
        {
            List<RouteDirection> rd = new List<RouteDirection>();
            XDocument xdoc = XDocument.Load(path);
            foreach (XElement item in xdoc.Elements())
            {
                foreach (XElement it in item.Elements())
                {
                    string s = it.Attribute(DirAttri).Value;
                    rd.Add((RouteDirection)Enum.Parse(typeof(RouteDirection), s));
                }
            }
            for (int i = 0; i < rd.Count; i++)
            {
                Routes[i].Dir = rd[i];
            }

        }


        /// <summary>
        /// 计算距离
        /// </summary>
        void InitDistance()
        {
            foreach (ATSRoute route in Routes)
            {
                int sk = 0;
                foreach (Device d in route.InSections)
                {
                    if (d is Section)
                    {
                        Section s = d as Section;
                        route.Distance += s.Distance;
                    }
                    else if(d is RailSwitch)
                    {
                        RailSwitch rs = d as RailSwitch;
                        route.Distance += route.SwitchPositions[sk++] == RailSwitch.SwitchPosition.PosNormal ? rs.NormalDistance : rs.ReverseDistance;
                    }
                }
            }
        }

        /// <summary>
        /// 建立用于寻路的进路图
        /// </summary>
        void BuildRoutesGraph()
        {
            for (int i = 0; i < Routes.Count; i++)
            {
                //Device IncomingDevice=null;
                if (Routes[i].InSections != null && Routes[i].InSections.Count != 0)
                {
                    foreach (var IncomingDevice in Routes[i].InSections)
                    {
                        List<ATSRoute> tempRoutes = null;
                        tempRoutes = Routes.FindAll((ATSRoute route) =>
                        {
                            foreach (Device d in route.InCommingSections)
                            {
                                if (d.Name == IncomingDevice.Name) return true;
                            }
                            return false;
                        });
                        if (tempRoutes != null) Routes[i].OptionalRoutes.AddRange(tempRoutes);
                    }
                    Routes[i].OptionalRoutes.Sort();
                }
            }
        }
    }
}
