﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using AlarmBot.Models;
using AlarmBot.Topics;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Core.State;

namespace AlarmBot
{
    public class AlarmBot : IBot
    {
        public async Task OnReceiveActivity(ITurnContext context)
        {
            // Get the current ActiveTopic from my persisted conversation state
            var conversation = await context.ConversationState().Get<ConversationData>();

            var handled = false;
            
            // if we don't have an active topic yet
            if (conversation.ActiveTopic == null)
            {
                // use the default topic
                conversation.ActiveTopic = new DefaultTopic();
                handled = await conversation.ActiveTopic.StartTopic(context);
            }
            else
            {
                // we do have an active topic, so call it 
                handled = await conversation.ActiveTopic.ContinueTopic(context);
            }

            // if activeTopic's result is false and the activeTopic is NOT already the default topic
            if (handled == false && !(conversation.ActiveTopic is DefaultTopic))
            {
                // Use DefaultTopic as the active topic
                conversation.ActiveTopic = new DefaultTopic();
                await conversation.ActiveTopic.ResumeTopic(context);
            }
        }
    }
}