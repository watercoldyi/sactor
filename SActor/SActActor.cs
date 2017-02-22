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
        protected delegate void SActMessageHandler(SActMessage msg);

        bool _exit;
        Dictionary<int, SActMessageHandler> _handlers = new Dictionary<int, SActMessageHandler>();
        uint _session;
        Dictionary<uint, Action<bool,object>> _waits = new Dictionary<uint, Action<bool,object>>();
        Dictionary<uint, Action> _timer = new Dictionary<uint, Action>();


        public SActActor()
        {
            _handlers[(int)SActMessageType.Exit] = ExitHandler;
            _handlers[(int)SActMessageType.Message] = MessageHandler;
            _handlers[(int)SActMessageType.Request] = CommandHandler;
            _handlers[(int)SActMessageType.Response] = ResponseHandler;
            _handlers[(int)SActMessageType.Error] = ErrorHandler;
            _handlers[(int)SActMessageType.Timer] = TimerHandler;
            _handlers[(int)SActMessageType.Init] = InitHandler;
            _handlers[(int)SActMessageType.Socket] = SocketHandler;
            MsgBox = new MessageBox();
            MsgBox.Act = this;

        }

        internal MessageBox MsgBox{ get; private set; }

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
                }
                catch (Exception e)
                {
                    Log(e.Message + e.StackTrace);
                }
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

        protected void Log(string s)
        {
            SActor.Send(this,SActor._logger,(int)SActMessageType.Message,0,s);
        }


        protected void TimeOut(int ms, Action cb)
        {
            uint session = Session();
            _timer[session] = cb;
            SActTimer.TimeOut(ms, session, this);

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

        protected void Call(SActActor target,Action<bool,object> cb,params object[] p)
        {
            Debug.Assert(cb != null);
            uint session = Session();
            _waits.Add(session, cb);
            SActor.Send(this, target, (int)SActMessageType.Request, session, p);
        }

        protected int GetMessageCount()
        {
            return MsgBox.MessageCount();
        }

        protected void SetMessageHandler(int port, SActMessageHandler handler)
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

        protected virtual void ProcessSocketMessage(SActSocketMessage msg) { }

        private void CommandHandler(SActMessage msg)
        {
            object[] ps  = (object[])msg.Data;
            MethodInfo func = GetType().GetMethod((string)ps[0]);
            if (func != null)
            {
                uint session = msg.Session;
                SActActor target = msg.Source;
                Action<object> reply = a =>
                {
                    SActor.Send(this, target, (int)SActMessageType.Response, session, a);

                };
                object[] p = new object[ps.Length];
                p[0] = reply;
                Array.Copy(ps, 1, p, 1, ps.Length - 1);
                try
                {
                    func.Invoke(this, p);
                }
                catch ( Exception)
                {
                    SActor.Send(this, target, (int)SActMessageType.Error, session, null);
                    throw;
                }
            }
            else
            {
                throw new SActException(string.Format("invalid command {0}",ps[0]));
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
               throw new SActException(string.Format("invalid message {1}",ps[0]));
            }
        }

        private void ResponseHandler(SActMessage m)
        {
            if (_waits.ContainsKey(m.Session))
            {
                Action<bool, object> func = _waits[m.Session];
                _waits.Remove(m.Session);
                func(true,m.Data);
            }
            else
            {
                throw new SActException(string.Format("invalid session {0},{1} to {2}",m.Session,m.Source.GetType().Name,this.GetType().Name));
            }
        }

        private void ExitHandler(SActMessage m)
        {
            Exit();
        }

        private void ErrorHandler(SActMessage m)
        {
            if (_waits.ContainsKey(m.Session))
            {
                Action<bool, object> func = _waits[m.Session];
                _waits.Remove(m.Session);
                func(false, null);
            }
            else
            {
               throw new SActException(string.Format("invalid session {0},{1} to {2}",m.Session,m.Source.GetType().Name,this.GetType().Name));
            }
        }

        private void TimerHandler(SActMessage m)
        {
            Action func = _timer[m.Session];
            _timer.Remove(m.Session);
            func();
        }

        private void InitHandler(SActMessage m)
        {
            Init(m.Data);
            Log("launch");
        }

        private void SocketHandler(SActMessage m)
        {
            ProcessSocketMessage((SActSocketMessage)m.Data);
        }

        public bool HasExit()
        {
            return _exit;
        }
    }
}
