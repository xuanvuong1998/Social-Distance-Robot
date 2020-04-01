using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace robot_head
{
    class SynchronisationData
    {
        //This data model is used when synchronising data from body to head and vice versa
        public string Navigation { get; set; } = null;
        public string BaseMovement { get; set; } = null;
        public string Gesture { get; set; } = null;
        public string GenerateQR { get; set; } = null;
        public string Speech { get; set; } = null;
        public string SpeechAsync { get; set; } = null;
        public string PicturePath { get; set; } = null;
        public string VideoPath { get; set; } = null;        
        public string ChatBot { get; set; } = null;
        public string Telepresence { get; set; } = null;
        public string GuidedTour { get; set; } =  null;
        public string AskQuestion { get; set; } = null;
        public string DisplayMedia { get; set; } = null;

        public static SynchronisationData PackDataSingle(string header, string data)
        {
            SynchronisationData syncData = new SynchronisationData();
            switch (header)
            {
                case "Navigation":                    
                    syncData.Navigation = data;
                    break;
                case "BaseMovement":
                    syncData.BaseMovement = data;
                    break;
                case "Gesture":
                    syncData.Gesture = data;
                    break;
                case "GenerateQR":
                    syncData.GenerateQR = data;
                    break;
                case "Speech":
                    syncData.Speech = data;
                    break;
                case "PicturePath":
                    syncData.PicturePath = data;
                    break;
                case "VideoPath":
                    syncData.VideoPath = data;
                    break;
                case "Telepresence":
                    syncData.Telepresence = data;
                    break;
                case "ChatBot":
                    syncData.ChatBot = data;
                    break;
                case "GuidedTour":
                    syncData.GuidedTour = data;
                    break;
                case "AskQuestion":
                    syncData.AskQuestion = data;
                    break;
                case "DisplayMedia":
                    syncData.DisplayMedia = data;
                    break;                
            }

            return syncData;
        }

    }
}
