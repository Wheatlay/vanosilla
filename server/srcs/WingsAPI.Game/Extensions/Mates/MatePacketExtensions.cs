using System;
using System.Collections.Generic;
using System.Linq;
using PhoenixLib.MultiLanguage;
using WingsEmu.DTOs.Maps;
using WingsEmu.DTOs.Skills;
using WingsEmu.Game._i18n;
using WingsEmu.Game._ItemUsage.Configuration;
using WingsEmu.Game.Algorithm;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Items;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Skills;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Game.Extensions.Mates;

public static class MatePacketExtensions
{
    private static ICharacterAlgorithm _algorithm => StaticCharacterAlgorithmService.Instance;

    public static string GenerateMateControl(this IBattleEntity mateEntity) => $"ctl 2 {mateEntity.Id} 3 0";

    public static string GeneratePartnerSkillResetCooldown(this IClientSession session, short slot) => $"psr {slot}";

    public static string GeneratePetSkill(this IMateEntity mateEntity)
    {
        IBattleEntitySkill skill = mateEntity.Skills.FirstOrDefault(sk =>
            sk.Skill.SkillType == SkillType.PartnerSkill && mateEntity.IsTeamMember && mateEntity.MateType == MateType.Pet);
        string packet = "petski ";
        if (skill == null)
        {
            return packet + "-1";
        }

        return packet + $"{skill.Skill.Id.ToString()}";
    }

    public static string GenerateMateSkillResetCooldown(this IClientSession session) => "petsr 0";

    public static string GenerateMateDelayPacket(this IMateEntity mateEntity, int delay, GuriType type, string argument) => $"pdelay {delay} {(byte)type} {argument}";

    public static string GenerateCMode(this IMateEntity mateEntity, short morphId) => $"c_mode 2 {mateEntity.Id} {morphId} 0 0";

    public static string GenerateCond(this IMateEntity mateEntity) => $"cond 2 {mateEntity.Id} {(mateEntity.CanAttack() ? 0 : 1)} {(mateEntity.CanMove() ? 0 : 1)} {mateEntity.Speed}";

    public static string GenerateCond(this INpcEntity npcEntity) => $"cond 2 {npcEntity.Id} {(npcEntity.CanPerformAttack() ? 0 : 1)} {(npcEntity.CanPerformMove() ? 0 : 1)} {npcEntity.Speed}";

    public static string GenerateEInfo(this IMateEntity mateEntity, IGameLanguageService gameLanguage, RegionLanguageType language, ISpPartnerConfiguration spPartnerConfiguration)
    {
        string name;
        if (mateEntity.IsUsingSp && mateEntity.Specialist != null)
        {
            GameDialogKey specialistNameKey = Enum.Parse<GameDialogKey>(spPartnerConfiguration.GetByMorph(mateEntity.Specialist.GameItem.Morph).Name);
            name = gameLanguage.GetLanguage(specialistNameKey, language);
            name = name.Replace(' ', '^');
        }
        else
        {
            name = gameLanguage.GetLanguage(GameDataType.NpcMonster, mateEntity.Name, language);
            name = string.IsNullOrEmpty(mateEntity.MateName) || mateEntity.Name == mateEntity.MateName ? name.Replace(' ', '^') : mateEntity.MateName.Replace(' ', '^');
        }

        return
            "e_info " +
            "10 " +
            $"{mateEntity.NpcMonsterVNum} " +
            $"{mateEntity.Level} " +
            $"{mateEntity.Element} " +
            $"{(byte)mateEntity.AttackType} " +
            $"{mateEntity.ElementRate} " +
            $"{mateEntity.Attack} " +
            $"{mateEntity.StatisticsComponent.MinDamage} " +
            $"{mateEntity.StatisticsComponent.MaxDamage} " +
            $"{mateEntity.StatisticsComponent.HitRate} " +
            $"{mateEntity.StatisticsComponent.CriticalChance} " +
            $"{mateEntity.StatisticsComponent.CriticalDamage} " +
            $"{mateEntity.Defence} " +
            $"{mateEntity.StatisticsComponent.MeleeDefense} " +
            $"{mateEntity.StatisticsComponent.MeleeDodge} " +
            $"{mateEntity.StatisticsComponent.RangeDefense} " +
            $"{mateEntity.StatisticsComponent.RangeDodge} " +
            $"{mateEntity.StatisticsComponent.MagicDefense} " +
            $"{mateEntity.StatisticsComponent.FireResistance} " +
            $"{mateEntity.StatisticsComponent.WaterResistance} " +
            $"{mateEntity.StatisticsComponent.LightResistance} " +
            $"{mateEntity.StatisticsComponent.ShadowResistance} " +
            $"{mateEntity.MaxHp} " +
            $"{mateEntity.MaxMp} " +
            "-1 " +
            $"{name}";
    }

    public static string GetPartnerName(this IMateEntity mateEntity, IGameLanguageService gameLanguage, RegionLanguageType language, ISpPartnerConfiguration config, bool foe = false)
    {
        if (mateEntity.Specialist != null && mateEntity.IsUsingSp & !foe)
        {
            SpPartnerInfo partnerInfo = config?.GetByMorph(mateEntity.Specialist.GameItem.Morph);
            if (partnerInfo != null && Enum.TryParse(partnerInfo.Name, out GameDialogKey key))
            {
                return gameLanguage.GetLanguage(key, language);
            }
        }

        string name = string.IsNullOrEmpty(mateEntity.MateName) || mateEntity.MateName == mateEntity.Name
            ? gameLanguage.GetLanguage(GameDataType.NpcMonster, mateEntity.Name, language)
            : mateEntity.MateName;
        name = name.Replace(' ', '^');
        if (foe)
        {
            name = "!§$%&/()=?*+~#";
        }

        return name;
    }


    public static string GenerateIn(this IMateEntity mateEntity, IGameLanguageService gameLanguage, RegionLanguageType language, ISpPartnerConfiguration config,
        bool foe = false) =>
        mateEntity.GenerateIn(mateEntity.GetPartnerName(gameLanguage, language, config, foe));

    public static string GenerateIn(this IMateEntity mateEntity, string name)
    {
        int faction = 0;
        if (mateEntity.MapInstance != null && mateEntity.MapInstance.HasMapFlag(MapFlags.ACT_4))
        {
            faction = (byte)mateEntity.Owner.Faction + 2;
        }

        if (mateEntity.Specialist != null && mateEntity.IsUsingSp)
        {
            string mateSkills = mateEntity.Specialist.GenerateSkillInfo(0);
            mateSkills = mateSkills.Remove(mateSkills.Length - 1);

            return
                "in " +
                "2 " +
                $"{mateEntity.NpcMonsterVNum} " +
                $"{mateEntity.Id} " +
                $"{mateEntity.PositionX} " +
                $"{mateEntity.PositionY} " +
                $"{mateEntity.Direction} " +
                $"{mateEntity.GetHpPercentage()} " +
                $"{mateEntity.GetMpPercentage()} " +
                "0 " +
                $"{faction} " +
                "3 " +
                $"{mateEntity.CharacterId} " +
                "1 " +
                $"{(mateEntity.IsSitting ? 1 : 0)} " +
                $"{(mateEntity.IsUsingSp ? mateEntity.Specialist.GameItem.Morph : mateEntity.Skin != 0 ? mateEntity.Skin : -1)} " +
                $"{name.Replace(" ", "^")} " +
                "1 " +
                "0 " +
                "1 " +
                $"{mateSkills} " +
                $"{(mateEntity.SkillRankS(0) ? "4237" : "0")} " +
                $"{(mateEntity.SkillRankS(1) ? "4238" : "0")} " +
                $"{(mateEntity.SkillRankS(2) ? "4239" : "0")} " +
                "0 " +
                "0";
        }

        return
            "in " +
            "2 " +
            $"{mateEntity.NpcMonsterVNum} " +
            $"{mateEntity.Id} " +
            $"{mateEntity.PositionX} " +
            $"{mateEntity.PositionY} " +
            $"{mateEntity.Direction} " +
            $"{mateEntity.GetHpPercentage()} " +
            $"{mateEntity.GetMpPercentage()} " +
            "0 " +
            $"{faction} " +
            "3 " +
            $"{mateEntity.CharacterId} " +
            "1 " +
            $"{(mateEntity.IsSitting ? 1 : 0)} " +
            $"{(mateEntity.IsUsingSp && mateEntity.Specialist != null ? mateEntity.Specialist.GameItem.Morph : mateEntity.Skin != 0 ? mateEntity.Skin : -1)} " +
            $"{name} " +
            $"{(byte)mateEntity.MateType + 1} " +
            "-1 " +
            "0 " +
            "0 " +
            "0 " +
            "0 " +
            "0 " +
            "0 " +
            "0 " +
            "0 " +
            "0 " +
            "0 " +
            "0";
    }

    public static string GenerateOut(this IMateEntity mateEntity) => $"out 2 {mateEntity.Id}";

    public static string GeneratePst(this IMateEntity mateEntity)
    {
        IReadOnlyList<Buff> buffs = mateEntity.BuffComponent.GetAllBuffs();
        string buffString = buffs.Aggregate(string.Empty, (current, buff) => current + $"{buff.CardId}.{buff.CasterLevel} ");
        return $"pst 2 {mateEntity.Id} {(int)mateEntity.MateType} {mateEntity.GetHpPercentage()} {mateEntity.GetMpPercentage()} {mateEntity.Hp} {mateEntity.Mp} 0 0 0 {buffString}";
    }

    public static string GeneratePski(this IMateEntity mateEntity)
    {
        if (mateEntity.IsUsingSp && mateEntity.Specialist != null)
        {
            return $"pski {mateEntity.Specialist.GenerateSkillInfo(1)}";
        }

        return "dpski";
    }

    public static string GenerateRc(this IMateEntity mateEntity, int heal) => $"rc 2 {mateEntity.Id} {heal} 0";

    public static string GenerateRest(this IMateEntity mateEntity) => $"rest 2 {mateEntity.Id} {(mateEntity.IsSitting ? 1 : 0)}";

    public static string GenerateSayPacket(this IMateEntity mateEntity, string message, int type) => $"say 2 {mateEntity.Id} 2 {message}";

    public static string GenerateScPacket(this IMateEntity mateEntity, IGameLanguageService gameLanguage, RegionLanguageType language)
    {
        mateEntity.RefreshStatistics();

        string name = string.IsNullOrEmpty(mateEntity.MateName) || mateEntity.Name == mateEntity.MateName
            ? gameLanguage.GetLanguage(GameDataType.NpcMonster, mateEntity.Name, language)
            : mateEntity.MateName;

        if (mateEntity.MateType == MateType.Partner)
        {
            return
                "sc_n " +
                $"{mateEntity.PetSlot} " +
                $"{mateEntity.NpcMonsterVNum} " +
                $"{mateEntity.Id} " +
                $"{mateEntity.Level} " +
                $"{mateEntity.Loyalty} " +
                $"{mateEntity.Experience} " +
                $"{(mateEntity.Weapon != null ? $"{mateEntity.Weapon.ItemVNum}.{mateEntity.Weapon.Rarity}.{mateEntity.Weapon.Upgrade}" : "-1")} " +
                $"{(mateEntity.Armor != null ? $"{mateEntity.Armor.ItemVNum}.{mateEntity.Armor.Rarity}.{mateEntity.Armor.Upgrade}" : "-1")} " +
                $"{(mateEntity.Gloves != null ? $"{mateEntity.Gloves.ItemVNum}.0.0" : "-1")} " +
                $"{(mateEntity.Boots != null ? $"{mateEntity.Boots.ItemVNum}.0.0" : "-1")} " +
                "0 " +
                "0 " +
                $"{(byte)mateEntity.AttackType} " +
                $"{mateEntity.GeneratePartnerEqStats()} " +
                $"{mateEntity.Hp} " +
                $"{mateEntity.MaxHp} " +
                $"{mateEntity.Mp} " +
                $"{mateEntity.MaxMp} " +
                $"{(byte)(mateEntity.IsTeamMember ? 1 : 0)} " +
                $"{_algorithm.GetLevelXp(mateEntity.Level, true, mateEntity.MateType)} " +
                $"{name.Replace(' ', '^')} " +
                $"{(mateEntity.IsUsingSp && mateEntity.Specialist != null ? mateEntity.Specialist.GameItem.Morph : mateEntity.Skin != 0 ? mateEntity.Skin : -1)} " +
                $"{(mateEntity.IsSummonable ? 1 : 0)} " +
                $"{(mateEntity.Specialist != null ? $"{mateEntity.Specialist.ItemVNum}.{mateEntity.Specialist.Agility}" : "-1")} " +
                $"{mateEntity.Specialist.GenerateSkillInfo(2)} " +
                "0 " +
                "0";
        }

        return
            "sc_p " +
            $"{mateEntity.PetSlot} " +
            $"{mateEntity.NpcMonsterVNum} " +
            $"{mateEntity.Id} " +
            $"{mateEntity.Level} " +
            $"{mateEntity.Loyalty} " +
            $"{mateEntity.Experience} " +
            $"{(byte)mateEntity.AttackType} " +
            $"{mateEntity.Attack} " +
            $"{mateEntity.DamagesMinimum} " +
            $"{mateEntity.DamagesMaximum} " +
            $"{mateEntity.HitRate} " +
            $"{mateEntity.HitCriticalChance} " +
            $"{mateEntity.HitCriticalDamage} " +
            $"{mateEntity.Defence} " +
            $"{mateEntity.CloseDefence} " +
            $"{mateEntity.DefenceDodge} " +
            $"{mateEntity.DistanceDefence} " +
            $"{mateEntity.DistanceDodge} " +
            $"{mateEntity.MagicDefence} " +
            $"{mateEntity.Element} " +
            $"{mateEntity.FireResistance} " +
            $"{mateEntity.WaterResistance} " +
            $"{mateEntity.LightResistance} " +
            $"{mateEntity.DarkResistance} " +
            $"{mateEntity.Hp} " +
            $"{mateEntity.MaxHp} " +
            $"{mateEntity.Mp} " +
            $"{mateEntity.MaxMp} " +
            $"{(byte)(mateEntity.IsTeamMember ? 1 : 0)} " +
            $"{_algorithm.GetLevelXp(mateEntity.Level, true, mateEntity.MateType)} " +
            $"{(byte)(mateEntity.CanPickUp ? 1 : 0)} " +
            $"{name.Replace(' ', '^')} " +
            $"{(byte)(mateEntity.IsSummonable ? 1 : 0)} " +
            "0";
    }

    public static string GeneratePartnerEqStats(this IMateEntity mateEntity)
    {
        GameItemInstance mateWeapon = mateEntity.Weapon;
        GameItemInstance mateArmor = mateEntity.Armor;

        mateEntity.Attack = 0;
        mateEntity.Defence = 0;

        if (mateWeapon != null)
        {
            mateEntity.Attack = mateWeapon.Upgrade;
        }

        if (mateArmor != null)
        {
            mateEntity.Defence = mateArmor.Upgrade;
        }

        return
            $"{mateEntity.Attack} " +
            $"{mateEntity.StatisticsComponent.MinDamage} " +
            $"{mateEntity.StatisticsComponent.MaxDamage} " +
            $"{mateEntity.StatisticsComponent.HitRate} " +
            $"{mateEntity.StatisticsComponent.CriticalChance} " +
            $"{mateEntity.StatisticsComponent.CriticalDamage} " +
            $"{mateEntity.Defence} " +
            $"{mateEntity.StatisticsComponent.MeleeDefense} " +
            $"{mateEntity.StatisticsComponent.MeleeDodge} " +
            $"{mateEntity.StatisticsComponent.RangeDefense} " +
            $"{mateEntity.StatisticsComponent.RangeDodge} " +
            $"{mateEntity.StatisticsComponent.MagicDefense} " +
            $"{mateEntity.Element} " +
            $"{mateEntity.StatisticsComponent.FireResistance} " +
            $"{mateEntity.StatisticsComponent.WaterResistance} " +
            $"{mateEntity.StatisticsComponent.LightResistance} " +
            $"{mateEntity.StatisticsComponent.ShadowResistance}";
    }

    public static string GenerateStatInfo(this IMateEntity mateEntity) =>
        $"st 2 {mateEntity.Id} {mateEntity.Level} 0 {mateEntity.GetHpPercentage()} {mateEntity.GetMpPercentage()} {mateEntity.Hp} {mateEntity.Mp}";

    public static string GenerateMateDance(this IMateEntity mateEntity) => $"guri 2 2 {mateEntity.Id}";

    public static string GenerateMateSpCooldown(this IMateEntity mateEntity, short time) => $"psd {time}";

    public static string GenerateRemoveMateSpSkills(this IMateEntity mateEntity) => "dpski";

    public static void SendPetInfo(this IClientSession session, IMateEntity mate, IGameLanguageService gameLanguage) =>
        session.SendPacket(mate.GenerateScPacket(gameLanguage, session.UserLanguage));

    public static void SendCondMate(this IClientSession session, IMateEntity mate) => session.SendPacket(mate.GenerateCond());
    public static void SendCondMate(this IClientSession session, INpcEntity npc) => session.SendPacket(npc.GenerateCond());
    public static void SendOutMate(this IClientSession session, IMateEntity mate) => session.SendPacket(mate.GenerateOut());
    public static void SendMatePskiPacket(this IClientSession session, IMateEntity mate) => session.SendPacket(mate.GeneratePski());
    public static void SendMateSkillPacket(this IClientSession session, IMateEntity mateEntity) => session.SendPacket(mateEntity.GeneratePetSkill());
    public static void SendMateSpCooldown(this IClientSession session, IMateEntity mate, short time) => session.SendPacket(mate.GenerateMateSpCooldown(time));

    public static void SendMateDelay(this IClientSession session, IMateEntity mateEntity, int delay, GuriType type, string argument) =>
        session.SendPacket(mateEntity.GenerateMateDelayPacket(delay, type, argument));

    public static void SendRemoveMateSpSkills(this IClientSession session, IMateEntity mateEntity) => session.SendPacket(mateEntity.GenerateRemoveMateSpSkills());
    public static void SendMateControl(this IClientSession session, IBattleEntity mateEntity) => session.SendPacket(mateEntity.GenerateMateControl());
    public static void SendMateSkillCooldownReset(this IClientSession session) => session.SendPacket(session.GenerateMateSkillResetCooldown());
    public static void SendPartnerSkillCooldown(this IClientSession session, short slot) => session.SendPacket(session.GeneratePartnerSkillResetCooldown(slot));
    public static void SendMateLife(this IClientSession session, IMateEntity mate) => session.SendPacket(mate.GeneratePst());
}