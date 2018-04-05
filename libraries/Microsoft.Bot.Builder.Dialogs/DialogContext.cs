// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Dialogs
{
    public class DialogContext<C>
    {
        private DialogSet<C> _dialogs;
        private C _context;
        private Stack<DialogInstance> _stack;

        /// <summary>
        /// Creates a new DialogContext instance.
        /// </summary>
        /// <param name="dialogs">Parent dialog set.</param>
        /// <param name="context">Context for the current turn of conversation with the user.</param>
        /// <param name="stack">Current dialog stack.</param>
        public DialogContext(DialogSet<C> dialogs, C context, Stack<DialogInstance> stack)
        {
            _dialogs = dialogs;
            _context = context;
            _stack = stack;
        }

        /// <summary>
        /// Returns the cached instance of the active dialog on the top of the stack or `undefined` if the stack is empty.
        /// </summary>
        public DialogInstance Instance()
        {
            return _stack.LastOrDefault();
        }

        /// <summary>
        /// Pushes a new dialog onto the dialog stack.
        /// </summary>
        /// <param name="dialogId">ID of the dialog to start.</param>
        /// <param name="dialogArgs">(Optional) additional argument(s) to pass to the dialog being started.</param>
        public async Task<DialogResult> Begin(string dialogId, object dialogArgs = null)
        {
            // Lookup dialog
            var dialog = _dialogs.Find(dialogId);
            if (dialog == null)
            {
                throw new Exception($"DialogContext.begin(): A dialog with an id of '{dialogId}' wasn't found.");
            }
            
            // Push new instance onto stack. 
            var instance = new DialogInstance
            {
                Id = dialogId,
                State = new object()
            };

            _stack.Push(instance);

            // Call dialogs begin() method.
            return EnsureDialogResult(await dialog.DialogBegin(this, dialogArgs));
        }

        /// <summary>
        /// Helper function to simplify formatting the options for calling a prompt dialog. This helper will
        /// construct a `PromptOptions` structure and then call[begin(context, dialogId, options)](#begin).
        /// </summary>
        /// <param name="dialogId">ID of the prompt to start.</param>
        /// <param name="prompt">Initial prompt to send the user.</param>
        /// <param name="choicesOrOptions">(Optional) array of choices to prompt the user for or additional prompt options.</param>

        //TODO: TypeScript implementation relies on Choice - need botbuilder-choice
            
        //public static Task<DialogResult> Prompt<O>(string dialogId, Tuple<string, Activity> prompt, Tuple<string, Choice> choicesOrOptions = null)
        //{
        //    throw new NotImplementedException("Helper function to simplify formatting...");
        //}

        /// <summary>
        /// Continues execution of the active dialog, if there is one, by passing the context object to
        /// its `Dialog.continue()` method. You can check `context.responded` after the call completes
        /// to determine if a dialog was run and a reply was sent to the user.
        /// </summary>
        public async Task<DialogResult> Continue()
        {
            // Check for a dialog on the stack
            var instance = Instance();
            if (instance != null)
            {

                // Lookup dialog
                var dialog = _dialogs.Find(instance.Id);
                if (dialog != null)
                {
                    throw new Exception($"DialogSet.continue(): Can't continue dialog. A dialog with an id of '{instance.Id}' wasn't found.");
                }

                // Check for existence of a continue() method
                if (dialog.HasDialogContinue)
                {
                        // Continue execution of dialog
                        return EnsureDialogResult(await dialog.DialogContinue(this));
                }
                else
                {
                    // Just end the dialog
                    return await End();
                }
            }
            else
            {
                return new DialogResult { Active = false };
            }
        }

        /// <summary>
        /// Ends a dialog by popping it off the stack and returns an optional result to the dialogs
        /// parent.The parent dialog is the dialog the started the on being ended via a call to 
        /// either[begin()](#begin) or [prompt()](#prompt). 

        /// The parent dialog will have its `Dialog.resume()` method invoked with any returned
        /// result. If the parent dialog hasn't implemented a `resume()` method then it will be
        /// automatically ended as well and the result passed to its parent. If there are no more
        /// parent dialogs on the stack then processing of the turn will end.
        /// </summary>
        /// @param result (Optional) result to pass to the parent dialogs `Dialog.resume()` method.
        public async Task<DialogResult> End(object result = null)
        {
            // Pop active dialog off the stack
            if (!_stack.Any())
            {
                _stack.Pop();
            }

            // Resume previous dialog
            var instance = Instance();
            if (instance != null)
            {
                // Lookup dialog
                var dialog = _dialogs.Find(instance.Id);
                if (dialog == null)
                {
                    throw new Exception($"DialogContext.end(): Can't resume previous dialog. A dialog with an id of '{instance.Id}' wasn't found.");
                }

                // Check for existence of a resumeDialog() method
                if (dialog.HasDialogResume)
                {
                    // Return result to previous dialog
                    return EnsureDialogResult(await dialog.DialogResume(this, result));
                }
                else
                {
                    // Just end the dialog and pass result to parent dialog
                    return await End(result);
                }
            }
            else
            {
                return new DialogResult
                {
                    Active = false,
                    Result = result
                };
            }
        }

        /// <summary>
        /// Deletes any existing dialog stack thus cancelling all dialogs on the stack.
        /// </summary>
        public DialogContext<C> EndAll()
        {
            // Cancel any active dialogs
            _stack.Clear();
            return this;
        }

        /// <summary>
        /// Ends the active dialog and starts a new dialog in its place. This is particularly useful
        /// for creating loops or redirecting to another dialog.
        /// </summary>
        /// <param name="dialogId">ID of the new dialog to start.</param>
        /// <param name="dialogArgs">(Optional) additional argument(s) to pass to the new dialog.</param>
        public Task<DialogResult> Replace(string dialogId, object dialogArgs = null)
        {
            // Pop stack
            if (_stack.Any())
            {
                _stack.Pop();
            }

            // Start replacement dialog
            return Begin(dialogId, dialogArgs);
        }

        private DialogResult EnsureDialogResult(object result)
        {
            return result is DialogResult ? (DialogResult)result : new DialogResult { Active = _stack.Any() };
        }
    }
}
