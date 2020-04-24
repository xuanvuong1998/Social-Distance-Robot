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
    class PythonCommunicationHelper
    {
        #region Constants

        private const string PYTHON_WORKING_DIR = @"C:\RobotReID\person_re_id-master\";

        private const string DETECT_SOCIAL_DIS_BY_CAMERA =
                PYTHON_WORKING_DIR + @"social_distancing_camera.py";
        private const string DETECT_SOCIAL_DIS_BY_CAMERA_LIDAR =
                PYTHON_WORKING_DIR + @"social_distance_lidar.py";

        private const string DETECT_SOCIAL_DIS_AND_MASK_BY_CAMERA_LIDAR =
                PYTHON_WORKING_DIR + @"social_distancing_lidar_mask_detection_camera.py";
        #endregion

        #region Properties
        private const string PYTHON_EXE_PATH = @"C:\ProgramData\Anaconda3\python.exe";

        private const string ACTIVE_PYTHON_FILE = DETECT_SOCIAL_DIS_AND_MASK_BY_CAMERA_LIDAR;

        private static Process pythonProcess;

        #endregion

        #region Python Process
        private static void KeepReadingData()
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false, 
                FileName = PYTHON_EXE_PATH ,
                WorkingDirectory = PYTHON_WORKING_DIR,
                Arguments = ACTIVE_PYTHON_FILE ,
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
            if (ViolationDetectionHelper.IsDetected) return;

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
                ViolationDetectionHelper.StartWarning(
                    ViolationDetectionHelper.SOCIAL_DISTANCING_VIOLATION);
                EvidenceHelper.SaveEvidenceToServer(
                    ViolationDetectionHelper.SOCIAL_DISTANCING_VIOLATION);
                
                   
            }else if (e.Data.Contains("facial_mask_violation"))
            {
                ViolationDetectionHelper.StartWarning(ViolationDetectionHelper.MASK_VIOLATION);
                EvidenceHelper.SaveEvidenceToServer(ViolationDetectionHelper.MASK_VIOLATION);
            }
            
        }
        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine(e.Data);
        }

        #endregion

       
    }
}
