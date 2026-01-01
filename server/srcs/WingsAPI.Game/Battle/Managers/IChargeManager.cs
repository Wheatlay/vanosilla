// WingsEmu
// 
// Developed by NosWings Team

namespace WingsEmu.Game.Battle.Managers;

public interface IChargeComponent
{
    public void SetCharge(int chargeValue);
    public int GetCharge();
    public void ResetCharge();
}