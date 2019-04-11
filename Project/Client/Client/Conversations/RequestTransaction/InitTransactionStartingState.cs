﻿using Client.Models;
using log4net;
using Shared;
using Shared.Comms.ComService;
using Shared.Comms.Messages;
using Shared.Conversations;
using Shared.Conversations.SharedStates;
using Shared.PortfolioResources;
using System.Net;

namespace Client.Conversations
{
    public class InitTransactionStartingState : ConversationState
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public InitTransactionStartingState(Conversation conv) : base(conv, null) {
            
        }

        public override ConversationState HandleMessage(Envelope incomingMessage)
        {
            Log.Debug($"{nameof(HandleMessage)} (enter)");

            ConversationState nextState = null;

            switch (incomingMessage.Contents)
            {
                case PortfolioUpdateMessage m:
                    Log.Info($"Received PortfolioUpdate message as reply.");

                    if(TraderModel.Current != null && TraderModel.Current.Portfolio!=null)
                    {
                        var updatedPortfolio = new Portfolio(TraderModel.Current.Portfolio) { Assets = m.Assets };
                        TraderModel.Current.Portfolio = updatedPortfolio;
                    }
                    else
                    {
                        Log.Error("Transaction verified, but no local portfolio to update.");
                    }
                    
                    nextState = new ConversationDoneState(Conversation, this);
                    break;
                case ErrorMessage m:
                    Log.Error($"Received error message as reply...\n{m.ErrorText}");
                    nextState = new ConversationDoneState(Conversation, this);
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

            var m = MessageFactory.GetMessage<TransactionRequestMessage>(Config.GetInt(Config.CLIENT_PROCESS_NUM), 0);
            m.ConversationID = Conversation.Id;

            Envelope env = new Envelope(m, Config.GetString(Config.BROKER_IP), Config.GetInt(Config.BROKER_PORT));

            Log.Debug($"{nameof(Prepare)} (exit)");
            return env;
        }
    }
}
