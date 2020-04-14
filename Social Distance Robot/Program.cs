using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DarrenLee.Media;
using SpeechLibrary;

namespace robot_head
{
    static class Program
    {
        ///// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //SocialDistanceChecker.StartWarning();

            //Thread.Sleep(1000 * 10);
            //return;
            //Console.ReadKey();

            //var x = Synthesizer.GetInstalledVoicesName();

            //SocialDistanceChecker.CheckDistance("490,0.91%295,4.45"); return;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

       
    }
}
