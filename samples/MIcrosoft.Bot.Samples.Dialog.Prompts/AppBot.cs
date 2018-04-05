using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Samples.Dailog.Prompts
{
    public class AppBot : IBot
    {
        public AppBot() { }

        public async Task OnReceiveActivity(ITurnContext context)
        {
            switch (context.Activity.Type)
            {
                case ActivityTypes.Message:

                    /*
                    // Create dialog context
                    const state = conversationState.get(context);
                    const dc = dialogs.createContext(context, state);

                    // Check for cancel
                    const utterance = (context.activity.text || '').trim().toLowerCase();
                    if (utterance === 'menu' || utterance === 'cancel')
                    {
                        await dc.endAll();
                    }

                    // Continue the current dialog
                    await dc.continue();

                    // Show menu if no response sent
                    if (!context.responded)
                    {
                        await dc.begin('mainMenu');
                    }
                    */

                    //TODO: remove this
                    await context.SendActivity($"TESTING '{context.Activity.Text}'");

                    break;

                case ActivityTypes.ConversationUpdate:
                    foreach (var newMember in context.Activity.MembersAdded)
                    {
                        if (newMember.Id != context.Activity.Recipient.Id)
                        {
                            await context.SendActivity("Hello and welcome to the prompt bot.");
                        }
                    }
                    break;
            }
        }

        //private async Task ProcessActivity(PaymentRequest, res)
    }
}
