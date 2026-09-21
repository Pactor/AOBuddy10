using System;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.DataTypes;

namespace AOSharp.Common.Unmanaged.Imports
{
    public class N3EngineClientAnarchy_t
    {
        [DllImport("Gamecode.dll", EntryPoint = "??0n3EngineClientAnarchy_t@@QAE@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr Constructor(IntPtr pThis);

        [DllImport("Gamecode.dll", EntryPoint = "?GetPlayfieldFactory@n3EngineClientAnarchy_t@@UAEPAVn3PlayfieldFactory_i@@ABVPlayfieldProxy_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetPlayfieldFactory(IntPtr pThis, ref PlayfieldProxy playfieldProxy);

        [DllImport("Gamecode.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?OpenClient@n3EngineClientAnarchy_t@@QAEXPAVResourceDatabase_t@@I@Z")]
        public static extern void OpenClient(IntPtr pThis, IntPtr pResourceDatabase, int clientInst);

        //GetQuestWorldPos
        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_GetQuestWorldPos@n3EngineClientAnarchy_t@@QBE_NABVIdentity_t@@AAV2@AAVVector3_t@@2@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool GetQuestWorldPos(IntPtr pThis, ref Identity mission, ref Identity playfield, ref Vector3 universePos, ref Vector3 ZonePos);

        //DebugSpellListToChat
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_DebugSpellListToChat@n3EngineClientAnarchy_t@@QBEXHABVIdentity_t@@W4SpellList_e@GameData@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void DebugSpellListToChat(IntPtr pThis, int unk, ref Identity identity, int spellList);

        //SecondarySpecialAttack
        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_SecondarySpecialAttack@n3EngineClientAnarchy_t@@QBE_NABVIdentity_t@@W4Stat_e@GameData@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool SecondarySpecialAttack(IntPtr pThis, ref Identity target, Stat stat);

        //DefaultAttack
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_DefaultAttack@n3EngineClientAnarchy_t@@QBEXABVIdentity_t@@_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void DefaultAttack(IntPtr pThis, ref Identity target, bool unk);

        //SplitItem
        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_SplitItem@n3EngineClientAnarchy_t@@QAE_NABVIdentity_t@@H@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool SplitItem(IntPtr pThis, ref Identity slot, int count);

        //TeamJoinRequest
        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_TeamJoinRequest@n3EngineClientAnarchy_t@@QBE_NABVIdentity_t@@_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool TeamJoinRequest(IntPtr pThis, ref Identity identity, bool force);

        //StopAttack
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_StopAttack@n3EngineClientAnarchy_t@@QBEXXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern void StopAttack(IntPtr pThis);

        //GetSkill
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_GetSkill@n3EngineClientAnarchy_t@@QBEHABVIdentity_t@@W4Stat_e@GameData@@H0@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern int GetSkill(IntPtr pThis, ref Identity dynel, Stat stat, int detail, ref Identity unk);

        //GetSkillMax
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_GetSkillMax@n3EngineClientAnarchy_t@@QAEHW4Stat_e@GameData@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern int GetSkillMax(IntPtr pThis, Stat stat);

        //PersonalResearchGoals
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_PersonalResearchGoals@n3EngineClientAnarchy_t@@QAEXAAV?$vector@U?$pair@I_N@std@@V?$allocator@U?$pair@I_N@std@@@2@@std@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr PersonalResearchGoals(IntPtr pThis, IntPtr pVector);

        //IsSecondarySpecialAttackAvailable
        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_IsSecondarySpecialAttackAvailable@n3EngineClientAnarchy_t@@QBE_NW4Stat_e@GameData@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool IsSecondarySpecialAttackAvailable(IntPtr pThis, Stat stat);

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_IsResearch@n3EngineClientAnarchy_t@@QBE_NI@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool IsResearch(IntPtr pThis, int id);

        //GetAttackRange
        [DllImport("Gamecode.dll", EntryPoint = "?GetAttackRange@n3EngineClientAnarchy_t@@QBEMXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern float GetAttackRange(IntPtr pThis);

        //CastNanoSpell
        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_CastNanoSpell@n3EngineClientAnarchy_t@@QAEXABVIdentity_t@@0@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool CastNanoSpell(IntPtr pThis, ref Identity nano, ref Identity target);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, SetLastError = true)]
        public unsafe delegate bool DCastNanoSpell(IntPtr pThis, ref Identity nanoIdentity, ref Identity targetIdentity);

        //GetCorrectActionId
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_GetCorrectActionID@n3EngineClientAnarchy_t@@QBEXAAVIdentity_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void GetCorrectActionId(IntPtr pThis, ref Identity identity);

        //PerformSpecialAction
        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_PerformSpecialAction@n3EngineClientAnarchy_t@@QAE_NABVIdentity_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool PerformSpecialAction(IntPtr pThis, ref Identity action);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate bool DPerformSpecialAction(IntPtr pThis, ref Identity identity);

        //GetPFName
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_GetPFName@n3EngineClientAnarchy_t@@QBEPBDI@Z", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr GetPFName(int id);

        //GetName
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_GetName@n3EngineClientAnarchy_t@@QBEPBDABVIdentity_t@@0@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetName(IntPtr pThis, ref Identity identity, ref Identity identityUnk);

        //GetPerkName
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_GetPerkName@n3EngineClientAnarchy_t@@QBE?AV?$basic_string@DU?$char_traits@D@std@@V?$allocator@D@2@@std@@I_N@Z", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr GetPerkName(IntPtr retStr, int perkId, bool unk);

        //IsFormulaReady
        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_IsFormulaReady@n3EngineClientAnarchy_t@@QBE_NABVIdentity_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool IsFormulaReady(IntPtr pThis, ref Identity identity);

        //HasPerk
        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_HasPerk@n3EngineClientAnarchy_t@@QAE_NI@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool HasPerk(IntPtr pThis, int perkId);

        //IsAttacking
        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_IsAttacking@n3EngineClientAnarchy_t@@QBE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool IsAttacking(IntPtr pThis);

        //GetSpecialActionList
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_GetSpecialActionList@n3EngineClientAnarchy_t@@QAEPAV?$list@VSpecialAction_t@@V?$allocator@VSpecialAction_t@@@std@@@std@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern unsafe StdObjList* GetSpecialActionList(IntPtr pThis);

        //GetNanoSpellList
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_GetNanoSpellList@n3EngineClientAnarchy_t@@QAEPBV?$list@HV?$allocator@H@std@@@std@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern unsafe StdObjList* GetNanoSpellList(IntPtr pThis);

        //GetNanoTemplateInfoList
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_GetNanoTemplateInfoList@n3EngineClientAnarchy_t@@QBEPAV?$list@VNanoTemplateInfo_c@@V?$allocator@VNanoTemplateInfo_c@@@std@@@std@@ABVIdentity_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern unsafe StdObjList* GetNanoTemplateInfoList(IntPtr pThis, Identity* identity);

        //IsMoving
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_IsMoving@n3EngineClientAnarchy_t@@QBE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern void IsMoving(IntPtr pThis);

        //MovementChanged
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_MovementChanged@n3EngineClientAnarchy_t@@QAEXW4MovementAction_e@Movement_n@@MM_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void MovementChanged(IntPtr pThis, MovementAction action, float unk1, float unk2, bool unk3);

        //GetNumberOfFreeInventorySlots
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_GetNumberOfFreeInventorySlots@n3EngineClientAnarchy_t@@QAEHXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int GetNumberOfFreeInventorySlots(IntPtr pThis);

        //GetContainerInventoryList
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_GetContainerInventoryList@n3EngineClientAnarchy_t@@QBEPBV?$list@VInventoryEntry_t@@V?$allocator@VInventoryEntry_t@@@std@@@std@@ABVIdentity_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetContainerInventoryList(IntPtr pThis, ref Identity identity);

        //GetInventoryVec
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_GetInventoryVec@n3EngineClientAnarchy_t@@QAEPBV?$vector@PAVNewInventoryEntry_t@@V?$allocator@PAVNewInventoryEntry_t@@@std@@@std@@ABVIdentity_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetInventoryVec(IntPtr pThis, ref Identity identity);

        //IsInTeam
        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_IsInTeam@n3EngineClientAnarchy_t@@QBE_NABVIdentity_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern unsafe bool IsInTeam(IntPtr pThis, Identity* identity);

        //TradeskillCombine
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_TradeskillCombine@n3EngineClientAnarchy_t@@QBEXABVIdentity_t@@0@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr TradeskillCombine(IntPtr pThis, IntPtr source, IntPtr destination);

        //GetClientDynelId
        [DllImport("Gamecode.dll", EntryPoint = "?GetClientDynelId@n3EngineClientAnarchy_t@@UBE?AVIdentity_t@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern unsafe Identity* GetClientDynelId(IntPtr pThis);

        //SelectedTarget
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_SelectedTarget@n3EngineClientAnarchy_t@@QAEXABVIdentity_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr SelectedTarget(IntPtr pThis, ref Identity target);

        //IsInRaidTeam
        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_IsInRaidTeam@n3EngineClientAnarchy_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool IsInRaidTeam(IntPtr pThis);

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_IsInTeam@n3EngineClientAnarchy_t@@QBE_NABVIdentity_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool IsInTeam(IntPtr pThis, ref Identity identity);

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_IsTeamLeader@n3EngineClientAnarchy_t@@QBE_NABVIdentity_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool IsTeamLeader(IntPtr pThis, ref Identity identity);

        //GetTeamMemberList
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_GetTeamMemberList@n3EngineClientAnarchy_t@@QAEPAV?$vector@PAVTeamEntry_t@@V?$allocator@PAVTeamEntry_t@@@std@@@std@@H@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern unsafe StdObjVector* GetTeamMemberList(IntPtr pThis, int teamIndex);

        //GetFullPerkMap
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_GetFullPerkMap@n3EngineClientAnarchy_t@@QBEABV?$vector@VPerk_t@@V?$allocator@VPerk_t@@@std@@@std@@XZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern unsafe StdStructVector* GetFullPerkMap(IntPtr pThis);

        //IsTeamLeader
        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_IsTeamLeader@n3EngineClientAnarchy_t@@QBE_NABVIdentity_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern unsafe bool IsTeamLeader(IntPtr pThis, Identity* target);

        //GetItemByTemplate
        [DllImport("Gamecode.dll", EntryPoint = "?GetItemByTemplate@n3EngineClientAnarchy_t@@ABEPAVDummyItemBase_t@@VIdentity_t@@ABV3@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern unsafe IntPtr GetItemByTemplate(IntPtr pThis, Identity template, ref Identity unk);

        //GetBuffCurrentTime
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_GetBuffCurrentTime@n3EngineClientAnarchy_t@@QAEHABVIdentity_t@@0@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern unsafe int GetBuffCurrentTime(IntPtr pThis, ref Identity identity, ref Identity unk);

        //GetBuffTotalTime
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_GetBuffTotalTime@n3EngineClientAnarchy_t@@QAEHABVIdentity_t@@0@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern int GetBuffTotalTime(IntPtr pThis, ref Identity  identity, ref Identity unk);

        //RemoveBuff
        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_RemoveBuff@n3EngineClientAnarchy_t@@QAE_NABVIdentity_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool RemoveBuff(IntPtr pThis, ref Identity identity);

        //CreateDummyItemID
        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_CreateDummyItemID@n3EngineClientAnarchy_t@@QBE_NAAVIdentity_t@@ABVACGItem_t@GameData@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool CreateDummyItemID(IntPtr pThis, ref Identity template, ref ACGItemQueryData acgItem);

        //TextCommand
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_TextCommand@n3EngineClientAnarchy_t@@QAE_NHPBDABVIdentity_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr TextCommand(IntPtr pThis, IntPtr unk, IntPtr text, IntPtr identity);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate IntPtr DTextCommand(IntPtr pThis, IntPtr unk, IntPtr text, IntPtr identity);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public unsafe delegate StdObjList* GetMissionListDelegate(IntPtr pThis, IntPtr unk);
        public static GetMissionListDelegate GetMissionList;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate IntPtr GetItemActionInfoDelegate(IntPtr pThis, ItemActionInfo action);
        public static GetItemActionInfoDelegate GetItemActionInfo;


        [DllImport("Gamecode.dll", EntryPoint = "?ToClientN3Message@n3EngineClientAnarchy_t@@UBEXABVIdentity_t@@PAVACE_Data_Block@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void ToClientN3Message(IntPtr pThis, ref Identity identity, IntPtr pDataBlock);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate void ToClientN3MessageDelegate(IntPtr pThis, ref Identity identity, IntPtr pDataBlock);

        [DllImport("N3.dll", EntryPoint = "?GetPlayfield@n3EngineClient_t@@SAPAVn3Playfield_t@@XZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetPlayfield();

        [DllImport("Gamecode.dll", EntryPoint = "?RunEngine@n3EngineClientAnarchy_t@@UAEXM@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void RunEngine(IntPtr pThis, float deltaTime);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate void DRunEngine(IntPtr pThis, float unk);

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_SendInPlayMessage@n3EngineClientAnarchy_t@@QBE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool SendInPlayMessage(IntPtr pThis);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate bool DSendInPlayMessage(IntPtr pThis);

        [DllImport("Gamecode.dll", EntryPoint = "?PlayfieldInit@n3EngineClientAnarchy_t@@UAEXI@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void PlayfieldInit(IntPtr pThis, uint id);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate void DPlayfieldInit(IntPtr pThis, uint id);

        [return: MarshalAs(UnmanagedType.U1)]
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_IsPerk@n3EngineClientAnarchy_t@@QBE_NI@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool IsPerk(IntPtr pThis, int id);

        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_GetSpecialActionState@n3EngineClientAnarchy_t@@QAEHABVIdentity_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern SpecialActionState GetSpecialActionState(IntPtr pThis, ref Identity action);
        
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_UseItem@n3EngineClientAnarchy_t@@QAEXABVIdentity_t@@_N@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void UseItem(IntPtr pThis, ref Identity identity, bool unknown);

        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_UseItemOnItem@n3EngineClientAnarchy_t@@QAEXABVIdentity_t@@0@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void UseItemOnItem(IntPtr pThis, ref Identity source, ref Identity target);
        
        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_UseItemOnCharacter@n3EngineClientAnarchy_t@@QAEXABVIdentity_t@@0@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void UseItemOnCharacter(IntPtr pThis, ref Identity source, ref Identity target);

        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_SetStat@n3EngineClientAnarchy_t@@QAEXHW4Stat_e@GameData@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetStat(IntPtr pThis, int value, Stat stat);

        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_TemplateIDToDynelID@n3EngineClientAnarchy_t@@QBE?AVIdentity_t@@ABV2@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr TemplateIDToDynelID(IntPtr pThis, out Identity dynelId, ref Identity templateId);

        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_GetTemplateID@n3EngineClientAnarchy_t@@QBE?BVIdentity_t@@ABV2@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr GetTemplateID(IntPtr pThis, ref Identity templateId);

        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_UseSkill@n3EngineClientAnarchy_t@@QAEXW4Stat_e@GameData@@ABVIdentity_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void UseSkill(IntPtr pThis, Stat stat, ref Identity identity);

        [DllImport("Gamecode.dll", EntryPoint = "?N3Msg_StartCamping@n3EngineClientAnarchy_t@@QAE_NXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern bool StartCamping(IntPtr pThis);
    }
}
