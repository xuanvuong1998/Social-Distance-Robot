using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Synthesis;
using System.Diagnostics;

namespace robot_head
{
    class SpeechGeneration
    {
        public static SpeechSynthesizer speechSyn = new SpeechSynthesizer();
        public static bool IsSpeaking = false;


        public static void SetUp(VoiceGender gender, VoiceAge age)
        {
            //Customise the voice 
         
            //speechSyn.SelectVoice("Vocalizer Expressive Samantha Harpo 22kHz");
            speechSyn.SelectVoiceByHints(gender, age, 0);
            speechSyn.SpeakStarted += SpeechSyn_SpeakStarted;
            speechSyn.SpeakCompleted += SpeechSyn_SpeakCompleted;
        }

        private static void SpeechSyn_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            IsSpeaking = false;
        }

        private static void SpeechSyn_SpeakStarted(object sender, SpeakStartedEventArgs e)
        {
            IsSpeaking = true;
        }

       

        public static void SpeakAsync(string msg)
        {
            //GlobalFlowControl.SendToBase("SpeechAsync", msg);
            if (IsSpeaking)
            {
                Stop();
                
            }
            speechSyn.SpeakAsync(msg);
        }
        public static void SpeakSync(string msg)
        {
            //GlobalFlowControl.SendToBase("Speech", msg);            
            //GlobalData.SendToBase("Speech", msg);
            speechSyn.Speak(msg);
        }

        public static void SpeakInBody(string msg)
        {
            GlobalFlowControl.SendToBase("Speech", msg);
        }

        internal static void Stop()
        {
            IsSpeaking = false;
            try
            {
                speechSyn.SpeakAsyncCancelAll();
            }
            catch 
            {
              
            }
            
        }
    }
}
