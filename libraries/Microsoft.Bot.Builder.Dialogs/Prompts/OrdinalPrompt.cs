// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Prompts;
using static Microsoft.Bot.Builder.Prompts.PromptValidatorEx;

namespace Microsoft.Bot.Builder.Dialogs.Prompts
{
    public class OrdinalPrompt : NumberPrompt<int>
    {
        public OrdinalPrompt(string culture, PromptValidator<NumberResult<int>> validator = null)
            : base(new Builder.Prompts.OrdinalPrompt(culture, validator), validator)
        {
        }
    }
}
