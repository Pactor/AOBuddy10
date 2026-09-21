using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;
using AOSharp.Common.Helpers;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.Unmanaged.Imports;
using static SmokeLounge.AOtomation.Messaging.Messages.N3Messages.FullCharacterMessage;

namespace AOSharp.Common.Unmanaged.Interfaces
{
    public class N3EngineClientAnarchy
    {
        public static string GetDesc(Identity identity)
        {
            IntPtr instance = N3InterfaceModule_t.GetInstance();

            if (instance == IntPtr.Zero)
                return string.Empty;

            return Marshal.PtrToStringAnsi(N3InterfaceModule_t.GetDesc(instance, ref identity, 0));
        }

        public static string GetPFName(int id)
        {
            return Marshal.PtrToStringAnsi(N3EngineClientAnarchy_t.GetPFName(id));
        }

        public static string GetPerkName(int perkId, bool unk = false)
        {
            StdString retStr = StdString.Create();

            IntPtr pStr = N3EngineClientAnarchy_t.GetPerkName(retStr.Pointer, perkId, unk);

            if (pStr == IntPtr.Zero)
                return string.Empty;

            return retStr.ToString();
        }

        public static float GetPerkProgress(uint perkId)
        {
            return N3InterfaceModule_t.GetPerkProgress(N3InterfaceModule_t.GetInstance(), perkId);
        }

        public static List<uint> GetCompletedPersonalResearchGoals()
        {
            StdStructVector vector = new StdStructVector();
            N3InterfaceModule_t.GetCompletedPersonalResearchGoals(N3InterfaceModule_t.GetInstance(), ref vector);
            return vector.ToList<uint>();
        }

        public static List<ResearchGoal> GetPersonalResearchGoals()
        {
            StdStructVector vector = new StdStructVector();
            N3InterfaceModule_t.GetPersonalResearchGoals(N3InterfaceModule_t.GetInstance(), ref vector);
            return vector.ToList<ResearchGoal>();
        }

        public static void DebugSpellListToChat(Identity identity, int unk, int spellList)
        {
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine == IntPtr.Zero)
                return;

            N3EngineClientAnarchy_t.DebugSpellListToChat(pEngine, unk, ref identity, spellList);
        }

        public static Identity GetTemplateID(Identity identity)
        {
            N3EngineClientAnarchy_t.TemplateIDToDynelID(N3Engine_t.GetInstance(), out Identity dynelId, ref identity);

            return dynelId;
        }

        public static Identity TemplateIDToDynelID(Identity templateId)
        {
            N3EngineClientAnarchy_t.TemplateIDToDynelID(N3Engine_t.GetInstance(), out Identity dynelId, ref templateId);

            return dynelId;
        }

        public static string GetName(Identity identity)
        {
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine == IntPtr.Zero)
                return null;
            
            Identity garbage = new Identity();
            
            return Utils.UnsafePointerToString(N3EngineClientAnarchy_t.GetName(pEngine, ref identity, ref garbage));
        }

        public static bool HasPerk(int perkId)
        {
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine == IntPtr.Zero)
                throw new NullReferenceException("Could not get N3Engine instance");

            return N3EngineClientAnarchy_t.HasPerk(pEngine, perkId);
        }
        
        public static void UseItem(Identity identity, bool unknown = false)
        {
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine != IntPtr.Zero)
            {
                N3EngineClientAnarchy_t.UseItem(pEngine, ref identity, unknown);
            }
        }
        
        public static void UseItemOnItem(Identity source, Identity target)
        {
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine != IntPtr.Zero)
            {
                N3EngineClientAnarchy_t.UseItemOnItem(pEngine, ref source, ref target);
            }
        }
        
        public static void UseItemOnCharacter(Identity source, Identity target)
        {
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine != IntPtr.Zero)
            {
                N3EngineClientAnarchy_t.UseItemOnCharacter(pEngine, ref source, ref target);
            }
        }

        public static void SplitItem(Identity slot, int count)
        {
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine != IntPtr.Zero)
                N3EngineClientAnarchy_t.SplitItem(pEngine, ref slot, count);
        }

        public static bool GetQuestWorldPos(Identity mission, out Identity playfield, out Vector3 universePos, out Vector3 zonePos)
        {
            playfield = Identity.None;
            universePos = Vector3.Zero;
            zonePos = Vector3.Zero;
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine == IntPtr.Zero)
                return false;

            return N3EngineClientAnarchy_t.GetQuestWorldPos(pEngine, ref mission, ref playfield, ref universePos, ref zonePos);
        }

        public static int GetNumberOfFreeInventorySlots()
        {
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine == IntPtr.Zero)
                return 0;

            return N3EngineClientAnarchy_t.GetNumberOfFreeInventorySlots(pEngine);
        }

        public static void SetStat(Stat stat, int value)
        {
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine != IntPtr.Zero)
                N3EngineClientAnarchy_t.SetStat(pEngine, value, stat);
        }

        public static bool StartCamping()
        {
            IntPtr pEngine = N3Engine_t.GetInstance();

            return pEngine != IntPtr.Zero && N3EngineClientAnarchy_t.StartCamping(pEngine);
        }
    }
}
