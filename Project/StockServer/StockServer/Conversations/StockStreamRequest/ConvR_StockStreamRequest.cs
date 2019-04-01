﻿using System.Net;
using log4net;
using Shared.Client;
using Shared.Comms.MailService;
using Shared.Conversations;

namespace StockServer.Conversations.StockStreamRequest
{
    public class ConvR_StockStreamRequest : Conversation
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public readonly IPEndPoint ClientIp;

        public ConvR_StockStreamRequest(Envelope e) : base(e.Contents.ConversationID)
        {
            Log.Debug($"{nameof(ConvR_StockStreamRequest)} (enter)");

            ClientIp = e.To;

            //TODO: save endpoint/connection/postbox to use in future stock price update
            ClientManager.TryToAdd(e.To);

            Log.Debug($"{nameof(ConvR_StockStreamRequest)} (exit)");
        }
    }
}