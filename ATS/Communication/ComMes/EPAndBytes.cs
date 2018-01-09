using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace ATS
{
    [Serializable]
    class EPAndBytes
    {
        public EPAndBytes(EndPoint ep,byte[] bytes)
        {
            EP = ep;
            Mes = bytes;
        }
        public EndPoint EP { get; set; }
        public byte[] Mes { get; set; }
    }
}
