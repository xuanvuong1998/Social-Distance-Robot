using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace robot_head
{
    class FileHelper
    {
        public static readonly string PYTHON_CSHARP_SHARING_FILE = @"C:\RobotReID\Depth_Output\depth_value.txt";

        public static bool WriteContentToFile(string fileName, string content)
        {
            try
            {
                using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(fileName))
                {
                    file.WriteLine(content);
                };

                return true;
            }
            catch
            {
                return false;
            }
           
        }
    }
}
