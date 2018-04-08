// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Dialogs
{
    public class Waterfall<T> : Dialog<T>
    {
        private WaterfallStep<T>[] _steps;

        public Waterfall(WaterfallStep<T>[] steps)
        {
            _steps = steps;
        }

        public override bool HasDialogContinue => true;

        public override bool HasDialogResume => true;

        public override Task<DialogResult<T>> DialogBegin(DialogContext dc, object dialogArgs = null)
        {
            var instance = (WaterfallInstance)dc.Instance;
            instance.Step = 0;
            return RunStep(dc, dialogArgs);
        }

        public override Task<DialogResult<T>> DialogContinue(DialogContext dc)
        {
            var instance = (WaterfallInstance)dc.Instance;
            instance.Step++;
            return RunStep(dc, dc.Context.Activity.Text);
        }

        public override Task<DialogResult<T>> DialogResume(DialogContext dc, T result)
        {
            var instance = (WaterfallInstance)dc.Instance;
            instance.Step++;
            return RunStep(dc, result);
        }

        private Task<DialogResult<T>> RunStep(DialogContext dc, object result = null)
        {
            var instance = (WaterfallInstance)dc.Instance;
            var step = instance.Step;
            if (step >= 0 && step < _steps.Length)
            {
                // Execute step
                return _steps[step](
                    dc,
                    result,
                    (r) => {
                        // Skip to next step
                        instance.Step++;
                        return RunStep(dc, r);
                    });
            }
            else
            {
                // End of waterfall so just return to parent
                return dc.End((T)result);
            }
        }
    }
}
