// WingsEmu
// 
// Developed by NosWings Team

using System.Collections.Generic;
using WingsEmu.Plugins.BasicImplementations.ServerConfigs.ImportObjects.Files;
using YamlDotNet.Serialization;

namespace WingsEmu.Plugins.BasicImplementations.ServerConfigs.ImportObjects.Drops;

public class DropImportFile : IFileData
{
    [YamlMember(Alias = "drops", ApplyNamingConventions = true)]
    public List<DropObject> Drops { get; set; }
}