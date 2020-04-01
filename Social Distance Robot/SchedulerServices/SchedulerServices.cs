using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Timer = System.Timers.Timer;

namespace robot_head.scheduler
{

    public class SchedulerService
    {
        private static List<Timer> timers = new List<Timer>();
        private static List<Timer> dailyTimers = new List<Timer>();
        private int ticks, loops;
        private Action task;
        private Timer taskTimer;

        public static void DeleteAllTimersForToday()
        {
            foreach (var timer in timers)
            {
                timer.Stop();
                timer.Dispose();
            }

            timers.Clear();
        }

        public static void DeleteAllTimersForEveryday()
        {
            foreach (var timer in dailyTimers)
            {
                timer.Stop();
                timer.Dispose();
            }

            timers.Clear();
        }
        private SchedulerService() { }

        public static SchedulerService Instance => new SchedulerService();

        public void ScheduleTask(bool SchedulerType, int hour, int min, int sec, int intervalInMiliSeconds, int loops, Action taskDemanded)
        {
            DateTime now = DateTime.Now;

            DateTime firstRun = new DateTime(now.Year, now.Month, now.Day, hour, min, sec);

            TimeSpan timeToGo = firstRun - now;
          
            if (timeToGo <= TimeSpan.Zero)
            {
                if (-timeToGo.TotalMinutes > 10)
                {
                    return;
                }
                else
                {
                    timeToGo = TimeSpan.FromSeconds(2);
                }               
            }

            task = taskDemanded;

            taskTimer = new Timer() { Interval = intervalInMiliSeconds, AutoReset = true };

            taskTimer.Elapsed += MainTimer_Elapsed;
            ticks = 0;
            this.loops = loops;
            var timer = new Timer { Interval = timeToGo.TotalMilliseconds, AutoReset = false };

            if (!SchedulerType)
            {
                timers.Add(taskTimer);
            }
            else
            {
                dailyTimers.Add(taskTimer);
            }
            timer.Elapsed += (sender, e) => {

                taskTimer.Start();
                Debug.WriteLine("Timer start");
            };
            Debug.WriteLine("Time to go: " + timeToGo.TotalMilliseconds);
            timer.Start();
        }

        private void MainTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            task.Invoke();
            ticks++;
            Debug.WriteLine("Tick: " + ticks);
            if (ticks == loops)
            {
                taskTimer.Stop();
                taskTimer.Dispose();
            }

        }

    }
}