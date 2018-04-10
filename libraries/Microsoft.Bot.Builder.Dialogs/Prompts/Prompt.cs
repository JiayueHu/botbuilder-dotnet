// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Prompts;
using static Microsoft.Bot.Builder.Prompts.PromptValidatorEx;

namespace Microsoft.Bot.Builder.Dialogs
{
    /// <summary>
    /// Basic configuration options supported by all prompts.
    /// </summary>
    public abstract class Prompt<T> : Control<T> where T : PromptResult
    {
        protected readonly PromptValidator<T> _validator;

        public Prompt(PromptValidator<T> validator)
        {
            _validator = validator;
        }

        public override bool HasDialogContinue => true;

        public override bool HasDialogResume => false;

        protected abstract Task OnPrompt(DialogContext dc, PromptOptions options, bool isRetry);

        protected abstract Task<DialogResult<T>> OnRecognize(DialogContext dc, PromptOptions options);

        public override async Task<DialogResult<T>> DialogBegin(DialogContext dc, object dialogArgs)
        {
            var promptOptions = new PromptOptions((IDictionary<string, object>)dialogArgs);

            // Persist options
            var instance = dc.Instance;
            instance.State = promptOptions;

            // Send initial prompt
            await OnPrompt(dc, promptOptions, false);

            return new DialogResult<T> { Active = true };
        }

        public override async Task<DialogResult<T>> DialogContinue(DialogContext dc)
        {
            // Recognize value
            var instance = dc.Instance;
            var recognized = await OnRecognize(dc, (PromptOptions)instance.State);

            if (_validator != null)
            {
                await _validator(dc.Context, recognized.Result);
            }

            if (recognized != null)
            {
                return await dc.End(recognized.Result);
            }
            else if (!dc.Context.Responded)
            {
                await OnPrompt(dc, (PromptOptions)instance.State, true);

                return new DialogResult<T> { Active = true };
            }
            else
            {
                return recognized;
            }
        }
    }
}
