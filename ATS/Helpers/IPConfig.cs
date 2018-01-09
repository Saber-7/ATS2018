using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Xml.Serialization;

namespace ATS.Helpers
{
    [Serializable]
    public class IPConfig
    {
        public IPConfig()
        {
            CBIEndPoints = new List<IPEndPoint>();
         
           //SocketAddress sc=new SocketAddress()
            CBIs = new List<IPPortPair>();
        }

        [System.Xml.Serialization.XmlIgnore]
        public IPEndPoint Local4ZCPoint { get; set; }
        public IPPortPair Local4ZC{get;set;}

        [System.Xml.Serialization.XmlIgnore]
        public IPEndPoint Local4CBIPoint { get; set; }
        public IPPortPair Local4CBI { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public IPEndPoint ZCEndPoint{get;set;}
        public IPPortPair ZC{get;set;}
        [System.Xml.Serialization.XmlIgnore]
        public List<IPEndPoint> CBIEndPoints { get; set; }
        public List<IPPortPair> CBIs{get;set;}

        public void InitEndpoint()
        {
            Local4ZCPoint = Local4ZC.GetEndPoint();
            Local4CBIPoint = Local4CBI.GetEndPoint();
            ZCEndPoint = ZC.GetEndPoint();
            for (int i = 0; i < CBIs.Count; i++)
            {
                CBIEndPoints.Add(CBIs[i].GetEndPoint());
            }
        }
        

    }

    [Serializable]
    public class IPPortPair
    {
        public string IP{get;set;}
        public int Port{set;get;}
        public IPPortPair(string Ip, int port)
        {
            IP = Ip;
            Port = port;
        }
        public IPPortPair() { }
        public  IPEndPoint GetEndPoint()
        {
            return new IPEndPoint(IPAddress.Parse(IP), Port);
        }
    }
}
