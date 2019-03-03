﻿using System;
using Shared.Comms.MailService;

namespace Shared.Conversations.SharedStates
{
    public class EndConversationState : ConversationState
    {

        public EndConversationState(string conversationID) : base(conversationID) { }

        public override void OnStateStart()
        {
            ConversationManager.RemoveConversation(ConversationID);
        }

        public override ConversationState GetNextStateFromMessage(Envelope newMessage)
        {
            //This function should never be called for EndConversationState, as the OnStateStart method removes it from the ConversationManager.
            //-Dsphar 2/22/2019
            throw new MethodAccessException("GetNextStateFromMesaage should never be called on an EndConversationState.");
        }

        public override void OnStateEnd()
        {
            //This function should never be called for EndConversationState, as the OnStateStart method removes it from the ConversationManager.
            //-Dsphar 2/22/2019
            throw new MethodAccessException("OnStateEnd should never be called on an EndConversationState.");
        }
    }
}