using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Robot;
using Robot.Data;
using ROS = Robot.Data.ROS;
using Timer = System.Timers.Timer;

namespace robot_head
{
    public class BaseHelper
    {
        public static readonly double DEFAULT_LINEAR_SPEED = 0.3;
        public static readonly double DEFAULT_ANGULAR_SPEED = 0.6;


        private static double linearSpeed;

        public static double LinearSpeed
        {
            get { return linearSpeed; }
            set
            {
                linearSpeed = value;
                rBase.LinearSpeed = value;
            }
        }

        private static double angularSpeed;

        public static double AngularSpeed
        {
            get { return angularSpeed; }
            set
            {
                angularSpeed = value;
                rBase.AngularSpeed = value;
            }
        }


        private static Base rBase = new Base();
        private static Timer rBaseStopTimer = new Timer();
        private static readonly double METER_PER_ROUND = 1.27484;

        static BaseHelper()
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

        public static void TurnLeft()
        {
            //Move(ROS.BaseDirection.ANTICLOCKWISE);

            rBase.Move(0, rBase.AngularSpeed);
        }

        public static void TurnRight()
        {
            rBase.Move(0, -rBase.AngularSpeed);
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
            rBase.Move(rBase.LinearSpeed, 0);
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
            rBase.Move(-rBase.LinearSpeed, 0);
        }

        public static void Move(double linearSpeed, double angularSpeed)
        {
            rBase.Move(linearSpeed, angularSpeed);
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
            rBase.Stop();
            rBase.CancelNavigation();
        }
        static public void Go(string location)
        {
            try
            {
                GlobalFlowControl.Navigation.ResetBeforeNavigation();

                rBase.Go(location);
            }
            catch { }
        }

        static public void GoUntilReachedGoalOrCanceled(string location)
        {
            Go(location);
            
            while (GlobalFlowControl.Navigation.Moving == true) ;
        }

        private static void RBase_NavigationStatusChanged(object o, NavigationStatusEventArgs e)
        {
            if (e.Status == "Goal reached.")
            {
                GlobalFlowControl.Navigation.ReachedGoal = true;

            }
            else if (e.Status.Length == 0)
            {
                GlobalFlowControl.Navigation.Canceled = true;
            }
            else
            {
                GlobalFlowControl.Navigation.Stucked = true;
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
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        static public void Disconnect()
        {
            rBase.Disconnect();
        }
        #endregion

        #region Cores
        static public void Stop()
        {
            rBase.Move(0, 0); // Set linear and angular speed to zero
            //rBase.Stop();
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



    }
}
