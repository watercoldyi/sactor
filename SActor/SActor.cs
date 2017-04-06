using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
namespace SActor
{
    public class SActor
    {
        static uint _threads = 4;
        internal static SActDispatcher _dispatcher;
        internal static SActActor _logger;
        static ConcurrentDictionary<string, SActActor> _nameMap = new ConcurrentDictionary<string, SActActor>();

        public static uint Threads
        {
            get { return _threads; }
            set
            {
                if (value == 0)
                {
                    _threads = 4;
                }
                else
                {
                    _threads = value;
                }
            }
        }

        public static string Logger{ get; set; }

        public static void Init()
        {
            _dispatcher = new SActDispatcher(_threads);
            _logger = Launch<SActLogger>();
            SActSAEAPool.Init(12);
        }

        public static void Send(SActActor source, SActActor target, int port, uint session, object data)
        {
            if (target.HasExit())
            {
                throw new SActException(target.GetType().Name + "  has exit");
            }
            SActMessage m = new SActMessage();
            m.Source = source;
            m.Port = port;
            m.Session = session;
            m.Data = data;
            target.MsgBox.PushMessage(m);
        }

        public static SActActor Launch<T>(object param = null)
            where T:SActActor,new()
        {
            SActActor obj = new T();
            _dispatcher.Push(obj);
            Send(null, obj, (int)SActMessageType.Init, 0, param);
            return obj;
        }

        public static void Kill(SActActor act)
        {
            if (!act.HasExit())
            {
                Send(null, act, (int)SActMessageType.Exit, 0, null);
            }
        }

        public static void Log(string s)
        {
            Send(null, _logger, (int)SActMessageType.Message, 0, s);
        }

        public static void Log(Exception e)
        {
            Log(string.Format("error {0} {1}", e.Message, e.StackTrace));
        }

        public static void Name(string name, SActActor act)
        {
            _nameMap.TryAdd(name, act);            
        }

        public static SActActor Query(string name)
        {
            SActActor act;
            if (_nameMap.TryGetValue(name, out act))
            {
                return act;
            }
            return null;
        }
    }
}
