// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Dialogs
{
    /// <summary>
    /// Interface of Dialog objects that can be added to a `DialogSet`. The dialog should generally
    /// be a singleton and added to a dialog set using `DialogSet.add()` at which point it will be 
    /// assigned a unique ID.
    /// </summary>
    public interface IDialog<C>
    {
        /// <summary>
        /// Method called when a new dialog has been pushed onto the stack and is being activated.
        /// </summary>
        /// <param name="dc">The dialog context for the current turn of conversation.</param>
        /// <param name="dialogArgs">(Optional) arguments that were passed to the dialog during `begin()` call that started the instance.</param>  
        Task<object> DialogBegin(DialogContext<C> dc, object dialogArgs = null);

        /// <summary>
        /// Indicates whether the class supports DialogContinue
        /// </summary>
        bool HasDialogContinue { get; }

        /// <summary>
        /// (Optional) method called when an instance of the dialog is the "current" dialog and the 
        /// user replies with a new activity. The dialog will generally continue to receive the users 
        /// replies until it calls either `DialogSet.end()` or `DialogSet.begin()`.
        /// If this method is NOT implemented then the dialog will automatically be ended when the user replies.
        /// </summary>
        /// <param name="dc">The dialog context for the current turn of conversation.</param>
        Task<object> DialogContinue(DialogContext<C> dc);

        /// <summary>
        /// Indicates whether the class supports DialogResume
        /// </summary>
        bool HasDialogResume { get; }

        /// <summary>
        /// (Optional) method called when an instance of the dialog is being returned to from another
        /// dialog that was started by the current instance using `DialogSet.begin()`.
        /// If this method is NOT implemented then the dialog will be automatically ended with a call
        /// to `DialogSet.endDialogWithResult()`. Any result passed from the called dialog will be passed
        /// to the current dialogs parent.
        /// </summary>
        /// <param name="dc">The dialog context for the current turn of conversation.</param>
        /// <param name="result">(Optional) value returned from the dialog that was called. The type of the value returned is dependant on the dialog that was called.</param>
        Task<object> DialogResume(DialogContext<C> dc, object result = null);
    }
}
