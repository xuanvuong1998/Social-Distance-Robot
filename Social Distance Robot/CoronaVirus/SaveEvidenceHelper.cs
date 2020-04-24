using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace robot_head
{
    class SaveEvidenceHelper
    {
        private const string SOCIAL_DIS_VIOLATION_EVIDENCE_FOLDER = @"C:\RobotReID\SocialDistancingEvidences\Evidence.jpg";
        private const string MASK_VIOLATION_EVIDENCE_FOLDER = @"C:\RobotReID\MaskViolationEvidences\Evidence.jpg";

        public static void SaveEvidenceToServer(string violation_type)
        {
            string path = null;
            if (violation_type == ViolationHelper.SOCIAL_DISTANCING_VIOLATION)
            {
                path = SOCIAL_DIS_VIOLATION_EVIDENCE_FOLDER;
            }else if (violation_type == ViolationHelper.MASK_VIOLATION)
            {
                path = MASK_VIOLATION_EVIDENCE_FOLDER;
            }

            if (path == null) return;

            try
            {
                using (Image image = Image.FromFile(path))
                {
                    using (MemoryStream m = new MemoryStream())
                    {
                        image.Save(m, image.RawFormat);
                        byte[] imageBytes = m.ToArray();

                        Console.WriteLine(imageBytes.Length);

                        // Convert byte[] to Base64 String
                        string base64String = Convert.ToBase64String(imageBytes);

                        SyncHelper.SaveEvidenceToServer(base64String, violation_type);
                    }
                }
            }
            catch (Exception ex)
            {

                Debug.WriteLine(ex.Message);
            }
            
        }
    }
}
