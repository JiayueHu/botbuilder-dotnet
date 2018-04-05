// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Dialogs
{
    public class Control<R, C> : IDialog<C>
    {
        public bool HasDialogContinue => true;

        public bool HasDialogResume => true;

        public Task<object> DialogBegin(DialogContext<C> dc, object dialogArgs = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<object> DialogContinue(DialogContext<C> dc)
        {
            throw new System.NotImplementedException();
        }

        public Task<object> DialogResume(DialogContext<C> dc, object result = null)
        {
            throw new System.NotImplementedException();
        }
    }
}
