using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DarrenLee.Media;

namespace robot_head
{
    public partial class CameraTest : Form
    {
        Camera camera = new Camera();
        public CameraTest()
        {
            InitializeComponent();

            cbxCameraDevices.DataSource = camera.GetCameraSources();

            cbxResolutions.DataSource = camera.GetSupportedResolutions();

        }

        public CameraTest(Image image): this(){
            picCapture.Image = image;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            camera.Start();
            camera.OnFrameArrived += Camera_OnFrameArrived;
        }

        private void Camera_OnFrameArrived(object source, FrameArrivedEventArgs e)
        {
            picCapture.Image = e.GetFrame();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string fileName = @"C:\RobotReID\CapturedImages\capture.jpg";

            camera.Capture(fileName);

        }
    }
}
