using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Collections.Concurrent;
namespace ATS
{
    class Communication
    {


        public Communication(IPEndPoint lEP)
        {
            socketMain = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socketMain.Bind(lEP);
            REPs = new List<EndPoint>();
        }

        public Communication(IPEndPoint lEP,BlockingCollection<byte[]> mesQueue)
        {
            socketMain = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                socketMain.Bind(lEP);
            }
            catch
            {
                //处理端口冲突
                //MessageBox.Show(LEP.ToString()+"端口或IP占用！");
            }
            MesQueue = mesQueue;
            REPs = new List<EndPoint>();


            uint IOC_IN = 0x80000000; uint IOC_VENDOR = 0x18000000;
            uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
            socketMain.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
        }


        public Socket socketMain {get;set;}

        BlockingCollection<byte[]> MesQueue;
        

        List<EndPoint> _rEPs;

        public List<EndPoint> REPs
        {
            get { return _rEPs; }
            set { _rEPs = value; }
        }

        

        EndPoint _LEP;

        public EndPoint LEP
        {
            get { return _LEP; }
            set { _LEP = value; }
        }

       
        byte[] receiveDataArray = new byte[1200];




        #region 监听端口
        public void ListenData()
        {
            receiveDataArray = new byte[1024*5];
            if (socketMain != null)
            {
                try
                {
                    EndPoint _REP = new IPEndPoint(IPAddress.Any, 0);
                    socketMain.BeginReceiveFrom(receiveDataArray, 0, receiveDataArray.Length, SocketFlags.None, ref _REP, new AsyncCallback(ReceiveData), null);
                    //socketMain.BeginReceive(receiveDataArray, 0, receiveDataArray.Length, SocketFlags.None, new AsyncCallback(ReceiveControlData), null);
                }
                catch
                {

                }
            }
        }

        #endregion
        #region 接收信息

        public void ReceiveData(IAsyncResult iar)
        {

            if (socketMain != null)
            {

                //int len=socketMain.EndReceive(iar);
                EndPoint _REP = new IPEndPoint(IPAddress.Any, 0);
                int len = socketMain.EndReceiveFrom(iar, ref _REP);
                MesQueue.Add(receiveDataArray);
                if (!REPs.Contains(_REP))
                {
                    REPs.Add(_REP);
                }

                //回调函数
                ListenData();
            }
        }
        #endregion




        #region 发送函数

        public void SendData(byte[] sendControlPacket,EndPoint Ep)
        {
            socketMain.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 0);
            try
            {
                socketMain.SendTo(sendControlPacket, 0,sendControlPacket.Length, SocketFlags.None, Ep);
            }
            catch
            {
            }

        }

        public void SendData(byte[] sendControlPacket, EndPoint Ep,int len)
        {
            socketMain.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 0);
            try
            {
                socketMain.SendTo(sendControlPacket, 0, len, SocketFlags.None, Ep);
            }
            catch
            {
            }

        }

        #endregion
    }

}
