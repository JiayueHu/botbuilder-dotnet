// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Prompts;
using static Microsoft.Bot.Builder.Prompts.PromptValidatorEx;

namespace Microsoft.Bot.Builder.Dialogs.Prompts
{
    public class DimensionPrompt : NumberWithUnitPrompt
    {
        public DimensionPrompt(string culture, PromptValidator<NumberWithUnit> validator = null)
            : base(new Builder.Prompts.DimensionPrompt(culture, validator), validator)
        {
        }
    }
}
