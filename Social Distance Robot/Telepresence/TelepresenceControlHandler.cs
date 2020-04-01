using Robot;
using System.Diagnostics;
using robot_head.scheduler;
using System;
using System.Configuration;
using ConfManager = System.Configuration.ConfigurationManager;

namespace robot_head
{ 
    public class TelepresenceControlHandler
    {        
        public void InterpretMessage(string recMsg)
        {
            if (recMsg == null) return;

            //System.Windows.Forms.MessageBox.Show(recMsg);
            string[] msg = recMsg.Split(new[] { '/' }, 2);
                       
            //msg[0] contains the header of the recMsg
            switch (msg[0])
            {
                case "StartCall":
                    StartCall();
                    break;
                case "Announcement":                    
                    Annoucement(msg[1]);
                    break;
                case "Speech":  
                case "Text":
                    InterpretText(msg[1]); 
                    break;
                case "GuidedTour": 
                    StartGuidedTour(msg[1]);
                    GlobalFlowControl.SendToBase(msg[0], msg[1]);
                    break;
                case "Navigation":
                    BaseHelper.GoUntilReachedGoalOrCanceled(msg[1]);
                    break;
                case "Gesture":  
                    break;
                case "BaseMovement":

                    BaseHelper.DoBaseMovements(msg[1]);
                   
                    //GlobalFlowControl.SendToBase(msg[0], msg[1]);
                    break;
                case "EndCall":
                    GlobalFlowControl.TelepresenceMode = false;
                    BaseHelper.Stop();
                    //GlobalFlowControl.SendToBase("Telepresence", "quit");                                                            
                    break;
            }
        }

        private void StartGuidedTour(string status)
        {                        
            if (status == "StartTour")
            {
                ChatModule.End();
            }
        }

        private void StartCall()
        {
            GlobalFlowControl.TelepresenceMode = true;
            GlobalFlowControl.IsRoving = false;
            //ChatModule.End();            
            //GlobalFlowControl.SendToBase("Telepresence", "StartTele");
        }

        public static void LoadDailyAnnouncement()
        {
            int anncmntCount = ConfManager.AppSettings["AnnMess"].Split('|').Length;
         
            for(int i = 0; i < anncmntCount; i++)
            {
                string command = ConfManager.AppSettings["AnnMess"].Split('|')[i];
                if (command.Length == 0) return;
                int loops = int.Parse(ConfManager.AppSettings["AnnLoops"].Split('|')[i]);
                int interval = int.Parse(ConfManager.AppSettings["AnnInterval"].Split('|')[i] );
                int h = int.Parse(ConfManager.AppSettings["AnnTime"].Split('|')[i].Split(':')[0]);
                int m = int.Parse(ConfManager.AppSettings["AnnTime"].Split('|')[i].Split(':')[1]);
                Action action = new Action(() =>
                {                    
                    SpeechGeneration.SpeakAsync(command);
                });

                TelepresenceScheduler.DailyIntervalInSeconds(h, m, 0, interval, loops, action);
            }
            
        }

        public static void UpdateDailyAnnoucement(int h, int m, int s, int interval, int loops, string command)
        {

            Configuration config = ConfManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            string delim = ConfManager.AppSettings["AnnMess"] == "" ? "" : "|";
            
            string annMess = ConfManager.AppSettings["AnnMess"] + delim + command;
            string annTime = ConfManager.AppSettings["AnnTime"] + delim + h + ":" + m;
            string annLoops = ConfManager.AppSettings["AnnLoops"] + delim + loops;
            string annInterval = ConfManager.AppSettings["AnnInterval"] +  delim + interval;

            config.AppSettings.Settings["AnnMess"].Value = annMess;
            config.AppSettings.Settings["AnnTime"].Value = annTime;
            config.AppSettings.Settings["AnnLoops"].Value = annLoops;
            config.AppSettings.Settings["AnnInterval"].Value = annInterval;
            config.Save(ConfigurationSaveMode.Modified);
            ConfManager.RefreshSection("appSettings");


            //TelepresenceScheduler.DeleteDailyAnnouncement();
            //LoadDailyAnnouncement();
            Action action = new Action(() =>
            {
                
                SpeechGeneration.SpeakAsync(command);
            });

            TelepresenceScheduler.DailyIntervalInSeconds(h, m, s, interval, loops, action);


        }
        private void Annoucement(string msg)
        {
            //ammend again after update the website   
            if (msg == null) return;

            if (msg == "DeleteToday")
            {
                TelepresenceScheduler.DeleteToday();
                return;
            }
            else if (msg == "DeleteDaily")
            {
                TelepresenceScheduler.DeleteDailyAnnouncement();
                return;
            }

            string startTimeType = msg.Split('/')[1];
            string command = msg.Split('/')[0];
            int loopsCount = int.Parse(msg.Split('/')[3]);
            int interval = int.Parse(msg.Split('/')[4]);

            int hour, min, sec;

            if (startTimeType == "Immediate")
            {
                
                SpeechGeneration.SpeakAsync(command);
                DateTime now = DateTime.Now;
                hour = now.Hour;
                min = now.Minute;
                sec = now.Second;
            }
            else
            {
                string startTime = msg.Split('/')[2];
                hour = int.Parse(startTime.Split(':')[0]);
                min = int.Parse(startTime.Split(':')[1]);
                sec = 0;
            }

            Action action = new Action(() =>
            {
                
                SpeechGeneration.SpeakAsync(command);
            });

            if (startTimeType == "Daily")
            {              
                UpdateDailyAnnoucement(hour, min, sec, interval, loopsCount, command);
            }
            else
            {
                TelepresenceScheduler.IntervalInSeconds(hour, min, sec, interval, loopsCount, action);
            }
        }

        private void InterpretText(string text)
        {
            
            SpeechGeneration.SpeakAsync(text);
            //SpeechGeneration.SpeakInBody(text); 
     
        }

        

    }
}
