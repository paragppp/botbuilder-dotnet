﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text.RegularExpressions;
using AlarmBot.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Core.State;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlarmBot
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_ => Configuration);            
            services.AddBot<AlarmBot>(options =>
            {
                options.CredentialProvider = new SimpleCredentialProvider(Configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value, Configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppPasswordKey)?.Value);
                var middleware = options.Middleware;

                // Add middleware to send an appropriate message to the user if an exception occurs
                middleware.Add(new CatchExceptionMiddleware<Exception>(async (context, exception) =>
                    {
                        await context.SendActivity("Sorry, it looks like something went wrong!");
                    }));

                // Add middleware to send periodic typing activities until the bot responds. The initial
                // delay before sending a typing activity and the frequency of additional activities can also be specified
                middleware.Add(new StateManagerMiddleware()
                                    .AutoLoadAll()
                                    .AutoSaveAll());
                middleware.Add(new ShowTypingMiddleware());
                middleware.Add(new RegExpRecognizerMiddleware()
                                .AddIntent("showAlarms", new Regex("show alarm(?:s)*(.*)", RegexOptions.IgnoreCase))
                                .AddIntent("addAlarm", new Regex("add(?: an)* alarm(.*)", RegexOptions.IgnoreCase))
                                .AddIntent("deleteAlarm", new Regex("delete(?: an)* alarm(.*)", RegexOptions.IgnoreCase))
                                .AddIntent("help", new Regex("help(.*)", RegexOptions.IgnoreCase))
                                .AddIntent("cancel", new Regex("cancel(.*)", RegexOptions.IgnoreCase))
                                .AddIntent("confirmYes", new Regex("(yes|yep|yessir|^y$)", RegexOptions.IgnoreCase))
                                .AddIntent("confirmNo", new Regex("(no|nope|^n$)", RegexOptions.IgnoreCase)));
                            
            });

            services.AddSingleton<IStateStore>(new MemoryStateStore());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseBotFramework();
        }
    }
}
