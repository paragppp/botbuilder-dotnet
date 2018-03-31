﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.Core.Extensions.Tests
{
    [TestClass]
    public class Intent_RegExRecognizerTests
    {
        [TestMethod]
        [TestCategory("Intent Recognizers")]
        [TestCategory("RegEx Intent Recognizer")]
        public async Task Regex_RecognizeHelpIntent()
        {
            RegExpRecognizerMiddleware helpRecognizer = new RegExpRecognizerMiddleware()
                .AddIntent("HelpIntent", new Regex("help", RegexOptions.IgnoreCase));

            TestAdapter adapter = new TestAdapter()                
                .Use(helpRecognizer);

            await new TestFlow(adapter, async (context) =>
                {
                    var recognized = context.Services.Get<IRecognizedIntents>();
                    if (recognized.TopIntent.Name == "HelpIntent")
                        await context.SendActivity("You selected HelpIntent");
                })
                .Test("help", "You selected HelpIntent")
                .StartTest();
        }

        [TestMethod]
        [TestCategory("Intent Recognizers")]
        [TestCategory("RegEx Intent Recognizer")]
        public async Task Regex_ExtractEntityGroupsNamedCaptureViaList()
        {
            Regex r = new Regex(@"how (.*) (.*)", RegexOptions.IgnoreCase);
            string input = "How 11111 22222";

            Intent i = RegExpRecognizerMiddleware.Recognize(input, r, new List<string>() { "One", "Two" }, 1.0);
            Assert.IsNotNull(i, "Expected an Intent");
            Assert.IsTrue(i.Entities.Count == 2, "Should match 2 groups");
            Assert.AreEqual(i.Entities[0].Value, "11111");
            Assert.IsTrue(i.Entities[0].GroupName == "One");

            Assert.AreEqual(i.Entities[1].Value, "22222");
            Assert.IsTrue(i.Entities[1].GroupName == "Two");
        }

        [TestMethod]
        [TestCategory("Intent Recognizers")]
        [TestCategory("RegEx Intent Recognizer")]
        public async Task Regex_ExtractEntityGroupsNamedCaptureNoList()
        {
            Regex r = new Regex(@"how (?<One>.*) (?<Two>.*)");
            string input = "how 11111 22222";

            Intent i = RegExpRecognizerMiddleware.Recognize(input, r, 1.0);
            Assert.IsNotNull(i, "Expected an Intent");
            Assert.IsTrue(i.Entities.Count == 2, "Should match 2 groups");
            Assert.AreEqual(i.Entities[0].Value, "11111");
            Assert.IsTrue(i.Entities[0].GroupName == "One");

            Assert.AreEqual(i.Entities[1].Value, "22222");
            Assert.IsTrue(i.Entities[1].GroupName == "Two");
        }


        [TestMethod]
        [TestCategory("Intent Recognizers")]
        [TestCategory("RegEx Intent Recognizer")]
        public async Task Regex_RecognizeIntentViaRegex()
        {
            TestAdapter adapter = new TestAdapter()
                .Use(new RegExpRecognizerMiddleware()
                        .AddIntent("aaaaa", new Regex("a", RegexOptions.IgnoreCase))
                        .AddIntent("bbbbb", new Regex("b", RegexOptions.IgnoreCase))
                );

            await new TestFlow(adapter, async (context) =>
                {
                    var recognized = context.Services.Get<IRecognizedIntents>();

                    if (new Regex("a").IsMatch(context.Activity.Text))
                        await context.SendActivity("aaaa Intent");
                    if (new Regex("b").IsMatch(context.Activity.Text))
                        await context.SendActivity("bbbb Intent");
                })
                .Test("aaaaaaaaa", "aaaa Intent")
                .Test("bbbbbbbbb", "bbbb Intent")
                .StartTest();
        }

        [TestMethod]
        [TestCategory("Intent Recognizers")]
        [TestCategory("RegEx Intent Recognizer")]
        public async Task Regex_RecognizeCancelIntent()
        {
            RegExpRecognizerMiddleware helpRecognizer = new RegExpRecognizerMiddleware()
                .AddIntent("CancelIntent", new Regex("cancel", RegexOptions.IgnoreCase));

            TestAdapter adapter = new TestAdapter()
                .Use(helpRecognizer);
            await new TestFlow(adapter, async (context) =>
                {
                    var recognized = context.Services.Get<IRecognizedIntents>();
                    if (recognized.TopIntent.Name == "CancelIntent")
                        await context.SendActivity("You selected CancelIntent");
                })
                .Test("cancel", "You selected CancelIntent")
                .StartTest();
        }

        [TestMethod]
        [TestCategory("Intent Recognizers")]
        [TestCategory("RegEx Intent Recognizer")]
        public async Task Regex_DoNotRecognizeCancelIntent()
        {
            RegExpRecognizerMiddleware helpRecognizer = new RegExpRecognizerMiddleware()
                .AddIntent("CancelIntent", new Regex("cancel", RegexOptions.IgnoreCase));

            TestAdapter adapter = new TestAdapter()
                .Use(helpRecognizer);

            await new TestFlow(adapter, async (context) =>
                {
                    var recognized = context.Services.Get<IRecognizedIntents>();
                    if (recognized.TopIntent?.Name == "CancelIntent")
                        await context.SendActivity("You selected CancelIntent");
                    else
                        await context.SendActivity("Bot received activity of type message");
                })
                .Test("tacos", "Bot received activity of type message")
                .Test("cancel", "You selected CancelIntent")
                .StartTest();
        }

        [TestMethod]
        [TestCategory("Intent Recognizers")]
        [TestCategory("RegEx Intent Recognizer")]
        public async Task Regex_MultipleIntents()
        {
            RegExpRecognizerMiddleware helpRecognizer = new RegExpRecognizerMiddleware()
                .AddIntent("HelpIntent", new Regex("help", RegexOptions.IgnoreCase))
                .AddIntent("CancelIntent", new Regex("cancel", RegexOptions.IgnoreCase))
                .AddIntent("TacoIntent", new Regex("taco", RegexOptions.IgnoreCase));

            TestAdapter adapter = new TestAdapter()                
                .Use(helpRecognizer);
            await new TestFlow(adapter, async (context) =>
                {
                    var recognized = context.Services.Get<IRecognizedIntents>();
                    if (recognized.TopIntent.Name == "HelpIntent")
                        await context.SendActivity("You selected HelpIntent");
                    else if (recognized.TopIntent.Name == "CancelIntent")
                        await context.SendActivity("You selected CancelIntent");
                    else if (recognized.TopIntent.Name == "TacoIntent")
                        await context.SendActivity("You selected TacoIntent");
                })
                .Send("help").AssertReply("You selected HelpIntent")
                .Send("cancel").AssertReply("You selected CancelIntent")
                .Send("taco").AssertReply("You selected TacoIntent")
                .StartTest();
        }
    }
}
