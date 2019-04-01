﻿using System.Collections;
using System.Net;
using System.Threading;
using Broker;
using Broker.Conversations.GetPortfolio;
using Broker.Conversations.LeaderBoardUpdate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shared;
using Shared.Client;
using Shared.Comms.MailService;
using Shared.Comms.Messages;
using Shared.Conversations;
using Shared.Conversations.SharedStates;
using Shared.MarketStructures;
using Shared.PortfolioResources;

namespace BrokerTest.Conversations
{
    [TestClass]
    [DoNotParallelize]
    public class LeaderboardSendUpdateTest
    {
        private readonly Mock<GetPortfolioReceiveState> mock;

        private readonly ValuatedStock user1VStock = new ValuatedStock()
        {
            Symbol = "U1STK",
            Name = "User 1 stock",
            Open = 1.0F,
            High = 1.0F,
            Low = 1.0F,
            Close = 1.0F,
            Volume = 1
        };

        private ValuatedStock user2VStock = new ValuatedStock()
        {
            Symbol = "U2STK",
            Name = "User 2 stock",
            Open = 2.0F,
            High = 2.0F,
            Low = 2.0F,
            Close = 2.0F,
            Volume = 2
        };

        private readonly ValuatedStock user3VStock = new ValuatedStock()
        {
            Symbol = "U3STK",
            Name = "User 3 stock",
            Open = 3.0F,
            High = 3.0F,
            Low = 3.0F,
            Close = 3.0F,
            Volume = 3
        };

        [TestInitialize]
        public void TestInitialize()
        {
            PostOffice.AddBox("0.0.0.0:0");
            ConversationManager.Start(null);

            //create fake portfolios to populate leaderboard
            PortfolioManager.TryToCreate("port1", "pass1", out Portfolio port);
            port.ModifyAsset(new Asset(user1VStock, 1));
            PortfolioManager.ReleaseLock(port);

            PortfolioManager.TryToCreate("port2", "pass2", out port);
            port.ModifyAsset(new Asset(user2VStock, 1));
            PortfolioManager.ReleaseLock(port);

            PortfolioManager.TryToCreate("port3", "pass3", out port);
            port.ModifyAsset(new Asset(user3VStock, 1));
            PortfolioManager.ReleaseLock(port);

            //clear connected clients (if any leftover from other tests)
            var clients = ClientManager.Clients;
            while (!clients.IsEmpty)
            {
                clients.TryTake(out IPEndPoint someItem);
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ConversationManager.Stop();
            PostOffice.RemoveBox("0.0.0.0:0");

            foreach (Portfolio p in PortfolioManager.Portfolios.Values)
            {
                PortfolioManager.TryToRemove(p.PortfolioID);
            }
            Assert.IsTrue(PortfolioManager.Portfolios.Count == 0);
        }


        [TestMethod]
        public void Succeed()
        {
            var conv = new LeaderBoardUpdateRequestConversation(42);
            SortedList Records = new SortedList();
            IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.Parse("111.11.2.3"), 124);

            ValuatedStock[] day1 = { user1VStock, user2VStock, user3VStock };
            LeaderboardManager.Market = new MarketDay("day-1", day1);

            //setup response message and mock
            var mock = new Mock<LeaderboardSendUpdateState>(clientEndpoint, conv, null) { CallBase = true };
            mock.Setup(st => st.Send())//Pretend message is sent and response comes back...
                .Callback(() =>
                {
                    Records = ((mock.Object as LeaderboardSendUpdateState).OutboundMessage.Contents as UpdateLeaderBoardMessage).Records;
                    var responseMessage = new AckMessage() { ConversationID = conv.Id, MessageID = "responseMessageID1234" };
                    var responseEnv = new Envelope(responseMessage);
                    ConversationManager.ProcessIncomingMessage(responseEnv);
                }).CallBase().Verifiable();

            ////execute test
            conv.SetInitialState(mock.Object as LeaderboardSendUpdateState);
            Assert.IsTrue(ClientManager.TryToAdd(clientEndpoint));

            Assert.IsTrue(conv.CurrentState is LeaderboardSendUpdateState);
            mock.Verify(state => state.Prepare(), Times.Never);
            mock.Verify(state => state.Send(), Times.Never);

            ConversationManager.AddConversation(conv);

            Assert.IsFalse(conv.CurrentState is LeaderboardSendUpdateState);
            Assert.IsTrue(conv.CurrentState is ConversationDoneState);
            mock.Verify(state => state.Prepare(), Times.Once);
            mock.Verify(state => state.Send(), Times.Once);

            Assert.AreEqual("port1", Records.GetValueList()[0]);
            Assert.AreEqual("port2", Records.GetValueList()[1]);
            Assert.AreEqual("port3", Records.GetValueList()[2]);

            //new market day, change stock price, re-update leaderboard

            var conv2 = new LeaderBoardUpdateRequestConversation(42);
            SortedList Records2 = new SortedList();

            user2VStock.Close = 100;
            ValuatedStock[] day2 = { user1VStock, user2VStock, user3VStock };
            LeaderboardManager.Market = new MarketDay("day-2", day1);

            //setup response message and mock
            var mock2 = new Mock<LeaderboardSendUpdateState>(clientEndpoint, conv2, null) { CallBase = true };
            mock2.Setup(st => st.Send())//Pretend message is sent and response comes back...
                .Callback(() =>
                {
                    Records2 = ((mock2.Object as LeaderboardSendUpdateState).OutboundMessage.Contents as UpdateLeaderBoardMessage).Records;
                    var responseMessage = new AckMessage() { ConversationID = conv2.Id, MessageID = "responseMessageID1234" };
                    var responseEnv = new Envelope(responseMessage);
                    ConversationManager.ProcessIncomingMessage(responseEnv);
                }).CallBase().Verifiable();

            ////execute test
            conv2.SetInitialState(mock2.Object as LeaderboardSendUpdateState);

            Assert.IsTrue(conv2.CurrentState is LeaderboardSendUpdateState);
            mock2.Verify(state => state.Prepare(), Times.Never);
            mock2.Verify(state => state.Send(), Times.Never);

            ConversationManager.AddConversation(conv2);

            Assert.IsFalse(conv2.CurrentState is LeaderboardSendUpdateState);
            Assert.IsTrue(conv2.CurrentState is ConversationDoneState);
            mock2.Verify(state => state.Prepare(), Times.Once);
            mock2.Verify(state => state.Send(), Times.Once);

            Assert.AreEqual("port1", Records2.GetValueList()[0]);
            Assert.AreEqual("port3", Records2.GetValueList()[1]);
            Assert.AreEqual("port2", Records2.GetValueList()[2]);

            Assert.IsTrue(ClientManager.TryToRemove(clientEndpoint));
        }

        [TestMethod]
        public void Timeout()
        {
            var conv = new LeaderBoardUpdateRequestConversation(42);
            SortedList Records = new SortedList();
            var clientEndpoint = new IPEndPoint(IPAddress.Parse("111.11.2.3"), 124);

            ValuatedStock[] day1 = { user1VStock, user2VStock, user3VStock };
            LeaderboardManager.Market = new MarketDay("day-1", day1);

            //setup response message and mock
            var mock = new Mock<LeaderboardSendUpdateState>(clientEndpoint, conv, null) { CallBase = true };
            mock.Setup(st => st.Send())//Pretend message is sent and response comes back...
                .Callback(() =>
                {
                    //pretend client never responds, do nothing
                }).CallBase().Verifiable();

            ////execute test
            conv.SetInitialState(mock.Object as LeaderboardSendUpdateState);
            var clients = ClientManager.Clients;
            Assert.IsTrue(ClientManager.TryToAdd(clientEndpoint));

            Assert.IsTrue(conv.CurrentState is LeaderboardSendUpdateState);
            mock.Verify(state => state.Prepare(), Times.Never);
            mock.Verify(state => state.Send(), Times.Never);

            ConversationManager.AddConversation(conv);

            Assert.IsTrue(conv.CurrentState is LeaderboardSendUpdateState);
            mock.Verify(state => state.Prepare(), Times.Once);
            mock.Verify(state => state.Send(), Times.Once);
            mock.Verify(state => state.HandleTimeout(), Times.Never);

            Assert.AreEqual(1, ClientManager.Clients.Count);

            Thread.Sleep((int)(Config.GetInt(Config.DEFAULT_TIMEOUT) * (Config.GetInt(Config.DEFAULT_RETRY_COUNT) + 1) * 1.1));

            Assert.AreEqual(0, ClientManager.Clients.Count);

            Assert.IsFalse(conv.CurrentState is LeaderboardSendUpdateState);
            Assert.IsTrue(conv.CurrentState is ConversationDoneState);
            mock.Verify(state => state.HandleMessage(It.IsAny<Envelope>()), Times.Never);
            mock.Verify(state => state.Prepare(), Times.Once);
            mock.Verify(state => state.Send(), Times.Exactly(3));
            mock.Verify(state => state.HandleTimeout(), Times.Exactly(3));
        }
    }
}