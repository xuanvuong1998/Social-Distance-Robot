﻿using System;
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
        #region Properties

     
        // Indicate robot detected someone are close each other or not
        public static bool IsDetected { get; set; }
        private const int MIN_DETECTED_TIME_REQUIRED = 5; 
        private const int FRAMES_PER_CHECK = 8;
        private const int MAX_FRAME_CHECKING_INTERVAL = 6; // seconds
        private static DateTime _first_detected_time;
        
        private static List<string> _capturedFrames = new List<string>();

        // Value of smallest distance from robot to one of detected group
        public static double MinDetectedDistance { get; set; } = 10000000.0;
        private const double CM_PER_PIXEL = 0.34;
        private const double DEPTH_ERROR_OFFSET = 0.1;
            
        private const double MIN_X_DELTA_IN_CM = 15;
        private const double CAMERA_VIEW_RANGE_IN_PIXEL = 550; // from left most to right most

        public const double MAX_ALLOWED_DISTANCE = 105; // extra 10cm (because the calculated distance is 
                                // distance between 2 people's nest while we will mesaure 
                                // the social distance between their 2 most side

        private static Process pythonProcess;

        private static FrmWarning frmWarning = new FrmWarning();

        #endregion

        #region Python Process
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
        #endregion

        #region Extract Data
        private static double GetX(string data)
        {
            return double.Parse(data.Split(',')[0]);
        }
        private static double GetD(string data)
        {
            return double.Parse(data.Split(',')[1]);
        }
        #endregion

        #region Check Social Distance

        private static bool IsClose(string p1, string p2)
        {
            double x1 = GetX(p1);
            double d1 = GetD(p1) * 100;
            double x2 = GetX(p2);
            double d2 = GetD(p2) * 100;

            if (d1 == 0 || d2 == 0) return false;

            if (d1 > d2)
            {
                // swap d1 and d2
                double t = d1;
                d1 = d2; d2 = t;

                t = x1; x1 = x2; x2 = t;
            }

            if (d2 * (1 - DEPTH_ERROR_OFFSET) >= d1)
            {
                d2 *= 1 - DEPTH_ERROR_OFFSET;
            }

            double dDelta = Math.Abs(d1 - d2);

            if (dDelta >= MAX_ALLOWED_DISTANCE)
            {
                Debug.Write("--Distance: " + dDelta);
                return false;
            }


            //MinDetectedDistance = Math.Min(MinDetectedDistance, Math.Min(d1, d2));

            double averageCmPerPixel = ((d1 + d2) / 2) / CAMERA_VIEW_RANGE_IN_PIXEL;
            double xDelta = Math.Abs(x1 - x2) * averageCmPerPixel;
            
            
            // Fault detection
            if (xDelta >= MAX_ALLOWED_DISTANCE || xDelta < MIN_X_DELTA_IN_CM)
            {
                Debug.Write("--Distance: " + xDelta);
                return false;
            }

            double dis = Math.Sqrt(dDelta * dDelta + xDelta * xDelta);

            Debug.Write("--Distance: " + dis);
            return dis < MAX_ALLOWED_DISTANCE;
        }

        public static bool CheckDistance(string data)
        {
            string[] detectedPeople = data.Split('%');
            if (detectedPeople.Length > 1)
            {
                Debug.Write(data);
            }

            for(int i = 0; i < detectedPeople.Length - 1; i++)
            {
                for(int j = i + 1; j < detectedPeople.Length; j++)
                {
                    if (IsClose(detectedPeople[i], detectedPeople[j]))
                    {
                        Debug.WriteLine("");
                        return true;
                    }
                }
            }
           
            if (detectedPeople.Length > 1)
            {
                Debug.WriteLine("");
            }
            return false;
        }
        #endregion

        #region Flow
        public static void StartChecking()
        {
            //Roving.Start();

            ThreadHelper.StartNewThread(new Action(() => KeepReadingData()));
        }

        private static void Wait(int miliSec)
        {
            Thread.Sleep(miliSec);
        }
        #endregion

        #region Process Detected Data

        public static void StartWarning()
        {
            BaseHelper.CancelNavigation();
            Thread thread = new Thread(new ThreadStart(() =>
            {
                frmWarning.ShowDialog(); 
            }));
            
            thread.Start();
            
            Task.Factory.StartNew(new Action(() => WarningTarget()));

        }

        private static bool IsNotValidData(string mess)
        {
            if (string.IsNullOrEmpty(mess) 
                || mess.Contains("%") == false
                || mess.Contains(",0.0")) return false;
            
            return true;
        }

        #endregion

        #region Events
        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            string mess = e.Data;

            if (IsNotValidData(mess) == false) return;

            mess = mess.Remove(e.Data.Length - 1); // remove last %

            if (IsDetected == false)
            {

                // Detected first frame
                if (_capturedFrames.Count >= 1)
                {
                    var next_frame_detected_time = (DateTime.Now - _first_detected_time).TotalSeconds;

                    if (next_frame_detected_time > MAX_FRAME_CHECKING_INTERVAL)
                    {
                        Debug.WriteLine("Time out! Cancelled first detected");
                        _capturedFrames.Clear();
                        Debug.WriteLine("New First Detected! Keep checking");
                        _first_detected_time = DateTime.Now;
                    }

                    _capturedFrames.Add(mess);

                    Debug.WriteLine(_capturedFrames.Count + ". ");
                    if (_capturedFrames.Count == FRAMES_PER_CHECK)
                    {
                        int detectedTime = 0;
                        foreach (var frameData in _capturedFrames)
                        {
                            detectedTime += CheckDistance(frameData) ? 1 : 0;
                        }

                        bool warning = detectedTime >= MIN_DETECTED_TIME_REQUIRED;

                        if (warning)
                        {   
                            Console.WriteLine("Detected! "
                                + detectedTime + "times in " + FRAMES_PER_CHECK
                                + " times TOTAL");
                            IsDetected = true;
                            StartWarning();
                            Debug.WriteLine("Continue after start warning!");
                        }

                        _capturedFrames.Clear();
                    }                
                }
                else
                {
                    if (CheckDistance(mess))
                    {
                        Debug.WriteLine("First Detected. Keep Checking");
                        _first_detected_time = DateTime.Now;
                        _capturedFrames.Add(mess);
                    }
                }

            }
            else
            {
                Debug.WriteLine(mess + " --- SKIP");
            }
        }
        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine(e.Data);
        }
       
        #endregion

        #region Warning

        private static void WarningTarget()
        {
            AudioHelper.PlayAlarmSound();

            Synthesizer.Speak("Please practice social distancing for your own safety! At least 1 meter apart");
            Synthesizer.Speak("Again, Keep 1 meter apart please");


            //Synthesizer.Speak("Đụ mạ mi, đứng cách nhau một mét coi, Tau gọi công an đó");

            Wait(1000 * 2);
            
            IsDetected = false;
        }
        #endregion

    }
}
