using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Reflection;
using System.Diagnostics;
namespace SActor
{


    public abstract class SActActor
    {
        public delegate void SActMessageHandler(SActMessage msg);
        public delegate void Response(bool ok, object d);

        bool _exit;
        Dictionary<int, SActMessageHandler> _handlers = new Dictionary<int, SActMessageHandler>();
        uint _session;
        Dictionary<uint, Action> _timer = new Dictionary<uint, Action>();
        Dictionary<uint, Task> _call = new Dictionary<uint, Task>();
        Queue<Action> _task = new Queue<Action>();
        object _callResult;
        bool _callOK;

        public SActActor()
        {
            _handlers[(int)SActMessageType.Exit] = ExitHandler;
            _handlers[(int)SActMessageType.Message] = MessageHandler;
            _handlers[(int)SActMessageType.Request] = CommandHandler;
            _handlers[(int)SActMessageType.Response] = ResponseHandler;
            _handlers[(int)SActMessageType.Error] = ErrorHandler;
            _handlers[(int)SActMessageType.Timer] = TimerHandler;
            _handlers[(int)SActMessageType.Init] = InitHandler;
            MsgBox = new MessageBox();
            MsgBox.Act = this;

        }

        internal MessageBox MsgBox { get; private set; }

        protected abstract void Init(object param);

        uint Session()
        {
            return ++_session;
        }

        internal void DispatchMessage(SActMessage msg)
        {
            if (_handlers.ContainsKey(msg.Port))
            {
                try
                {
                    _handlers[msg.Port](msg);
                    DispatchTask();
                }
                catch (Exception e)
                {
                    Log(e.Message + e.StackTrace);
                }
            }
        }

        void DispatchTask()
        {
            while (_task.Count > 0)
            {
                Action t = _task.Dequeue();
                t();
            }
        }

        internal void CleanMessage()
        {
            SActMessage msg = MsgBox.PopMessage();
            while (msg != null)
            {
                if (msg.Session > 0)
                {
                    SActor.Send(this, msg.Source, (int)SActMessageType.Error, msg.Session, null);
                }
                msg = MsgBox.PopMessage();
            }
        }

        protected void Fork(Action task)
        {
            _task.Enqueue(task);
        }

        protected Task Sleep(uint ms)
        {
            return TimeOut(ms, () => { });
        }

        public void Log(string s)
        {
            SActor.Send(this, SActor._logger, (int)SActMessageType.Message, 0, s);
        }

        public void Log(Exception e)
        {
            Log(string.Format("{0}{1}", e.Message, e.StackTrace));
        }


        protected Task TimeOut(uint ms, Action cb)
        {
            uint session = Session();
            Task t = new Task(cb);
            _call[session] = t;
            SActTimer.TimeOut(ms, session, this);
            return t;
        }

        protected void Exit()
        {
            _exit = true;
            Log("kill");
        }

        protected void Send(SActActor target, params object[] p)
        {
            SActor.Send(this, target, (int)SActMessageType.Message, 0, p);
        }

        protected Task<T> Call<T>(SActActor target,params object[] p)
        {
            uint session = Session();
            Task<T> t = new Task<T>(() =>
            {
                if (_callOK)
                {
                    return (T)_callResult;
                }
                throw new SActException("call fail");
            });
            _call.Add(session, t);
            SActor.Send(this, target, (int)SActMessageType.Request, session, p);
            return t;
        }

        protected Task<T> Call2<T>(SActActor target,object[] p)
        {
            uint session = Session();
            Task<T> t = new Task<T>(() =>
            {
                if (_callOK)
                {
                    return (T)_callResult;
                }
                throw new SActException("call fail");
            });
            _call.Add(session, t);
            SActor.Send(this, target, (int)SActMessageType.Request, session, p);
            return t;
        }

        protected int GetMessageCount()
        {
            return MsgBox.MessageCount();
        }

        public void SetMessageHandler(int port, SActMessageHandler handler)
        {
            if (_handlers.ContainsKey(port))
            {
                _handlers[port] = handler;
            }
            else
            {
                _handlers.Add(port, handler);
            }
        }

        private void CommandHandler(SActMessage msg)
        {
            object[] ps = (object[])msg.Data;
            MethodInfo func = GetType().GetMethod((string)ps[0]);
            if (func != null)
            {
                uint session = msg.Session;
                SActActor target = msg.Source;
                Response reply = (ok, a) =>
                {
                    if (ok)
                    {
                        SActor.Send(this, target, (int)SActMessageType.Response, session, a);
                    }
                    else
                    {
                        SActor.Send(this, target, (int)SActMessageType.Error, session, null);
                    }

                };
                object[] p = new object[ps.Length];
                p[0] = reply;
                Array.Copy(ps, 1, p, 1, ps.Length - 1);
                try
                {
                    func.Invoke(this, p);
                }
                catch (Exception)
                {
                    SActor.Send(this, target, (int)SActMessageType.Error, session, null);
                    throw;
                }
            }
            else
            {
                Log(string.Format("invalid command {0}", ps[0]));
            }
        }

        private void MessageHandler(SActMessage msg)
        {
            object[] ps = (object[])msg.Data;
            MethodInfo func = GetType().GetMethod((string)ps[0]);
            object[] p = new object[ps.Length - 1];
            Array.Copy(ps, 1, p, 0, p.Length);
            if (func != null)
            {
                func.Invoke(this, p);
            }
            else
            {
                Log(string.Format("invalid message {1}", ps[0]));
            }
        }

        private void ResponseHandler(SActMessage m)
        {
            if (_call.ContainsKey(m.Session))
            {
                var t = _call[m.Session];
                _call.Remove(m.Session);
                _callOK = true;
                _callResult = m.Data;
                t.RunSynchronously();
            }
            else
            {
                Log(string.Format("invalid session {0},{1} to {2}", m.Session, m.Source.GetType().Name, this.GetType().Name));
            }
        }

        private void ExitHandler(SActMessage m)
        {
            Exit();
        }

        private void ErrorHandler(SActMessage m)
        {
            if (_call.ContainsKey(m.Session))
            {
                var t = _call[m.Session];
                _call.Remove(m.Session);
                _callOK = false;
                _callResult = null;
                t.RunSynchronously();
            }
            else
            {
                Log(string.Format("invalid session {0},{1} to {2}", m.Session, m.Source.GetType().Name, this.GetType().Name));
            }
        }

        private void TimerHandler(SActMessage m)
        {
            Task t = _call[m.Session];
            _call.Remove(m.Session);
            t.RunSynchronously();
        }

        private void InitHandler(SActMessage m)
        {
            Init(m.Data);
            Log("launch");
        }
        public bool HasExit()
        {
            return _exit;
        }

    }
}
