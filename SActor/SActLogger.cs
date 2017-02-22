using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SActor
{
    class SActLogger:SActActor
    {
        protected override void Init(object param)
        {
            SetMessageHandler((int)SActMessageType.Message, MessageHandler);
        }


        void MessageHandler(SActMessage m)
        {
            string log = string.Format("{0} [{1}]:{2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ms"),m.Source!= null?m.Source.GetType().Name:"",m.Data);
            if (SActor.Logger == null)
            {
                Console.WriteLine(log);
            }
        }
    }
}
