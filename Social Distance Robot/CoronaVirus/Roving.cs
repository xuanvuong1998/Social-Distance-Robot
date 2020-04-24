using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
    
namespace robot_head
{
    class Roving
    {
        private const int NEXT_GOAL_DELAY = 500;
        public static void Start()
        {
            var rovingLocations = DatabaseHelper.LocationDespDB.GetRovingLocations();

            int curLocationIndex = -1;

            Action action = new Action(() =>
            {
                while (true)
                {                      
                    if (PythonCSharpCommunicationHelper.IsDetected == false)
                    {
                        Console.Write("Robot is moving");
                        curLocationIndex = (curLocationIndex + 1) % (rovingLocations.Length);
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
