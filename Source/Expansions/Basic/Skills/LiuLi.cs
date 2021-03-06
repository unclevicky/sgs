﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Sanguosha.Core.Triggers;
using Sanguosha.Core.Cards;
using Sanguosha.Core.UI;
using Sanguosha.Core.Skills;
using Sanguosha.Expansions.Basic.Cards;
using Sanguosha.Core.Games;
using Sanguosha.Core.Players;
using Sanguosha.Core.Exceptions;

namespace Sanguosha.Expansions.Basic.Skills
{
    /// <summary>
    /// 流离-当你成为【杀】的目标时，你可以弃置一张牌将此【杀】转移给你攻击范围内的一名其他角色，该角色不得是此【杀】的使用者。
    /// </summary>
    public class LiuLi : TriggerSkill
    {
        class LiuLiVerifier : CardUsageVerifier
        {
            public override VerifierResult FastVerify(Player source, ISkill skill, List<Card> cards, List<Player> players)
            {
                if (skill != null)
                {
                    return VerifierResult.Fail;
                }
                if (players != null && players.Count > 1)
                {
                    return VerifierResult.Fail;
                }
                if (players != null && players.Count != 0 && (players[0] == source || players[0] == ShaSource))
                {
                    return VerifierResult.Fail;
                }
                if (cards != null && cards.Count > 1)
                {
                    return VerifierResult.Fail;
                }
                if ((cards == null || cards.Count == 0) && players != null && players.Count > 0)
                {
                    // if we have players but we don't have cards. it's not allowed
                    return VerifierResult.Fail;
                }
                if (cards == null || cards.Count == 0)
                {
                    return VerifierResult.Partial;
                }
                if (!Game.CurrentGame.PlayerCanDiscardCard(source, cards[0]))
                {
                    return VerifierResult.Fail;
                }
                CardHandler handler = new Sha();
                handler.HoldInTemp(cards);
                if (players != null && players.Count != 0 && Game.CurrentGame.DistanceTo(source, players[0]) > source[Player.AttackRange] + 1)
                {
                    handler.ReleaseHoldInTemp();
                    return VerifierResult.Fail;
                }
                handler.ReleaseHoldInTemp();
                if (players == null || players.Count == 0)
                {
                    return VerifierResult.Partial;
                }
                return VerifierResult.Success;
            }

            public override IList<CardHandler> AcceptableCardTypes
            {
                get { return null; }
            }

            Player ShaSource { get; set; }
            public LiuLiVerifier(Player p)
            {
                ShaSource = p;
            }
        }

        public void OnPlayerIsCardTarget(Player owner, GameEvent gameEvent, GameEventArgs eventArgs)
        {
            if (!(eventArgs.ReadonlyCard.Type is Sha) || Game.CurrentGame.AlivePlayers.Count <= 2 && eventArgs.Source != owner)
            {
                return;
            }
            ISkill skill;
            List<Card> cards;
            List<Player> players;
            if (Game.CurrentGame.UiProxies[owner].AskForCardUsage(new CardUsagePrompt("LiuLi"), new LiuLiVerifier(eventArgs.Source), out skill, out cards, out players))
            {
                NotifySkillUse(players);
                Game.CurrentGame.HandleCardDiscard(owner, cards);
                int index = eventArgs.Targets.IndexOf(owner);
                eventArgs.Targets.Remove(owner);

                GameEventArgs newArgs = new GameEventArgs();
                newArgs.Source = eventArgs.Source;
                newArgs.UiTargets = players;
                newArgs.Targets = players;
                newArgs.Card = eventArgs.Card;
                newArgs.ReadonlyCard = eventArgs.ReadonlyCard;
                newArgs.InResponseTo = eventArgs.InResponseTo;
                Game.CurrentGame.Emit(GameEvent.CardUsageTargetConfirming, newArgs);

                eventArgs.Targets.Insert(index, newArgs.Targets[0]);
            }
        }

        public LiuLi()
        {
            var trigger = new RelayTrigger(
                OnPlayerIsCardTarget,
                TriggerCondition.OwnerIsTarget
            );
            Triggers.Add(GameEvent.CardUsageTargetConfirming, trigger);
            IsAutoInvoked = null;
        }
    }
}
