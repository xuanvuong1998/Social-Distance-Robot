using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Robot;
using Robot.Data;
using SpeechLibrary;
using ROS = Robot.Data.ROS;
using Timer = System.Timers.Timer;

namespace robot_head
{
    public class ROSHelper
    {
        public static readonly double DEFAULT_LINEAR_SPEED = 0.2;
        public static readonly double DEFAULT_ANGULAR_SPEED = 0.2;


        private static double linearSpeed;

        public static double LinearSpeed {
            get { return linearSpeed; }
            set {
                linearSpeed = value;
                rBase.LinearSpeed = value;
            }
        }

        private static double angularSpeed;

        public static double AngularSpeed {
            get { return angularSpeed; }
            set {
                angularSpeed = value;
                rBase.AngularSpeed = value;
            }
        }


        private static Base rBase = new Base();
        private static Timer rBaseStopTimer = new Timer();
        private static readonly double METER_PER_ROUND = 1.27484;

        static ROSHelper()
        {
            rBaseStopTimer.Interval = 1000;
            rBaseStopTimer.Elapsed += RBaseStopTimer_Elapsed;
            rBaseStopTimer.AutoReset = false;

            AngularSpeed = DEFAULT_ANGULAR_SPEED;
            LinearSpeed = DEFAULT_LINEAR_SPEED;
        }

        #region Locations

        public static List<string> GetAllLocations()
        {
            return rBase.GetLocations();
        }

        public static void DeleteLocation(string location)
        {
            rBase.DeleteLocation(location);
        }

        public static void SaveLocation(string location)
        {
            rBase.SaveLocation(location);
        }

        #endregion

        #region events
        private static void RBaseStopTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Stop();

        }

        static void WaitToStop(double time)
        {
            rBaseStopTimer.Interval = time;
            rBaseStopTimer.Start();
        }

        public static void Wait(int delayTime)
        {
            Thread.Sleep(delayTime);
        }

        #endregion

        #region spinning       

        public static void MoveOne(double linearSpeed, double angularSpeed)
        {
            //Move(linearSpeed, angularSpeed);
            rBase.MoveOne(linearSpeed, angularSpeed); 
        }

        public static void TurnLeft()
        {          
            //rBase.Move(0, rBase.AngularSpeed);
            MoveOne(0, rBase.AngularSpeed);
        }

        public static void TurnRight()
        {
            MoveOne(0, -rBase.AngularSpeed);
        }
        public static void TurnLeft(int rounds)
        {
            double totalM = rounds * METER_PER_ROUND;
            double vel = AngularSpeed;

            double time = totalM / vel;

            TurnLeft();

            if (rounds > 0) WaitToStop(time);
        }

        public static void TurnRight(int rounds)
        {
            double totalM = rounds * METER_PER_ROUND;
            double vel = AngularSpeed;

            double time = totalM / vel;

            TurnRight();

            if (rounds > 0) WaitToStop(time);
        }

        public static void TurnLeftDuring(double interval)
        {
            interval *= 1000;
            TurnLeft();
            WaitToStop(interval);
        }

        public static void TurnRightDuring(double interval)
        {
            interval *= 1000;
            TurnRight();
            WaitToStop(interval);
        }

        #endregion

        #region Moves
        public static void Forward()
        {
            MoveOne(rBase.LinearSpeed, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="interval">movement period (seconds)</param>
        public static void ForwardDuring(int interval)
        {
            interval *= 1000;
            Forward();
            WaitToStop(interval);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="interval">seconds</param>
        public static void BackwardDuring(int interval)
        {
            interval *= 1000;
            Backward();
            WaitToStop(interval);
        }

        public static void Forward(int meters)
        {
            double vel = LinearSpeed;

            double time = meters / vel;

            time *= 1000;

            Forward();

            if (meters > 0) WaitToStop(time);
        }

        public static void Backward(int meters)
        {
            double vel = LinearSpeed;

            double time = meters / vel;

            time *= 1000;

            Backward();

            if (meters > 0) WaitToStop(time);
        }

        public static void Backward()
        {
            MoveOne(-rBase.LinearSpeed, 0);
        }

        public static void Move(double linearSpeed, double angularSpeed)
        {
            MoveOne(linearSpeed, angularSpeed);
            //rBase.Move(linearSpeed, angularSpeed);
        }
        #endregion

        #region Speed

        public static void SetStaticSpeed(int angularSpeed, int linearSpeed)
        {
            AngularSpeed = angularSpeed;
            LinearSpeed = linearSpeed;
        }

        public static void ResetSpeed()
        {
            AngularSpeed = DEFAULT_ANGULAR_SPEED;
            LinearSpeed = DEFAULT_LINEAR_SPEED;
        }
        public static void SpeedUp()
        {
            AngularSpeed += 0.1;
            LinearSpeed += 0.1;
        }

        public static void SlowDown()
        {
            AngularSpeed -= 0.1;
            LinearSpeed -= 0.1;
        }

        #endregion

        #region commands
        public static void DoBaseMovements(string direction)
        {
            switch (direction)
            {
                case "forward":
                    Forward();
                    break;
                case "backward":
                    Backward();
                    break;
                case "left-turn":
                    TurnLeft();
                    break;
                case "right-turn":
                    TurnRight();
                    break;
                case "speed-up":
                    SpeedUp();
                    break;
                case "slow-down":
                    SlowDown();
                    break;
                case "reset-speed":
                    ResetSpeed();
                    break;
                case "stop":
                    Stop();
                    break;
                default:
                    Stop();
                    break;
            }
        }
        #endregion

        #region Navigation
        public static bool IsCancelledNavigation()
        {
            return string.IsNullOrEmpty(GetNavigationStatus());
        }

        public static bool IsReachedGoal() => GetNavigationStatus() == "Goal reached.";

        public static string GetNavigationStatus()
        {
            return ROS.DataList[ROSTopic.NAVIGATION_STATUS].Data;
        }
        static public void CancelNavigation()
        {
            try
            {
                rBase.CancelNavigation();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        static public void Go(string location)
        {
            try
            {
                GlobalFlowControl.Navigation.ResetBeforeNavigation();

                rBase.Go(location);
            }
            catch
            {
                GlobalFlowControl.Navigation.Reset();
            }
        }

        static public void Go(decimal x, decimal y, decimal z, decimal w)
        {
            rBase.Go(new BotLocation(x, y, z, w));
        }

        static public void GoUntilReachedGoalOrCanceled(string location)
        {
            try
            {
                Go(location);

                while (GlobalFlowControl.Navigation.Moving == true) ;
            }
            catch (Exception)
            {

            }
        }


        #endregion

        #region Setup
        static public void Connect()
        {
            try
            {
                rBase.Connect(GlobalData.ROS_IP);
                rBase.Initialise();
                rBase.NavigationStatusChanged += RBase_NavigationStatusChanged;
                rBase.GeneralMessageReceived += RBase_GeneralMessageReceived;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public static void StopCheckingTeleOP()
        {
            MoveOne(-100, 0);
        }

        static public void Disconnect()
        {
            rBase.Disconnect();
        }
        #endregion

        #region Cores
        static public void Stop()
        {
            MoveOne(0, 0);

            CancelNavigation();
            rBaseStopTimer.Stop();
        }

        static public void Move(ROS.BaseDirection direction)
        {
            try
            {
                rBase.AngularSpeed = AngularSpeed;
                rBase.LinearSpeed = LinearSpeed;
                rBase.Move(direction);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Message From ROS
        private static void RBase_GeneralMessageReceived(object sender, GeneralMessageEventArgs e)
        {
            Debug.WriteLine("General Message Received: " + e.Message);
            
            if (ViolationDetectionHelper.IsDetected)
            {
                Debug.WriteLine("--- ROBOT IS WARNING");
                return;
            }

            if (e.Message.Contains("object_detected_depth"))
            {
                var firstComasIndex = e.Message.IndexOf(',');

                string depths = e.Message.Substring(firstComasIndex + 1);

                Debug.WriteLine("Depth received from ROS: " + depths);

                bool res;
                do
                {
                    res = FileHelper.WriteContentToFile(
                        FileHelper.PYTHON_CSHARP_SHARING_FILE, depths);
                    ThreadHelper.Wait(50);
                } while (res == false);

            }

        }

        private static void RBase_NavigationStatusChanged(object o, NavigationStatusEventArgs e)
        {
            if (e.Status == "Goal reached.")
            {
                Roving.NavigationIncompleted = false;
                GlobalFlowControl.Navigation.ReachedGoal = true;
            }
            else if (e.Status.Length == 0)
            {
                Roving.NavigationIncompleted = true;
                GlobalFlowControl.Navigation.Canceled = true;
            }
            else
            {
                Roving.NavigationIncompleted = true;
                GlobalFlowControl.Navigation.Stucked = true;
            }

        }

        #endregion

        #region People Detection

        public static void SendDetectedAngleToROS(string angle)
        {
            rBase.SendDetectedAngle(angle);
        }

        #endregion
    }
}
