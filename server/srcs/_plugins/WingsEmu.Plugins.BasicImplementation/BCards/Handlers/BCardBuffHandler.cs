// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Linq;
using WingsEmu.DTOs.Skills;
using WingsEmu.Game;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Skills;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.BCards.Handlers;

public class BCardBuffHandler : IBCardEffectAsyncHandler
{
    private readonly IBuffFactory _buffFactory;
    private readonly ICardsManager _cards;
    private readonly IGameLanguageService _gameLanguage;
    private readonly IRandomGenerator _randomGenerator;

    public BCardBuffHandler(IRandomGenerator randomGenerator, IBuffFactory buffFactory, IGameLanguageService gameLanguage, ICardsManager cards)
    {
        _randomGenerator = randomGenerator;
        _buffFactory = buffFactory;
        _gameLanguage = gameLanguage;
        _cards = cards;
    }

    public BCardType HandledType => BCardType.Buff;

    public void Execute(IBCardEffectContext ctx)
    {
        IBattleEntity sender = ctx.Sender;
        IBattleEntity target = ctx.Target;

        switch ((AdditionalTypes.Buff)ctx.BCard.SubType)
        {
            case AdditionalTypes.Buff.ChanceCausing:
                if (sender == null)
                {
                    return;
                }

                Buff b = _buffFactory.CreateBuff(ctx.BCard.SecondData, sender);
                if (b == null)
                {
                    return;
                }

                double debuffCounter = target.CheckForResistance(b, _cards, out double buffCounter, out double specializedResistance);

                int randomNumber = _randomGenerator.RandomNumber();
                int debuffRandomNumber = _randomGenerator.RandomNumber();
                int buffRandomNumber = _randomGenerator.RandomNumber();
                int specializedRandomNumber = _randomGenerator.RandomNumber();
                if (b.CardId == (short)BuffVnums.MEMORIAL && sender.BuffComponent.HasBuff((short)BuffVnums.MEMORIAL))
                {
                    return;
                }

                if (randomNumber > ctx.BCard.FirstData)
                {
                    return;
                }

                if (specializedRandomNumber >= (int)(specializedResistance * 100))
                {
                    if (target is not IPlayerEntity c)
                    {
                        return;
                    }

                    string message = _gameLanguage.GetLanguage(GameDialogKey.BUFF_CHATMESSAGE_EFFECT_RESISTANCE, c.Session.UserLanguage);
                    c.Session.SendChatMessage(message, ChatMessageColorType.Buff);
                    return;
                }

                if (ctx.Skill?.Vnum is (short)SkillsVnums.FIRE_MINE or (short)SkillsVnums.BOMB)
                {
                    if (sender.IsSameEntity(target))
                    {
                        return;
                    }
                }

                switch (b.BuffGroup)
                {
                    case BuffGroup.Bad when debuffRandomNumber >= (int)(debuffCounter * 100):
                    {
                        if (target is not IPlayerEntity c)
                        {
                            return;
                        }

                        string message = _gameLanguage.GetLanguage(GameDialogKey.BUFF_CHATMESSAGE_EFFECT_RESISTANCE, c.Session.UserLanguage);
                        c.Session.SendChatMessage(message, ChatMessageColorType.Buff);
                        return;
                    }
                    case BuffGroup.Good when buffRandomNumber >= (int)(buffCounter * 100):
                    {
                        if (target is not IPlayerEntity c)
                        {
                            return;
                        }

                        string message = _gameLanguage.GetLanguage(GameDialogKey.BUFF_CHATMESSAGE_EFFECT_RESISTANCE, c.Session.UserLanguage);
                        c.Session.SendChatMessage(message, ChatMessageColorType.Buff);
                        return;
                    }
                    case BuffGroup.Bad when target is IMonsterEntity monsterEntity:
                        monsterEntity.MapInstance.AddEntityToTargets(monsterEntity, sender);
                        break;
                }

                switch (sender)
                {
                    case IMonsterEntity monster when monster.SummonerId != null && monster.SummonerId == target.Id && monster.SummonerType != null && monster.SummonerType == target.Type:
                        return;
                    case IMateEntity { IsUsingSp: true } mateEntity:
                    {
                        IBattleEntitySkill skill = mateEntity.LastUsedPartnerSkill;
                        if (skill is not PartnerSkill partnerSkill)
                        {
                            return;
                        }

                        int buffVnum = ctx.BCard.SecondData;

                        Buff partnerBuff = _buffFactory.CreateBuff(buffVnum + (buffVnum.IsPartnerRankBuff() ? partnerSkill.Rank - 1 : 0), sender);
                        target.AddBuffAsync(partnerBuff).ConfigureAwait(false).GetAwaiter().GetResult();
                        return;
                    }
                }

                if (target is IMateEntity { IsUsingSp: true } mate)
                {
                    IBattleEntitySkill skill = mate.LastUsedPartnerSkill;

                    int buffVnum = ctx.BCard.SecondData;
                    if (skill != null && skill.Skill.TargetType == TargetType.Self && sender.Id == target.Id && skill is PartnerSkill partnerSkill)
                    {
                        Buff partnerBuff = _buffFactory.CreateBuff(buffVnum + (buffVnum.IsPartnerRankBuff() ? partnerSkill.Rank - 1 : 0), sender);
                        target.AddBuffAsync(partnerBuff).ConfigureAwait(false).GetAwaiter().GetResult();
                        return;
                    }
                }

                if (b.CardId == (int)BuffVnums.SONG_OF_THE_SIRENS && target is IPlayerEntity)
                {
                    Buff sirensBuff = _buffFactory.CreateBuff((int)BuffVnums.SONG_OF_THE_SIRENS_PVP, sender);
                    target.AddBuffAsync(sirensBuff).ConfigureAwait(false).GetAwaiter().GetResult();
                    return;
                }

                Buff buff = _buffFactory.CreateBuff(ctx.BCard.SecondData, sender);
                int firstRandomNumber = _randomGenerator.RandomNumber();
                int secondRandomNumber = _randomGenerator.RandomNumber();
                if (target.BCardComponent.HasBCard(BCardType.TauntSkill, (byte)AdditionalTypes.TauntSkill.ReflectBadEffect) && firstRandomNumber <= secondRandomNumber &&
                    buff.BuffGroup == BuffGroup.Bad)
                {
                    sender.AddBuffAsync(buff).ConfigureAwait(false).GetAwaiter().GetResult();
                    return;
                }

                target.AddBuffAsync(buff).ConfigureAwait(false).GetAwaiter().GetResult();
                break;
            case AdditionalTypes.Buff.ChanceRemoving:
                if (!target.BuffComponent.HasBuff(ctx.BCard.SecondData))
                {
                    return;
                }

                if (_randomGenerator.RandomNumber() > ctx.BCard.FirstData)
                {
                    return;
                }

                Buff chanceRemoving = target.BuffComponent.GetBuff(ctx.BCard.SecondData);
                target.RemoveBuffAsync(false, chanceRemoving).ConfigureAwait(false).GetAwaiter().GetResult();
                break;
            case AdditionalTypes.Buff.CancelGroupOfEffects:

                int firstDataValue = ctx.BCard.FirstDataValue(target.Level);
                int secondDataValue = ctx.BCard.SecondDataValue(target.Level);

                target.RemoveBuffAsync(false,
                    target.BuffComponent.GetAllBuffs().Where(x => x.GroupId == firstDataValue && x.Level <= secondDataValue).ToArray()).ConfigureAwait(false).GetAwaiter().GetResult();

                break;
            case AdditionalTypes.Buff.CounteractPoison:

                firstDataValue = ctx.BCard.FirstDataValue(target.Level);
                secondDataValue = ctx.BCard.SecondDataValue(target.Level);

                if (!Enum.TryParse(firstDataValue.ToString(), out BuffCategory buffCategory))
                {
                    return;
                }

                target.RemoveBuffAsync(false,
                    target.BuffComponent.GetAllBuffs().Where(x => x.BuffCategory == buffCategory && x.Level <= secondDataValue).ToArray()).ConfigureAwait(false).GetAwaiter().GetResult();

                break;
        }
    }
}