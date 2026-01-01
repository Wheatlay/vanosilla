using System.Collections.Generic;

namespace WingsEmu.Plugins.BasicImplementations.Event.Items;

public class DropRarityConfiguration
{
    public List<RarityChance> Equipment { get; set; }
    public List<RarityChance> Shells { get; set; }
}