using System;
using System.Diagnostics;
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
        
        public void InitUI()
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;            
            // Top Most : Don't use TopMost property. It will freeze your UI
        }
        public void ActivateChatBot()
        {
            if (GlobalFlowControl.moduleActivated) return;            
            Task.Run(() => ChatModule.Start()).ConfigureAwait(false);
        }

        public MainForm()
        {
            InitializeComponent();
        }

        private void LoadAnnc()
        {
            TelepresenceControlHandler.LoadDailyAnnouncement();
        }

        private void DisplayRobotFace()
        {
            pictureBox1.Show();
            pictureBox1.Location = new System.Drawing.Point(0, 0);
            pictureBox1.Size = this.Size;
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            pictureBox1.Hide(); 
            DisplayWebFace();           
           
            InitUI(); 
            //DisplayRobotFace();
            InitSpeech();
          
            //ChatModule.Init();
            //LoadAnnc();
            //InitExcelHelper();
            //ChatModule.Init();
            //ChatModule.Start();

            BaseHelper.Connect();

            SocialDistanceChecker.StartChecking();
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
            RobotFaceBrowser.Load(GlobalData.TELEPRESENCE_URL, "winformFuncAsync", new TelepresenceControlHandler());

            //Add browser to the form
            this.Controls.Add(RobotFaceBrowser.browser);

            //Make the browser fill the entire form
            RobotFaceBrowser.browser.Dock = DockStyle.Fill;

            RobotFaceBrowser.ChooseRobotIDAutoimatically();
        }

        private void MainForm_FormClosing_1(object sender, FormClosingEventArgs e)
        {
            SocialDistanceChecker.KillPython();

            BaseHelper.CancelNavigation();
            BaseHelper.Stop();

            //Environment.Exit(0);
        }

        private void RestartApplication()
        {
            Process.Start(Application.ExecutablePath);
            Process.GetCurrentProcess().Kill();
            Environment.Exit(0);
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Space)
            {
                BaseHelper.Stop();
            }
            if (e.KeyData == Keys.Enter)
            {
                GlobalFlowControl.Robot.IsFollowing = false;
                BaseHelper.Stop();
            }

            if (e.KeyData == Keys.R)
            {
                RestartApplication();
            }
        }
    }
}
