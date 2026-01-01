namespace WingsAPI.Scripting.Enum
{
    public enum SMapFlags
    {
        /* ACTS */
        ACT_1,
        ACT_2,
        ACT_3,
        ACT_4,
        ACT_5_1,
        ACT_5_2,
        ACT_6_1,
        ACT_6_2,
        ACT_7,

        /* FACTION */
        ANGEL_SIDE = 30,
        DEMON_SIDE,

        NOSVILLE = 40,
        PORT_ALVEUS,

        /* TYPES */
        IS_BASE_MAP = 50,
        IS_MINILAND_MAP,

        /* FLAGS */
        HAS_PVP_ENABLED = 100,
        HAS_PVP_FACTION_ENABLED,
        HAS_PVP_FAMILY_ENABLED,
        HAS_USER_SHOPS_DISABLED,
        HAS_DROP_DIRECTLY_IN_INVENTORY_ENABLED,
        HAS_CHAMPION_EXPERIENCE_ENABLED,
        HAS_SEALED_VESSELS_DISABLED,
        HAS_RAID_TEAM_SUMMON_STONE_ENABLED,
        HAS_SIGNPOSTS_ENABLED,

        /* DEBUFFS */
        HAS_IMMUNITY_ON_MAP_CHANGE_ENABLED = 200,
        HAS_BURNING_SWORD_ENABLED
    }
}