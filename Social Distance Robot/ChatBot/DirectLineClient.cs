using Microsoft.Bot.Connector.DirectLine;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;



namespace robot_head
{
    public class DirectLineClient
    {
        private string directLineSecret;
        private string botId;
        private static string fromUser = "User";
        private static string id = "default-user";
        private Conversation conversation;
        Microsoft.Bot.Connector.DirectLine.DirectLineClient client = null;
        string watermark = null;


        public DirectLineClient(string secret, string id)
        {
            this.directLineSecret = secret;
            this.botId = id;
        }


        public void Initialize()
        {

            // connect to directline
            client = new Microsoft.Bot.Connector.DirectLine.DirectLineClient(directLineSecret);
            //if next line shows error, it means no internet / poor connection at start up.
            conversation = client.Conversations.StartConversation();

        }

        public async Task<IEnumerable<Activity>> ReadBotMessagesAsyncDriver()
        {
            return await ReadBotMessagesAsync(client, conversation.ConversationId);
        }

        private async Task<IEnumerable<Activity>> ReadBotMessagesAsync(Microsoft.Bot.Connector.DirectLine.DirectLineClient client, string conversationId)
        {

            var activitySet = await client.Conversations.GetActivitiesAsync(conversationId, watermark).ConfigureAwait(false);
            watermark = activitySet.Watermark;
            var activities = from x in activitySet.Activities
                             where x.From.Id == botId
                             select x;
            return activities;

         
        }

        public async Task PostQuestionToBotAsync(string input)
        {

            Activity userMsg = new Activity
            {
                From = new ChannelAccount(id, fromUser),
                Speak = input,
                Text = input,
                Type = ActivityTypes.Message,
                TextFormat = "plain"
            };

            //send question to base to display 
            await client.Conversations.PostActivityAsync(this.conversation.ConversationId, userMsg);
        }

        public void EndConversion()
        {

            //client = null;
        }

    }
}