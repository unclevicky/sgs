﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sanguosha.Core.Cards;
using Sanguosha.Core.Games;
using Sanguosha.Core.Players;
using Sanguosha.Core.Skills;
using Sanguosha.Core.UI;

namespace Sanguosha.Core.Network
{
    public class ServerAsyncUiProxy : IAsyncUiProxy
    {
        public ServerAsyncUiProxy()
        {
        }

        public Player HostPlayer { get; set; }

        private NetworkGamer _gamer;

        public NetworkGamer Gamer 
        {
            get
            {
                return _gamer;
            }
            set
            {
                if (_gamer == value) return;
                if (_gamer != null)
                {
                    _gamer.OnGameDataPacketReceived -= OnGameDataPacketReceived;
                }                
                if (value != null)
                {
                    value.OnGameDataPacketReceived += OnGameDataPacketReceived;
                }
                _gamer = value;
            }
        }

        private void AnswerCardUsage(ISkill skill, List<Card> cards, List<Player> players)
        {
            var handler = CardUsageAnsweredEvent;
            if (handler == null) return;
            handler(skill, cards, players);
        }

        private void AnswerCardChoice(List<List<Card>> cards)
        {
            var handler = CardChoiceAnsweredEvent;
            if (handler == null) return;
            handler(cards);
        }

        private void AnswerMultipleChoice(int choice)
        {
            var handler = MultipleChoiceAnsweredEvent;
            if (handler == null) return;
            handler(choice);
        }

        private void OnGameDataPacketReceived(GameDataPacket packet)
        {
            if (packet is GameResponse)
            {
                if (CurrentQuestionState == QuestionState.None) return;
                Game.CurrentGame.HandCardSwitcher.HandleHandCardMovements();
                var state = CurrentQuestionState;
                CurrentQuestionState = QuestionState.None;
                switch (state)
                {
                    case QuestionState.AskForCardUsage:
                        var response = packet as AskForCardUsageResponse;
                        if (response == null || response.Id != QuestionId)
                        {
                            AnswerCardUsage(null, null, null);
                        }
                        ISkill skill;
                        List<Card> cards;
                        List<Player> players;
                        response.ToAnswer(out skill, out cards, out players);
                        AnswerCardUsage(skill, cards, players);
                        break;
                    case QuestionState.AskForCardChoice:
                        var response2 = packet as AskForCardChoiceResponse;
                        if (response2 == null || response2.Id != QuestionId)
                        {
                            AnswerCardChoice(null);
                        }
                        int opt;
                        var result = response2.ToAnswer(out opt);
                        currentChoiceOptions.OptionResult = opt;
                        AnswerCardChoice(result);
                        break;
                    case QuestionState.AskForMultipleChoice:
                        var response3 = packet as AskForMultipleChoiceResponse;
                        if (response3 == null || response3.Id != QuestionId)
                        {
                            AnswerMultipleChoice(0);
                        }
                        AnswerMultipleChoice(response3.ChoiceIndex);
                        break;
                }
            }
            else if (packet is CardRearrangementNotification)
            {
            }
            else if (packet is HandCardMovementNotification)
            {
                var notif = packet as HandCardMovementNotification;
                Game.CurrentGame.HandCardSwitcher.QueueHandCardMovement(notif);
            }
        }

        
        private enum QuestionState
        {
            None,
            AskForCardUsage,
            AskForCardChoice,
            AskForMultipleChoice
        }

        QuestionState CurrentQuestionState { get; set; }
        public int QuestionId { get; set; }

        public void AskForCardUsage(Prompt prompt, ICardUsageVerifier verifier, int timeOutSeconds)
        {
            CurrentQuestionState = QuestionState.AskForCardUsage;
            Gamer.Receive();
        }

        AdditionalCardChoiceOptions currentChoiceOptions;

        public void AskForCardChoice(Prompt prompt, List<Cards.DeckPlace> sourceDecks, List<string> resultDeckNames, List<int> resultDeckMaximums, ICardChoiceVerifier verifier, int timeOutSeconds, AdditionalCardChoiceOptions options, CardChoiceRearrangeCallback callback)
        {
            currentChoiceOptions = options;
            CurrentQuestionState = QuestionState.AskForCardChoice;
            Gamer.Receive();
        }

        public void AskForMultipleChoice(Prompt prompt, List<OptionPrompt> questions, int timeOutSeconds)
        {
            CurrentQuestionState = QuestionState.AskForMultipleChoice;
            Gamer.Receive();
        }

        public event CardUsageAnsweredEventHandler CardUsageAnsweredEvent;

        public event CardChoiceAnsweredEventHandler CardChoiceAnsweredEvent;

        public event MultipleChoiceAnsweredEventHandler MultipleChoiceAnsweredEvent;

        public void Freeze()
        {
            CurrentQuestionState = QuestionState.None;
        }
    }
}