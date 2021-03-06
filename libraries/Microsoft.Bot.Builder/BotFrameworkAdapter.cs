﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Rest.TransientFaultHandling;

namespace Microsoft.Bot.Builder.Adapters
{
    /// <summary>
    /// A bot adapter that can connect a bot to a service endpoint. 
    /// </summary>
    /// <remarks>The bot adapter encapsulates authentication processes and sends 
    /// activities to and receives activities from the Bot Connector Service. When your 
    /// bot receives an activity, the adapter creates a context object, passes it to your 
    /// bot's application logic, and sends responses back to the user's channel.
    /// <para>Use <see cref="Use(IMiddleware)"/> to add <see cref="IMiddleware"/> objects 
    /// to your adapter’s middleware collection. The adapter processes and directs 
    /// incoming activities in through the bot middleware pipeline to your bot’s logic 
    /// and then back out again. As each activity flows in and out of the bot, each piece 
    /// of middleware can inspect or act upon the activity, both before and after the bot 
    /// logic runs.</para>
    /// </remarks>
    /// <seealso cref="ITurnContext"/>
    /// <seealso cref="IActivity"/>
    /// <seealso cref="IBot"/>
    /// <seealso cref="IMiddleware"/>
    public class BotFrameworkAdapter : BotAdapter
    {
        private readonly ICredentialProvider _credentialProvider;
        private readonly HttpClient _httpClient;
        private readonly RetryPolicy _connectorClientRetryPolicy;
        private Dictionary<string, MicrosoftAppCredentials> _appCredentialMap = new Dictionary<string, MicrosoftAppCredentials>();                

        private const string InvokeReponseKey = "BotFrameworkAdapter.InvokeResponse";

        /// <summary>
        /// Initializes a new instance of the <see cref="BotFrameworkAdapter"/> class,
        /// using a credential provider.
        /// </summary>
        /// <param name="credentialProvider">The credential provider.</param>
        /// <param name="connectorClientRetryPolicy">Retry policy for retrying HTTP operations.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="middleware">The middleware to initially add to the adapter.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="credentialProvider"/> is <c>null</c>.</exception>
        /// <remarks>Use a <see cref="MiddlewareSet"/> object to add multiple middleware
        /// components in the conustructor. Use the <see cref="Use(IMiddleware)"/> method to 
        /// add additional middleware to the adapter after construction.
        /// </remarks>
        public BotFrameworkAdapter(ICredentialProvider credentialProvider, RetryPolicy connectorClientRetryPolicy = null, HttpClient httpClient = null, IMiddleware middleware = null)
        {
            _credentialProvider = credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));
            _httpClient = httpClient ?? new HttpClient();
            _connectorClientRetryPolicy = connectorClientRetryPolicy;

            if (middleware != null)
            {
                this.Use(middleware);
            }
        }

        /// <summary>
        /// Sends a proactive message from the bot to a conversation.
        /// </summary>
        /// <param name="botAppId">The application ID of the bot.</param>
        /// <param name="reference">A reference to the conversation to continue.</param>
        /// <param name="callback">The method to call for the resulting bot turn.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="botAppId"/>, <paramref name="reference"/>, or
        /// <paramref name="callback"/> is <c>null</c>.</exception>
        /// <remarks>Call this method to proactively send a message to a conversation.
        /// Most channels require a user to initaiate a conversation with a bot
        /// before the bot can send activities to the user.
        /// <para>This method registers the following services for the turn.<list type="bullet">
        /// <item><see cref="IIdentity"/> (key = "BotIdentity"), a claims identity for the bot.</item>
        /// <item><see cref="IConnectorClient"/>, the channel connector client to use this turn.</item>
        /// </list></para>
        /// </remarks>
        /// <seealso cref="ProcessActivity(string, Activity, Func{ITurnContext, Task})"/>
        /// <seealso cref="BotAdapter.RunPipeline(ITurnContext, Func{ITurnContext, Task}, System.Threading.CancellationTokenSource)"/>
        public async Task ContinueConversation(string botAppId, ConversationReference reference, Func<ITurnContext, Task> callback)
        {
            if (string.IsNullOrWhiteSpace(botAppId))
                throw new ArgumentNullException(nameof(botAppId));

            if (reference == null)
                throw new ArgumentNullException(nameof(reference));

            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            using (var context = new TurnContext(this, reference.GetPostToBotMessage()))
            {
                // Hand craft Claims Identity.
                var claimsIdentity = new ClaimsIdentity(new List<Claim>
                {
                    // Adding claims for both Emulator and Channel.
                    new Claim(AuthenticationConstants.AudienceClaim, botAppId),
                    new Claim(AuthenticationConstants.AppIdClaim, botAppId)
                });

                context.Services.Add<IIdentity>("BotIdentity", claimsIdentity);
                var connectorClient = await this.CreateConnectorClientAsync(reference.ServiceUrl, claimsIdentity);
                context.Services.Add<IConnectorClient>(connectorClient);
                await RunPipeline(context, callback);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BotFrameworkAdapter"/> class,
        /// using an application ID and secret.
        /// </summary>
        /// <param name="appId">The application ID of the bot.</param>
        /// <param name="appPassword">The application secret for the bot.</param>
        /// <param name="connectorClientRetryPolicy">Retry policy for retrying HTTP operations.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="middleware">The middleware to initially add to the adapter.</param>
        /// <remarks>Use a <see cref="MiddlewareSet"/> object to add multiple middleware
        /// components in the conustructor. Use the <see cref="Use(IMiddleware)"/> method to 
        /// add additional middleware to the adapter after construction.
        /// </remarks>
        public BotFrameworkAdapter(string appId, string appPassword, RetryPolicy connectorClientRetryPolicy = null, HttpClient httpClient = null, IMiddleware middleware = null) 
            : this(new SimpleCredentialProvider(appId, appPassword), connectorClientRetryPolicy, httpClient, middleware)
        {
        }

        /// <summary>
        /// Adds middleware to the adapter's pipeline.
        /// </summary>
        /// <param name="middleware">The middleware to add.</param>
        /// <returns>The updated adapter object.</returns>
        /// <remarks>Middleware is added to the adapter at initialization time.
        /// For each turn, the adapter calls middleware in the order in which you added it.
        /// </remarks>
        public new BotFrameworkAdapter Use(IMiddleware middleware)
        {
            base._middlewareSet.Use(middleware);
            return this;
        }

        /// <summary>
        /// Creates a turn context and runs the middleware pipeline for an incoming activity.
        /// </summary>
        /// <param name="authHeader">The HTTP authentication header of the request.</param>
        /// <param name="activity">The incoming activity.</param>
        /// <param name="callback">The code to run at the end of the adapter's middleware
        /// pipeline.</param>
        /// <returns>A task that represents the work queued to execute. If the activity type
        /// was 'Invoke' and the corresponding key (channelId + activityId) was found
        /// then an InvokeResponse is returned, otherwise null is returned.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="activity"/> is <c>null</c>.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// authentication failed.</exception>
        /// <remarks>Call this method to reactively send a message to a conversation.
        /// <para>This method registers the following services for the turn.<list type="bullet">
        /// <item><see cref="IIdentity"/> (key = "BotIdentity"), a claims identity for the bot.</item>
        /// <item><see cref="IConnectorClient"/>, the channel connector client to use this turn.</item>
        /// </list></para>
        /// </remarks>
        /// <seealso cref="ContinueConversation(string, ConversationReference, Func{ITurnContext, Task})"/>
        /// <seealso cref="BotAdapter.RunPipeline(ITurnContext, Func{ITurnContext, Task}, System.Threading.CancellationTokenSource)"/>
        public async Task<InvokeResponse> ProcessActivity(string authHeader, Activity activity, Func<ITurnContext, Task> callback)
        {
            BotAssert.ActivityNotNull(activity);
            var claimsIdentity =  await JwtTokenValidation.AuthenticateRequest(activity, authHeader, _credentialProvider, _httpClient);

            using (var context = new TurnContext(this, activity))
            {
                context.Services.Add<IIdentity>("BotIdentity", claimsIdentity);
                var connectorClient = await this.CreateConnectorClientAsync(activity.ServiceUrl, claimsIdentity);
                context.Services.Add<IConnectorClient>(connectorClient);
                await base.RunPipeline(context, callback).ConfigureAwait(false);

                // Handle Invoke scenarios, which deviate from the request/response model in that 
                // the Bot will return a specific body and return code. 
                if (activity.Type == ActivityTypes.Invoke)
                {
                    Activity invokeResponse = context.Services.Get<Activity>(InvokeReponseKey);
                    if (invokeResponse == null)
                    {
                        // ToDo: Trace Here           
                        throw new InvalidOperationException("Bot failed to return a valid 'invokeResponse' activity.");
                    }
                    else
                    {
                        return (InvokeResponse)invokeResponse.Value;
                    }
                }

                // For all non-invoke scenarios, the HTTP layers above don't have to mess 
                // withthe Body and return codes. 
                return null;
            }
        }

        /// <summary>
        /// Sends activities to the conversation.
        /// </summary>       
        /// <param name="context">The context object for the turn.</param>
        /// <param name="activities">The activities to send.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the activities are successfully sent, the task result contains
        /// an array of <see cref="ResourceResponse"/> objects containing the IDs that 
        /// the receiving channel assigned to the activities.</remarks>
        /// <seealso cref="ITurnContext.OnSendActivities(SendActivitiesHandler)"/>
        public override async Task<ResourceResponse[]> SendActivities(ITurnContext context, Activity[] activities)
        {
            List<ResourceResponse> responses = new List<ResourceResponse>(); 

            foreach (var activity in activities)
            {
                ResourceResponse response;

                if (activity.Type == ActivityTypesEx.Delay)
                {
                    // The Activity Schema doesn't have a delay type build in, so it's simulated
                    // here in the Bot. This matches the behavior in the Node connector. 
                    int delayMs = (int)activity.Value;
                    await Task.Delay(delayMs).ConfigureAwait(false);

                    // In the case of a Delay, just create a fake one. Match the incoming activityId if it's there. 
                    response = new ResourceResponse(activity.Id ?? string.Empty); 
                }
                else if (activity.Type == "invokeResponse") // Aligning name with Node            
                {                    
                    context.Services.Add<Activity>(InvokeReponseKey, activity); 
                    
                    // In the case of Invoke, just create a fake one. Match the incoming activityId if it's there. 
                    response = new ResourceResponse(activity.Id ?? string.Empty);
                }
                else
                {
                    var connectorClient = context.Services.Get<IConnectorClient>();
                    response = await connectorClient.Conversations.SendToConversationAsync(activity).ConfigureAwait(false);
                }

                // Collect all the responses that come from the service. 
                responses.Add(response); 
            }

            return responses.ToArray();
        }

        /// <summary>
        /// Replaces an existing activity in the conversation.
        /// </summary>
        /// <param name="context">The context object for the turn.</param>
        /// <param name="activity">New replacement activity.</param>        
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the activity is successfully sent, the task result contains
        /// a <see cref="ResourceResponse"/> object containing the ID that the receiving 
        /// channel assigned to the activity.
        /// <para>Before calling this, set the ID of the replacement activity to the ID
        /// of the activity to replace.</para></remarks>
        /// <seealso cref="ITurnContext.OnUpdateActivity(UpdateActivityHandler)"/>
        public override async Task<ResourceResponse> UpdateActivity(ITurnContext context, Activity activity)
        {
            var connectorClient = context.Services.Get<IConnectorClient>();
            return await connectorClient.Conversations.UpdateActivityAsync(activity);
        }

        /// <summary>
        /// Deletes an existing activity in the conversation.
        /// </summary>
        /// <param name="context">The context object for the turn.</param>
        /// <param name="reference">Conversation reference for the activity to delete.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>The <see cref="ConversationReference.ActivityId"/> of the conversation
        /// reference identifies the activity to delete.</remarks>
        /// <seealso cref="ITurnContext.OnDeleteActivity(DeleteActivityHandler)"/>
        public override async Task DeleteActivity(ITurnContext context, ConversationReference reference)
        {
            var connectorClient = context.Services.Get<IConnectorClient>();
            await connectorClient.Conversations.DeleteActivityAsync(reference.Conversation.Id, reference.ActivityId);
        }

        /// <summary>
        /// Creates a conversation on the specified channel.
        /// </summary>
        /// <param name="channelId">The ID for the channel.</param>
        /// <param name="serviceUrl">The channel's service URL endpoint.</param>
        /// <param name="credentials">The application credentials for the bot.</param>
        /// <param name="conversationParameters">The conversation information to use to 
        /// create the conversation.</param>
        /// <param name="callback">The method to call for the resulting bot turn.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>To start a conversation, your bot must know its account information 
        /// and the user's account information on that channel.
        /// Most channels only support initiating a direct message (non-group) conversation.
        /// <para>The adapter attempts to create a new conversation on the channel, and
        /// then sends a <c>conversationUpdate</c> activity through its middleware pipeline
        /// to the <paramref name="callback"/> method.</para>
        /// <para>If the conversation is established with the 
        /// specified users, the ID of the activity's <see cref="IActivity.Conversation"/>
        /// will contain the ID of the new conversation.</para>
        /// </remarks>
        public virtual async Task CreateConversation(string channelId, string serviceUrl, MicrosoftAppCredentials credentials, ConversationParameters conversationParameters, Func<ITurnContext, Task> callback)
        {
            var connectorClient = this.CreateConnectorClient(serviceUrl, credentials);

            var result = await connectorClient.Conversations.CreateConversationAsync(conversationParameters);

            // Create a conversation update activity to represent the result.
            var conversationUpdate = Activity.CreateConversationUpdateActivity();
            conversationUpdate.ChannelId = channelId;
            conversationUpdate.TopicName = conversationParameters.TopicName;
            conversationUpdate.ServiceUrl = serviceUrl;
            conversationUpdate.MembersAdded = conversationParameters.Members;
            conversationUpdate.Id = result.ActivityId ?? Guid.NewGuid().ToString("n");
            conversationUpdate.Conversation = new ConversationAccount(id: result.Id);
            conversationUpdate.Recipient = conversationParameters.Bot;

            using (TurnContext context = new TurnContext(this, (Activity)conversationUpdate))
            {
                await this.RunPipeline(context, callback);
            }
        }

        /// <summary>
        /// Creates the connector client asynchronous.
        /// </summary>
        /// <param name="serviceUrl">The service URL.</param>
        /// <param name="claimsIdentity">The claims identity.</param>
        /// <returns>ConnectorClient instance.</returns>
        /// <exception cref="NotSupportedException">ClaimsIdemtity cannot be null. Pass Anonymous ClaimsIdentity if authentication is turned off.</exception>
        private async Task<IConnectorClient> CreateConnectorClientAsync(string serviceUrl, ClaimsIdentity claimsIdentity)
        {
            if (claimsIdentity == null)
            {
                throw new NotSupportedException("ClaimsIdemtity cannot be null. Pass Anonymous ClaimsIdentity if authentication is turned off.");
            }

            // For requests from channel App Id is in Audience claim of JWT token. For emulator it is in AppId claim. For 
            // unauthenticated requests we have anonymouse identity provided auth is disabled.
            var botAppIdClaim = (claimsIdentity.Claims?.SingleOrDefault(claim => claim.Type == AuthenticationConstants.AudienceClaim)
                ??
                // For Activities coming from Emulator AppId claim contains the Bot's AAD AppId.
                claimsIdentity.Claims?.SingleOrDefault(claim => claim.Type == AuthenticationConstants.AppIdClaim));

            // For anonymous requests (requests with no header) appId is not set in claims.
            if (botAppIdClaim != null)
            {
                string botId = botAppIdClaim.Value;
                var appCredentials = await this.GetAppCredentialsAsync(botId);
                return this.CreateConnectorClient(serviceUrl, appCredentials);
            }
            else
            {
                return this.CreateConnectorClient(serviceUrl);
            }
        }

        /// <summary>
        /// Creates the connector client.
        /// </summary>
        /// <param name="serviceUrl">The service URL.</param>
        /// <param name="appCredentials">The application credentials for the bot.</param>
        /// <returns>Connector client instance.</returns>
        private IConnectorClient CreateConnectorClient(string serviceUrl, MicrosoftAppCredentials appCredentials = null)
        {
            ConnectorClient connectorClient;
            if (appCredentials != null)
            {
                connectorClient = new ConnectorClient(new Uri(serviceUrl), appCredentials);
            }
            else
            {
                connectorClient = new ConnectorClient(new Uri(serviceUrl));
            }

            if (this._connectorClientRetryPolicy != null)
            {
                connectorClient.SetRetryPolicy(this._connectorClientRetryPolicy);
            }

            return connectorClient;
        }

        /// <summary>
        /// Gets the application credentials. App Credentials are cached so as to ensure we are not refreshing
        /// token everytime.
        /// </summary>
        /// <param name="appId">The application identifier (AAD Id for the bot).</param>
        /// <returns>App credentials.</returns>
        private async Task<MicrosoftAppCredentials> GetAppCredentialsAsync(string appId)
        {
            if (appId == null)
            {
                return MicrosoftAppCredentials.Empty;
            }

            if (!_appCredentialMap.TryGetValue(appId, out var appCredentials))
            {
                string appPassword = await _credentialProvider.GetAppPasswordAsync(appId);
                appCredentials = new MicrosoftAppCredentials(appId, appPassword);
                _appCredentialMap[appId] = appCredentials;
            }

            return appCredentials;
        }
    }
}