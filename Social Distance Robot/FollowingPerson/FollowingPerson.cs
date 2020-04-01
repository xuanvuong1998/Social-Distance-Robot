using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace robot_head
{
    class FollowingPerson
    {
        const string outputFile = @"C:\Users\Surface\Desktop\VUONG\FollowingRobot\output.txt";
        const double D = 120, X = 330;
        const double MIN_LINEAR = 0.15, MAX_LINEAR = 0.7;
        const double MIN_ANGULAR = 0.3, MAX_ANGULAR = 1.5;
        
        const double P_Linear = 0.015, P_Angular = 0.007; // propotion
        public static void FollowTarget(double d, double x)
        {
            double dDelta = d - D; // Positive: turn right, negative: turn left
            double xDelta = x - X;

            double linearSpeed = dDelta * P_Linear;
            double angularSpeed = xDelta * P_Angular;

            int flag = angularSpeed > 0 ? 1 : -1;

            if (angularSpeed < 0) angularSpeed *= -1;
            
            //linearSpeed = Math.Max(linearSpeed, MIN_LINEAR);
            linearSpeed = Math.Min(linearSpeed, MAX_LINEAR);
            
            //angularSpeed = Math.Max(angularSpeed, MIN_ANGULAR);
            angularSpeed = Math.Min(angularSpeed, MAX_ANGULAR);
            
            if (linearSpeed < MIN_LINEAR) linearSpeed = 0;
            if (angularSpeed < MIN_LINEAR) angularSpeed = 0;
            
            Debug.WriteLine("Linear : " + linearSpeed + ", " +
                    "Angular: " + angularSpeed);

            BaseHelper.Move(linearSpeed, angularSpeed * flag); 
            // -1: in ROS, negative means left, positive means right
        }

        private static void Wait(int miliSec)
        {
            Thread.Sleep(miliSec);
        }
        public static void ReadChanges()
        {
            Thread thread = new Thread(new ThreadStart(() =>
            {
                GlobalFlowControl.Robot.IsFollowing = true;
                do
                {
                    string data = File.ReadAllText(outputFile);

                    double d = double.Parse(data.Split(' ')[1]); // meter
                    double x = double.Parse(data.Split(' ')[0]);

                    Debug.WriteLine("d : " + d + ", x : " + x);

                    FollowTarget(d * 100, x); // meter to cm

                    Wait(300); 
                } while (GlobalFlowControl.Robot.IsFollowing);
            }));

            thread.Start();
        }
    }
}
