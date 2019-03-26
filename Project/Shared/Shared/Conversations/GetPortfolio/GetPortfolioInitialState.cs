﻿using System;
using log4net;
using Shared.Comms.MailService;
using Shared.Comms.Messages;
using Shared.Conversations.SharedStates;

namespace Shared.Conversations.GetPortfolio
{
    public class GetPortfolioInitialState : ConversationState
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public GetPortfolioInitialState(Conversation conversation) : base(conversation)
        {
        }

        public override ConversationState HandleMessage(Envelope incomingMessage)
        {
            Log.Debug($"{nameof(HandleMessage)} (enter)");

            ConversationState nextState = null;

            switch (incomingMessage.Contents)
            {
                //TODO: Add state-specific message handling here. Given the incoming message,
                //you should set nextState to the next ConversationState expected in the conversation.
                case ErrorMessage m:
                    Log.Error($"Received error message as reply...\n{m.ErrorText}");
                    nextState = new ConversationDoneState(ParentConversation, this);
                    break;
                case PortfolioUpdateMessage m:
                    Log.Debug($"Received portfolio for ...\n{m.PortfolioID}");

                    // TODO: Update portfolio model data.

                    nextState = new ConversationDoneState(ParentConversation, this);
                    break;
                default:
                    Log.Error($"No logic to process incoming message of type {incomingMessage.Contents?.GetType()}. Ignoring message.");
                    break;
            }

            Log.Debug($"{nameof(HandleMessage)} (exit)");
            return nextState;
        }

        public override Envelope Prepare()
        {
            Log.Debug($"{nameof(Prepare)} (enter)");

            var message = MessageFactory.GetMessage<GetPortfolioRequest>(Config.GetInt(Config.CLIENT_PROCESS_NUM), 0);
            message.ConversationID = ParentConversation.Id;
            var env = new Envelope(message, Config.GetString(Config.BROKER_IP), Config.GetInt(Config.BROKER_PORT));
                        
            Log.Debug($"{nameof(Prepare)} (exit)");
            return env;
        }
    }
}
