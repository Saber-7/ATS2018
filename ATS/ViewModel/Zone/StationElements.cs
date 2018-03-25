using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;
using 线路绘图工具;
using System;
using System.Linq;

namespace ATS
{
    public class StationElements
    {
        [XmlElement("RelayButton",typeof(RelayButton))]
        [XmlElement("PSDoor",typeof(PSDoor))]
        [XmlElement("Section", typeof(Section))]
        [XmlElement("RailSwitch", typeof(RailSwitch))]
        [XmlElement("Signal", typeof(Signal))]
        [XmlElement("CommandButton", typeof(线路绘图工具.CommandButton))]
        [XmlElement("SmallButton", typeof(线路绘图工具.SmallButton))]
        [XmlElement("GraphicElement", typeof(线路绘图工具.GraphicElement))]



        public List<线路绘图工具.GraphicElement> Elements { get; set; }



        public static StationElements Open(string path)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                return new XmlSerializer(typeof(StationElements)).Deserialize(sr) as StationElements;
            }
        }

        public void AddElementsToCanvas(Canvas canvas)
        {
            InitLen();
            foreach (线路绘图工具.GraphicElement element in Elements)
            {
                canvas.Children.Add(element);
                if (element is IRailway)
                {
                    IRailway ir = element as IRailway;
                    ir.AddInsulation();
                }
                //else if(element is PSDoor)
                //{
                //    ((PSDoor)element).CreateDefaultGraphic();
                //}

            }
        }


        #region 计算图上元素长度

        void InitLen()
        {
            foreach (线路绘图工具.GraphicElement g in Elements)
            {
                CalLen(g);
            }
        }
        //完全特殊处理的
        void CalLen(线路绘图工具.GraphicElement Element)
        {
            if (Element is Section)
            {
                Section sc = Element as Section;
                double fk = 0, nk = 0;//之前的斜率和当前斜率
                foreach (Graphic g in sc.Graphics)
                {
                    Line l = g as Line;
                    nk = (l.Pt1.Y - l.Pt0.Y) / (l.Pt1.X - l.Pt0.X);
                    if (fk == nk)
                    {
                        sc.Lens[0] += CalDisOf2Point(l.Pt1, l.Pt0);
                    }
                    else
                    {
                        sc.Lens[1] += CalDisOf2Point(l.Pt1, l.Pt0);
                    }
                    fk = nk;
                }
            }
            else if (Element is RailSwitch)
            {
                // 直、定、反
                RailSwitch rs = Element as RailSwitch;
                foreach (int i in rs.SectionIndexList[0])
                {
                    Line l = rs.Graphics[i] as Line;
                    rs.Lens[0] += CalDisOf2Point(l.Pt0, l.Pt1);
                }
                foreach (int i in rs.SectionIndexList[1])
                {
                    Line l = rs.Graphics[i] as Line;
                    rs.Lens[1] += CalDisOf2Point(l.Pt0, l.Pt1);
                }
                foreach (int i in rs.SectionIndexList[2])
                {
                    Line l = rs.Graphics[i] as Line;
                    rs.Lens[2] += CalDisOf2Point(l.Pt0, l.Pt1);
                }
                Line l1 = rs.Graphics[rs.SectionIndexList[0].Last()] as Line;
                Line l2 = rs.Graphics[rs.SectionIndexList[1].First()] as Line;
                Line l3 = rs.Graphics[rs.SectionIndexList[2].First()] as Line;
                rs.Lens[1] += CalDisOf2Point(l1.Pt1, l2.Pt0);
                rs.Lens[2] += CalDisOf2Point(l1.Pt1, l3.Pt0);
            }
        }

        //计算两点距离
        double CalDisOf2Point(Point p1, Point p2)
        {
            double x2 = Math.Pow(p1.X - p2.X, 2);
            double y2 = Math.Pow(p1.Y - p2.Y, 2);
            return Math.Pow((y2 + x2), 0.5);
        }
        #endregion
        //internal void CheckSectionSwitches()
        //{
        //    for (int i = 0; i < Elements.Count; i++)
        //    {
        //        if (Elements[i] is RailSwitch)
        //        {
        //            RailSwitch rs_1 = Elements[i] as RailSwitch;
        //            for (int j = i + 1; j < Elements.Count; j++)
        //            {
        //                if (Elements[j] is RailSwitch)
        //                {
        //                    RailSwitch rs_2 = Elements[j] as RailSwitch;
        //                    if (rs_1.StationID == rs_2.StationID && rs_1.SectionID == rs_2.SectionID)
        //                    {
        //                        CheckSectionSwitches(rs_1, rs_2);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //private void CheckSectionSwitches(RailSwitch rs_1, RailSwitch rs_2)
        //{
        //    if (rs_1.IsLeft != rs_2.IsLeft)
        //    {
        //        if (rs_1.IsLeft)
        //        {
        //            rs_1.InsuLine = null;
        //            rs_1.InvalidateVisual();
        //        }

        //        if (rs_2.IsLeft)
        //        {
        //            rs_2.InsuLine = null;
        //            rs_2.InvalidateVisual();
        //        }
        //    }
        //}

        internal void CheckSectionSwitches()
        {
            for (int i = 0; i < Elements.Count; i++)
            {
                if (Elements[i] is RailSwitch)
                {
                    RailSwitch rs_1 = Elements[i] as RailSwitch;
                    for (int j = i + 1; j < Elements.Count; j++)
                    {
                        if (Elements[j] is RailSwitch)
                        {
                            RailSwitch rs_2 = Elements[j] as RailSwitch;
                            if (rs_1.StationID == rs_2.StationID && rs_1.SectionID == rs_2.SectionID)
                            {
                                CheckSectionSwitches(rs_1, rs_2);
                            }
                        }
                    }
                }
            }
        }

        private void CheckSectionSwitches(RailSwitch rs_1, RailSwitch rs_2)
        {
            if (rs_1.IsLeft != rs_2.IsLeft && rs_1.IsUp == rs_2.IsUp)
            {
                if (rs_1.NNSwitch == null && rs_2.NNSwitch == null)
                {
                    rs_1.NNSwitch = rs_2;
                    rs_2.NNSwitch = rs_1;

                    // 处理绝缘节
                    if (rs_1.IsLeft)
                    {
                        rs_1.InsuLine = null;
                    }

                    if (rs_2.IsLeft)
                    {
                        rs_2.InsuLine = null;
                    }
                }
            }
            else
            {
                List<Point> nsPoints = new List<Point>();
                List<Point> sPoints = new List<Point>();
                if (rs_1.IsLeft)
                {
                    rs_1.GetLeftPoints(nsPoints);
                    rs_2.GetRightPoints(sPoints);

                    if (Line.IsPointsMatch(nsPoints[1], sPoints[0], 0.1))
                    {
                        rs_2.RSSwitch = rs_1;
                    }
                }
            }
        }

    }
    //public class StationElements
    //{
    //    [XmlElement("Section", typeof(Section))]
    //    [XmlElement("RailSwitch", typeof(RailSwitch))]
    //    [XmlElement("Signal", typeof(Signal))]
    //    [XmlElement("CommandButton", typeof(线路绘图工具.CommandButton))]
    //    [XmlElement("SmallButton", typeof(线路绘图工具.SmallButton))]
    //    [XmlElement("GraphicElement", typeof(线路绘图工具.GraphicElement))]
    //    public List<线路绘图工具.GraphicElement> Elements { get; set; }

    //    public static StationElements Open(string path)
    //    {
    //        using (StreamReader sr = new StreamReader(path))
    //        {
    //            return new XmlSerializer(typeof(StationElements)).Deserialize(sr) as StationElements;
    //        }
    //    }

    //    public void AddElementsToCanvas(Canvas canvas)
    //    {
    //        foreach (线路绘图工具.GraphicElement element in Elements)
    //        {
    //            canvas.Children.Add(element);
    //            if (element is IRailway)
    //            {
    //                IRailway ir = element as IRailway;
    //                ir.AddInsulation();
    //            }
    //            //else if(element is PSDoor)
    //            //{
    //            //    ((PSDoor)element).CreateDefaultGraphic();
    //            //}
    //        }
    //    }

        //internal void CheckSectionSwitches()
        //{
        //    for (int i = 0; i < Elements.Count; i++)
        //    {
        //        if (Elements[i] is RailSwitch)
        //        {
        //            RailSwitch rs_1 = Elements[i] as RailSwitch;
        //            for (int j = i + 1; j < Elements.Count; j++)
        //            {
        //                if (Elements[j] is RailSwitch)
        //                {
        //                    RailSwitch rs_2 = Elements[j] as RailSwitch;
        //                    if (rs_1.StationID == rs_2.StationID && rs_1.SectionID == rs_2.SectionID)
        //                    {
        //                        CheckSectionSwitches(rs_1, rs_2);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //private void CheckSectionSwitches(RailSwitch rs_1, RailSwitch rs_2)
        //{
        //    if (rs_1.IsLeft != rs_2.IsLeft)
        //    {
        //        if (rs_1.IsLeft)
        //        {
        //            rs_1.InsuLine = null;
        //            rs_1.InvalidateVisual();
        //        }

        //        if (rs_2.IsLeft)
        //        {
        //            rs_2.InsuLine = null;
        //            rs_2.InvalidateVisual();
        //        }
        //    }
        //}

    //    internal void CheckSectionSwitches()
    //    {
    //        for (int i = 0; i < Elements.Count; i++)
    //        {
    //            if (Elements[i] is RailSwitch)
    //            {
    //                RailSwitch rs_1 = Elements[i] as RailSwitch;
    //                for (int j = i + 1; j < Elements.Count; j++)
    //                {
    //                    if (Elements[j] is RailSwitch)
    //                    {
    //                        RailSwitch rs_2 = Elements[j] as RailSwitch;
    //                        if (rs_1.StationID == rs_2.StationID && rs_1.SectionID == rs_2.SectionID)
    //                        {
    //                            CheckSectionSwitches(rs_1, rs_2);
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    private void CheckSectionSwitches(RailSwitch rs_1, RailSwitch rs_2)
    //    {
    //        if (rs_1.IsLeft != rs_2.IsLeft && rs_1.IsUp == rs_2.IsUp)
    //        {
    //            if (rs_1.NNSwitch == null && rs_2.NNSwitch == null)
    //            {
    //                rs_1.NNSwitch = rs_2;
    //                rs_2.NNSwitch = rs_1;

    //                // 处理绝缘节
    //                if (rs_1.IsLeft)
    //                {
    //                    rs_1.InsuLine = null;
    //                }

    //                if (rs_2.IsLeft)
    //                {
    //                    rs_2.InsuLine = null;
    //                }
    //            }
    //        }
    //        else
    //        {
    //            List<Point> nsPoints = new List<Point>();
    //            List<Point> sPoints = new List<Point>();
    //            if (rs_1.IsLeft)
    //            {
    //                rs_1.GetLeftPoints(nsPoints);
    //                rs_2.GetRightPoints(sPoints);

    //                if (Line.IsPointsMatch(nsPoints[1], sPoints[0], 0.1))
    //                {
    //                    rs_2.RSSwitch = rs_1;
    //                }
    //            }
    //        }
    //    }

    //}
}
