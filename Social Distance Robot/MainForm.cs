using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Robot;
using SpeechLibrary;
using Timer = System.Timers.Timer;

namespace robot_head
{
    public partial class MainForm : Form
    {
        
        public void InitUI()
        {
            pictureBox1.Hide();
            if (GlobalData.WindowMaximizedNoneBorder)
            {
                FormBorderStyle = FormBorderStyle.None;
            }
            WindowState = FormWindowState.Maximized;
            // Top Most : Don't use TopMost property. It will freeze your UI
        }
 
        public MainForm()
        {
            InitializeComponent();

            LoadAnnc();
            
        }

        private void LoadAnnc()
        {          
            TelepresenceControlHandler.LoadDailyAnnouncement();
        }

        private void DisplayRobotFace()
        {
            pictureBox1.Enabled = true;
            pictureBox1.Show();
            pictureBox1.Location = new System.Drawing.Point(0, 0);
            pictureBox1.Size = this.Size;
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            InitSpeech();

            Synthesizer.SpeakAsync("Please wait a few seconds to load the " +
                "whole program");

            if (GlobalData.TelepresenceEnabled)
            {
                DisplayWebFace();
                Thread.Sleep(1000);
                Synthesizer.SpeakAsync("telepresence is ready");
            }

            InitUI();

            ViolationDetectionHelper.InitForms();
            PythonCommunicationHelper.StartChecking();

            if (GlobalData.TelepresenceEnabled == false)
            {
                DisplayRobotFace();
            }

            ROSHelper.Connect();
            
            if (GlobalData.Covid19ViolationDetectEnabled)
            {
                while (ROSHelper.ROS_STATUS == "") { }

                if (ROSHelper.ROS_STATUS == "online")
                {
                    Synthesizer.SpeakAsync("ROS BRIDGE is ready");
                }
                else
                {
                    Synthesizer.Speak("ROS BRIDGE IS NOT READY! PLEASE CHECK the network");
                    this.Close();
                }

                var timeFlag = DateTime.Now;
                while (PythonCommunicationHelper.CameraDetectionReady == "")
                {
                    Thread.Sleep(1000);

                    var elapsed = DateTime.Now - timeFlag;

                    if (elapsed.TotalSeconds >= 25)
                    {
                        Synthesizer.Speak("Sorry, SOCIAL DISTANCING and face mask detection " +
                            "is not ready yet. ");
                        this.Close();
                    }
                }

                if (PythonCommunicationHelper.CameraDetectionReady != "ready")
                {
                    this.Close();
                }

                Thread.Sleep(1000 * 2);
                Synthesizer.SpeakAsync("Safe distancing and face mask " +
                    "detection are ready");

            }
            else
            {
                Synthesizer.SpeakAsync("ROS BRIDGE IS READY");
            }

            Synthesizer.Speak("Everything is Ok now!");

            if (GlobalData.RovingEnable)
            {
                Roving.Start();
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            
        }

        private void InitSpeech()
        {
            Synthesizer.SelectVoiceByName(GlobalData.Voice2);
        }

       
        private void DisplayWebFace()
        {
            RobotFaceBrowser.Load(GlobalData.TELEPRESENCE_URL
                , GlobalData.CEF_BINDING_NAME, new TelepresenceControlHandler());

            //Add browser to the form
            this.Controls.Add(RobotFaceBrowser.browser);

            //Make the browser fill the entire form
            RobotFaceBrowser.browser.Dock = DockStyle.Fill;

            RobotFaceBrowser.ChooseRobotIDAutoimatically();
        }

        private void MainForm_FormClosing_1(object sender, FormClosingEventArgs e)
        {
            Synthesizer.Speak("The system is closing now");
            ROSHelper.CancelNavigation();
            Roving.Stop();
            PythonCommunicationHelper.KillPython();
            
            ROSHelper.Disconnect();

            Environment.Exit(0);
        }

        private void RestartApplication()
        {
            Process.Start(Application.ExecutablePath);
            Process.GetCurrentProcess().Kill();
            Environment.Exit(0);
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.KeyData == Keys.Space)
            //{
            //    ROSHelper.SendDetectedAngleToROS("20,30,40.5");
            //}
        }
    }
}
