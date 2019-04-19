﻿using Broker.Conversations.CreatePortfolio;
using Broker.Conversations.GetPortfolio;
using Broker.Conversations.GetPortfolioResponse;
using Broker.Conversations.LeaderBoardUpdate;
using Broker.Conversations.StockUpdate;
using Broker.Conversations.TransactionRequest;
using log4net;
using Shared;
using Shared.Client;
using Shared.Comms.ComService;
using Shared.Comms.Messages;
using Shared.Conversations;
using Shared.Conversations.SendErrorMessage;
using Shared.Conversations.SharedStates;
using Shared.Conversations.StockStreamRequest.Initiator;
using Shared.MarketStructures;
using Shared.PortfolioResources;
using Shared.Security;
using System;
using System.Threading.Tasks;

namespace Broker
{
    internal class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        private static void Main(string[] args)
        {
            Log.Debug($"{nameof(Main)} (enter)");

            SignatureService.LoadPublicKey("Team1/StockServer");

            //TODO: remove the following 3 dummy portfolio creations
            PortfolioManager.TryToCreate("DevTrader", "password", out Portfolio devPortfolio);
            PortfolioManager.PerformTransaction(devPortfolio.PortfolioID, "AAPL", 60, 0, out devPortfolio, out string error);

            PortfolioManager.TryToCreate("SomeCompetitor", "password", out Portfolio competitorPortfolio);
            PortfolioManager.PerformTransaction(competitorPortfolio.PortfolioID, "AMZN", 60, 0, out competitorPortfolio, out  error);
                        
            PortfolioManager.TryToCreate("Otherguy", "password", out Portfolio otherguyPortfolio);
            PortfolioManager.PerformTransaction(otherguyPortfolio.PortfolioID, "FB", 60, 0, out otherguyPortfolio, out error);

            ConversationManager.Start(ConversationBuilder);
            ComService.AddUdpClient(Config.DEFAULT_UDP_CLIENT, Config.GetInt(Config.BROKER_PORT));

            RequestStockStream();
            PrintMenu();

            var input = Console.ReadLine();
            while (!input.Equals("exit"))
            {
                RequestStockStream();
                PrintMenu();
                input = Console.ReadLine();
            }

            ConversationManager.Stop();

            Log.Debug($"{nameof(Main)} (exit)");
        }

        private static void PrintMenu()
        {
            Console.WriteLine("Input Options:");
            Console.WriteLine("   -\"exit\" to end");
            Console.WriteLine("   -anything else to request a stock stream.");
        }

        public static Conversation ConversationBuilder(Envelope e)
        {
            Conversation conv = null;

            switch (e.Contents)
            {
                case CreatePortfolioRequestMessage m:
                    conv = new CreatePortfoliolResponseConversation(m.ConversationID);
                    conv.SetInitialState(new CreatePortfolioReceiveState(e, conv));
                    break;

                case GetPortfolioRequest m:
                    conv = HandleGetPortfolio(e);
                    break;

                case StockPriceUpdate m:
                    conv = HandleStockPriceUpdate(e);
                    break;

                case TransactionRequestMessage m:
                    conv = new RespondTransactionConversation(e);
                    conv.SetInitialState(new RespondTransaction_InitialState(conv, e));
                    break;
            }
            return conv;
        }

        private static Conversation HandleStockPriceUpdate(Envelope e)
        {
            Conversation conv = null;

            StockPriceUpdate m = e.Contents as StockPriceUpdate;
            Log.Info($"Processing StockPriceUpdate message");
            var sigServ = new SignatureService();
            var bits = Convert.FromBase64String(m.SerializedStockList);
            var StocksList = sigServ.Deserialize<MarketDay>(bits);

            if (!sigServ.VerifySignature(StocksList, m.StockListSignature))
            {
                Log.Error("Stock Price Update signature validation failed. Ignoring message.");
            }
            else
            {
                LeaderboardManager.Market = StocksList;
                Log.Info("Stock Price Update signature verified, updating leaderboard.");
                var t = new Temp();
                t.LogStockDay(StocksList);

                conv = new StockUpdateConversation(m.ConversationID);
                conv.SetInitialState(new ProcessStockUpdateState(e, conv));

                //Send updated leaderboard to clients.
                Task.Run(() =>
                {
                    foreach (var clientIp in ClientManager.Clients.Keys)
                    {
                        var stockUpdateConv = new LeaderBoardUpdateRequestConversation(Config.GetInt(Config.BROKER_PROCESS_NUM));
                        stockUpdateConv.SetInitialState(new LeaderboardSendUpdateState(clientIp, stockUpdateConv, null));
                        ConversationManager.AddConversation(stockUpdateConv);
                    }
                });
            }
            return conv;
        }

        private static Conversation HandleGetPortfolio(Envelope e)
        {
            Conversation conv = null;

            GetPortfolioRequest m = e.Contents as GetPortfolioRequest;

            var user = m.Account.Username;
            var pass = m.Account.Password;

            if (PortfolioManager.TryToGet(user, pass, out Portfolio portfolio))
            {
                conv = new GetPortfoliolResponseConversation(m.ConversationID);
                conv.SetInitialState(new GetPortfolioReceiveState(e, conv));
            }
            else
            {
                conv = new SendErrorMessageConversation(m.ConversationID);
                conv.SetInitialState(new SendErrorMessageState("Invalid login credentials.", e, conv, null, Config.GetInt(Config.BROKER_PROCESS_NUM)));
            }

            return conv;
        }

        private static void RequestStockStream()
        {
            var conv = new ConvI_StockStreamRequest(Config.GetInt(Config.BROKER_PROCESS_NUM));
            conv.SetInitialState(new InitialState_ConvI_StockStreamRequest(conv));
            ConversationManager.AddConversation(conv);
        }
    }
}