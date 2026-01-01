// WingsEmu
// 
// Developed by NosWings Team

using System.Threading.Tasks;
using PhoenixLib.DAL;

namespace WingsAPI.Data.Families;

public interface IFamilyDAO : IGenericAsyncLongRepository<FamilyDTO>
{
    Task<FamilyDTO> GetByNameAsync(string reqName);
}