﻿using Microsoft.AspNet.SignalR.Client;
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
        //private const string _baseAddress = "http://robo-ta.com/";
        private const string _baseAddress = "http://robo.sg/";
        //private const string _baseAddress = "https://localhost:44353/";

        private const string SAVE_EVIDENCE_METHOD = "TestSaveImage"; 

        static SyncHelper()
        {
            _hubConnection = new HubConnection(_baseAddress);
            _myHub = _hubConnection.CreateHubProxy("MyHub");

            //_myHub.On("displayRankings", (rankingsType)
            //    => OnDisplayResult(rankingsType));
            //_myHub.On<List<List<string>>>("updateGroupResult", (results) =>
            //        OnUpdateGroupResult(results));
            //_myHub.On<string>("SendGroupResultToRobot", (groupSub)
            //     => OnUpdateGroupSubmission(groupSub));
            //_myHub.On("RequireGroupChallengeFromRobot", () =>
            //        OnRequireGroupChallengeResults());

            //_syncHub.On<string>("statusChanged", (status) => OnStatusChanged(status));
            //_syncHub.On<RobotCmd>("haveRobotCommands", (command) => OnRobotCommand(command));

            _hubConnection.Start().Wait();

            _myHub.Invoke("Notify", CLIENT_NAME, _hubConnection.ConnectionId);
        }
            
        public static void SaveEvidenceToServer(string base64StringImage)
        {
            _myHub.Invoke<string>(SAVE_EVIDENCE_METHOD, 
                        base64StringImage);
        }

        
    }

}