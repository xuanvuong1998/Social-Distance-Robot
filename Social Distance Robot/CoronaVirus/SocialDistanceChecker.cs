using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using SpeechLibrary;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;

namespace robot_head
{
    class SocialDistanceChecker
    {
        #region Properties
        private const string pythonExePath = @"C:\ProgramData\Anaconda3\python.exe";
        private const string pythonFile = @"C:\RobotReID\person_re_id-master\my_social_distance.py";
        private const string PYTHON_WORKING_DIR = @"C:\RobotReID\person_re_id-master\";
        private const string EVIDENCE_FOLDER = @"C:\RobotReID\SocialDistancingEvidences\Evidence.jpg";
        private const string WARNING_MESSAGE = "Please practice social " +
            "distancing for your own safety! At least 1 meter apart. Again, at least 1 " +
            "meter apart";

        public static bool IsDetectedByLidar { get; set; } = false; 
        private const int DELAY_AFTER_WARNING = 1000 * 2; // miliseconds

        public const double TIME_CHANCE_FOR_LIDAR = 1000 * 5;

        public static bool IsFrontDetected = true;
        public static DateTime LidarFirstDetectedTime;
        
        private static Process pythonProcess;

        private static FrmWarning frmWarning = new FrmWarning();

        public static bool IsDetected { get; internal set; }

        public static readonly double MAX_DISTANCE_IN_CHARGE = 500;
        public static readonly double MIN_DISTANCE_IN_CHARGE = 100;  
        public static readonly int BEEP_PLAY_LOOP_TIME = 1;           

        #endregion

        #region Python Process
        private static void KeepReadingData()
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false, 
                FileName = pythonExePath ,
                WorkingDirectory = PYTHON_WORKING_DIR,
                Arguments = pythonFile ,
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

        public static void KillPython()
        {
            try
            {
                pythonProcess.Kill();
            }
            catch
            {              
            }
        }
        #endregion

        #region Flow
        public static void StartChecking()
        {
            //Roving.Start();

            ThreadHelper.StartNewThread(new Action(() => KeepReadingData()));
        }

        #endregion
     
        #region Events

        public static void SaveEvidenceToServer()
        {
            string Path = EVIDENCE_FOLDER; 
            using (Image image = Image.FromFile(Path))
            {
                using (MemoryStream m = new MemoryStream())
                {
                    image.Save(m, image.RawFormat);
                    byte[] imageBytes = m.ToArray(); 

                    Console.WriteLine(imageBytes.Length);
                    
                    // Convert byte[] to Base64 String
                    string base64String = Convert.ToBase64String(imageBytes);

                    //SyncHelper.SaveEvidenceToServer(base64String);
                    //WebHelper.SaveEvidenceToServer(base64String);
                }
            }
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (IsDetected)
            {
                return; 
            }

            if (IsDetectedByLidar)
            {
                Debug.WriteLine("Is detected by lidar = " + IsDetectedByLidar);
                var now = DateTime.Now;

                var elapsed = (now - LidarFirstDetectedTime).TotalSeconds;

                if (elapsed > TIME_CHANCE_FOR_LIDAR)
                {
                    IsDetectedByLidar = false;
                    return;
                }
            }
            
            //Debug.WriteLine("DETECTED FROMCAMERA");
            if (e.Data != null)
            {
                Debug.WriteLine(e.Data);
            }
            
            if (e.Data != null && e.Data.Contains("social_distancing_warning"))
            {
                if (e.Data == "social_distancing_warning_front"
                    && IsFrontDetected)
                {
                    StartWarning();
                    SaveEvidenceToServer();
                }else if(e.Data == "social_distancing_warning_back"
                    && IsFrontDetected == false)
                {
                    StartWarning();
                    SaveEvidenceToServer(); 
                }
                else
                {
                    Debug.WriteLine("Ignore!!! not correct camera");
                }
                   

            }
            
        }
        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine(e.Data);
        }

        #endregion

        #region Warning

        public static void Wait(int miliSec)
        {
            Thread.Sleep(miliSec);
        }

        public static void StartReminding()
        {
            if (IsDetected == true) return;
            IsDetected = true;
            Thread thread = new Thread(new ThreadStart(() =>
            {
                //frmWarning.ShowDialog();
            }));

            
            Task.Factory.StartNew(new Action(() => Remind()));
        }

        public static void StartWarning()
        {
            if (IsDetected == true) return;
            IsDetected = true; 
            BaseHelper.CancelNavigation();
            Thread thread = new Thread(new ThreadStart(() =>
            {
                frmWarning.ShowDialog();
            })); 

            thread.Start();

            Task.Factory.StartNew(new Action(() => WarningTarget()));

        }

        private static void Remind()
        {
            for(int i = 1; i <= BEEP_PLAY_LOOP_TIME; i++)
            {
                AudioHelper.PlayRemindSound();
                AudioHelper.PlayRemindSound();
                AudioHelper.PlayRemindSound();
                Synthesizer.Speak(WARNING_MESSAGE);
            }
            IsDetected = false;
        }
        private static void WarningTarget()
        {
            AudioHelper.PlayAlarmSound();

            Synthesizer.Speak(WARNING_MESSAGE);
              
            Wait(DELAY_AFTER_WARNING); 

            IsDetected = false; 
            IsDetectedByLidar = false;

        }
        #endregion
    }
}
