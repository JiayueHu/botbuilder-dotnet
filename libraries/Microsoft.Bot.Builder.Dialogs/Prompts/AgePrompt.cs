// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Prompts;
using static Microsoft.Bot.Builder.Prompts.PromptValidatorEx;

namespace Microsoft.Bot.Builder.Dialogs.Prompts
{
    public class AgePrompt : NumberWithUnitPrompt
    {
        public AgePrompt(string culture, PromptValidator<NumberWithUnit> validator = null)
            : base(new Builder.Prompts.AgePrompt(culture, validator), validator)
        {
        }
    }
}
