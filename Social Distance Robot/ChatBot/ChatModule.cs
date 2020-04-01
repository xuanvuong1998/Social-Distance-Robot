using Microsoft.Bot.Connector.DirectLine;
using Robot;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using AIMLbot;

namespace robot_head
{
    class ChatModule
    {
        private static DirectLineClient directLineClient;
        private static Timer timer;
        private static int askAgainCount;
        private static int chatBotTimeOutCount;
        private const int CHAT_BOT_TIME_OUT = 30;
        private static Thread chatbotThread;
        
        public static void Init()
        {            
            timer = new Timer();
            timer.Interval = 1000;
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;
            directLineClient = new DirectLineClient(ConfigurationManager.AppSettings["DirectLineSecret"], ConfigurationManager.AppSettings["BotId"]);
            directLineClient.Initialize();
            HandShake();
        }
        
        static ChatModule()
        {            
        }

       
        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (GlobalFlowControl.IsRoving == true)
            {
                timer.Stop();
                return;
            }
            chatBotTimeOutCount++;
            //Debug.WriteLine("Time out: " + chatBotTimeOutCount + " Ticks");
            if (chatBotTimeOutCount >= CHAT_BOT_TIME_OUT)
            {
                //GlobalFlowControl.ChatBotDisabledByBody = false;
                Debug.WriteLine("ChatBot end timeout");
                End();
            }
        }


        private static void HandShake()
        {
            Task.Run( async () =>
            {
                await directLineClient.PostQuestionToBotAsync("this is a handshake").ConfigureAwait(false);
                IEnumerable<Activity> activities = await directLineClient.ReadBotMessagesAsyncDriver().ConfigureAwait(false);
            });

/*              Task.Factory.StartNew(async () =>
              {
                  await directLineClient.PostQuestionToBotAsync("this is a handshake").ConfigureAwait(false);
                  IEnumerable<Activity> activities = await directLineClient.ReadBotMessagesAsyncDriver().ConfigureAwait(false);
              }); */
        }
        public static void Start()
        {
            if (GlobalFlowControl.moduleActivated) return;
            GlobalFlowControl.ChatbotInterrupted = false;
            GlobalFlowControl.moduleActivated = true;
            ConversationGlobalFlow.AskAgain = false;

            chatbotThread = new Thread(new ThreadStart(() =>
            {
                Conversation().ConfigureAwait(false);
            }));

            chatbotThread.Start();
        }

        public static void End()
        {
            timer.Stop();
            GlobalFlowControl.moduleActivated = false;
            SpeechGeneration.Stop();            
        }

        public static void StopCheckingIdle()
        {
            timer.Stop();
        }

        private static async Task Conversation()
        {
            while (GlobalFlowControl.moduleActivated)
            {
                GlobalFlowControl.ChatbotInterrupted = false;
                if (timer.Enabled == false && !GlobalFlowControl.IsRoving
                    && !GlobalFlowControl.AskByPressingButton)
                {                    
                    CheckIdle();
                }
                string userQuery = null;

                userQuery = await SpeechRecognition.Recognize().ConfigureAwait(false);

                //System.Windows.Forms.MessageBox.Show(userQuery);

                if (GlobalFlowControl.ChatbotInterrupted) continue;

                if (GlobalFlowControl.moduleActivated == false) break;
               
                if (userQuery != null)
                {
                    if (GlobalFlowControl.ChatbotInterrupted) continue;
                    if (GlobalFlowControl.moduleActivated == false) break;
                    await directLineClient.PostQuestionToBotAsync(userQuery).ConfigureAwait(false);
                    
                    IEnumerable<Activity> activities = await directLineClient.ReadBotMessagesAsyncDriver().ConfigureAwait(false);                  
                    if (GlobalFlowControl.ChatbotInterrupted) continue;
                    if (GlobalFlowControl.moduleActivated == false) break;
                   
                    await HandleBotResponse(activities, userQuery).ConfigureAwait(false);
            
                    if (GlobalFlowControl.ChatbotInterrupted) continue;
                    if (GlobalFlowControl.moduleActivated == false) break;
                }
                else
                {
                    ConversationGlobalFlow.AskAgain = false;
                }
            }
         
            directLineClient.EndConversion();
            GlobalFlowControl.moduleActivated = false;
            timer.Stop();

            //System.Windows.Forms.MessageBox.Show("Chatbot end" + GlobalFlowControl.ChatBotDisabledByBody);
            if (GlobalFlowControl.TelepresenceMode == false
             && GlobalFlowControl.ChatBotDisabledByBody == false) GlobalFlowControl.SendToBase("ChatBot", "quit");

            GlobalFlowControl.ChatBotDisabledByBody = false;
            //System.Windows.Forms.MessageBox.Show("Chatbot end 2" + GlobalFlowControl.ChatBotDisabledByBody);
        }

        internal static void ResetChatBot()
        {
            GlobalFlowControl.ChatbotInterrupted = true;
            timer.Stop();
            try
            {
                SpeechGeneration.Stop();
                
            }
            catch
            {

            }
                    
            //GlobalFlowControl.ChatbotInterrupted = true;
            /*End();
            Thread.Sleep(300);
            GlobalFlowControl.moduleActivated = true;
            Start(); */
        }

        private static void SaySync(string speechStr)
        {
            SpeechGeneration.SpeakSync(speechStr);
        }

        private static void SayAsync(string speechStr)
        {
            SpeechGeneration.SpeakAsync(speechStr);
        }

        private static void DisplayDefaultBackgroundImage()
        {
            GlobalFlowControl.SendToBase("DisplayMedia", GlobalData.ChatBotActivationImg);
        }

        private static async Task HandleBotResponse(IEnumerable<Activity> activities, string userQuery)
        {
            foreach (Activity activity in activities)
            {
                if (activity.Text != null)
                {
                    string[] allAnswers = activity.Text.Split('|');

                    Random rand = new Random();

                    string randomAns = allAnswers[rand.Next(allAnswers.Length)];

                    AnswerTemplate answer = new AnswerTemplate();
                    answer.SplitMessage(randomAns);

                    ConversationGlobalFlow.AskAgain = false;
                    //answer.MessageToSpeak = "ask again"
                    Debug.WriteLine("Reply: " + answer.MessageToSpeak);

                    ExcelHelper.AddData(userQuery, answer.MessageToSpeak);
                    if (GlobalFlowControl.moduleActivated == false) return;
                    if (answer.MessageToSpeak.ToLower().Contains("ask again"))
                    {
                        if (GlobalFlowControl.moduleActivated == false
                            || GlobalFlowControl.ChatbotInterrupted == true)
                        {                            
                            return;
                        }
                        /*ConversationGlobalFlow.AskAgain = true;
                        askAgainCount++;
                        if (askAgainCount == 2)
                        {
                            GlobalFlowControl.SendToBase("ChatBot", "StopTalking");
                            askAgainCount = 0;
                            ConversationGlobalFlow.AskAgain = false;
                            return;
                        } */
                        //GlobalFlowControl.SendToBase("ChatBot", "IsTalking");
                        //SpeechGeneration.SpeakSync("Sorry. I don't understand!");
                        answer.MessageToSpeak = "Sorry. I don't understand";
                        //return;
                    }
                    else
                    {
                        askAgainCount = 0;
                    }


                    if (GlobalFlowControl.moduleActivated == false)
                    {                      
                        return;
                    }
                    if (answer.PicturePath != null)
                    {
                        if (answer.AutoNavLocation != null)
                        {
                            answer.PicturePath = @"locations\" + answer.PicturePath;
                        }
                        else
                        {
                            answer.PicturePath = @"qna\" + answer.PicturePath;
                        }

                        GlobalFlowControl.SendToBase("DisplayMedia", answer.PicturePath);
                        if (answer.DisplayMediaDuration == 0)
                        {
                            answer.DisplayMediaDuration += 2;
                        }
                    }

                    try
                    {
                        if (GlobalFlowControl.moduleActivated == false)
                        {
                            return;
                        }
                        GlobalFlowControl.SendToBase("ChatBot", "IsTalking");
                        SaySync(answer.MessageToSpeak);

                        if (answer.VideoPath != null)
                        {
                            GlobalFlowControl.SendToBase("VideoPath", answer.VideoPath);
                        }

                        if (answer.DisplayMediaDuration > 0)
                        {
                            Thread.Sleep(1000 * answer.DisplayMediaDuration);
                        }

                        if (answer.AutoNavLocation != null)
                        {
                            SaySync("Do you want me to lead you there?");
                            SayAsync("Yes or No?");
                            GlobalFlowControl.SendToBase("DisplayMedia", GlobalData.ListeningModeImg);
                            string reply = await SpeechRecognition.RecognizeQuery(3000).ConfigureAwait(false);
                            if (reply != null)
                            {
                                reply = reply.ToLower();
                                if (reply.Contains("yes") ||
                                    reply.Contains("yeah") ||
                                    reply.Contains("yup") ||
                                    reply.Contains("ok"))
                                {
                                    SaySync("OK! Follow me");
                                    GlobalFlowControl.IsNavigationInChatBot = true;
                                    GlobalFlowControl.IsReachedGoal = false;
                                    GlobalFlowControl.IsCancelledNavigation = false;

                                    var data = new SynchronisationData
                                    {
                                        Navigation = answer.AutoNavLocation,
                                        ChatBot = "NavigationOn"
                                    };                                    

                                    GlobalFlowControl.SendToBase(data);

                                    /*while (!GlobalFlowControl.IsReachedGoal
                                        && !GlobalFlowControl.IsCancelledNavigation) ;
                                    Thread.Sleep(500);
                                    GlobalFlowControl.SendToBase("Navigation", "Stop");                                    
                                    if (GlobalFlowControl.IsCancelledNavigation) return;  
                                    */
                                    return;
                                }

                                SaySync("Thank you, have a nice day");
                            }
                        }
                        
                    }
                    catch
                    {
                        if (GlobalFlowControl.ChatbotInterrupted) continue;
                    }
                    GlobalFlowControl.SendToBase("ChatBot", "StopTalking");

                }
            }
        }

        private static void CheckIdle()
        {
            if (!GlobalFlowControl.IsNavigationInChatBot)
            {
                chatBotTimeOutCount = 0;
                Debug.WriteLine("ChatBot Timeout start");
                timer.Start();
            }
            
        }

    }
}
