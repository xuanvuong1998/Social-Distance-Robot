using System;
using System.Diagnostics;

namespace robot_head
{
    class Roving
    {
        private static int NEXT_GOAL_DELAY = GlobalData.RovingLocationDelay;
        public static bool NavigationIncompleted { get; set; } = false;
        private static bool _isPausing { get; set; } = false;

        public static void Pause()
        {
            _isPausing = true; 
            ROSHelper.CancelNavigation();
        }

        public static void Resume()
        {
            _isPausing = false;
        }
        public static void Stop()
        {
            GlobalData.RovingEnable = false;
        }
        public static void Start()
        {
            var rovingLocations = DatabaseHelper.LocationDespDB.GetRovingLocations();

            Debug.WriteLine("Location");
            foreach (var lo in rovingLocations)
            {
                Debug.WriteLine(lo);
            }
            int curLocationIndex = -1;

            Action action = new Action(() =>
            {
                while (GlobalData.RovingEnable)
                {
                    if (_isPausing == false
                        && GlobalFlowControl.TelepresenceMode == false)
                    {
                        if (NavigationIncompleted == false)
                        {
                            curLocationIndex = (curLocationIndex + 1) % (rovingLocations.Length);
                        }
                        Console.WriteLine("Robot is moving to " + rovingLocations[curLocationIndex]);

                        ROSHelper.GoUntilReachedGoalOrCanceled(rovingLocations[curLocationIndex]);
                        Console.WriteLine("Reached goal! Waiting for next location");
                        ThreadHelper.Wait(NEXT_GOAL_DELAY);
                    } 
                    else
                    {
                        ThreadHelper.Wait(1000); // reduce workload (keep looping)
                    }
                }
            });

            ThreadHelper.StartNewThread(action);
        }
    }
}
