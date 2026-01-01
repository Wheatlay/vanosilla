using System.Collections.Generic;
using WingsEmu.Plugins.BasicImplementations.ServerConfigs.ImportObjects.Files;
using YamlDotNet.Serialization;

namespace WingsEmu.Plugins.BasicImplementations.ServerConfigs.ImportObjects.Portals;

public class PortalImportFile : IFileData
{
    [YamlMember(Alias = "portals", ApplyNamingConventions = true)]
    public List<PortalObject> Portals { get; set; }
}