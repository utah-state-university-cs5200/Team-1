﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shared.Comms.Messages;

namespace SharedTest.Messages
{
    [TestClass]
    public class StockStreamRequestMessageTest
    {
        public StockStreamRequestMessageTest()
        {

        }

        [TestMethod]
        public void DefaultConstructorTest()
        {
            var streamRequestMessage = new StockStreamRequestMessage();

            Assert.AreEqual(streamRequestMessage.TicksRequested, 30);
        }

        [TestMethod]
        public void InitializerConstructorTest()
        {
            var streamRequestMessage = new StockStreamRequestMessage
            {
                SourceID = 1,
                ConversationID = "3",
                MessageID = "1"
            };

            Assert.AreEqual(streamRequestMessage.TicksRequested, 30);
            Assert.AreEqual(streamRequestMessage.SourceID, 1);
            Assert.AreEqual(streamRequestMessage.ConversationID, "3");
            Assert.AreEqual(streamRequestMessage.MessageID, "1");
        }

        [TestMethod]
        public void InheritsMessageTest()
        {
            var streamRequestMessage = new StockStreamRequestMessage();

            Assert.IsNull(streamRequestMessage.MessageID);
            Assert.IsNull(streamRequestMessage.ConversationID);
            Assert.AreEqual(streamRequestMessage.SourceID, 0);
        }

        [TestMethod]
        public void SerializerTest()
        {
            var streamRequestMessage = new StockStreamRequestMessage
            {
                SourceID = 1,
                ConversationID = "3",
                MessageID = "1"
            };

            var serializedMessage = MessageFactory.GetMessage(streamRequestMessage.Encode(), false) as StockStreamRequestMessage;

            Assert.AreEqual(streamRequestMessage.TicksRequested, serializedMessage.TicksRequested);
        }
    }
}
