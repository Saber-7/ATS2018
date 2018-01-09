
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATS
{
    [Serializable] 
    public class Station
    {
        string _name;//车站名字

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        
        string _trainState;//列车状态

        public string TrainState
        {
            get { return _trainState; }
            set { _trainState = value; }
        }
        DateTime _setTime;//当前时刻

        public DateTime SetTime
        {
            get { return _setTime; }
            set { _setTime = value; }
        }
        double _distance;

        public double Distance
        {
            get { return _distance; }
            set { _distance = value; }
        }

        int _num;

        public int Num
        {
            get { return _num; }
            set { _num = value; }
        }

        bool _ableToTurnBack;

        public bool AbleToTurnBack
        {
            get { return _ableToTurnBack; }
            set { _ableToTurnBack = value; }
        }
        #region 顺、逆时针进路
        string _clockAccess;

        public string ClockAccess
        {
            get { return _clockAccess; }
            set { _clockAccess = value; }
        }

        string _antiClockAccess;

        public string AntiClockAccess
        {
            get { return _antiClockAccess; }
            set { _antiClockAccess = value; }
        }
        #endregion

        public Station(string name,DateTime setT,double distance,int num,bool turnBack)
        {
            this.Name = name;
           
            this.SetTime = setT;
            this.Distance = distance;
            this.Num = num;
            this.AbleToTurnBack = turnBack;
        }

        public Station()
        { 
        
        }

    }
}
