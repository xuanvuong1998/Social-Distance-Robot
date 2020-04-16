using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DarrenLee.Media;
using SpeechLibrary;

namespace robot_head
{
    static class Program
    {
        ///// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //string Path = @"C:\RobotReID\CapturedImages\Evidence.jpg";
            //using (Image image = Image.FromFile(Path))
            //{
            //    using (MemoryStream m = new MemoryStream())
            //    {
            //        image.Save(m, image.RawFormat);
            //        byte[] imageBytes = m.ToArray();
                    
            //        Console.WriteLine(imageBytes.Length); 

            //        // Convert byte[] to Base64 String
            //        string base64String = Convert.ToBase64String(imageBytes);

            //        Console.WriteLine(base64String.Substring(0, 100));
            //    }
            //}

            //return;


            //SocialDistanceChecker.CheckDistance("497,3.37%278,3.86"); return;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

       
    }
}
