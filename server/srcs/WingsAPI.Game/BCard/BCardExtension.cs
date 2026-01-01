using WingsAPI.Packets.Enums.Shells;
using WingsEmu.DTOs.BCards;
using WingsEmu.Game._enum;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Game.Buffs;

public static class BCardExtension
{
    public static int FirstDataValue(this BCardDTO bCard, int senderLevel)
    {
        int firstDataValue = bCard.FirstDataScalingType switch
        {
            BCardScalingType.NORMAL_VALUE => bCard.FirstData,
            BCardScalingType.LEVEL_MULTIPLIED => senderLevel * bCard.FirstData,
            BCardScalingType.LEVEL_DIVIDED => bCard.FirstData == 0 ? 0 : senderLevel / bCard.FirstData
        };

        return firstDataValue;
    }

    public static int SecondDataValue(this BCardDTO bCard, int senderLevel)
    {
        int secondDataValue = bCard.SecondDataScalingType switch
        {
            BCardScalingType.NORMAL_VALUE => bCard.SecondData,
            BCardScalingType.LEVEL_MULTIPLIED => senderLevel * bCard.SecondData,
            BCardScalingType.LEVEL_DIVIDED => bCard.SecondData == 0 ? 0 : senderLevel / bCard.SecondData
        };

        return secondDataValue;
    }

    public static BCardDTO TryCreateBuffBCard(this ShellEffectType type, int value)
    {
        int? buffVnum = type switch
        {
            ShellEffectType.MinorBleeding => (short)BuffVnums.MINOR_BLEEDING,
            ShellEffectType.Bleeding => (short)BuffVnums.BLEEDING,
            ShellEffectType.HeavyBleeding => (short)BuffVnums.HEAVY_BLEEDING,
            ShellEffectType.Blackout => (short)BuffVnums.BLACKOUT,
            ShellEffectType.Freeze => (short)BuffVnums.FREEZE,
            ShellEffectType.DeadlyBlackout => (short)BuffVnums.DEADLY_BLACKOUT,
            _ => null
        };

        if (buffVnum == null)
        {
            return null;
        }

        return new BCardDTO
        {
            Type = (short)BCardType.Buff,
            SubType = (byte)AdditionalTypes.Buff.ChanceCausing,
            FirstData = value,
            SecondData = buffVnum.Value
        };
    }
}