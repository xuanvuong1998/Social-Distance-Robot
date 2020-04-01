using Robot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace robot_head
{
    class GlobalFlowControl
    {

        public class Robot
        {
            public static bool IsFollowing { get; set; } = false;
        }
        public class Navigation
        {
            public static bool Moving { get; set; }

            private static bool reachedGoal;
            public static bool ReachedGoal
            {
                get { return reachedGoal; }
                set
                {
                    reachedGoal = value;

                    if (value == true)
                    {
                        Moving = false;
                    }
                }
            }

            private static bool stucked;
            public static bool Stucked
            {
                get { return stucked; }
                set
                {
                    stucked = value;
                    if (value == true)
                    {
                        Moving = false;
                    }
                }
            }

            private static bool canceled;

            public static bool Canceled
            {
                get { return canceled; }
                set
                {
                    canceled = value;
                    if (value == true)
                    {
                        Moving = false;
                    }
                }
            }

            /// <summary>
            /// not moving any more
            /// </summary>            
            public static void Reset()
            {
                Moving = false;
            }

            public static void ResetBeforeNavigation()
            {
                Moving = true;
                Canceled = false;
                Stucked = false;
                ReachedGoal = false;
            }
        }


        public static bool moduleActivated = false;

        public static bool AskByPressingButton = false;

        public static bool TelepresenceMode = false;

        public static bool ListeningMode = false;

        public static bool IsReachedGoal = false;

        public static bool IsCancelledNavigation = false;

        public static bool IsRoving = false;

        public static bool IsNavigationInChatBot = false;

        public static bool DataOk = false;

        public static bool ChatbotInterrupted = false;

        public static bool ChatBotDisabledByBody = false;

        public static SynchronisationData LatestData = null;

        public static void SendToBase(string header, string msg)
        {
            SynchronisationData syncData = SynchronisationData.PackDataSingle(header, msg);
            LatestData = syncData;
            LattePandaCommunication.SendObjectAsJson(syncData);
        }

        public static void SendToBase(string msg)
        {
            LattePandaCommunication.Send(msg);
        }
     
        public static void SendToBaseAgain()
        {
            if (LatestData != null)
            {
                LattePandaCommunication.SendObjectAsJson(LatestData);
            }            
        }

        public static void SendToBase(SynchronisationData data)
        {

            LattePandaCommunication.SendObjectAsJson(data);

        }
        
    }
}
