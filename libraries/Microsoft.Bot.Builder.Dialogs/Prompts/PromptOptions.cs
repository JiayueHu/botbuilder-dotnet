// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Dialogs
{
    public interface PromptOptions
    {
        /// <summary>
        /// (Optional) Initial prompt to send the user. As string.
        /// </summary>
        string PromptString { get; set; }

        /// <summary>
        /// (Optional) Initial prompt to send the user. As Activity.
        /// </summary>
        Activity PromptActivity { get; set; }

        /// <summary>
        /// (Optional) Initial SSML to send the user.
        /// </summary>
        string Speak { get; set; }

        /// <summary>
        /// (Optional) Retry prompt to send the user. As String.
        /// </summary>
        string RetryPromptString { get; set; }

        /// <summary>
        /// (Optional) Retry prompt to send the user. As Activity.
        /// </summary>
        Activity RetryPromptActivity { get; set; }

        /// <summary>
        /// (Optional) Retry SSML to send the user.
        /// </summary>
        string RetrySpeak { get; set; }
    }
}
