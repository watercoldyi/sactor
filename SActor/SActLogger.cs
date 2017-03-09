using System;
using System.Collections.Generic;
using System.IO;
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
            if (SActor.Logger != null)
            {
                TimeOut(60000, OnTime);
            }
        }

        void OnTime()
        {
            FileInfo f = new FileInfo(SActor.Logger);
            if (f.Exists)
            {
                if (f.Length >= 50 * 1024 * 1024)
                {
                    f.Delete();
                }
            }
            TimeOut(60000, OnTime);
        }


        void MessageHandler(SActMessage m)
        {
            string log = string.Format("{0} [{1}]:{2}{3}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ms"),m.Source!= null?m.Source.GetType().Name:"",m.Data,Environment.NewLine);
            if (SActor.Logger == null)
            {
                Console.WriteLine(log);
            }
            else
            {
                File.AppendAllText(SActor.Logger, log);
            }
        }
    }
}
