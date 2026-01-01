using System.Collections.Generic;

namespace WingsEmu.Plugins.BasicImplementations.Vehicles;

public class VehicleConfiguration
{
    /// <summary>
    ///     Vehicle vnum id
    /// </summary>
    public int VehicleVnum { get; set; }

    public int DefaultSpeed { get; set; }

    public int MaleMorphId { get; set; }

    public int FemaleMorphId { get; set; }

    public bool? RemoveItem { get; set; }

    public List<VehicleBoost> VehicleBoostType { get; set; }

    public List<VehicleMapSpeed> VehicleMapSpeeds { get; set; }
}

public class VehicleBoost
{
    public BoostType BoostType { get; set; }

    public short? FirstValue { get; set; }

    public short? SecondValue { get; set; }
}

public enum BoostType : byte
{
    INCREASE_SPEED = 0,
    REMOVE_BAD_EFFECTS = 2,
    RANDOM_TELEPORT_ON_MAP = 3,
    TELEPORT_FORWARD = 4,
    REGENERATE_HP_MP = 5,
    CREATE_BUFF = 6,
    CREATE_BUFF_ON_END = 7
}