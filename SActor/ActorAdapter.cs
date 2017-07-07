using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SActor
{
    /// <summary>
    /// actor适配器
    /// 给非actor对象提供调用actor的接口
    /// </summary>
    public sealed class ActorAdapter
    {
        /// <summary>
        /// 发送一个消息给actor
        /// </summary>
        /// <param name="act"></param>
        /// <param name="cmd"></param>
        /// <param name="p"></param>
        public static void Send(SActActor act, string cmd, params object[] p)
        {
            SActMessage msg = new SActMessage();
            object[] aguments = null;
            if (p == null)
            {
                aguments = new object[1];
            }
            else
            {
                aguments = new object[p.Length + 1];
            }
            aguments[0] = cmd;
            if (p != null) { p.CopyTo(aguments, 1); }
            SActor.Send(null, act, (int)SActMessageType.Message, 0, aguments);
        }

        public static void Send(string name, string cmd, params object[] p)
        {
            var act = SActor.Query(name);
            if (act == null)
            {
                throw new NullReferenceException(name + " not found");
            }
            Send(act, cmd, p);
        }
    }
}
