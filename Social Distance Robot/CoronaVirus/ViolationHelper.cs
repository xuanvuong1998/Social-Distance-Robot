using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace robot_head
{
    class ViolationHelper
    {
        public const string MASK_VIOLATION = "MASK_DETECTION";

        public const string MASK_VIOLATION_WARNING_MESSAGE
                = "For your own safety, please wear your mask";

        public const string SOCIAL_DIS_WARNING_MESSAGE = "Please practice social " +
            "distancing for your own safety! At least 1 meter apart. Again, at least 1 " +
            "meter apart";

        public const string SOCIAL_DISTANCING_VIOLATION = "SOCIAL_DISTANCING";
        public static string GetWarningMessageByType(string type)
        {
            if (type == MASK_VIOLATION) return MASK_VIOLATION_WARNING_MESSAGE;

            if (type == SOCIAL_DISTANCING_VIOLATION) return SOCIAL_DIS_WARNING_MESSAGE;

            return "warning";
        }
        
    }
}
