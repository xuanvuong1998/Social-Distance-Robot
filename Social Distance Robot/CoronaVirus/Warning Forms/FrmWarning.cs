using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace robot_head
{
    public partial class FrmWarning : Form
    {
        Timer timer = new Timer();
        
        public FrmWarning()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
           
            timer.Interval = 1000;
            timer.AutoReset = true;
            timer.Elapsed += Timer_Elapsed;
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (ViolationDetectionHelper.IsDetected)
            {
                timer.Stop();
                HideForm();
            }
        }

        private void HideForm()
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action(() => this.Hide()));
            }
            else
            {
                this.Hide();
            }
        }

        private void FrmWarning_Shown(object sender, EventArgs e)
        {
            pictureBox1.Location = new Point(0, 0);
            pictureBox1.Size = this.Size;
            timer.Start();
        }
    }
}
