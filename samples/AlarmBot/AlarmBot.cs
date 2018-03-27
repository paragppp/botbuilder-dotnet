// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using AlarmBot.Models;
using AlarmBot.Topics;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Core.State;

namespace AlarmBot
{
    public class AlarmBot : IBot
    {
        public async Task OnReceiveActivity(ITurnContext turnContext)
        {
            // Get the current ActiveTopic from my persisted conversation state
            var context = new AlarmBotContext(turnContext);

            var handled = false;

            var conversationStateManager = context.ConversationState();
            var conversationData = await conversationStateManager.Get<AlarmTopicState>();

            // if we don't have an active topic yet
            if (conversationData.ActiveTopic == null)
            {
                // use the default topic
                conversationData.ActiveTopic = new DefaultTopic();

                conversationStateManager.Set(conversationData);
                await conversationStateManager.SaveChanges();

                handled = await conversationData.ActiveTopic.StartTopic(context);
            }
            else
            {
                // we do have an active topic, so call it 
                handled = await conversationData.ActiveTopic.ContinueTopic(context);
            }

            // if activeTopic's result is false and the activeTopic is NOT already the default topic
            if (handled == false && !(conversationData.ActiveTopic is DefaultTopic))
            {
                // Use DefaultTopic as the active topic
                conversationData.ActiveTopic = new DefaultTopic();

                conversationStateManager.Set(conversationData);
                await conversationStateManager.SaveChanges();

                await conversationData.ActiveTopic.ResumeTopic(context);
            }
        }
    }
}