using SpeechLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace robot_head
{
    class ViolationDetectionHelper
    {
        #region Constants

        public const string MASK_VIOLATION = "MASK_DETECTION";

        public const string MASK_VIOLATION_WARNING_MESSAGE
                = "For your own safety, please wear your mask";

        public const string SOCIAL_DIS_WARNING_MESSAGE = "Please practice safe " +
            "distancing for your own safety! At least 1 meter apart. Again, at least 1 " +
            "meter apart";

        private const string ASK_PESON_GIVE_WAY_MES = "Hi, I am a Park Patrol robot. I'm " +
            "on duty now, could you please give way to me? Sorry for the inconvenience caused. " +
            "thank you.";

        public const string SOCIAL_DISTANCING_VIOLATION = "SOCIAL_DISTANCING";

        private const int DELAY_AFTER_WARNING = 1000 * 2; // miliseconds

        public const double CONFIRM_CHANCE_TIME = 1000 * 2; // miliseconds

        public static readonly double MAX_DISTANCE_IN_CHARGE = 500;
        public static readonly double MIN_DISTANCE_IN_CHARGE = 100;
        public static readonly int BEEP_PLAY_LOOP_TIME = 1;

        #endregion

        public static bool IsDetectedByLidar { get; set; } = false;

        public static bool IsFrontDetected { get; set; } = true;
        public static bool IsDetected { get; internal set; }

        private static FrmWarning frmWarning;
        private static FrmMaskWarning frmMaskWarning;

        public static void InitForms()
        {
            frmWarning = new FrmWarning();
            frmMaskWarning = new FrmMaskWarning();
        }
        
        public static string GetWarningMessageByType(string type)
        {
            if (type == MASK_VIOLATION) return MASK_VIOLATION_WARNING_MESSAGE;

            if (type == SOCIAL_DISTANCING_VIOLATION) return SOCIAL_DIS_WARNING_MESSAGE;

            return "warning";
        }

        #region Warning

        public static void StartReminding(string violationType)
        {
            if (IsDetected == true) return;
            IsDetected = true;
           
            Task.Factory.StartNew(new Action(() => Remind(violationType)));
        }

        public static void AskPersonGiveWay()
        {
            Roving.Pause();
            Synthesizer.Speak(ASK_PESON_GIVE_WAY_MES);
            Roving.Resume();
        }

        public static void StartWarning(string violationType)
        {
            if (IsDetected == true) return;
            IsDetected = true;
            //BaseHelper.CancelNavigation();

            if (violationType == MASK_VIOLATION)
            {
                Thread thread = new Thread(new ThreadStart(() =>
                {
                    frmMaskWarning.ShowDialog();
                }));

                thread.Start();
            }
            else
            {
                Thread thread = new Thread(new ThreadStart(() =>
                {
                    frmWarning.ShowDialog();
                }));

                thread.Start();
            }


            Task.Factory.StartNew(new Action(() => WarningTarget(violationType)));

        }

        private static void Remind(string violationType)

        {
            string message = GetWarningMessageByType(violationType);
            for (int i = 1; i <= BEEP_PLAY_LOOP_TIME; i++)
            {
                AudioHelper.PlayRemindSound();
                AudioHelper.PlayRemindSound();
                AudioHelper.PlayRemindSound();
                Synthesizer.Speak(message);
            }
            IsDetected = false;
        }
        private static void WarningTarget(string warningType)
        {

            AudioHelper.PlayAlarmSound();

            Synthesizer.Speak(GetWarningMessageByType(warningType));

            ThreadHelper.Wait(DELAY_AFTER_WARNING);


            IsDetected = false;
            IsDetectedByLidar = false;

        }
        #endregion


    }
}
