using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 线路绘图工具;
using ZCATSMes;

namespace ATS
{
    public class TrainMes
    {
        public TrainMes(string CBIPos,string RunMode,string Speed,string Destnation,string IsPlanTrain,string TrainPos) {
            this.CBIPos = String.Copy(CBIPos);
            this.RunMode = String.Copy(RunMode);
            this.Speed = String.Copy(Speed);
            this.Destnation = String.Copy(Destnation);
            this.IsPlanTrain = String.Copy(IsPlanTrain);
            this.TrainPos = String.Copy(TrainPos);
        }

        public TrainMes() { ;}

        public string CBIPos{get;set;}
        public string RunMode { get; set; }
        public string Speed { get; set; }
        public string Destnation { get; set; }
        public string IsPlanTrain { get; set; }
        public string TrainPos { get; set; }

        // override object.Equals
        public override bool Equals(object obj)
        {
            //       
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237  
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            TrainMes mes = obj as TrainMes;
            if (mes != null)
            {
                return mes.CBIPos == CBIPos
                    && mes.RunMode == RunMode
                    && mes.Speed == Speed
                    && mes.Destnation == Destnation
                    && mes.IsPlanTrain == IsPlanTrain
                    && mes.TrainPos == TrainPos;
            }
            return base.Equals(obj);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return CBIPos==null?0:CBIPos.GetHashCode();
        }

        TrainMes TrainMesFactory(GraphicElement ge, ZC2ATS info)
        {
            TrainMes mes = new TrainMes();
            if (ge is Section)
            {
                Section sc = ge as Section;
                mes.CBIPos = sc == null ? null : sc.StationID.ToString();

            }
            else
            {
                RailSwitch rs = ge as RailSwitch;
                mes.CBIPos = rs == null ? null : rs.StationID.ToString();
            }
            mes.RunMode = "AM";
            mes.Speed = "80";
            mes.Destnation = "无";
            mes.IsPlanTrain = "否";
            mes.TrainPos = ge == null ? "无" : ge.Name + " " + info.Offset.ToString();
            return mes;
        }
    }
}
