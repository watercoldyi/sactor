using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
namespace Test
{

    class Program
    {

        static void Main(string[] args)
        {
            SActor.SActor.Init();
            SActor.SActor.Launch<InvokeTest>();
            SActor.SActor.Launch<SocketTest>();
            SActor.SActor.Launch<TimerTest>();
            SActor.SActor.Launch<SleepTest>();
            Console.ReadLine();
        }

       
    }
}
