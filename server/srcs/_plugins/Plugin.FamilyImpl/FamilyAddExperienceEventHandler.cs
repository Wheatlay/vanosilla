using System;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.Game.Families;
using WingsEmu.Game.Families.Event;

namespace Plugin.FamilyImpl
{
    public class FamilyAddExperienceEventHandler : IAsyncEventProcessor<FamilyAddExperienceEvent>
    {
        private readonly IFamilyManager _familyManager;

        public FamilyAddExperienceEventHandler(IFamilyManager familyManager) => _familyManager = familyManager;

        public async Task HandleAsync(FamilyAddExperienceEvent e, CancellationToken cancellation) =>
            _familyManager.SendExperienceToFamilyServer(new ExperienceGainedSubMessage(e.Sender.PlayerEntity.Id, e.ExperienceGained, e.FamXpObtainedFromType, DateTime.UtcNow));
    }
}