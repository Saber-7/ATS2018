using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace ATS
{
    /// <summary>
    /// 结构体《=》byte[]转化,用于解包或者打包
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class StructBytes<T>
    {
        public  byte[] Serialize(T anything)
        {
            int rawsize =Marshal.SizeOf(anything);
            IntPtr buffer =Marshal.AllocHGlobal(rawsize);
            Marshal.StructureToPtr(anything,buffer, false);
            byte[] rawdata = new byte[rawsize];
            Marshal.Copy(buffer, rawdata,0, rawsize);
            Marshal.FreeHGlobal(buffer);
            return rawdata;
        }

        public  T DeSerialize(byte[] Mes)
        {
            int size = Marshal.SizeOf(typeof(T));
            IntPtr structPtr = Marshal.AllocHGlobal(size);
            Marshal.Copy(Mes, 0, structPtr, size);
            T res=(T)Marshal.PtrToStructure(structPtr,typeof(T));
            Marshal.FreeHGlobal(structPtr);
            return res;
        }
    }
}
