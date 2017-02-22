using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            SActor.SActor.Init();
            SActor.SActor.Launch<InvokeTest>();
            SActor.SActor.Launch<SocketTest>();
            Console.ReadLine();
        }
    }
}
