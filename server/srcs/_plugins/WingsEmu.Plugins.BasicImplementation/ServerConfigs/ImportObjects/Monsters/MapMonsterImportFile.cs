using System.Collections.Generic;
using WingsEmu.Plugins.BasicImplementations.ServerConfigs.ImportObjects.Files;
using YamlDotNet.Serialization;

namespace WingsEmu.Plugins.BasicImplementations.ServerConfigs.ImportObjects.Monsters;

public class MapMonsterImportFile : IFileData
{
    [YamlMember(Alias = "mapId", ApplyNamingConventions = true)]
    public int MapId { get; set; }

    [YamlMember(Alias = "monsters", ApplyNamingConventions = true)]
    public List<MapMonsterObject> Monsters { get; set; }
}