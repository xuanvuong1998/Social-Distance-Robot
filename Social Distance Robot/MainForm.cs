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
        private static Timer savingTimer = new Timer();
        private const int ROS_CONNECT_DELAY = 1000 * 2; 

        public void InitUI()
        {
            pictureBox1.Hide();
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            // Top Most : Don't use TopMost property. It will freeze your UI
        }
 
        public MainForm()
        {
            InitializeComponent();

            //InitUI();
            
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
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

            if (GlobalData.TelepresenceEnabled)
            {
                DisplayWebFace();
            }
            
            InitUI();

            ViolationDetectionHelper.InitForms();
            PythonCommunicationHelper.StartChecking();
                      
             
            if (GlobalData.TelepresenceEnabled == false)
            {
                DisplayRobotFace();
            }
            
            InitSpeech();

            ROSHelper.Connect();

            Thread.Sleep(ROS_CONNECT_DELAY);
            
            if (GlobalData.RovingEnable)
            {
                Roving.Start();
            }
           
        }

        private void InitSpeech()
        {
            Synthesizer.SelectVoiceByName(GlobalData.Voice2);

        }

        private void InitExcelHelper()
        {
            ExcelHelper.CreateTable();
            savingTimer.Interval = 1000 * 60 * 60;
            savingTimer.AutoReset = true;
            savingTimer.Elapsed += SavingTimer_Elapsed;
            savingTimer.Start();
        }

        private void SavingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (ExcelHelper.table.Rows.Count >= 10)
            {
                ExcelHelper.ExportToFile();
            }
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
            PythonCommunicationHelper.KillPython();
            ROSHelper.CancelNavigation();
            ROSHelper.Stop();
            ROSHelper.Disconnect();

            Roving.Stop();

            Environment.Exit(0);
            //Application.Exit();
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
            //if (e.KeyData == Keys.Enter)
            //{
            //    GlobalFlowControl.Robot.IsFollowing = false;
            //    ROSHelper.Stop();
            //}

            //if (e.KeyData == Keys.R)
            //{
            //    RestartApplication();
            //}
        }

      
    }
}
