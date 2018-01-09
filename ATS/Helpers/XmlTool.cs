using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;


namespace ATS
{
    class XmlTool<T>
    {
        public T DeSerialize(string filePath)
        {
            using (StreamReader sr=new StreamReader(filePath))
            {
                return (T)(new XmlSerializer(typeof(T)).Deserialize(sr));
            }
        }

        public void  Serialize(T obj,string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                new XmlSerializer(typeof(T)).Serialize(fs,obj);
            }
        }
    }
}
