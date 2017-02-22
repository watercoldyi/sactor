using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
namespace SActor
{
    internal class MessageBox
    {
        ConcurrentQueue<SActMessage> _msgs = new ConcurrentQueue<SActMessage>();
        public bool InGlobal{ get; set; }

        public SActActor Act{ get; set; }

        public int MessageCount()
        {
            return _msgs.Count;
        }

        internal void PushMessage(SActMessage msg)
        {
            _msgs.Enqueue(msg);
            if (!InGlobal)
            {
                SActor._dispatcher.Push(Act);
            }
        }

        internal SActMessage PopMessage()
        {
            SActMessage msg;
            return _msgs.TryDequeue(out msg)? msg:null;
        }

    }
}
