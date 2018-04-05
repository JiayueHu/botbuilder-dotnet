// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Dialogs
{
    class Waterfall<C> : IDialog<C>
    {
        public bool HasDialogContinue => true;

        public bool HasDialogResume => true;

        public Task<object> DialogBegin(DialogContext<C> dc, object dialogArgs = null)
        {
            throw new NotImplementedException();
        }

        public Task<object> DialogContinue(DialogContext<C> dc)
        {
            throw new NotImplementedException();
        }

        public Task<object> DialogResume(DialogContext<C> dc, object result = null)
        {
            throw new NotImplementedException();
        }
    }
}
