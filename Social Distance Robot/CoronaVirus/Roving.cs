using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
    
namespace robot_head
{
    class Roving
    {
        public static void Start()
        {
            var rovingLocations = DatabaseHelper.LocationDespDB.GetRovingLocations();

            int curLocationIndex = -1;

            Action action = new Action(() =>
            {
                while (true)
                {
                    if (SocialDistanceChecker.IsDetected == false)
                    {
                        Console.Write("Robot is moving");
                        curLocationIndex = (curLocationIndex + 1) % (rovingLocations.Length);
                        BaseHelper.GoUntilReachedGoalOrCanceled(rovingLocations[curLocationIndex]);
                        Console.WriteLine("Reached goal! Waiting for next location");
                        ThreadHelper.Wait(1000 * 10);
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
