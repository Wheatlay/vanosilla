using YamlDotNet.Serialization;

namespace WingsEmu.Plugins.BasicImplementations.ServerConfigs.ImportObjects.Npcs;

public class MapNpcShopSkillObject
{
    [YamlMember(Alias = "skillVnum", ApplyNamingConventions = true)]
    public short SkillVnum { get; set; }
}