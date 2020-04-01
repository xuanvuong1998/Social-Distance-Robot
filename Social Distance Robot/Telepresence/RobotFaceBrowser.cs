using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;

namespace robot_head
{
    class RobotFaceBrowser
    {
        public static ChromiumWebBrowser browser;
        
        public static void Load(string link, string binderName, object objectToBind)
        {
            CefSettings settings = new CefSettings();
            //enable local camera 
            settings.CefCommandLineArgs.Add("enable-media-stream", "1");
            Cef.EnableHighDPISupport();
            
            Cef.Initialize(settings);
            browser = new ChromiumWebBrowser(link);

            // name must be same for both js and c# code
            //bind function from javascript to c#                    
            browser.JavascriptObjectRepository.Register(binderName, objectToBind, true);
                       
        }
     
        public static void ChooseRobotIDAutoimatically()
        {           
           browser.ExecuteScriptAsyncWhenPageLoaded("document.getElementById('Testing').click();");
        }
        
        public static void InitiateCall()
        {           
            browser.ExecuteScriptAsync("document.getElementById('initiate-call').click();");
        }

        public static void SendReachedLocation()
        {
            browser.ExecuteScriptAsync("document.getElementById('reached-location').click();");
        }

    }

}
