using System;
using System.Configuration;
using ConfManager = System.Configuration.ConfigurationManager;


namespace robot_head.scheduler
{

    public static class TelepresenceScheduler
    {

        public static void DeleteDailyAnnouncement()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        
           
            config.AppSettings.Settings["AnnMess"].Value = "";
            config.AppSettings.Settings["AnnTime"].Value = "";
            config.AppSettings.Settings["AnnLoops"].Value = "";
            config.AppSettings.Settings["AnnInterval"].Value = "";
            config.Save(ConfigurationSaveMode.Modified);
            ConfManager.RefreshSection("appSettings");
            SchedulerService.DeleteAllTimersForEveryday();
        }

        public static void DeleteToday()
        {
            SchedulerService.DeleteAllTimersForToday();
        }
        public static void IntervalInSeconds(int hour, int min, int sec, int interval, int loops, Action task)
        {
            interval *= 1000;
            SchedulerService.Instance.ScheduleTask(false, hour, min, sec, interval, loops, task);
        }

        public static void DailyIntervalInSeconds(int hour, int min, int sec, int interval, int loops, Action task)
        {
            interval *= 1000;
            SchedulerService.Instance.ScheduleTask(true, hour, min, sec, interval, loops, task);
        }

    }
}