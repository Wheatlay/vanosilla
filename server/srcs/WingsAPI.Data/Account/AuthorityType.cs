// WingsEmu
// 
// Developed by NosWings Team

namespace WingsEmu.DTOs.Account;

public enum AuthorityType : short
{
    Closed = -3,
    Banned = -2,
    Unconfirmed = -1,
    User = 0,
    Vip = 1,
    VipPlus = 3,
    VipPlusPlus = 5,
    Donator = 10,
    DonatorPlus = 15,
    DonatorPlusPlus = 20,
    Moderator = 25,
    BetaGameTester = 30,
    GameMaster = 40,
    SuperGameMaster = 500,


    CommunityManager = 900,

    GameAdmin = 1000, // everything???

    Owner = 1337, // everything except giving rights & some Remote


    Root = 30000 // everything
}