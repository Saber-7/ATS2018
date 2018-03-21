using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
namespace ATS
{
    /// <summary>
    /// 特定长度且没有重复值的的ConcurrentQueue
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FixedConQueue<T>:ConcurrentQueue<T>
    {
        private readonly object syncObject = new object();
        public int Size { get; private set; }
        public bool IsChanged { get; private set; }
        public FixedConQueue(int size)
        {
            Size = size;
        }
        public new void Enqueue(T obj)
        {
            IsChanged = false;
            if (base.Count == 0 || !this.Last().Equals(obj))
            {
                IsChanged = true;
                base.Enqueue(obj);
            }
            lock (syncObject)
            {
                while (base.Count > Size)
                {
                    T outobj;
                    base.TryDequeue(out outobj);
                }
            }
        }
    }
}
