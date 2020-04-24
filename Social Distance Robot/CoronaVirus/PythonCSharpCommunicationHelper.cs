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
    class PythonCSharpCommunicationHelper
    {
        #region Properties
        private const string pythonExePath = @"C:\ProgramData\Anaconda3\python.exe";
        //private const string pythonFile = @"C:\RobotReID\person_re_id-master\my_social_distance_lidar.py";
        private const string pythonFile = @"C:\RobotReID\person_re_id-master\SocialDistancing_MaskDetection.py";
        private const string PYTHON_WORKING_DIR = @"C:\RobotReID\person_re_id-master\";
        private const string EVIDENCE_FOLDER = @"C:\RobotReID\SocialDistancingEvidences\Evidence.jpg";
        
       

        public static bool IsDetectedByLidar { get; set; } = false;
        private const int DELAY_AFTER_WARNING = 1000 * 2; // miliseconds

        public const double CONFIRM_CHANCE_TIME = 1000 * 2;

        public static bool IsFrontDetected { get; set; } = true;
        public static DateTime LidarFirstDetectedTime { get; set; } 
        
        private static Process pythonProcess;

        private static FrmWarning frmWarning = new FrmWarning();
        private static FrmMaskWarning frmMaskWarning = new FrmMaskWarning();

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

       

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            /*
            if (IsDetected || IsDetectedByLidar == false)
            {
                return; 
            } 

            if (IsDetectedByLidar)
            {
                Debug.WriteLine("Is detected by lidar = " + IsDetectedByLidar);
                var now = DateTime.Now;

                var elapsed = (now - LidarFirstDetectedTime).TotalSeconds;

                if (elapsed > CONFIRM_CHANCE_TIME) 
                {
                    IsDetectedByLidar = false;
                    return;
                }
            } */

            if (IsDetected) return;

            if (e.Data != null)
            {
                Debug.WriteLine(e.Data);
            }
            else
            {
                return;
            }
            
            if (e.Data.Contains("x_angle"))
            {
                int firstComasIndex = e.Data.IndexOf(',');
                    
                string angles = e.Data.Substring(firstComasIndex + 1);
                
                ROSHelper.SendDetectedAngleToROS(angles);
            }else if (e.Data.Contains("social_distancing_warning"))
            {               
                StartWarning(ViolationHelper.SOCIAL_DISTANCING_VIOLATION);
                SaveEvidenceHelper.SaveEvidenceToServer(ViolationHelper.SOCIAL_DISTANCING_VIOLATION);
                
                /*
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
                */
                   
            }else if (e.Data.Contains("facial_mask_violation"))
            {
                StartWarning(ViolationHelper.MASK_VIOLATION);
                SaveEvidenceHelper.SaveEvidenceToServer(ViolationHelper.MASK_VIOLATION);
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

        public static void StartWarning(string violationType)
        {
            if (IsDetected == true) return;
            IsDetected = true; 
            //BaseHelper.CancelNavigation();

            if (violationType == ViolationHelper.MASK_VIOLATION)
            {
                Thread thread = new Thread(new ThreadStart(() =>
                { 
                    frmMaskWarning.ShowDialog();
                }));

                thread.Start();
            }
            else
            {
                Thread thread = new Thread(new ThreadStart(() =>
                {
                    frmWarning.ShowDialog();
                }));

                thread.Start();
            }
            

            Task.Factory.StartNew(new Action(() => WarningTarget(violationType)));

        }

        private static void Remind()
        {
            for(int i = 1; i <= BEEP_PLAY_LOOP_TIME; i++)
            {
                AudioHelper.PlayRemindSound();
                AudioHelper.PlayRemindSound();
                AudioHelper.PlayRemindSound();
                //Synthesizer.Speak(WARNING_MESSAGE);
            }
            IsDetected = false;
        }
        private static void WarningTarget(string warningType)
        {
            
            AudioHelper.PlayAlarmSound();

            Synthesizer.Speak(ViolationHelper.GetWarningMessageByType(warningType));
              
            Wait(DELAY_AFTER_WARNING); 
            

            IsDetected = false; 
            IsDetectedByLidar = false;

        }
        #endregion
    }
}
