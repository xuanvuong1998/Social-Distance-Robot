using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace robot_head
{
    class AnswerTemplate
    {
        public string MessageToSpeak { get; set; } = null;
        public string PicturePath { get; set; } = null;
        public string VideoPath { get; set; } = null;
        public string AutoNavLocation { get; set; } = null;
        public string HeadMotion { get; set; } = null;
        public string HandsAction { get; set; } = null;
        public int DisplayMediaDuration { get; set; } = 0;

        public AnswerTemplate()
        {

        }
        /**
         * Split the message with format _*_*_*_*_*_*_
         *  1. Message to Speak
         * 2. Picture Path
         * 3. Video Path
         * 4. Auto Navigation (Location's name)
         * 5. Head Emotion
         * 6. Action (Hands)
         * 7. Info's showing Duration
        */
        public void SplitMessage(string message)
        {
            string[] parts = message.Split('*');

            MessageToSpeak = parts[0];
            
            if (parts.Length > 1 && parts[1] != "_") PicturePath = parts[1];
            if (parts.Length > 2 && parts[2] != "_") VideoPath = parts[2];
            if (parts.Length > 3 && parts[3] != "_") AutoNavLocation = parts[3];
            if (parts.Length > 4 && parts[4] != "_") HeadMotion = parts[4];
            if (parts.Length > 5 && parts[5] != "_") HandsAction = parts[5];
            try
            {
                if (parts.Length > 6 && parts[6] != "_") DisplayMediaDuration = int.Parse(parts[6]);
            }
            catch { }            
            
        }

    }
}
