// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Dialogs
{
    public class CompositeControl<T> : Dialog<T>
    {
        protected DialogSet Dialogs { get; set; }
        protected string DialogId { get; set; }
        protected object DefaultOptions { get; set; }

        public CompositeControl(DialogSet dialogs, string dialogId, object defaultOptions)
        {
            Dialogs = dialogs;
            DialogId = dialogId;
            DefaultOptions = defaultOptions;
        }

        public override bool HasDialogContinue => true;

        public override bool HasDialogResume => false;

        public Task<DialogResult<T>> Begin(TurnContext context, object state, object options)
        {
            var cdc = Dialogs.CreateContext(context, state);
            return cdc.Begin<T>(DialogId, options);
        }

        public Task<DialogResult<T>> Continue(TurnContext context, object state)
        {
            var cdc = Dialogs.CreateContext(context, state);
            return cdc.Continue<T>();
        }

        public override async Task<DialogResult<T>> DialogBegin(DialogContext dc, object dialogArgs = null)
        {
            // Start the controls entry point dialog. 
            var cdc = Dialogs.CreateContext(dc.Context, dc.Instance.State);
            var result = await cdc.Begin<T>(DialogId, dialogArgs);
            // End if the controls dialog ends.
            if (!result.Active)
            {
                return await dc.End(result.Result);
            }
            return result;
        }

        public override async Task<DialogResult<T>> DialogContinue(DialogContext dc)
        {
            // Start the controls entry point dialog. 
            var cdc = Dialogs.CreateContext(dc.Context, dc.Instance.State);
            var result = await cdc.Continue<T>();
            // End if the controls dialog ends.
            if (!result.Active)
            {
                return await dc.End(result.Result);
            }
            return result;
        }
    }
}
