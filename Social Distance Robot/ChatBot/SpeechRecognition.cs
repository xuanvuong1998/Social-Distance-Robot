using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.CognitiveServices.Speech;
using Robot;
using Timer = System.Timers.Timer;

namespace robot_head
{
    class SpeechRecognition
    {
        public static string SpeechRecognitionSubscriptionKey = ConfigurationManager.AppSettings["SpeechRecognitionSubscriptionKey"];
        public static string SpeechRegion = ConfigurationManager.AppSettings["SubscriptionRegion"];
        private static SpeechConfig config = SpeechConfig.FromSubscription(SpeechRecognitionSubscriptionKey, SpeechRegion);
        private static string queryResult;
        private static bool stopRecognizing;
        private static bool isAskingQuestion;
        static readonly string[] keywords = ConfigurationManager.AppSettings["ActivateRosyKeywords"].Split('|');

        private const int ACTIVATE_WAITING_TIME = 12000, ASKING_WAITING_TIME = 15000;
        private static Timer recognizingTimer;
        private static int recognizingCount = 0;

        private static int currentRecognizingCount = 0;
        private static Timer flowTimer;
        private static string flowStage = "";

        private static void InitTimer()
        {
            recognizingTimer = new Timer();
            recognizingTimer.Interval = 1200;
            recognizingTimer.Elapsed += RecognizingTimer_Elapsed;
            recognizingTimer.AutoReset = true;

            flowTimer = new Timer();
            flowTimer.Interval = 200;
            flowTimer.AutoReset = true;
            flowTimer.Elapsed += FlowTimer_Elapsed;

        }

        private static void FlowTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        public static void StartFlowTimer()
        {
            flowTimer.Start();
        }

        public static void StopFlowTimer()
        {
            flowTimer.Stop();
        }

        static SpeechRecognition()
        {
            InitTimer();
        }
        private static void RecognizingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (GlobalFlowControl.ChatbotInterrupted || GlobalFlowControl.moduleActivated == false)
            {
                recognizingTimer.Stop();
                return;
            }

            Debug.Write("Checking..." + currentRecognizingCount
                + " <> " + recognizingCount + "...");
            if (recognizingCount == currentRecognizingCount && recognizingCount > 0
                && queryResult.Length >= 10)
            {
                Debug.WriteLine("NO MORE ULTERNANCE!!");
                stopRecognizing = true;
                recognizingTimer.Stop();
            }
            else
            {
                Debug.WriteLine("KEEP RECOGNIZING HUMAN");
                currentRecognizingCount = recognizingCount;
            }
        }

        private static void SendToRobotBase(string header, string msg)
        {
            SynchronisationData syncdata = SynchronisationData.PackDataSingle(header, msg);
            LattePandaCommunication.SendObjectAsJson(syncdata);
        }

        private static bool IsContainKeyword(string userWord)
        {
            userWord = userWord.ToLower().Trim();
            /*if (!GlobalFlowControl.IsRoving)
            {
                if (userWord.Contains("hello")) return true;
            }*/

            foreach (var item in keywords)
            {
                if (userWord.Contains(item))
                {
                    return true;
                }
            }
            return false;
        }

        public static async Task<string> RecognizeQuery(int maxWaitingTime)
        {
            var recognizer = new SpeechRecognizer(config);
            recognizer.Recognizing += Recognizer_Recognizing;
            recognizer.Recognized += Recognizer_Recognized;
            using (recognizer)
            {
                stopRecognizing = false;
                Timer timer = new Timer();
                timer.Interval = maxWaitingTime;
                //timer.AutoReset = true;
                timer.Elapsed += ActivateTimer_Elapsed;
                queryResult = null;
                timer.Start();
                recognizingCount = 0;
                currentRecognizingCount = 0;
                if (isAskingQuestion) recognizingTimer.Start();
                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
                while (!stopRecognizing && GlobalFlowControl.ChatbotInterrupted == false && !GlobalFlowControl.AskByPressingButton && GlobalFlowControl.moduleActivated) ;
                await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                recognizingTimer.Stop();
                Debug.WriteLine("Final Recognized: " + queryResult);
                timer.Stop();
                timer.Dispose();
                return queryResult;
            }
        }

        private static void Recognizer_Recognized(object sender, SpeechRecognitionEventArgs e)
        {
            if (GlobalFlowControl.moduleActivated == false ||
                  GlobalFlowControl.ChatbotInterrupted == true)
            {
                queryResult = null;
                return;
            }

            if (stopRecognizing) return;
            Debug.WriteLine("Recognized: " + e.Result.Text);

            if (isAskingQuestion)
            {
                if (e.Result.Text.Length > 9)
                {
                    queryResult = e.Result.Text;

                    stopRecognizing = true;
                }
            }
            else
            {
                if (IsContainKeyword(e.Result.Text))
                {
                    //queryResult = e.Result.Text;
                    stopRecognizing = true;
                }
            }
        }

        private static void Recognizer_Recognizing(object sender, SpeechRecognitionEventArgs e)
        {
            if (GlobalFlowControl.moduleActivated == false ||
                GlobalFlowControl.ChatbotInterrupted == true)
            {
                queryResult = null;
                return;
            }
            if (GlobalFlowControl.AskByPressingButton)
            {
                return;
            }
            
            queryResult = e.Result.Text;
            Debug.WriteLine("Recognizing: " + e.Result.Text);
            if (stopRecognizing) return;

            if (isAskingQuestion == false)
            {
                if (IsContainKeyword(queryResult))
                {

                    //queryResult = e.Result.Text;
                    stopRecognizing = true;
                }
            }
            else
            {
                recognizingCount++;
            }
        }

        public static async Task<string> Recognize()
        {
            string text = null;

            if (GlobalFlowControl.AskByPressingButton)
            {
                GlobalFlowControl.AskByPressingButton = false;
                ChatModule.StopCheckingIdle();
                GlobalFlowControl.IsRoving = false;
                //SendToRobotBase("Navigation", "Stop");

                text = await AskQuestion().ConfigureAwait(false);
            }
            else
            {
                ConversationGlobalFlow.Activated = false;
                var result = await RecognizeQuery(ACTIVATE_WAITING_TIME).ConfigureAwait(false);
                if (result != null)
                {
                    if (IsContainKeyword(result.ToLower()))
                    {
                        ConversationGlobalFlow.Activated = true;
                        ChatModule.StopCheckingIdle();
                        //if (GlobalFlowControl.AskByPressingButton) return null;
                        GlobalFlowControl.IsRoving = false;
                        //SendToRobotBase("Navigation", "Stop");
                        SendToRobotBase("DisplayMedia", GlobalData.ListeningModeImg);
                        //if (GlobalFlowControl.AskByPressingButton) return null;
                        SpeechGeneration.SpeakAsync("Yes? ");
                        text = await AskQuestion().ConfigureAwait(false);
                    }
                    else
                    {

                    }
                }
            }
            return text;
        }

        private static void ActivateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            stopRecognizing = true;
        }

        internal static async Task<string> AskQuestion()
        {
            if (GlobalFlowControl.moduleActivated == false) return null;

            string text = null;
            isAskingQuestion = true;
            var result = await RecognizeQuery(ASKING_WAITING_TIME).ConfigureAwait(false);
            isAskingQuestion = false;

            //Debug.WriteLine(result);

            if (result != null)
            {
                if (!string.IsNullOrWhiteSpace(result))
                {
                    text = result;
                }
                else
                {
                    SendToRobotBase("ChatBot", "NoRespond");
                }
            }
            else
            {
                SendToRobotBase("ChatBot", "NoRespond");
            }

            return text;
        }
    }
}
