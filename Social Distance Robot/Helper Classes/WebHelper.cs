using Newtonsoft.Json;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace robot_head
{
    public static class WebHelper 
    { 
        private const string BASE_ADDRESS = "http://robo.sg/";
        //private const string BASE_ADDRESS = "http://robo-ta.com/";
        //private const string BASE_ADDRESS = "http://localhost:54297/";
        private const string ACCESS_TOKEN = "1H099XeDsRteM89yy91QonxH3mEd0DoE";
        private const string SaveEvidenceAPI = "api/ProcessImageAPI/SaveEvidence";
        private const string TestAPI = "api/ProcessImageAPI/DemoGet";
        private const string GETSTATUSAPI = "api/StatusApi/GetLessonStatus";
        private const string CLEARRESULT = "api/ResultsApi/ClearResults";
        private const string TEST2 = "api/ResultsApi/SaveEvidence";
        private const string TEST22 = "api/ProcessImageAPI/TestPost";
            
        private const string CHOOSEN_API = TEST22;

        public static async void GetStatus()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(BASE_ADDRESS);
                var response = await client.GetAsync(TestAPI);

                if (response.IsSuccessStatusCode)
                {
                    var statusJson = await response.Content.ReadAsStringAsync();

                    Debug.WriteLine(statusJson);
                    if (statusJson != null)
                    {
                        statusJson = Regex.Unescape(statusJson);
                        statusJson = statusJson.Substring(1, statusJson.Length - 2);
                    }
                }
            }
        }

        public static async void UpdateStatus()
        {

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(BASE_ADDRESS);

                using (var req = new HttpRequestMessage(HttpMethod.Post, "api/StatusApi/Update"))
                {
                    //req.Content = new StringContent(JsonConvert.SerializeObject(status), Encoding.UTF8, "application/json");
                    //await client.SendAsync(req);
                }
            }

        }
        public static async void DeleteLesson(string lessonName)
        {
            if (lessonName != null)
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(BASE_ADDRESS);
                    using (var req = new HttpRequestMessage(HttpMethod.Post, "api/LessonApi/DeleteLesson"))
                    {
                        req.Content = new StringContent(JsonConvert.SerializeObject(lessonName), Encoding.UTF8, "application/json");
                        await client.SendAsync(req);
                    }
                }
            }
        }

        public static async void SaveEvidenceToServer(string s)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(BASE_ADDRESS);

                using (var req = new HttpRequestMessage(
                    HttpMethod.Post, CHOOSEN_API))
                {
                    req.Content = new StringContent(
                        JsonConvert.SerializeObject(s),
                        Encoding.UTF8,
                        "application/json");

                    try
                    {
                        Debug.WriteLine("Before sending async");
                        var res = await client.SendAsync(req).ConfigureAwait(false);
                        Debug.WriteLine("AFTER SENDING");
                        Debug.WriteLine(res.StatusCode);

                        var respo = await res.Content.ReadAsStringAsync();

                        Debug.WriteLine(respo.Substring(0, 20) + "---" + respo.Length);
                     
                    }
                    catch (Exception ex)
                    {
                        
                        //Debug.WriteLine(ex.Message);
                    }
                }
            }


        }


    }
}
