using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.Game._enum;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Buffs.Events;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Skills;

namespace WingsEmu.Plugins.BasicImplementations.Event.Buffs;

public class AngelSpecialistElementalBuffEventHandler : IAsyncEventProcessor<AngelSpecialistElementalBuffEvent>
{
    private readonly IBuffFactory _buffFactory;

    public AngelSpecialistElementalBuffEventHandler(IBuffFactory buffFactory) => _buffFactory = buffFactory;

    public async Task HandleAsync(AngelSpecialistElementalBuffEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;
        IPlayerEntity character = session.PlayerEntity;
        SkillInfo skillInfo = e.Skill;

        if (character.AngelElement.HasValue)
        {
            return;
        }

        if (!character.UseSp)
        {
            return;
        }

        if (character.Specialist == null)
        {
            return;
        }

        if (!character.HasBuff(BuffVnums.MAGICAL_FETTERS))
        {
            return;
        }

        character.RemoveAngelElement();
        await character.RemoveBuffAsync(false, character.BuffComponent.GetBuff((short)BuffVnums.MAGICAL_FETTERS));
        Buff newBuff = _buffFactory.CreateBuff((short)BuffVnums.MAGIC_SPELL, character);
        await character.AddBuffAsync(newBuff);

        if (character.Specialist.SpLevel < 20)
        {
            return;
        }

        byte skillCastId = (ElementType)skillInfo.Element switch
        {
            ElementType.Neutral => 15,
            ElementType.Fire => 11,
            ElementType.Water => 12,
            ElementType.Light => 13,
            ElementType.Shadow => 14
        };

        var newComboState = new ComboSkillState
        {
            State = byte.MinValue,
            LastSkillByCastId = skillCastId,
            OriginalSkillCastId = skillCastId
        };

        character.SaveComboSkill(newComboState);
        session.SendMSlotPacket(skillCastId);
        character.AddAngelElement((ElementType)skillInfo.Element);
    }
}