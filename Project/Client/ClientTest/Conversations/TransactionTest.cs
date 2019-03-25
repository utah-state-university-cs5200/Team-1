﻿using System;
using System.Threading;
using Client.Conversations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shared;
using Shared.Comms.MailService;
using Shared.Comms.Messages;
using Shared.Conversations;
using Shared.Conversations.SharedStates;
using Shared.MarketStructures;

namespace ClientTest.Conversations
{
    [TestClass]
    public class TransactionTest
    {

        [TestInitialize]
        public void TestInitialize()
        {
            PostOffice.AddBox("0.0.0.0:0");
            ConversationManager.Start(null);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ConversationManager.Stop();
            PostOffice.RemoveBox("0.0.0.0:0");
        }


        [TestMethod]
        public void RequestSucceed()
        {
            int portfolioId = 42;
            var testStock = new Stock("TST", "Test Stock");
            var vStock = new ValuatedStock(("1984-02-22,11.0289,11.0822,10.7222,10.7222,197402").Split(','), testStock);
                                 
            var conv = new InitiateTransactionConversation(portfolioId,vStock,1);

            //setup response message and mock
            var mock = new Mock<InitTransactionStartingState>(conv);
            mock.Setup(prep => prep.DoPrepare()).Verifiable();//ensure DoPrepare is called.
            mock.Setup(st => st.HandleMessage(It.IsAny<Envelope>())).CallBase();//Skip mock's HandleMessage override.
            mock.Setup(st => st.Send())//Pretend message is sent and response comes back...
                .Callback(()=> {
                var responseMessage = new PortfolioUpdateMessage() { ConversationID = conv.Id };
                var responseEnv = new Envelope(responseMessage);
                ConversationManager.ProcessIncomingMessage(responseEnv); 
            });
            
            //execute test
            conv.SetInitialState(mock.Object as InitTransactionStartingState);

            Assert.IsTrue(conv.CurrentState is InitTransactionStartingState);
            mock.Verify(state => state.DoPrepare(), Times.Never);
            mock.Verify(state => state.Send(), Times.Never);

            ConversationManager.AddConversation(conv);

            Assert.IsFalse(conv.CurrentState is InitTransactionStartingState);
            Assert.IsTrue(conv.CurrentState is ConversationDoneState);
            mock.Verify(state => state.DoPrepare(), Times.Once);
            mock.Verify(state => state.Send(), Times.Once);
        }

        [TestMethod]
        public void RequestSingleTimeoutThenSucceed()
        {
            int portfolioId = 42;
            var testStock = new Stock("TST", "Test Stock");
            var vStock = new ValuatedStock(("1984-02-22,11.0289,11.0822,10.7222,10.7222,197402").Split(','), testStock);

            var conv = new InitiateTransactionConversation(portfolioId, vStock, 1);
            int requests = 0;

            //setup response message and mock
            var mock = new Mock<InitTransactionStartingState>(conv) { CallBase = true };
            mock.Setup(prep => prep.DoPrepare()).Verifiable();//ensure DoPrepare is called.
            mock.Setup(st => st.HandleMessage(It.IsAny<Envelope>())).CallBase();//Skip mock's HandleMessage override.
            mock.Setup(st => st.Send())//Pretend message is sent and response comes back...
                .Callback(() => {
                    if (++requests > 1)
                    {
                        var responseMessage = new PortfolioUpdateMessage() { ConversationID = conv.Id };
                        var responseEnv = new Envelope(responseMessage);
                        ConversationManager.ProcessIncomingMessage(responseEnv);
                    }
                });

            //execute test
            conv.SetInitialState(mock.Object as InitTransactionStartingState);

            Assert.IsTrue(conv.CurrentState is InitTransactionStartingState);
            mock.Verify(state => state.DoPrepare(), Times.Never);
            mock.Verify(state => state.Send(), Times.Never);

            ConversationManager.AddConversation(conv);

            Assert.IsTrue(conv.CurrentState is InitTransactionStartingState);
            mock.Verify(state => state.DoPrepare(), Times.Once);
            mock.Verify(state => state.Send(), Times.Once);
            mock.Verify(state => state.HandleTimeout(), Times.Never);

            Thread.Sleep((int)(Config.GetInt(Config.DEFAULT_TIMEOUT) * 1.5));

            Assert.IsFalse(conv.CurrentState is InitTransactionStartingState);
            Assert.IsTrue(conv.CurrentState is ConversationDoneState);
            mock.Verify(state => state.DoPrepare(), Times.Once);
            mock.Verify(state => state.Send(), Times.Exactly(2));
            mock.Verify(state => state.HandleTimeout(), Times.Exactly(1));
        }

        [TestMethod]
        public void RequestTimeout()
        {
            int portfolioId = 42;
            var testStock = new Stock("TST", "Test Stock");
            var vStock = new ValuatedStock(("1984-02-22,11.0289,11.0822,10.7222,10.7222,197402").Split(','), testStock);

            var conv = new InitiateTransactionConversation(portfolioId, vStock, 1);

            //setup response message and mock
            var mock = new Mock<InitTransactionStartingState>(conv) { CallBase = true };
            mock.Setup(prep => prep.DoPrepare()).Verifiable();//ensure DoPrepare is called.
            mock.Setup(st => st.HandleMessage(It.IsAny<Envelope>())).CallBase();//Skip mock's HandleMessage override.
            mock.Setup(st => st.Send()).Verifiable();

            //execute test
            conv.SetInitialState(mock.Object as InitTransactionStartingState);

            Assert.IsTrue(conv.CurrentState is InitTransactionStartingState);
            mock.Verify(state => state.DoPrepare(), Times.Never);
            mock.Verify(state => state.Send(), Times.Never);

            ConversationManager.AddConversation(conv);

            Assert.IsTrue(conv.CurrentState is InitTransactionStartingState);
            mock.Verify(state => state.DoPrepare(), Times.Once);
            mock.Verify(state => state.Send(), Times.Once);
            mock.Verify(state => state.HandleTimeout(), Times.Never);

            Thread.Sleep((int)(Config.GetInt(Config.DEFAULT_TIMEOUT) * (Config.GetInt(Config.DEFAULT_RETRY_COUNT)+1)*1.5 ));

            Assert.IsFalse(conv.CurrentState is InitTransactionStartingState);
            Assert.IsTrue(conv.CurrentState is ConversationDoneState);
            mock.Verify(state => state.DoPrepare(), Times.Once);
            mock.Verify(state => state.Send(), Times.Exactly(3));
            mock.Verify(state => state.HandleTimeout(), Times.Exactly(3));
            mock.Verify(state => state.HandleMessage(It.IsAny<Envelope>()), Times.Never);
        }
    }
}