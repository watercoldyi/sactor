using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
namespace SActor
{
    class SActDispatcher
    {
        ConcurrentQueue<SActActor> _boxs = new ConcurrentQueue<SActActor>();

        public SActDispatcher(uint thread)
        {
            for (uint i = 0; i < thread; i++)
            {
                Thread t = new Thread(new ThreadStart(Worker));
                t.IsBackground = true;
                t.Start();
            }
        }

        void Worker()
        {
            while (true)
            {
                SActActor act;
                if (_boxs.TryDequeue(out act))
                {
                    SActMessage msg = act.MsgBox.PopMessage();
                    if (msg != null)
                    {
                        act.DispatchMessage(msg);
                        if (act.HasExit())
                        {
                            act.CleanMessage();
                        }
                        else
                        {
                            Push(act);
                        }
                      
                    }
                    else
                    {
                        act.MsgBox.InGlobal = false;
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }

            }
        }

        public void Push(SActActor act)
        {
            act.MsgBox.InGlobal = true;
            _boxs.Enqueue(act);
        }
    }
}
