﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlarmBot.Models;
using AlarmBot.Responses;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Core.State;
using Microsoft.Bot.Schema;

namespace AlarmBot.Topics
{
    /// <summary>
    /// Class around topic of adding an alarm
    /// </summary>
    public class AddAlarmTopic : ITopic
    {
        /// <summary>
        /// enumeration of states of the converation
        /// </summary>
        public enum TopicStates
        {
            // initial state
            Started,

            // we showed card
            AddingCard,

            // we asked for confirmation to cancel
            CancelConfirmation,

            // we asked for confirmation to add
            AddConfirmation,

            // we asked for confirmation to show help instead of allowing help as the answer
            HelpConfirmation
        };

        /// <summary>
        /// Alarm object representing the information being gathered by the conversation before it is committed
        /// </summary>
        public Alarm Alarm { get; set; }

        /// <summary>
        /// Current state of the topic conversation
        /// </summary>
        public TopicStates TopicState { get; set; } = TopicStates.Started;

        public string Name { get; set; } = "AddAlarm";

        /// <summary>
        /// Called when the add alarm topic is started
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<bool> StartTopic(ITurnContext context)
        {
            var recognizedIntents = context.Services.Get<IRecognizedIntents>();
            var times = recognizedIntents.TopIntent?.Entities.Where(entity => entity.GroupName == "AlarmTime")
                    .Select(entity => DateTimeOffset.Parse(entity.ValueAs<string>()));

            this.Alarm = new Alarm()
            {
                // initialize from intent entities
                Title = recognizedIntents.TopIntent?.Entities.Where(entity => entity.GroupName == "AlarmTitle")
                    .Select(entity => entity.ValueAs<string>()).FirstOrDefault(),

                // initialize from intent entities
                Time = times.FirstOrDefault(t => t > DateTime.Now)
            };
            if (Alarm.Time == default(DateTimeOffset))
            {
                // use today 1 HOUR as default
                var defaultTime = DateTimeOffset.Now + TimeSpan.FromHours(1);
                Alarm.Time = new DateTimeOffset(defaultTime.Year, defaultTime.Month, defaultTime.Day, defaultTime.Hour, 0, 0, DateTimeOffset.Now.Offset);
            }
            this.TopicState = TopicStates.AddingCard;
            await AddAlarmTopicResponses.ReplyWithStartTopic(context, this.Alarm);
            return true;
        }


        /// <summary>
        /// we call for every turn while the topic is still active
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<bool> ContinueTopic(ITurnContext context)
        {
            var recognizedIntents = context.Services.Get<IRecognizedIntents>();
            // for messages
            if (context.Activity.Type == ActivityTypes.Message)
            {
                switch (recognizedIntents.TopIntent?.Name)
                {
                    case "showAlarms":
                        // allow show alarm to interrupt, but it's one turn, so we show the data without changing the topic
                        await new ShowAlarmsTopic().StartTopic(context);
                        return true;

                    case "help":
                        // show contextual help 
                        await AddAlarmTopicResponses.ReplyWithHelp(context, this.Alarm);
                        return true;

                    case "cancel":
                        // prompt to cancel
                        await AddAlarmTopicResponses.ReplyWithCancelPrompt(context, this.Alarm);
                        this.TopicState = TopicStates.CancelConfirmation;
                        return true;

                    default:
                        return await ProcessTopicState(context);
                }
            }
            return true;
        }

        private async Task<bool> ProcessTopicState(ITurnContext context)
        {
            string utterance = (context.Activity.Text ?? "").Trim();
            var userState = await context.UserState().Get<UserData>();

            // we are using TopicState to remember what we last asked
            switch (this.TopicState)
            {
                case TopicStates.AddingCard:
                    {
                        dynamic payload = context.Activity.Value;
                        if (payload != null)
                        {
                            if (payload.Action == "Submit")
                            {
                                this.Alarm.Title = payload.Title;
                                if (DateTimeOffset.TryParse((string)payload.Day, out DateTimeOffset date))
                                {
                                    if (DateTimeOffset.TryParse((string)payload.Time, out DateTimeOffset time))
                                    {
                                        this.Alarm.Time = new DateTimeOffset(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, date.Offset);
                                        if (userState.Alarms == null)
                                        {
                                            userState.Alarms = new List<Alarm>();
                                        }
                                        userState.Alarms.Add(this.Alarm);
                                        await AddAlarmTopicResponses.ReplyWithAddedAlarm(context, this.Alarm);
                                        // end topic
                                        return false;
                                    }
                                }
                            }
                            else if (payload.Action == "Cancel")
                            {
                                await AddAlarmTopicResponses.ReplyWithTopicCanceled(context, this.Alarm);
                                // End current topic
                                return false;
                            }
                        }
                    }
                    return true;

                case TopicStates.CancelConfirmation:
                    {

                        dynamic payload = context.Activity.Value;
                        switch ((string)payload.Action)
                        {
                            case "Yes":
                                {
                                    await AddAlarmTopicResponses.ReplyWithTopicCanceled(context, this.Alarm);
                                    // End current topic
                                    return false;
                                }
                            case "No":
                                {
                                    this.TopicState = TopicStates.AddingCard;
                                    return await this.ContinueTopic(context);
                                }
                            default:
                                {
                                    await AddAlarmTopicResponses.ReplyWithCancelReprompt(context, this.Alarm);
                                    return true;
                                }
                        }
                    }

                default:
                    return true;
            }
        }

        /// <summary>
        /// Called when this topic is resumed after being interrupted
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<bool> ResumeTopic(ITurnContext context)
        {
            // simply prompt again based on our state
            this.TopicState = TopicStates.AddingCard;
            await AddAlarmTopicResponses.ReplyWithStartTopic(context, this.Alarm);
            return true; 
        }
    }
}
