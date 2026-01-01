// WingsEmu
// 
// Developed by NosWings Team

using WingsEmu.DTOs.Items;

namespace WingsEmu.Game.Algorithm;

public interface ICellonGenerationAlgorithm
{
    public EquipmentOptionDTO GenerateOption(int cellonLevel);
}