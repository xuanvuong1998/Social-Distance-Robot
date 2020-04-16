using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace robot_head
{
    public static class WebHelper
    {
        private const string BASE_ADDRESS = "http://scout-robot.tech";
        //private const string BASE_ADDRESS = "http://localhost:54297/";
        private const string ACCESS_TOKEN = "1H099XeDsRteM89yy91QonxH3mEd0DoE";

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
                    HttpMethod.Post, "api/ImageApi/SaveEvidence"))
                {
                    req.Content = new StringContent(
                        JsonConvert.SerializeObject(s),
                        Encoding.UTF8,
                        "application/json");
                    await client.SendAsync(req);
                 }
            }
           

        }

        
    }
}
