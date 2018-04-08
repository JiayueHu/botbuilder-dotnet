// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Bot.Builder.Dialogs
{
    /// <summary>
    /// A related set of dialogs that can all call each other.
    /// </summary>
    /// <typeparam name="C">TurnContext</typeparam>
    public class DialogSet
    {
        private IDictionary<string, IDialog> _dialogs;

        public DialogSet()
        {
            _dialogs = new Dictionary<string, IDialog>();
        }

        /// <summary>
        /// Adds a new dialog to the set and returns the added dialog.
        /// </summary>
        public IDialog Add(string dialogId, IDialog dialog)
        {
            if (_dialogs.ContainsKey(dialogId))
            {
                throw new Exception($"DialogSet.add(): A dialog with an id of '{dialogId}' already added.");
            }
            return _dialogs[dialogId] = dialog;
        }

        /// <summary>
        /// Adds a new waterfall to the set and returns the added waterfall.
        /// </summary>
        public Waterfall<T> Add<T>(string dialogId, WaterfallStep<T>[] steps)
        {
            var waterfall = new Waterfall<T>(steps);
            Add(dialogId, waterfall);
            return waterfall;
        }

        public DialogContext CreateContext(ITurnContext context, object state)
        {
            var d = (IDictionary<string, object>)state;
            Stack<DialogInstance> dialogStack;
            object value;
            if (d.TryGetValue("dialogStack", out value))
            {
                dialogStack = (Stack<DialogInstance>)value;
            }
            else
            {
                dialogStack = new Stack<DialogInstance>();
            }
            return new DialogContext(this, context, dialogStack);
        }

        /// <summary>
        /// Finds a dialog that was previously added to the set using [add()](#add).
        /// </summary>
        /// <param name="dialogId">ID of the dialog/prompt to lookup.</param>
        /// <returns>dialog if found otherwise null</returns>
        public IDialog Find(string dialogId)
        {
            IDialog result;
            if (_dialogs.TryGetValue(dialogId, out result))
            {
                return result;
            }
            return null;
        }

        public Dialog<T> Find<T>(string dialogId)
        {
            return (Dialog<T>)Find(dialogId);
        }
    }
}
