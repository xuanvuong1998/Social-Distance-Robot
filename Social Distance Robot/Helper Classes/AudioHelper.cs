using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace robot_head
{
    class AudioHelper
    {
        public static void PlayAudioSync(string fileName)
        {
            string startupPath = Application.StartupPath + @"\media\";
            using (var player = new SoundPlayer(startupPath + fileName))
            {
                player.PlaySync();
            }
        }
        
        public static void PlayAudioAsync(string fileName)
        {
            string startupPath = Application.StartupPath + @"\media\";
            using (var player = new SoundPlayer(startupPath + fileName))
            {
                player.Play();
            }
        }

        public static void PlayAudioLooping(string fileName)
        {
            string startupPath = Application.StartupPath + @"\media\";
            using (var player = new SoundPlayer(startupPath + fileName))
            {
                player.PlayLooping();
            }
        }


        public static void PlayAlarmSound()
        {
            PlayAudioSync("Alarm1.wav");             
        }
    }
}
