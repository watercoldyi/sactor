using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SActor
{

    public enum SActSocketMessageType
    {
        Accept,
        Open,
        Data,
        Error,
        Close
    }
    public class SActSocketMessage
    {
        public SActSocket Socket{ get; set; }
        public SActSocketMessageType Type{ get; set; }

        public SActSocket AcceptSocket{ get; set; }
        public byte[] Data{ get; set; }
        public string Error{ get; set; }

        public int Size { get; set; }
    }
}
