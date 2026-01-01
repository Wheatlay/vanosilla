using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace WingsEmu.Plugins.BasicImplementations.ServerConfigs.ImportObjects.ItemBoxes;

public class RandomBoxCategory
{
    [YamlMember(Alias = "chance", ApplyNamingConventions = true)]
    public ushort Chance { get; set; }

    [YamlMember(Alias = "items", ApplyNamingConventions = true)]
    public List<RandomBoxItem> Items { get; set; }
}