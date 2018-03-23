using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATS
{
    public class HandleMes
    {
        public HandleMes(string CBIName,string handleButton,string ErrorMes1,string ErrorMes2)
        {
            this.CBIName = CBIName;
            this.HandleButton = handleButton;
            this.ErrorMes1 = ErrorMes1;
            this.ErrorMes2 = ErrorMes2;
        }

        public HandleMes()
        { ;}
        public string CBIName { get; set; }
        public string HandleButton { get; set; }
        public string ErrorMes1 { get; set; }
        public string ErrorMes2 {get;set;}

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

            HandleMes mes = obj as HandleMes;
            if(mes!=null)
            {
                return mes.CBIName == CBIName
                    && mes.ErrorMes1 == ErrorMes1
                    && mes.ErrorMes2 == ErrorMes2
                    && mes.HandleButton == HandleButton;
            }

            return base.Equals(obj);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return 0;
        }
    }
}
