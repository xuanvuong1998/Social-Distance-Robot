using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DarrenLee.Media;

namespace robot_head
{
    class CameraHelper
    {
        private static Camera cam = new Camera();


        public static void StartStreaming()
        {
            cam.Start();
            cam.OnFrameArrived += Cam_OnFrameArrived;
        }

        private static void Cam_OnFrameArrived(object source, FrameArrivedEventArgs e)
        {
           
        }
    }
}
