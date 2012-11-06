﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Sanguosha.Core.UI;
using Sanguosha.Core.Skills;
using Sanguosha.Core.Players;
using Sanguosha.Core.Games;
using Sanguosha.Core.Triggers;
using Sanguosha.Core.Exceptions;
using Sanguosha.Core.Cards;

namespace Sanguosha.Expansions.Basic.Cards
{

    public class ShanWrapper : CardTransformSkill
    {
        public override VerifierResult TryTransform(List<Card> cards, object arg, out CompositeCard card)
        {
            card = new CompositeCard();
            card.Type = new Shan();
            card.Subcards = new List<Card>(cards);
            return VerifierResult.Success;
        }

        public ShanWrapper(Player p)
        {
            Owner = p;
        }

        protected override void NotifyAction(Player source, List<Player> targets, CompositeCard card)
        {
        }
    }

    public class Sha : CardHandler
    {
        public virtual DamageElement ShaDamageElement
        {
            get { return DamageElement.None; }
        }

        public override void Process(Player source, List<Player> dests, ICard card)
        {
            source[NumberOfShaUsed]++;
            Game.CurrentGame.SortByOrderOfComputation(source, dests);
            base.Process(source, dests, card);
        }

        protected override void Process(Player source, Player dest, ICard card)
        {
            List<Player> sourceList = new List<Player>() { source };
            GameEventArgs args = new GameEventArgs()
            {
                Source = source,
                Targets = new List<Player>() { dest },
                Card = card,
                IntArg = 1,
                IntArg2 = 0
            };
            Game.CurrentGame.Emit(PlayerShaTargetModifier, args);
            int numberOfShanRequired = args.IntArg;
            bool cannotUseShan = args.IntArg2 == 1 ? true : false;
            try
            {
                Game.CurrentGame.Emit(PlayerShaTargetArmorModifier, args);
            }
            catch (TriggerResultException e)
            {
                Trace.Assert(e.Status == TriggerResult.Fail);
                args.Source = source;
                args.Targets = new List<Player>();
                args.Targets.Add(dest);
                args.Card = card;
                Game.CurrentGame.Emit(PlayerShaTargetEnd, args);
                return;
            }
            bool cannotProvideShan = false;
            while (numberOfShanRequired > 0 && !cannotUseShan)
            {
                args.Source = dest;
                args.Targets = sourceList;
                args.Card = new CompositeCard();
                args.Card.Type = new Shan();
                try
                {
                    Game.CurrentGame.Emit(GameEvent.PlayerRequireCard, args);
                }
                catch (TriggerResultException e)
                {
                    if (e.Status == TriggerResult.Success)
                    {
                        Game.CurrentGame.HandleCardPlay(dest, new ShanWrapper(dest), args.Cards, sourceList);
                        numberOfShanRequired--;
                        continue;
                    }
                }
                while (true)
                {
                    IUiProxy ui = Game.CurrentGame.UiProxies[dest];
                    SingleCardUsageVerifier v1 = new SingleCardUsageVerifier((c) => { return c.Type is Shan; });
                    ISkill skill;
                    List<Player> p;
                    List<Card> cards;
                    if (!ui.AskForCardUsage(new CardUsagePrompt("Sha.Shan", source), v1, out skill, out cards, out p))
                    {
                        cannotProvideShan = true;
                        break;
                    }
                    if (!Game.CurrentGame.HandleCardPlay(dest, skill, cards, sourceList))
                    {
                        continue;
                    }
                    break;
                }
                if (cannotProvideShan)
                {
                    break;
                }
                numberOfShanRequired--;
            }
            if (cannotUseShan || numberOfShanRequired > 0)
            {
                Game.CurrentGame.DoDamage(source, dest, 1, ShaDamageElement, card);
            }
            else
            {
                Trace.TraceInformation("Successfully dodged");
                args = new GameEventArgs();
                args.Source = source;
                args.Targets = new List<Player>();
                args.Targets.Add(dest);
                args.Card = card;
                try
                {
                    Game.CurrentGame.Emit(PlayerShaTargetDodged, args);
                }
                catch (TriggerResultException e)
                {
                    if (e.Status == TriggerResult.Retry)
                    {
                        Process(source, dest, card);
                    }
                    Trace.Assert(false);
                }
            }
        }

        public VerifierResult ShaVerifyForJieDaoShaRenOnly(Player source, ICard card, List<Player> targets)
        {
            return Verify(source, card, targets);
        }

        protected override VerifierResult Verify(Player source, ICard card, List<Player> targets)
        {
            if (targets != null && targets.Count > 0)
            {
                ShaEventArgs args = new ShaEventArgs();
                args.Source = source;
                args.Card = card;
                args.RangeApproval = new List<bool>(targets.Count);
                args.TargetApproval = new List<bool>(targets.Count);
                foreach (Player t in targets)
                {
                    if (t == source)
                    {
                        return VerifierResult.Fail;
                    }
                    if (Game.CurrentGame.DistanceTo(source, t) <= source[Player.AttackRange] + 1)
                    {
                        args.RangeApproval.Add(true);
                    }
                    else
                    {
                        args.RangeApproval.Add(false);
                    }
                    args.TargetApproval.Add(false);
                }
                if (source[NumberOfShaUsed] == 0)
                {
                    args.TargetApproval[0] = true;
                }

                try
                {
                    Game.CurrentGame.Emit(PlayerShaTargetValidation, args);
                }
                catch (TriggerResultException)
                {
                    throw new NotImplementedException();
                }

                foreach (bool b in args.TargetApproval.Concat(args.RangeApproval))
                {
                    if (!b)
                    {
                        return VerifierResult.Fail;
                    }
                }
            }
            if (targets == null || targets.Count == 0)
            {
                if (source[NumberOfShaUsed] == 0)
                {
                    return VerifierResult.Partial;
                }
                try
                {
                    GameEventArgs args = new GameEventArgs();
                    args.Source = source;
                    Game.CurrentGame.Emit(PlayerNumberOfShaCheck, args);
                }
                catch (TriggerResultException e)
                {
                    if (e.Status == TriggerResult.Success)
                    {
                        return VerifierResult.Partial;
                    }
                }
                return VerifierResult.Fail;
            }
            return VerifierResult.Success;
        }

        public override CardCategory Category
        {
            get { return CardCategory.Basic; }
        }
        public static PlayerAttribute NumberOfShaUsed = PlayerAttribute.Register("NumberOfShaUsed", true);
        /// <summary>
        /// 玩家使用杀的目标检测
        /// </summary>
        public static readonly GameEvent PlayerShaTargetValidation = new GameEvent("PlayerShaTargetValidation");
        /// <summary>
        /// 是否可以使用杀 
        /// </summary>
        public static readonly GameEvent PlayerNumberOfShaCheck = new GameEvent("PlayerNumberOfShaCheck");
        /// <summary>
        /// 杀目标的修正
        /// </summary>
        public static readonly GameEvent PlayerShaTargetModifier = new GameEvent("PlayerShaTargetModifier");
        /// <summary>
        /// 杀被闪
        /// </summary>
        public static readonly GameEvent PlayerShaTargetDodged = new GameEvent("PlayerShaTargetDodged");
        /// <summary>
        /// 杀结算结束
        /// </summary>
        public static readonly GameEvent PlayerShaTargetEnd = new GameEvent("PlayerShaTargetEnd");
        /// <summary>
        /// 杀目标防具的闪的数目和杀的有效性修正
        /// </summary>
        public static readonly GameEvent PlayerShaTargetArmorModifier = new GameEvent("PlayerShaTargetArmorModifier");
    }


    public class ShaEventArgs : GameEventArgs
    {
        List<bool> rangeApproval;

        public List<bool> RangeApproval
        {
            get { return rangeApproval; }
            set { rangeApproval = value; }
        }

        List<bool> targetApproval;

        public List<bool> TargetApproval
        {
            get { return targetApproval; }
            set { targetApproval = value; }
        }
    }
}
