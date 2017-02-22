using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SActor
{

    public enum SActMessageType
    {
        Message = 1,
        Request,
        Response,
        Timer,
        Socket,
        Exit,
        Init,
        Error
    }

    public class SActMessage
    {
        public SActActor Source{ get; set; }
        public int Port{ get; set; }
        public object Data{ get; set; }
        public uint Session{ get; set; }
    }
}
