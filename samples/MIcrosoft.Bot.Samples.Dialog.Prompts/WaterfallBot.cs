// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Samples.Dialog.Prompts
{
    public class WaterfallBot : IBot
    {
        private DialogSet _dialogs;

        public WaterfallBot()
        {
            _dialogs = new DialogSet();

            _dialogs.Add("waterfall", CreateWaterfall());
        }

        public async Task OnReceiveActivity(ITurnContext turnContext)
        {
            try
            {
                switch (turnContext.Activity.Type)
                {
                    case ActivityTypes.Message:
                        var state = ConversationState<ConversationData>.Get(turnContext);
                        var dc = _dialogs.CreateContext(turnContext, state);

                        // TODO: add code to start and resume waterfalls

                       await turnContext.SendActivity($"DEBUG> waterfall");

                        break;

                    case ActivityTypes.ConversationUpdate:
                        foreach (var newMember in turnContext.Activity.MembersAdded)
                        {
                            if (newMember.Id != turnContext.Activity.Recipient.Id)
                            {
                                await turnContext.SendActivity("Hello and welcome to the waterfall bot.");
                            }
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                await turnContext.SendActivity($"Exception: {e.Message}");
            }
        }

        private WaterfallStep<string>[] CreateWaterfall()
        {
            return new WaterfallStep<string>[] {
                WaterfallStep1,
                WaterfallStep2,
                WaterfallStep3
            };
        }

        private Task<DialogResult<string>> WaterfallStep1(DialogContext dc, object args, SkipStepFunction next)
        {
            dc.Context.SendActivity("step1");

            return Task.FromResult(new DialogResult<string> { });
        }
        private Task<DialogResult<string>> WaterfallStep2(DialogContext dc, object args, SkipStepFunction next)
        {
            dc.Context.SendActivity("step2");

            return Task.FromResult(new DialogResult<string> { });
        }
        private Task<DialogResult<string>> WaterfallStep3(DialogContext dc, object args, SkipStepFunction next)
        {
            dc.Context.SendActivity("step3");

            return Task.FromResult(new DialogResult<string> { });
        }
    }
}
