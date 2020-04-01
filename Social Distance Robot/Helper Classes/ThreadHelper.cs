using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace robot_head
{
    class ThreadHelper
    {
        public static void Wait(int miliSec)
        {
            Thread.Sleep(miliSec);
        }

        public static void StartNewThread(Action action)
        {
            Thread thread = new Thread(new ThreadStart(action));

            thread.Start();
        }
    }
}
