using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace robot_head
{
    public static class SyncHelper
    {
        private static HubConnection _hubConnection;
        private static IHubProxy _myHub;
        private const string CLIENT_NAME = "WINFORM-ROBOT";
        private const string _baseAddress = "https://robo.sg/";
        //private const string _baseAddress = "https://localhost:44353/";

        private const string SAVE_EVIDENCE_METHOD = "TestSaveImage"; 

        static SyncHelper()
        {
            _hubConnection = new HubConnection(_baseAddress);
            _myHub = _hubConnection.CreateHubProxy("MyHub");

            _hubConnection.Start().Wait();

            _myHub.Invoke("Notify", CLIENT_NAME, _hubConnection.ConnectionId);
        }
            


        public static void SaveEvidenceToServer(string base64StringImage, string violationType)
        {
            try
            {
                
                _myHub.Invoke<string>(SAVE_EVIDENCE_METHOD,
                        base64StringImage, violationType); 
            }
            catch
            {
                Debug.WriteLine("");              
            }
            
        }

        
    }

}
