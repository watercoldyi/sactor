using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SActor
{
    public class SActException:Exception
    {
        public SActException(string message):base(message)
        {
            
        }

        public SActException(string msg, Exception inner) : base(msg, inner) { }
    }
}
