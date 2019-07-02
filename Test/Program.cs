using System;
using System.Threading.Tasks;
namespace Test
{

    class Program
    {
        static void Main(string[] args)
        {
            SActor.SActor.Init();
            SActor.SActor.Launch<InvokeTest>();
            SActor.SActor.Launch<TimerTest>();
            SActor.SActor.Launch<SleepTest>();
            Console.ReadLine();
        }

       
    }
}
