﻿using log4net;
using Shared.Comms.ComService;
using Shared.Conversations.SharedStates;
using System;

namespace Shared.Conversations
{
    public abstract class Conversation
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ConversationState CurrentState { get; private set; }
        public readonly string Id;
        public bool ConversationStarted { get; private set; }
        public DateTime LastUpdateTime { get; private set; }


        /// <summary>
        /// Creates a conversation with a newly generated ConversationId.
        /// </summary>
        public Conversation(long processId)
        {
            Id = ConversationManager.GenerateNextId(processId);
        }

        /// <summary>
        /// Creates a new conversation and assigns it the provided ConversationId.
        /// </summary>
        /// <param name="conversationId"></param>
        public Conversation(string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ArgumentException("Conversation constructor was given an empty ConversationId.");
            }
            else
            {
                Id = conversationId;
            }
        }

        public void SetInitialState(ConversationState state)
        {
            if (CurrentState != null)
            {
                Log.Error("Cannot set initial conversation state more than once.");
            }
            else
            {
                LastUpdateTime = DateTime.Now;
                CurrentState = state;
            }
        }

        public void StartConversation()
        {
            if (ConversationStarted)
            {
                Log.Error("Cannot start conversation more than once.");
            }
            else if (CurrentState == null)
            {
                Log.Error("Cannot start conversation without setting an initial state.");
            }
            else
            {
                CurrentState.DoPrepare();
                CurrentState.Send();
                LastUpdateTime = DateTime.Now;
                ConversationStarted = true;
            }
        }

        public void UpdateState(Envelope incomingEnvelope)
        {
            Log.Debug($"{nameof(UpdateState)} (enter)");

            var nextState = CurrentState?.OnHandleMessage(incomingEnvelope,1);
            if (nextState != null)
            {
                UpdateState(nextState);
            }
            else
            {
                Log.Warn($"Cannot create next {Id} conversation state from message {incomingEnvelope.Contents.MessageID}. Ignoring.");
            }

            Log.Debug($"{nameof(UpdateState)} (exit)");
        }

        public void UpdateState(ConversationState nextState)
        {
            Log.Debug($"{nameof(UpdateState)} (enter)");

            if (CurrentState == null)
            {
                Log.Error($"Conversation {Id} has no current state set. Ignoring message.");
            }
            else
            {
                if (nextState != null)
                {
                    LastUpdateTime = DateTime.Now;
                    CurrentState.Cleanup();
                    CurrentState = nextState;
                    CurrentState.DoPrepare();
                    CurrentState.Send();
                }
                else
                {
                    Log.Warn($"Cannot update conversation to a null state.");
                }
            }

            Log.Debug($"{nameof(UpdateState)} (exit)");
        }

        public void HandleTimeout()
        {
            if (!(CurrentState is ConversationDoneState))
            {
                Log.Warn($"Raising timeout event for Conversation {Id}.");
            }
            LastUpdateTime = DateTime.Now;
            CurrentState.HandleTimeout();
        }
    }
}
