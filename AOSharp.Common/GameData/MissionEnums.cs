using System;
using AOSharp.Common.Helpers;

namespace AOSharp.Common.GameData
{
    public enum MissionScope : byte
    {
        Solo = 1,
        Team = 2,
    }

    public enum MissionActionType
    {
        KillPerson = 0x01,
        UseItemOnItem = 0x08,
        FindItem = 0x0F,
        FindPerson = 0x10,
        KillMultiPerson = 0x14
    }

    public enum MissionDirection
    {
        Ascending,
        Descending,
        Boss
    }

    public static class MissionDirectionMethods
    {
        public static MissionDirection Invert(this MissionDirection direction)
        {
            return direction == MissionDirection.Descending ? MissionDirection.Ascending : MissionDirection.Descending;
        }
    }
}