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

        public const double MAX_DISTANCE = 100; //1 meter

        private static Process pythonProcess;

        private static void KeepReadingData()
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = @"C:\Users\nkk01\AppData\Local\Programs\Python\Python38-32\python.exe",
                Arguments = @"C:\Users\nkk01\.spyder-py3\temp.py",
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
            double x1 = GetX(p1);
            double d1 = GetD(p1) * 100;
            double x2 = GetX(p2);
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

        private static bool CheckDistance(string data)
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
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
            
            if (e.Data != null && IsDetected == false)
            {
                Console.WriteLine("Processing");
                bool warning = CheckDistance(e.Data);

                if (warning)
                {
                    IsDetected = true;
                    ProcessData(e.Data);
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

            Synthesizer.SelectVoiceByName("IVONA 2 Brian OEM");

            Synthesizer.Speak("Please practice social distancing! At least 1 meter apart");
            Synthesizer.Speak("Again, at least 1 meter apart please");

            Wait(4000);

            IsDetected = false;
        }
    }
}
