﻿using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;
using 线路绘图工具;
namespace ATS
{
    public class StationElements
    {
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
}
