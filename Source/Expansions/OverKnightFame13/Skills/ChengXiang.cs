﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sanguosha.Core.Cards;
using Sanguosha.Core.UI;
using Sanguosha.Core.Skills;
using Sanguosha.Expansions.Battle.Cards;
using Sanguosha.Core.Players;
using Sanguosha.Core.Games;
using System.Diagnostics;
using Sanguosha.Expansions.Basic.Cards;
using Sanguosha.Core.Triggers;
using Sanguosha.Core.Exceptions;

namespace Sanguosha.Expansions.OverKnightFame13.Skills
{
    /// <summary>
    /// 称象-每当你受到一次伤害后，你可以展示所有手牌，若点数之和小于13，你摸一张牌。你可以重复此流程，直至你的所有手牌点数之和等于或大于13为止。
    /// </summary>
    public class ChengXiang : TriggerSkill
    {
        public ChengXiang()
        {
            var trigger = new AutoNotifyPassiveSkillTrigger(
                this,
                (p, e, a) =>
                {
                    List<Card> chengXiangCards = new List<Card>();
                    while (chengXiangCards.Sum(c => c.Rank) < 13 && AskForSkillUse())
                    {
                        NotifySkillUse();
                        Game.CurrentGame.SyncImmutableCardAll(Game.CurrentGame.PeekCard(0));
                        Card card = Game.CurrentGame.PeekCard(0);
                        Game.CurrentGame.DrawCards(p, 1);
                        chengXiangCards.Add(card);
                    }
                },
                TriggerCondition.OwnerIsTarget
            ) { AskForConfirmation = false, IsAutoNotify = false };
            Triggers.Add(GameEvent.AfterDamageInflicted, trigger);
        }
    }
}