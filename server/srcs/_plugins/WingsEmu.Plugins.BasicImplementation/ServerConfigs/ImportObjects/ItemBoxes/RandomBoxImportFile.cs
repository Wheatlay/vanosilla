using System.Collections.Generic;
using WingsEmu.Plugins.BasicImplementations.ServerConfigs.ImportObjects.Files;
using YamlDotNet.Serialization;

namespace WingsEmu.Plugins.BasicImplementations.ServerConfigs.ImportObjects.ItemBoxes;

public class RandomBoxImportFile : IFileData
{
    [YamlMember(Alias = "randomBoxes", ApplyNamingConventions = true)]
    public List<RandomBoxObject> Items { get; set; }
}