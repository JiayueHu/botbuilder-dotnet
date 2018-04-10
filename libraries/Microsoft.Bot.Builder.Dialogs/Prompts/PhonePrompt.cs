// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Bot.Builder.Prompts;
using static Microsoft.Bot.Builder.Prompts.PromptValidatorEx;

namespace Microsoft.Bot.Builder.Dialogs
{
    public class PhonePrompt : Prompt<TextResult>
    {
        private Builder.Prompts.PhoneNumberPrompt _prompt;

        public PhonePrompt(string culture, PromptValidator<TextResult> validator = null)
            : base(validator)
        {
            _prompt = new Builder.Prompts.PhoneNumberPrompt(culture, validator);
        }

        protected override Task OnPrompt(DialogContext dc, PromptOptions options, bool isRetry)
        {
            if (isRetry)
            {
                if (options.RetryPromptActivity != null)
                {
                    return _prompt.Prompt(dc.Context, options.RetryPromptActivity.AsMessageActivity());
                }
                if (options.RetryPromptString != null)
                {
                    return _prompt.Prompt(dc.Context, options.RetryPromptString, options.RetrySpeak);
                }
            }
            else
            {
                if (options.PromptActivity != null)
                {
                    return _prompt.Prompt(dc.Context, options.PromptActivity);
                }
                if (options.PromptString != null)
                {
                    return _prompt.Prompt(dc.Context, options.PromptString, options.Speak);
                }
            }
            return Task.CompletedTask;
        }

        protected override async Task<DialogResult<TextResult>> OnRecognize(DialogContext dc, PromptOptions options)
        {
            return new DialogResult<TextResult>
            {
                Active = false,
                Result = await _prompt.Recognize(dc.Context)
            };
        }
    }
}
