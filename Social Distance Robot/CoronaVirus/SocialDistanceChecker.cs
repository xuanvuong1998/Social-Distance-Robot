using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using SpeechLibrary;
using System.Threading.Tasks;

namespace robot_head
{
    class SocialDistanceChecker
    {
        // Indicate robot detected someone are close each other or not
        public static bool IsDetected { get; set; }

        // Value of smallest distance from robot to one of detected group
        public static double MinDetectedDistance { get; set; } = 10000000.0;
        private const double CM_PER_PIXEL = 0.3421;

        public const double MAX_DISTANCE = 100; //1 meter

        private static Process pythonProcess;

        private static FrmWarning frmWarning = new FrmWarning();

        private static void KeepReadingData()
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = @"C:\RobotReID\person_re_id-master",
                FileName = @"C:\ProgramData\Anaconda3\python.exe",
                Arguments = @"C:\RobotReID\person_re_id-master\my_social_distance.py",
                RedirectStandardOutput = true,
                RedirectStandardError = true 
            };

            using (pythonProcess = Process.Start(processInfo))
            {
                pythonProcess.OutputDataReceived += Process_OutputDataReceived;
                pythonProcess.ErrorDataReceived += Process_ErrorDataReceived;

                pythonProcess.BeginOutputReadLine();

                pythonProcess.WaitForExit();
                pythonProcess.CancelOutputRead();
            }
        }

        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine(e.Data);
        }

        private static double GetX(string data)
        {
            return double.Parse(data.Split(',')[0]);
        }
        private static double GetD(string data)
        {
            return double.Parse(data.Split(',')[1]);
        }
        private static bool IsClose(string p1, string p2)
        {
            double x1 = GetX(p1) * CM_PER_PIXEL;
            double d1 = GetD(p1) * 100;
            double x2 = GetX(p2) * CM_PER_PIXEL;
            double d2 = GetD(p2) * 100;

            MinDetectedDistance = Math.Min(MinDetectedDistance, Math.Min(d1, d2));

            if (d1 > d2)
            {
                // swap d1 and d2
                double t = d1;
                d1 = d2; d2 = t;
            }

            double xDelta = Math.Abs(x1 - x2);

            double tmp = d2 - Math.Sqrt(d1 * d1 - xDelta * xDelta);

            double dis = Math.Sqrt(tmp * tmp + xDelta * xDelta);
            
            return dis < MAX_DISTANCE;
        }

        public static bool CheckDistance(string data)
        {
            string[] dectectedPeople = data.Split('%');

            for(int i = 0; i < dectectedPeople.Length - 1; i++)
            {
                for(int j = i + 1; j < dectectedPeople.Length; j++)
                {
                    if (IsClose(dectectedPeople[i], dectectedPeople[j]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static void StartChecking()
        {
            Roving.Start();

            ThreadHelper.StartNewThread(new Action(() => KeepReadingData()));
        }

        private static void ProcessData(string mess)
        {
            BaseHelper.CancelNavigation();
            Thread thread = new Thread(new ThreadStart(() =>
            {
                WarningTarget();
            }));

            thread.Start();

            frmWarning.ShowDialog();
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            string mess = e.Data;
            Console.WriteLine(e.Data);

            if (string.IsNullOrEmpty(mess) || mess.Contains("%") == false) return;
            
            mess = mess.Remove(e.Data.Length - 1); // remove last %
            
            if (IsDetected == false)
            {
                Console.WriteLine("Processing");
                bool warning = CheckDistance(mess);

                if (warning)
                {
                    IsDetected = true;
                    ProcessData(mess);
                }
            }
        }
        
        private static void Wait(int miliSec)
        {
            Thread.Sleep(miliSec);
        }
        private static void WarningTarget()
        {
            
            AudioHelper.PlayAlarmSound();

            Synthesizer.Speak("Please practice social distancing! At least 1 meter apart");
            Synthesizer.Speak("Again, at least 1 meter apart please");

            Wait(1000);

            IsDetected = false;
        }
    }
}
