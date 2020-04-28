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

        public static bool TelepresenceMode { get; set; } = false;

    }
}
