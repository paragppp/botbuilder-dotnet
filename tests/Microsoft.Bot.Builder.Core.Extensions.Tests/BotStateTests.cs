﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Core.State;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Core.Extensions.Tests
{
    public class TestState
    {
        public string Value { get; set; }
    }

    public class TestPocoState
    {
        public string Value { get; set; }
    }

    [TestClass]
    [TestCategory("State Management")]
    public class BotStateTests
    {
        [TestMethod]
        public async Task State_DoNOTRememberContextState()
        {

            TestAdapter adapter = new TestAdapter();

            await new TestFlow(adapter, async (context) =>
                   {
                       var state = await context.ConversationState().Get<TestState>();
                       Assert.IsNull(state);
                   }
                )
                .Send("set value")
                .StartTest();
        }

        [TestMethod]
        public async Task State_RememberIStoreItemUserState()
        {
            var adapter = new TestAdapter()
                .UseService(, new MemoryStateStore());

            await new TestFlow(adapter,
                    async (context) =>
                    {
                        var userState = await context.UserState().Get<TestState>();
                        Assert.IsNotNull(userState, "user state should exist");
                        switch (context.Activity.Text)
                        {
                            case "set value":
                                userState.Value = "test";
                                await context.SendActivity("value saved");
                                break;
                            case "get value":
                                await context.SendActivity(userState.Value);
                                break;
                        }
                    }
                )
                .Test("set value", "value saved")
                .Test("get value", "test")
                .StartTest();
        }

        [TestMethod]
        public async Task State_RememberPocoUserState()
        {
            var adapter = new TestAdapter()
                .Use(new UserState<TestPocoState>(new MemoryStorage()));
            await new TestFlow(adapter,
                    async (context) =>
                    {
                        var userState = await context.UserState().Get<TestPocoState>();
                        Assert.IsNotNull(userState, "user state should exist");
                        switch (context.Activity.AsMessageActivity().Text)
                        {
                            case "set value":
                                userState.Value = "test";
                                await context.SendActivity("value saved");
                                break;
                            case "get value":
                                await context.SendActivity(userState.Value);
                                break;
                        }
                    }
                )
                .Test("set value", "value saved")
                .Test("get value", "test")
                .StartTest();
        }

        [TestMethod]
        public async Task State_RememberIStoreItemConversationState()
        {
            TestAdapter adapter = new TestAdapter()
                .Use(new ConversationState<TestState>(new MemoryStorage()));
            await new TestFlow(adapter,
                    async (context) =>
                    {
                        var conversationState = await context.ConversationState().Get<TestState>();
                        Assert.IsNotNull(conversationState, "state.conversation should exist");
                        switch (context.Activity.AsMessageActivity().Text)
                        {
                            case "set value":
                                conversationState.Value = "test";
                                await context.SendActivity("value saved");
                                break;
                            case "get value":
                                await context.SendActivity(conversationState.Value);
                                break;
                        }
                    }
                )
                .Test("set value", "value saved")
                .Test("get value", "test")
                .StartTest();
        }

        [TestMethod]
        public async Task State_RememberPocoConversationState()
        {
            TestAdapter adapter = new TestAdapter()
                .Use(new ConversationState<TestPocoState>(new MemoryStorage()));
            await new TestFlow(adapter,
                    async (context) =>
                    {
                        var conversationState = await context.ConversationState().Get<TestPocoState>();
                        Assert.IsNotNull(conversationState, "state.conversation should exist");
                        switch (context.Activity.AsMessageActivity().Text)
                        {
                            case "set value":
                                conversationState.Value = "test";
                                await context.SendActivity("value saved");
                                break;
                            case "get value":
                                await context.SendActivity(conversationState.Value);
                                break;
                        }
                    }
                )
                .Test("set value", "value saved")
                .Test("get value", "test")
                .StartTest();
        }

        [TestMethod]
        public async Task State_CustomStateManagerTest()
        {

            string testGuid = Guid.NewGuid().ToString();
            TestAdapter adapter = new TestAdapter()
                .Use(new CustomKeyState(new MemoryStorage()));
            await new TestFlow(adapter, async (context) =>
                    {
                        var customState = await context.State("custom").Get<CustomState>();
                        switch (context.Activity.AsMessageActivity().Text)
                        {
                            case "set value":
                                customState.CustomString = testGuid;
                                await context.SendActivity("value saved");
                                break;
                            case "get value":
                                await context.SendActivity(customState.CustomString);
                                break;
                        }
                    }
                )
                .Test("set value", "value saved")
                .Test("get value", testGuid.ToString())
                .StartTest();
        }

        public class TypedObject
        {
            public string Name { get; set; }
        }

        [TestMethod]
        public async Task State_RoundTripTypedObject()
        {
            TestAdapter adapter = new TestAdapter()
                .Use(new ConversationState<TypedObject>(new MemoryStorage()));

            await new TestFlow(adapter,
                    async (context) =>
                    {
                        var conversation = await context.ConversationState().Get<TypedObject>();
                        Assert.IsNotNull(conversation, "conversationstate should exist");
                        switch (context.Activity.AsMessageActivity().Text)
                        {
                            case "set value":
                                conversation.Name = "test";
                                await context.SendActivity("value saved");
                                break;
                            case "get value":
                                await context.SendActivity(conversation.GetType().Name);
                                break;
                        }
                    }
                )
                .Test("set value", "value saved")
                .Test("get value", "TypedObject")
                .StartTest();
        }

        public class CustomState
        {
            public string CustomString { get; set; }
        }
    }
}
