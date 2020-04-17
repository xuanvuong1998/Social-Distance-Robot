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
      

        private static Process pythonProcess;

        private static FrmWarning frmWarning = new FrmWarning();

        public static bool IsDetected { get; internal set; }

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
            Roving.Start();

            ThreadHelper.StartNewThread(new Action(() => KeepReadingData()));
        }

        #endregion
     
        #region Events

        private static void SaveEvidenceToServer()
        {
            string Path = @"C:\RobotReID\CapturedImages\Evidence.jpg";
            using (Image image = Image.FromFile(Path))
            {
                using (MemoryStream m = new MemoryStream())
                {
                    image.Save(m, image.RawFormat);
                    byte[] imageBytes = m.ToArray(); 

                    Console.WriteLine(imageBytes.Length);
                    
                    // Convert byte[] to Base64 String
                    string base64String = Convert.ToBase64String(imageBytes);

                    WebHelper.SaveEvidenceToServer(base64String);
                }
            }
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Debug.WriteLine(e.Data);
            }
            if (e.Data != null && e.Data == "social_distancing_warning")
            {
                StartWarning();
                SaveEvidenceToServer();            
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
        public static void StartWarning()
        {
            IsDetected = true;
            BaseHelper.CancelNavigation();
            Thread thread = new Thread(new ThreadStart(() =>
            {
                frmWarning.ShowDialog();
            })); 

            thread.Start();

            Task.Factory.StartNew(new Action(() => WarningTarget()));

        }
        private static void WarningTarget()
        {
            AudioHelper.PlayAlarmSound();

            Synthesizer.Speak("Please practice social distancing for your own safety! At least 1 meter apart");
            Synthesizer.Speak("Again, Keep 1 meter apart please");

            Wait(1000 * 2);

            IsDetected = false;

        }
        #endregion
    }
}
