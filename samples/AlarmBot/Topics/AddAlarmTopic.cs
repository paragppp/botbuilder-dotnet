﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlarmBot.Models;
using AlarmBot.Responses;
using Microsoft.Bot.Builder;
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

            // we asked for title
            TitlePrompt,

            // we asked for time
            TimePrompt,

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
        public Task<bool> StartTopic(AlarmBotContext context)
        {
            var times = context.RecognizedIntents.TopIntent?.Entities.Where(entity => entity.GroupName == "AlarmTime")
                    .Select(entity => DateTimeOffset.Parse((string)entity.Value));
            this.Alarm = new Alarm()
            {
                // initialize from intent entities
                Title = context.RecognizedIntents.TopIntent?.Entities.Where(entity => entity.GroupName == "AlarmTitle")
                    .Select(entity => (string)entity.Value).FirstOrDefault(),

                // initialize from intent entities
                Time = times.Any() ? times.First() : (DateTimeOffset?)null
            };

            return PromptForMissingData(context);
        }


        /// <summary>
        /// we call for every turn while the topic is still active
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<bool> ContinueTopic(AlarmBotContext context)
        {
            // for messages
            if (context.Activity.Type == ActivityTypes.Message)
            {
                switch (context.RecognizedIntents.TopIntent?.Name)
                {
                    case "showAlarms":
                        // allow show alarm to interrupt, but it's one turn, so we show the data without changing the topic
                        await new ShowAlarmsTopic().StartTopic(context);
                        return await this.PromptForMissingData(context);

                    case "help":
                        // show contextual help 
                        await AddAlarmResponses.ReplyWithHelp(context, this.Alarm);
                        return await this.PromptForMissingData(context);

                    case "cancel":
                        // prompt to cancel
                        await AddAlarmResponses.ReplyWithCancelPrompt(context, this.Alarm);
                        this.TopicState = TopicStates.CancelConfirmation;
                        return true;

                    default:
                        return await ProcessTopicState(context);
                }
            }
            return true;
        }

        private async Task<bool> ProcessTopicState(AlarmBotContext context)
        {
            string utterance = (context.Activity.AsMessageActivity().Text ?? "").Trim();

            // we ar eusing TopicState to remember what we last asked
            switch (this.TopicState)
            {
                case TopicStates.TitlePrompt:
                    this.Alarm.Title = utterance;
                    return await this.PromptForMissingData(context);

                case TopicStates.TimePrompt:
                    // take first one in the future if a valid time has been parsed
                    var times = context.GetDateTimes();                    
                    if(times.Any(t => t > DateTimeOffset.Now))
                        this.Alarm.Time = times.FirstOrDefault(t => t > DateTimeOffset.Now);
                    return await this.PromptForMissingData(context);

                case TopicStates.CancelConfirmation:
                    switch (context.RecognizedIntents.TopIntent?.Name)
                    {
                        case "confirmYes":
                            await AddAlarmResponses.ReplyWithTopicCanceled(context);
                            // End current topic
                            return false;

                        case "confirmNo":
                            // Re-prompt user for current field.
                            await AddAlarmResponses.ReplyWithTopicCanceled(context);
                            return await this.PromptForMissingData(context);

                        default:
                            // prompt again to confirm the cancelation
                            await AddAlarmResponses.ReplyWithCancelReprompt(context, this.Alarm);
                            return true;
                    }

                case TopicStates.AddConfirmation:
                    switch (context.RecognizedIntents.TopIntent?.Name)
                    {
                        case "confirmYes":
                            var userStateManager = context.UserState();
                            var userData = await userStateManager.Get<AlarmUserState>() ?? new AlarmUserState();

                            if (userData.Alarms == null)
                            {
                                userData.Alarms = new List<Alarm>();
                            }
                            userData.Alarms.Add(this.Alarm);

                            userStateManager.Set(userData);
                            await userStateManager.SaveChanges();

                            await AddAlarmResponses.ReplyWithAddedAlarm(context, this.Alarm);
                            // end topic
                            return false;

                        case "confirmNo":
                            await AddAlarmResponses.ReplyWithTopicCanceled(context);
                            // End current topic
                            return false;
                        default:
                            return await this.PromptForMissingData(context);
                    }

                default:
                    return await this.PromptForMissingData(context);
            }
        }

        /// <summary>
        /// Called when this topic is resumed after being interrupted
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task<bool> ResumeTopic(AlarmBotContext context)
        {
            // simply prompt again based on our state
            return this.PromptForMissingData(context);
        }

        /// <summary>
        /// Shared method to get missing information
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task<bool> PromptForMissingData(ITurnContext context)
        {
            // If we don't have a title (or if its too long), prompt the user to get it.
            if (String.IsNullOrWhiteSpace(this.Alarm.Title))
            {
                this.TopicState = TopicStates.TitlePrompt;
                await AddAlarmResponses.ReplyWithTitlePrompt(context, this.Alarm);
                return true;
            }
            // if title exists but is not valid, then provide feedback and prompt again
            else if (this.Alarm.Title.Length < 1 || this.Alarm.Title.Length > 100)
            {
                this.Alarm.Title = null;
                this.TopicState = TopicStates.TitlePrompt;
                await AddAlarmResponses.ReplyWithTitleValidationPrompt(context, this.Alarm);
                return true;
            }

            // If we don't have a time, prompt the user to get it.
            if (this.Alarm.Time == null)
            {
                this.TopicState = TopicStates.TimePrompt;
                await AddAlarmResponses.ReplyWithTimePromptFuture(context, this.Alarm);
                return true;
            }
            
            // ask for confirmation that we want to add it
            await AddAlarmResponses.ReplyWithAddConfirmation(context, this.Alarm);
            this.TopicState = TopicStates.AddConfirmation;
            return true;
        }
    }
}
