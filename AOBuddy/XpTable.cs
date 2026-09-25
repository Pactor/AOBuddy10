using System;

namespace AOBuddy
{
    /// <summary>
    /// XP required to advance through each level (1-149), transcribed from
    /// wiki.aodb.us/wiki/Level_Parameters (Helpbot's in-game data - the page ends
    /// at level 149, so 150+ reports no percentage). Stat.XP (0x34) is the
    /// character's progress inside the CURRENT level, so the way toward the next
    /// level is simply XP / table[level].
    /// </summary>
    internal static class XpTable
    {
        // Index = level, value = XP needed to get through that level. Row comments
        // give the level range on that line.
        private static readonly int[] XpPerLevel =
        {
            0,                                                                  // 0 (unused)
            1450,    2600,    3100,    4000,    4500,    5000,    5500,    6000,    6500,    7000,    // 1-10
            7700,    8300,    8900,    9600,    10400,   11000,   11900,   12700,   13700,   15400,   // 11-20
            16400,   17600,   18800,   20100,   21500,   22900,   24500,   26100,   27800,   30900,   // 21-30
            33000,   35100,   37400,   39900,   42400,   45100,   47900,   50900,   54000,   57400,   // 31-40
            60900,   64500,   68400,   76400,   81000,   85900,   91000,   96400,   101900,  108000,  // 41-50
            114300,  120800,  127700,  135000,  142600,  150700,  161900,  167800,  177100,  203500,  // 51-60
            214700,  226700,  239100,  251900,  265700,  280000,  294800,  310600,  327000,  344400,  // 61-70
            362300,  381100,  401000,  421600,  443300,  508100,  534200,  561600,  590200,  620000,  // 71-80
            651000,  683700,  717900,  753500,  790800,  829400,  870000,  912600,  956800,  1003000, // 81-90
            1051300, 1101500, 1153900, 1208800, 1266000, 1325500, 1387700, 1452300, 1519900, 1590300, // 91-100
            1663500, 1739900, 1819600, 1902200, 1988900, 2078600, 2172100, 2269800, 2371100, 2476600, // 101-110
            2586600, 2701000, 2819800, 2943600, 3072400, 3205800, 3345200, 3489700, 3640200, 3796500, // 111-120
            3958900, 4128000, 4303400, 4485700, 4674800, 4871700, 5075700, 5288100, 5508200, 5736800, // 121-130
            5974600, 6220700, 6474500, 6742200, 7017500, 7303700, 7600100, 7907600, 8227000, 8557700, // 131-140
            8901000, 9256800, 9625800, 10008600, 10405300, 10816600, 11242500, 11684300, 12141900     // 141-149
        };

        /// <summary>
        /// How far through the given level the XP value is, as a whole percent
        /// (0-100), or -1 when the level is outside the table (150+) or the data
        /// is missing.
        /// </summary>
        internal static int PercentToNext(int level, int xp)
        {
            if (level < 1 || level >= XpPerLevel.Length) return -1;
            int pct = (int)((long)xp * 100 / XpPerLevel[level]);
            return Math.Max(0, Math.Min(100, pct));
        }
    }
}
