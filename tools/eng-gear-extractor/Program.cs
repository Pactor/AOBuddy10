using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

// Symbiant extractor (profession-parameterised; the folder name is historical).
//
// Reads the converted OmniCell item pack (Datafiles/items.ocp, "OMNICELL-CONTENT" v3)
// byte-for-byte (same reader as mp-weapon-extractor) and, for EVERY wearable template
// whose itemnames.sql name matches --match (default "Symbiant"), captures:
//   - quality (QL)
//   - the item Stats dictionary (statnumber -> value)
//   - ToWear (ActionType 6) requirements  => equip requirements (Treatment/ability/level)
//   - every event function's stat-modify args => the skill bonuses granted when worn
// Nothing is invented; every field is read from items.ocp and names joined from itemnames.sql.

class Program
{
    const int ToWield = 8, ToWear = 6, ToUse = 3;
    const int OpEqualTo = 0, OpLessThan = 1, OpGreaterThan = 2, OpOr = 3, OpAnd = 4,
              OpBitAnd = 22, OpUnequal = 24, OpNot = 42, OpNotBitAnd = 107;

    // Stat number -> name. Abilities + level + treatment + the nano/skill set relevant to symbiants.
    static readonly Dictionary<int, string> StatNames = new()
    {
        {0x0,"Flags"},
        {0x1,"MaxHealth"},
        {0x2,"Mass"},
        {0x3,"AttackSpeed"},
        {0x4,"Breed"},
        {0x5,"Clan"},
        {0x6,"Team"},
        {0x7,"State"},
        {0x8,"TimeExist"},
        {0x9,"MapFlags"},
        {0xA,"ProfessionLevel"},
        {0xB,"PreviousHealth"},
        {0xC,"Mesh"},
        {0xD,"Anim"},
        {0xE,"Name"},
        {0xF,"Info"},
        {0x10,"Strength"},
        {0x11,"Agility"},
        {0x12,"Stamina"},
        {0x13,"Intelligence"},
        {0x14,"Sense"},
        {0x15,"Psychic"},
        {0x16,"AMS"},
        {0x17,"StaticInstance"},
        {0x18,"MaxMass"},
        {0x19,"StaticType"},
        {0x1A,"Energy"},
        {0x1B,"Health"},
        {0x1C,"Height"},
        {0x1D,"DMS"},
        {0x1E,"Can"},
        {0x1F,"Face"},
        {0x20,"HairMesh"},
        {0x21,"Side"},
        {0x22,"DeadTimer"},
        {0x23,"AccessCount"},
        {0x24,"AttackCount"},
        {0x25,"TitleLevel"},
        {0x26,"BackMesh"},
        {0x27,"ShoulderMesh"},
        {0x28,"AlienXP"},
        {0x29,"FabricType"},
        {0x2A,"CATMesh"},
        {0x2B,"ParentType"},
        {0x2C,"ParentInstance"},
        {0x2D,"BeltSlots"},
        {0x2E,"BandolierSlots"},
        {0x2F,"Fatness"},
        {0x30,"ClanLevel"},
        {0x31,"InsuranceTime"},
        {0x32,"InventoryTimeout"},
        {0x33,"AggDef"},
        {0x34,"XP"},
        {0x35,"IP"},
        {0x36,"Level"},
        {0x37,"InventoryId"},
        {0x38,"TimeSinceCreation"},
        {0x39,"LastXP"},
        {0x3A,"Age"},
        {0x3B,"Sex"},
        {0x3C,"Profession"},
        {0x3D,"Cash"},
        {0x3E,"AlignmentClanTokens"},
        {0x3F,"Attitude"},
        {0x40,"HeadMesh"},
        {0x41,"HairTexture"},
        {0x42,"MissionBits6"},
        {0x43,"HairColourRGB"},
        {0x44,"NumConstructedQuest"},
        {0x45,"MaxConstructedQuest"},
        {0x46,"SpeedPenalty"},
        {0x47,"TotalMass"},
        {0x48,"ItemType"},
        {0x49,"RepairDifficulty"},
        {0x4A,"Value"},
        {0x4B,"NanoStrain"},
        {0x4C,"ItemClass"},
        {0x4D,"RepairSkill"},
        {0x4E,"CurrentMass"},
        {0x4F,"Icon"},
        {0x50,"PrimaryItemType"},
        {0x51,"PrimaryItemInstance"},
        {0x52,"SecondaryItemType"},
        {0x53,"SecondaryItemInstance"},
        {0x54,"UserType"},
        {0x55,"UserInstance"},
        {0x56,"AreaType"},
        {0x57,"AreaInstance"},
        {0x58,"DefaultPos"},
        {0x59,"Race"},
        {0x5A,"ProjectileAC"},
        {0x5B,"MeleeAC"},
        {0x5C,"EnergyAC"},
        {0x5D,"ChemicalAC"},
        {0x5E,"RadiationAC"},
        {0x5F,"ColdAC"},
        {0x60,"PoisonAC"},
        {0x61,"FireAC"},
        {0x62,"StateAction"},
        {0x63,"ItemAnim"},
        {0x64,"MartialArts"},
        {0x65,"MultiMelee"},
        {0x66,"_1hBlunt"},
        {0x67,"_1hEdged"},
        {0x68,"MeleeEnergy"},
        {0x69,"Skill2hEdged"},
        {0x6A,"Piercing"},
        {0x6B,"_2hBlunt"},
        {0x6C,"SharpObject"},
        {0x6D,"Grenade"},
        {0x6E,"HeavyWeapons"},
        {0x6F,"Bow"},
        {0x70,"Pistol"},
        {0x71,"Rifle"},
        {0x72,"MGSMG"},
        {0x73,"Shotgun"},
        {0x74,"AssaultRifle"},
        {0x75,"VehicleWater"},
        {0x76,"MeleeInit"},
        {0x77,"RangedInit"},
        {0x78,"PhysicalInit"},
        {0x79,"BowSpecialAttack"},
        {0x7A,"SensoryImprovement"},
        {0x7B,"FirstAid"},
        {0x7C,"Treatment"},
        {0x7D,"MechanicalEngineering"},
        {0x7E,"ElectricalEngineering"},
        {0x7F,"MaterialMetamorphosis"},
        {0x80,"BiologicalMetamorphosis"},
        {0x81,"PsychologicalModification"},
        {0x82,"MaterialCreation"},
        {0x83,"SpaceTime"},
        {0x84,"NanoPool"},
        {0x85,"RangedEnergy"},
        {0x86,"MultiRanged"},
        {0x87,"TrapDisarm"},
        {0x88,"Perception"},
        {0x89,"Adventuring"},
        {0x8A,"Swimming"},
        {0x8B,"VehicleAir"},
        {0x8C,"MapNavigation"},
        {0x8D,"Tutoring"},
        {0x8E,"Brawl"},
        {0x8F,"Riposte"},
        {0x90,"Dimach"},
        {0x91,"Parry"},
        {0x92,"SneakAttack"},
        {0x93,"FastAttack"},
        {0x94,"Burst"},
        {0x95,"NanoCInit"},
        {0x96,"FlingShot"},
        {0x97,"AimedShot"},
        {0x98,"BodyDevelopment"},
        {0x99,"DuckExp"},
        {0x9A,"DodgeRanged"},
        {0x9B,"EvadeClsC"},
        {0x9C,"RunSpeed"},
        {0x9D,"QuantumFT"},
        {0x9E,"WeaponSmithing"},
        {0x9F,"Pharmaceuticals"},
        {0xA0,"NanoProgramming"},
        {0xA1,"ComputerLiteracy"},
        {0xA2,"Psychology"},
        {0xA3,"Chemistry"},
        {0xA4,"Concealment"},
        {0xA5,"BreakingEntry"},
        {0xA6,"VehicleGround"},
        {0xA7,"FullAuto"},
        {0xA8,"NanoResist"},
        {0xA9,"AlienLevel"},
        {0xAA,"HealthChangeBest"},
        {0xAB,"HealthChangeWorst"},
        {0xAC,"HealthChange"},
        {0xAD,"CurrentMovementMode"},
        {0xAE,"PrevMovementMode"},
        {0xAF,"AutoLockTimeDefault"},
        {0xB0,"AutoUnlockTimeDefault"},
        {0xB1,"MoreFlags"},
        {0xB2,"AlienNextXP"},
        {0xB3,"NPCFlags"},
        {0xB4,"CurrentNCU"},
        {0xB5,"MaxNCU"},
        {0xB6,"Specialization"},
        {0xB7,"EffectIcon"},
        {0xB8,"BuildingType"},
        {0xB9,"BuildingInstance"},
        {0xBA,"CardOwnerType"},
        {0xBB,"CardOwnerInstance"},
        {0xBC,"BuildingComplexInst"},
        {0xBD,"ExitInstance"},
        {0xBE,"NextDoorInBuilding"},
        {0xBF,"LastConcretePlayfieldInstance"},
        {0xC0,"ExtenalPlayfieldInstance"},
        {0xC1,"ExtenalDoorInstance"},
        {0xC2,"InPlay"},
        {0xC3,"AccessKey"},
        {0xC4,"ConflictReputation"},
        {0xC5,"OrientationMode"},
        {0xC6,"SessionTime"},
        {0xC7,"RP"},
        {0xC8,"Conformity"},
        {0xC9,"Aggressiveness"},
        {0xCA,"Stability"},
        {0xCB,"Extroverty"},
        {0xCC,"Taunt"},
        {0xCD,"ReflectProjectileAC"},
        {0xCE,"ReflectMeleeAC"},
        {0xCF,"ReflectEnergyAC"},
        {0xD0,"ReflectChemicalAC"},
        {0xD1,"WeaponMesh"},
        {0xD2,"RechargeDelay"},
        {0xD3,"EquipDelay"},
        {0xD4,"MaxEnergy"},
        {0xD5,"TeamSide"},
        {0xD6,"CurrentNano"},
        {0xD7,"GmLevel"},
        {0xD8,"ReflectRadiationAC"},
        {0xD9,"ReflectColdAC"},
        {0xDA,"ReflectNanoAC"},
        {0xDB,"ReflectFireAC"},
        {0xDC,"CurrBodyLocation"},
        {0xDD,"MaxNanoEnergy"},
        {0xDE,"AccumulatedDamage"},
        {0xDF,"CanChangeClothes"},
        {0xE0,"Features"},
        {0xE1,"ReflectPoisonAC"},
        {0xE2,"ShieldProjectileAC"},
        {0xE3,"ShieldMeleeAC"},
        {0xE4,"ShieldEnergyAC"},
        {0xE5,"ShieldChemicalAC"},
        {0xE6,"ShieldRadiationAC"},
        {0xE7,"ShieldColdAC"},
        {0xE8,"ShieldNanoAC"},
        {0xE9,"ShieldFireAC"},
        {0xEA,"ShieldPoisonAC"},
        {0xEB,"BerserkMode"},
        {0xEC,"InsurancePercentage"},
        {0xED,"ChangeSideCount"},
        {0xEE,"AbsorbProjectileAC"},
        {0xEF,"AbsorbMeleeAC"},
        {0xF0,"AbsorbEnergyAC"},
        {0xF1,"AbsorbChemicalAC"},
        {0xF2,"AbsorbRadiationAC"},
        {0xF3,"AbsorbColdAC"},
        {0xF4,"AbsorbFireAC"},
        {0xF5,"AbsorbPoisonAC"},
        {0xF6,"AbsorbNanoAC"},
        {0xF7,"TemporarySkillReduction"},
        {0xF8,"BirthDate"},
        {0xF9,"LastSaved"},
        {0xFA,"SoundVolume"},
        {0xFB,"Pets"},
        {0xFC,"MetersWalked"},
        {0xFD,"QuestLevelsSolved"},
        {0xFE,"MonsterLevelsKilled"},
        {0xFF,"PvPLevelsKilled"},
        {0x100,"MissionBits1"},
        {0x101,"MissionBits2"},
        {0x102,"AccessGrant"},
        {0x103,"DoorFlags"},
        {0x104,"ClanHierarchy"},
        {0x105,"QuestStat"},
        {0x106,"ClientActivated"},
        {0x107,"PersonalResearchLevel"},
        {0x108,"GlobalResearchLevel"},
        {0x109,"PersonalResearchGoal"},
        {0x10A,"GlobalResearchGoal"},
        {0x10B,"TurnSpeed"},
        {0x10C,"LiquidType"},
        {0x10D,"GatherSound"},
        {0x10E,"CastSound"},
        {0x10F,"TravelSound"},
        {0x110,"HitSound"},
        {0x111,"SecondaryItemTemplate"},
        {0x112,"EquippedWeapons"},
        {0x113,"XPKillRange"},
        {0x114,"AddAllOff"},
        {0x115,"AddAllDef"},
        {0x116,"ProjectileDamageModifier"},
        {0x117,"MeleeDamageModifier"},
        {0x118,"EnergyDamageModifier"},
        {0x119,"ChemicalDamageModifier"},
        {0x11A,"RadiationDamageModifier"},
        {0x11B,"ItemHateValue"},
        {0x11C,"CriticalBonus"},
        {0x11D,"MaxDamage"},
        {0x11E,"MinDamage"},
        {0x11F,"AttackRange"},
        {0x120,"HateValueModifier"},
        {0x121,"TrapDifficulty"},
        {0x122,"StatOne"},
        {0x123,"NumAttackEffects"},
        {0x124,"DefaultAttackType"},
        {0x125,"ItemSkill"},
        {0x126,"AttackDelay"},
        {0x127,"ItemOpposedSkill"},
        {0x128,"ItemSIS"},
        {0x129,"InteractionRadius"},
        {0x12A,"Slot"},
        {0x12B,"LockDifficulty"},
        {0x12C,"Members"},
        {0x12D,"MinMembers"},
        {0x12E,"ClanPrice"},
        {0x12F,"ClanUpkeep"},
        {0x130,"ClanType"},
        {0x131,"ClanInstance"},
        {0x132,"VoteCount"},
        {0x133,"MemberType"},
        {0x134,"MemberInstance"},
        {0x135,"GlobalClanType"},
        {0x136,"GlobalClanInstance"},
        {0x137,"ColdDamageModifier"},
        {0x138,"ClanUpkeepInterval"},
        {0x139,"TimeSinceUpkeep"},
        {0x13A,"ClanFinalized"},
        {0x13B,"NanoDamageModifier"},
        {0x13C,"FireDamageModifier"},
        {0x13D,"PoisonDamageModifier"},
        {0x13E,"NPCostModifier"},
        {0x13F,"XPModifier"},
        {0x140,"BreedLimit"},
        {0x141,"GenderLimit"},
        {0x142,"LevelLimit"},
        {0x143,"PlayerKilling"},
        {0x144,"TeamAllowed"},
        {0x145,"WeaponDisallowedType"},
        {0x146,"WeaponDisallowedInstance"},
        {0x147,"Taboo"},
        {0x148,"Compulsion"},
        {0x149,"SkillDisabled"},
        {0x14A,"ClanItemType"},
        {0x14B,"ClanItemInstance"},
        {0x14C,"DebuffFormula"},
        {0x14D,"PvP_Rating"},
        {0x14E,"SavedXP"},
        {0x14F,"DoorBlockTime"},
        {0x150,"OverrideTexture"},
        {0x151,"OverrideMaterial"},
        {0x152,"DeathReason"},
        {0x153,"DamageType1"},
        {0x154,"BrainType"},
        {0x155,"XPBonus"},
        {0x156,"HealInterval"},
        {0x157,"HealDelta"},
        {0x158,"MonsterTexture"},
        {0x159,"HasAlwaysLootable"},
        {0x15A,"TradeLimit"},
        {0x15B,"FaceTexture"},
        {0x15C,"SpecialCondition"},
        {0x15D,"AutoAttackFlags"},
        {0x15E,"NextXP"},
        {0x15F,"TeleportPauseMilliSeconds"},
        {0x160,"SISCap"},
        {0x161,"AnimSet"},
        {0x162,"AttackType"},
        {0x163,"NanoFocusLevel"},
        {0x164,"NPCHash"},
        {0x165,"CollisionRadius"},
        {0x166,"OuterRadius"},
        {0x167,"MonsterData"},
        {0x168,"Scale"},
        {0x169,"HitEffectType"},
        {0x16A,"ResurrectDest"},
        {0x16B,"NanoInterval"},
        {0x16C,"NanoDelta"},
        {0x16D,"ReclaimItem"},
        {0x16E,"GatherEffectType"},
        {0x16F,"VisualBreed"},
        {0x170,"VisualProfession"},
        {0x171,"VisualSex"},
        {0x172,"RitualTargetInst"},
        {0x173,"SkillTimeOnSelectedTarget"},
        {0x174,"LastSaveXP"},
        {0x175,"ExtendedTime"},
        {0x176,"BurstRecharge"},
        {0x177,"FullAutoRecharge"},
        {0x178,"GatherAbstractAnim"},
        {0x179,"CastTargetAbstractAnim"},
        {0x17A,"CastSelfAbstractAnim"},
        {0x17B,"CriticalIncrease"},
        {0x17C,"RangeIncreaserWeapon"},
        {0x17D,"NanoRange"},
        {0x17E,"SkillLockModifier"},
        {0x17F,"InterruptModifier"},
        {0x180,"ACGEntranceStyles"},
        {0x181,"ChanceOfBreakOnSpellAttack"},
        {0x182,"ChanceOfBreakOnDebuff"},
        {0x183,"DieAnim"},
        {0x184,"TowerType"},
        {0x185,"Expansion"},
        {0x186,"LowresMesh"},
        {0x187,"CritialResistance"},
        {0x188,"OldTimeExist"},
        {0x189,"ResistModifier"},
        {0x18A,"ChestFlags"},
        {0x18B,"PrimaryTemplateID"},
        {0x18C,"NumberOfItems"},
        {0x18D,"SelectedTargetType"},
        {0x18E,"Corpse_Hash"},
        {0x18F,"AmmoName"},
        {0x190,"Rotation"},
        {0x191,"CATAnim"},
        {0x192,"CATAnimFlags"},
        {0x193,"DisplayCATAnim"},
        {0x194,"DisplayCATMesh"},
        {0x195,"School"},
        {0x196,"NanoSpeed"},
        {0x197,"NanoPoints"},
        {0x198,"TrainSkill"},
        {0x199,"TrainSkillCost"},
        {0x19A,"NumFightingOpponents"},
        {0x19B,"NextFormula"},
        {0x19C,"MultipleCount"},
        {0x19D,"EffectType"},
        {0x19E,"ImpactEffectType"},
        {0x19F,"CorpseType"},
        {0x1A0,"CorpseInstance"},
        {0x1A1,"CorpseAnimKey"},
        {0x1A2,"UnarmedTemplateInstance"},
        {0x1A3,"TracerEffectType"},
        {0x1A4,"AmmoType"},
        {0x1A5,"CharRadius"},
        {0x1A6,"ChanceOfUse"},
        {0x1A7,"CurrentState"},
        {0x1A8,"ArmourType"},
        {0x1A9,"RestModifier"},
        {0x1AA,"BuyModifier"},
        {0x1AB,"SellModifier"},
        {0x1AC,"CastEffectType"},
        {0x1AD,"NPCBrainState"},
        {0x1AE,"WaitState"},
        {0x1AF,"SelectedTarget"},
        {0x1B0,"ErrorCode"},
        {0x1B1,"OwnerInstance"},
        {0x1B2,"CharState"},
        {0x1B3,"ReadOnly"},
        {0x1B4,"DamageType2"},
        {0x1B5,"CollideCheckInterval"},
        {0x1B6,"PlayfieldType"},
        {0x1B7,"NPCCommand"},
        {0x1B8,"InitiativeType"},
        {0x1B9,"CharTmp1"},
        {0x1BA,"CharTmp2"},
        {0x1BB,"CharTmp3"},
        {0x1BC,"CharTmp4"},
        {0x1BD,"NPCCommandArg"},
        {0x1BE,"NameTemplate"},
        {0x1BF,"DesiredTargetDistance"},
        {0x1C0,"VicinityRange"},
        {0x1C1,"NPCIsSurrendering"},
        {0x1C2,"StateMachine"},
        {0x1C3,"NPCSurrenderInstance"},
        {0x1C4,"NPCHasPatrolList"},
        {0x1C5,"NPCVicinityChars"},
        {0x1C6,"ProximityRangeOutdoors"},
        {0x1C7,"NPCFamily"},
        {0x1C8,"CommandRange"},
        {0x1C9,"NPCHatelistSize"},
        {0x1CA,"NPCNumPets"},
        {0x1CB,"ODMinSizeAdd"},
        {0x1CC,"EffectRed"},
        {0x1CD,"EffectGreen"},
        {0x1CE,"EffectBlue"},
        {0x1CF,"ODMaxSizeAdd"},
        {0x1D0,"DurationModifier"},
        {0x1D1,"NPCCryForHelpRange"},
        {0x1D2,"LOSHeight"},
        {0x1D3,"PetReq1"},
        {0x1D4,"PetReq2"},
        {0x1D5,"PetReq3"},
        {0x1D6,"MapOptions"},
        {0x1D7,"MapsA"},
        {0x1D8,"MapsB"},
        {0x1D9,"FixtureFlags"},
        {0x1DA,"FallDamage"},
        {0x1DB,"MaxReflectedProjectileDmg"},
        {0x1DC,"MaxReflectedMeleeDmg"},
        {0x1DD,"MaxReflectedEnergyDmg"},
        {0x1DE,"MaxReflectedChemicalDmg"},
        {0x1DF,"MaxReflectedRadiationDmg"},
        {0x1E0,"MaxReflectedColdDmg"},
        {0x1E1,"MaxReflectedNanoDmg"},
        {0x1E2,"MaxReflectedFireDmg"},
        {0x1E3,"MaxReflectedPoisonDmg"},
        {0x1E4,"ProximityRangeIndoors"},
        {0x1E5,"PetReqVal1"},
        {0x1E6,"PetReqVal2"},
        {0x1E7,"PetReqVal3"},
        {0x1E8,"TargetFacing"},
        {0x1E9,"Backstab"},
        {0x1EA,"OriginatorType"},
        {0x1EB,"QuestInstance"},
        {0x1EC,"QuestIndex1"},
        {0x1ED,"QuestIndex2"},
        {0x1EE,"QuestIndex3"},
        {0x1EF,"QuestIndex4"},
        {0x1F0,"QuestIndex5"},
        {0x1F1,"QTDungeonInstance"},
        {0x1F2,"QTNumMonsters"},
        {0x1F3,"QTKilledMonsters"},
        {0x1F4,"AnimPos"},
        {0x1F5,"AnimPlay"},
        {0x1F6,"AnimSpeed"},
        {0x1F7,"QTKillNumMonsterID1"},
        {0x1F8,"QTKillNumMonsterCount1"},
        {0x1F9,"QTKillNumMonsterID2"},
        {0x1FA,"QTKillNumMonsterCount2"},
        {0x1FB,"QTKillNumMonsterID3"},
        {0x1FC,"QTKillNumMonsterCount3"},
        {0x1FD,"QuestIndex0"},
        {0x1FE,"QuestTimeout"},
        {0x1FF,"Tower_NPCHash"},
        {0x200,"PetType"},
        {0x201,"OnTowerCreation"},
        {0x202,"OwnedTowers"},
        {0x203,"TowerInstance"},
        {0x204,"AttackShield"},
        {0x205,"SpecialAttackShield"},
        {0x206,"NPCVicinityPlayers"},
        {0x207,"NPCUseFightModeRegenRate"},
        {0x208,"Rnd"},
        {0x209,"SocialStatus"},
        {0x20A,"LastRnd"},
        {0x20B,"AttackDelayCap"},
        {0x20C,"RechargeDelayCap"},
        {0x20D,"PercentRemainingHealth"},
        {0x20E,"PercentRemainingNano"},
        {0x20F,"TargetDistance"},
        {0x210,"TeamCloseness"},
        {0x211,"NumberOnHateList"},
        {0x212,"ConditionState"},
        {0x213,"ExpansionPlayfield"},
        {0x214,"ShadowBreed"},
        {0x215,"NPCFovStatus"},
        {0x216,"DudChance"},
        {0x217,"HealMultiplier"},
        {0x218,"NanoDamageMultiplier"},
        {0x219,"NanoVulnerability"},
        {0x21A,"AMSCap"},
        {0x21B,"ProcInitiative1"},
        {0x21C,"ProcInitiative2"},
        {0x21D,"ProcInitiative3"},
        {0x21E,"ProcInitiative4"},
        {0x21F,"FactionModifier"},
        {0x220,"MissionBits8"},
        {0x221,"MissionBits9"},
        {0x222,"StackingLine2"},
        {0x223,"StackingLine3"},
        {0x224,"StackingLine4"},
        {0x225,"StackingLine5"},
        {0x226,"StackingLine6"},
        {0x227,"StackingOrder"},
        {0x228,"ProcNano1"},
        {0x229,"ProcNano2"},
        {0x22A,"ProcNano3"},
        {0x22B,"ProcNano4"},
        {0x22C,"ProcChance1"},
        {0x22D,"ProcChance2"},
        {0x22E,"ProcChance3"},
        {0x22F,"ProcChance4"},
        {0x230,"OTArmedForces"},
        {0x231,"ClanSentinels"},
        {0x232,"OTMed"},
        {0x233,"ClanGaia"},
        {0x234,"OTTrans"},
        {0x235,"ClanVanguards"},
        {0x236,"GOS"},
        {0x237,"OTFollowers"},
        {0x238,"OTOperator"},
        {0x239,"OTUnredeemed"},
        {0x23A,"ClanDevoted"},
        {0x23B,"ClanConserver"},
        {0x23C,"ClanRedeemed"},
        {0x23D,"SK"},
        {0x23E,"LastSK"},
        {0x23F,"NextSK"},
        {0x240,"PlayerOptions"},
        {0x241,"LastPerkResetTime"},
        {0x242,"CurrentTime"},
        {0x243,"ShadowBreedTemplate"},
        {0x244,"NPCVicinityFamily"},
        {0x245,"NPCScriptAMSScale"},
        {0x246,"ApartmentsAllowed"},
        {0x247,"ApartmentsOwned"},
        {0x248,"ApartmentAccessCard"},
        {0x249,"MapsC"},
        {0x24A,"MapsD"},
        {0x24B,"NumberOfTeamMembers"},
        {0x24C,"ActionCategory"},
        {0x24D,"PlayfieldProxy"},
        {0x24E,"DistrictNano"},
        {0x24F,"DistrictNanoInterval"},
        {0x250,"UnsavedXP"},
        {0x251,"RegainXPPercentage"},
        {0x252,"TempSaveTeamID"},
        {0x253,"TempSavePlayfield"},
        {0x254,"TempSaveX"},
        {0x255,"TempSaveY"},
        {0x256,"ExtendedFlags"},
        {0x257,"ShopPrice"},
        {0x258,"NewbieHP"},
        {0x259,"HPLevelUp"},
        {0x25A,"HPPerSkill"},
        {0x25B,"NewbieNP"},
        {0x25C,"NPLevelUp"},
        {0x25D,"NPPerSkill"},
        {0x25E,"MaxShopItems"},
        {0x25F,"PlayerID"},
        {0x260,"ShopRent"},
        {0x261,"SynergyHash"},
        {0x262,"ShopFlags"},
        {0x263,"ShopLastUsed"},
        {0x264,"ShopType"},
        {0x265,"LockDownTime"},
        {0x266,"LeaderLockDownTime"},
        {0x267,"InvadersKilled"},
        {0x268,"KilledByInvaders"},
        {0x269,"MissionBits10"},
        {0x26A,"MissionBits11"},
        {0x26B,"MissionBits12"},
        {0x26C,"HouseTemplate"},
        {0x26D,"PercentFireDamage"},
        {0x26E,"PercentColdDamage"},
        {0x26F,"PercentMeleeDamage"},
        {0x270,"PercentProjectileDamage"},
        {0x271,"PercentPoisonDamage"},
        {0x272,"PercentRadiationDamage"},
        {0x273,"PercentEnergyDamage"},
        {0x274,"PercentChemicalDamage"},
        {0x275,"TotalDamage"},
        {0x276,"TrackProjectileDamage"},
        {0x277,"TrackMeleeDamage"},
        {0x278,"TrackEnergyDamage"},
        {0x279,"TrackChemicalDamage"},
        {0x27A,"TrackRadiationDamage"},
        {0x27B,"TrackColdDamage"},
        {0x27C,"TrackPoisonDamage"},
        {0x27D,"TrackFireDamage"},
        {0x27E,"NPCSpellArg"},
        {0x27F,"NPCSpellRet"},
        {0x280,"CityInstance"},
        {0x281,"DistanceToSpawnpoint"},
        {0x282,"CityTerminalRechargePercent"},
        {0x289,"HasUnreadMail"},
        {0x28B,"AdvantageHash1"},
        {0x28C,"AdvantageHash2"},
        {0x28D,"AdvantageHash3"},
        {0x28E,"AdvantageHash4"},
        {0x28F,"AdvantageHash5"},
        {0x290,"ShopIndex"},
        {0x291,"ShopID"},
        {0x292,"IsVehicle"},
        {0x293,"DamageToNano"},
        {0x294,"AccountFlags"},
        {0x295,"DamageToNanoMultiplier"},
        {0x296,"MechData"},
        {0x297,"PointValue"},
        {0x298,"VehicleAC"},
        {0x299,"VehicleDamage"},
        {0x29A,"VehicleHealth"},
        {0x29B,"VehicleSpeed"},
        {0x29C,"BattlestationSide"},
        {0x29D,"VP"},
        {0x29E,"BattlestationRep"},
        {0x29F,"PetState"},
        {0x2A0,"PaidPoints"},
        {0x2A1,"VisualFlags"},
        {0x2A2,"PVPDuelKills"},
        {0x2A3,"PVPDuelDeaths"},
        {0x2A4,"PVPProfessionDuelKills"},
        {0x2A5,"PVPProfessionDuelDeaths"},
        {0x2A6,"PVPRankedSoloKills"},
        {0x2A7,"PVPRankedSoloDeaths"},
        {0x2A8,"PVPRankedTeamKills"},
        {0x2A9,"PVPRankedTeamDeaths"},
        {0x2AA,"PVPSoloScore"},
        {0x2AB,"PVPTeamScore"},
        {0x2AC,"PVPDuelScore"},
        {0x2AD,"MissionBits14"},
        {0x2AE,"MissionBits15"},
        {0x2B0,"Rarity"},
        {0x2B1,"HealReactivity"},
        {0x2B2,"EquippedRHWeapon"},
        {0x2B3,"FullIPRPoints"},
        {0x2B4,"MissionBits16"},
        {0x2B5,"MissionBits17"},
        {0x2B6,"MissionBits18"},
        {0x2B7,"Commendations"},
        {0x2B8,"DailyMissionResets"},
        {0x2B9,"VeteranMonths"},
        {0x2BC,"ACGItemSeed"},
        {0x2BD,"ACGItemLevel"},
        {0x2BE,"ACGItemTemplateID"},
        {0x2BF,"ACGItemTemplateID2"},
        {0x2C0,"ACGItemCategoryID"},
        {0x300,"HasKnubotData"},
        {0x320,"QuestBoothDifficulty"},
        {0x321,"QuestASMinimumRange"},
        {0x322,"QuestASMaximumRange"},
        {0x378,"VisualLODLevel"},
        {0x379,"TargetDistanceChange"},
        {0x384,"TideRequiredDynelID"},
        {0x3E7,"StreamCheckMagic"},
        {0x3E9,"Type"},
    };
    static string SN(int n) => StatNames.TryGetValue(n, out var s) ? s : ("stat_" + n);

    static readonly Dictionary<int, string> ProfNames = new()
    {
        {1,"Soldier"},{2,"MartialArtist"},{3,"Engineer"},{4,"Fixer"},{5,"Agent"},
        {6,"Adventurer"},{7,"Trader"},{8,"Bureaucrat"},{9,"Enforcer"},{10,"Doctor"},
        {11,"NanoTechnician"},{12,"Metaphysicist"},{13,"Monster"},{14,"Keeper"},{15,"Shade"},
    };

    class Req { public int ChildOperator, Operator, Statnumber, Target, Value; }
    class Func { public int FunctionType, Target; public List<object> Args = new(); }

    // ---------------------------------------------------------------------
    // Parameterised symbiant extractor.
    //
    //   mp-symbiant-extractor [--out <file>] [--prof <id>] [--slug <name>]
    //                         [--match <substring>] [--ocp <path>] [--names <path>]
    //
    // Default --prof 12 (Metaphysicist). Switching profession for the other 13
    // classes is ONLY a --prof/--slug change; nothing below is MP-specific.
    //
    // It walks every template in items.ocp, keeps the ones whose itemnames.sql
    // name contains --match (default "Symbiant") AND that carry a ToWear action,
    // and for each records: QL, ToWear requirements, the profession lock set
    // (stat 60 Profession / stat 368 VisualProfession EqualTo values), whether
    // the target profession may wear it, and the worn stat bonuses (event
    // functions of type 53045 = ModifyStat, arg0 = stat, arg1 = amount).
    // Name-derived fields (line / unit / slot / tier word) are parsed from the
    // item name only - never invented.
    // ---------------------------------------------------------------------
    const int FuncModifyStat = 53045;

    static readonly string[] SymbiantLines = { "Artillery", "Control", "Extermination", "Infantry", "Support" };

    static readonly string[] SymbiantSlots = {
        "Brain", "Head", "Ocular", "Ear", "Chest", "Left Arm", "Right Arm", "Left Wrist", "Right Wrist",
        "Left Hand", "Right Hand", "Waist", "Thigh", "Feet"
    };

    // Slot word in the item name -> AOSharp EquipSlot (EquipSlot.cs Imp_* entries).
    static readonly Dictionary<string, string> SlotToEquipSlot = new()
    {
        {"Brain","Imp_Head"}, {"Head","Imp_Head"}, {"Ocular","Imp_Eye"}, {"Ear","Imp_Ear"}, {"Chest","Imp_Body"},
        {"Left Arm","Imp_LeftArm"}, {"Right Arm","Imp_RightArm"},
        {"Left Wrist","Imp_LeftWrist"}, {"Right Wrist","Imp_RightWrist"},
        {"Left Hand","Imp_LeftHand"}, {"Right Hand","Imp_RightHand"},
        {"Waist","Imp_Waist"}, {"Thigh","Imp_Legs"}, {"Feet","Imp_Feet"},
    };

    // ------------------------------------------------------------------
    // Gear extractor: every ToWear template in items.ocp, with its Slot
    // bitmask (stat 0x12A), ToWear requirements, profession lock and worn
    // stat bonuses. Nothing invented - every field read from the pack.
    //   eng-gear-extractor [--prof N] [--slug s] [--match sub] [--out f]
    //                      [--minbonus N] [--statfilter 130,131,...]
    // ------------------------------------------------------------------

    // Clothing-page slot bit = (EquipSlot & 0x0F) for the Cloth_* entries.
    static readonly Dictionary<int,string> ClothBits = new()
    {
        {1,"Neck"},{2,"Head"},{3,"Back"},{4,"RightShoulder"},{5,"Body"},{6,"LeftShoulder"},
        {7,"RightArm"},{8,"Hands"},{9,"LeftArm"},{10,"RightWrist"},{11,"Legs"},{12,"LeftWrist"},
        {13,"RightFinger"},{14,"Feet"},{15,"LeftFinger"},
    };
    static readonly Dictionary<int,string> WeapBits = new()
    {
        {1,"HUD1"},{2,"HUD3"},{3,"Utils1"},{4,"Utils2"},{5,"Utils3"},{6,"RightHand"},{7,"Belt"},
        {8,"LeftHand"},{9,"Deck1"},{10,"Deck2"},{11,"Deck3"},{12,"Deck4"},{13,"Deck5"},{14,"Deck6"},
        {15,"HUD2"},
    };

    static List<string> DecodeSlots(int mask, Dictionary<int,string> bits)
    {
        var l = new List<string>();
        for (int b = 0; b < 16; b++)
            if ((mask & (1 << b)) != 0 && bits.TryGetValue(b, out var n)) l.Add(n);
        return l;
    }

    static void Main(string[] args)
    {
        string ocp = @"E:\Funcom\OmniCell\OmniCell\Datafiles\items.ocp";
        string namesPath = @"E:\Funcom\attic\extracted-client-data\itemnames.sql";
        string outPath = null, slug = null, match = null, statFilter = null;
        int prof = 3, minBonus = 0;
        bool includeWield = false;

        // --nodrop <out.bin>: every template whose item Flags (stat 0) has NoDrop (bit 26, OmniCell ItemFlags /
        // ItemTemplate.IsNoDrop), for AOBuddy's sell rule (GameData/ItemNoDrop.bin: "AOND", 1, count, ids).
        int nd = Array.IndexOf(args, "--nodrop");
        if (nd >= 0) { NoDropTable(ocp, namesPath, args[nd + 1]); return; }

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--out":        outPath    = args[++i]; break;
                case "--prof":       prof       = int.Parse(args[++i]); break;
                case "--slug":       slug       = args[++i]; break;
                case "--match":      match      = args[++i]; break;
                case "--ocp":        ocp        = args[++i]; break;
                case "--names":      namesPath  = args[++i]; break;
                case "--minbonus":   minBonus   = int.Parse(args[++i]); break;
                case "--statfilter": statFilter = args[++i]; break;
                case "--wield":      includeWield = true; break;
                default:
                    if (outPath == null && !args[i].StartsWith("--")) outPath = args[i];
                    break;
            }
        }
        slug ??= (ProfNames.TryGetValue(prof, out var pn) ? pn.ToLowerInvariant() : ("prof" + prof));
        outPath ??= Path.Combine(Path.GetTempPath(), slug + "_gear_raw.json");
        HashSet<int> wanted = statFilter == null ? null
            : new HashSet<int>(statFilter.Split(',').Select(x => int.Parse(x.Trim())));

        var names = LoadNames(namesPath);
        Console.WriteLine($"names loaded: {names.Count}");
        Console.WriteLine($"profession: {prof}  slug={slug}  match={match ?? "(all)"}  minbonus={minBonus}");

        var items = new List<object>();
        int total = 0, named = 0, wearable = 0, usable = 0, emitted = 0;

        using (var fs = File.OpenRead(ocp))
        using (var gz = new GZipStream(fs, CompressionMode.Decompress))
        using (var r = new BinaryReader(gz, Encoding.UTF8))
        {
            string magic = r.ReadString();
            if (magic != "OMNICELL-CONTENT") throw new InvalidDataException("bad magic: " + magic);
            int version = r.ReadInt32();
            byte kind = r.ReadByte();
            int count = r.ReadInt32();
            Console.WriteLine($"pack: magic={magic} version={version} kind={kind} count={count}");

            for (int i = 0; i < count; i++)
            {
                int id = r.ReadInt32();
                int flags = r.ReadInt32();
                int itemType = r.ReadInt32();
                int multipleCount = r.ReadInt32();
                int nothing = r.ReadInt32();
                int quality = r.ReadInt32();
                SkipDict(r);
                SkipDict(r);
                var stats = ReadDict(r);
                SkipIntList(r);
                var actions = ReadActions(r);
                var events = ReadEvents(r, version);
                if (version >= 2) ReadRecordData(r, version);
                total++;

                if (!names.TryGetValue(id, out var nm)) continue;
                named++;
                if (match != null && nm.name.IndexOf(match, StringComparison.OrdinalIgnoreCase) < 0) continue;

                var wearActions = actions.Where(a => a.type == ToWear).ToList();
                string equipVia = "ToWear";
                if (wearActions.Count == 0)
                {
                    if (!includeWield) continue;
                    wearActions = actions.Where(a => a.type == ToWield).ToList();
                    equipVia = "ToWield";
                    if (wearActions.Count == 0) continue;
                }
                wearable++;

                var wearReqs = wearActions.SelectMany(a => a.reqs).Where(x => x.Statnumber != 0).ToList();
                int itemFlags = stats.TryGetValue(0, out var fv) ? fv : 0;
                int slotMask = stats.TryGetValue(0x12A, out var sm) ? sm : 0;

                var lockSet = wearReqs
                    .Where(x => (x.Statnumber == 60 || x.Statnumber == 368) && x.Operator == OpEqualTo)
                    .Select(x => x.Value).Distinct().OrderBy(v => v).ToList();
                bool profUsable = lockSet.Count == 0 || lockSet.Contains(prof);
                if (!profUsable) continue;
                usable++;

                var bonuses = events.SelectMany(e => e.funcs)
                    .Where(f => f.FunctionType == FuncModifyStat && f.Args.Count >= 2
                                && f.Args[0] is int && f.Args[1] is int)
                    .Select(f => new { statNum = (int)f.Args[0], amount = (int)f.Args[1] })
                    .GroupBy(x => x.statNum)
                    .Select(g => new { stat = SN(g.Key), statNum = g.Key, amount = g.Sum(x => x.amount) })
                    .ToList();

                if (wanted != null && !bonuses.Any(b => wanted.Contains(b.statNum) && b.amount >= minBonus)) continue;
                if (wanted == null && minBonus > 0 && !bonuses.Any(b => b.amount >= minBonus)) continue;
                emitted++;

                var clothSlots = DecodeSlots(slotMask, ClothBits);
                var weapSlots  = DecodeSlots(slotMask, WeapBits);

                items.Add(new {
                    id,
                    name = nm.name,
                    itemTypeName = nm.type,
                    ql = quality,
                    itemType,
                    equipVia,
                    slotMask,
                    clothSlots,
                    weaponSlots = weapSlots,
                    flags = itemFlags,
                    noDrop = (itemFlags & (1 << 26)) != 0,
                    unique = (itemFlags & (1 << 27)) != 0,
                    professionLock = lockSet,
                    professionLockNames = lockSet.Select(v => ProfNames.TryGetValue(v, out var x) ? x : ("prof" + v)).ToList(),
                    toWear = wearReqs.Select(x => new { stat = SN(x.Statnumber), statNum = x.Statnumber, op = OpName(x.Operator), value = x.Value }).ToList(),
                    itemClass = stats.TryGetValue(0x4C, out var ic) ? ic : 0,
                    primaryItemType = stats.TryGetValue(0x50, out var pit) ? pit : 0,
                    can = stats.TryGetValue(0x1E, out var cn) ? cn : 0,
                    bonuses
                });
            }
        }

        Console.WriteLine($"templates={total} named={named} wearable={wearable} usableByProf{prof}={usable} emitted={emitted}");
        var opts = new JsonSerializerOptions { WriteIndented = false };
        File.WriteAllText(outPath, JsonSerializer.Serialize(new {
            profession = prof,
            professionName = ProfNames.TryGetValue(prof, out var p3) ? p3 : null,
            slug, match, statFilter, minBonus,
            source = new { ocp, names = namesPath },
            counts = new { templatesRead = total, named, wearable, usableByProfession = usable, emitted },
            items
        }, opts));
        Console.WriteLine($"dump written: {outPath}");
    }

    static void NoDropTable(string ocp, string namesPath, string outBin)
    {
        var names = LoadNames(namesPath);
        var ids = new List<int>();
        int total = 0;
        using (var fs = File.OpenRead(ocp))
        using (var gz = new GZipStream(fs, CompressionMode.Decompress))
        using (var r = new BinaryReader(gz, Encoding.UTF8))
        {
            if (r.ReadString() != "OMNICELL-CONTENT") throw new InvalidDataException("bad magic");
            int version = r.ReadInt32(); r.ReadByte(); int count = r.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                int id = r.ReadInt32();
                r.ReadInt32(); r.ReadInt32(); r.ReadInt32(); r.ReadInt32(); r.ReadInt32();
                SkipDict(r); SkipDict(r);
                var stats = ReadDict(r);
                SkipIntList(r);
                ReadActions(r); ReadEvents(r, version);
                if (version >= 2) ReadRecordData(r, version);
                total++;
                if (stats.TryGetValue(0, out var f) && (f & (1 << 26)) != 0) ids.Add(id);
            }
        }
        using (var w = new BinaryWriter(File.Create(outBin)))
        {
            w.Write(Encoding.ASCII.GetBytes("AOND")); w.Write(1); w.Write(ids.Count);
            foreach (var id in ids) w.Write(id);
        }
        Console.WriteLine($"templates={total} nodrop={ids.Count} -> {outBin}");
        foreach (var id in ids.Where(names.ContainsKey).Take(25)) Console.WriteLine($"  {id} {names[id].name}");
    }

    static string OpName(int op) => op switch
    {
        OpEqualTo => "EqualTo", OpLessThan => "LessThan", OpGreaterThan => "GreaterThan",
        OpUnequal => "Unequal", OpBitAnd => "BitAnd", OpNotBitAnd => "NotBitAnd", OpNot => "Not",
        OpOr => "Or", OpAnd => "And", _ => $"Op{op}"
    };

    // ---- format readers (mirror OmniCellContentPack) ----
    static void SkipDict(BinaryReader r) { int c = r.ReadInt32(); for (int i = 0; i < c; i++) { r.ReadInt32(); r.ReadInt32(); } }
    static Dictionary<int,int> ReadDict(BinaryReader r) { int c = r.ReadInt32(); var d = new Dictionary<int,int>(c); for (int i = 0; i < c; i++) { int k = r.ReadInt32(); int v = r.ReadInt32(); d[k] = v; } return d; }
    static void SkipIntList(BinaryReader r) { int c = r.ReadInt32(); for (int i = 0; i < c; i++) r.ReadInt32(); }
    static List<int> ReadIntList(BinaryReader r) { int c = r.ReadInt32(); var l = new List<int>(c); for (int i = 0; i < c; i++) l.Add(r.ReadInt32()); return l; }
    static byte[] ReadBytes(BinaryReader r) { int c = r.ReadInt32(); return r.ReadBytes(c); }

    static List<Req> ReadRequirements(BinaryReader r)
    {
        int c = r.ReadInt32();
        var l = new List<Req>(c);
        for (int i = 0; i < c; i++)
            l.Add(new Req { ChildOperator = r.ReadInt32(), Operator = r.ReadInt32(), Statnumber = r.ReadInt32(), Target = r.ReadInt32(), Value = r.ReadInt32() });
        return l;
    }

    static List<(int type, List<Req> reqs)> ReadActions(BinaryReader r)
    {
        int c = r.ReadInt32();
        var l = new List<(int, List<Req>)>(c);
        for (int i = 0; i < c; i++) { int t = r.ReadInt32(); l.Add((t, ReadRequirements(r))); }
        return l;
    }

    static List<(int type, List<Func> funcs)> ReadEvents(BinaryReader r, int version)
    {
        int c = r.ReadInt32();
        var evs = new List<(int, List<Func>)>(c);
        for (int i = 0; i < c; i++)
        {
            int evType = r.ReadInt32();
            int fns = r.ReadInt32();
            var fl = new List<Func>(fns);
            for (int j = 0; j < fns; j++) fl.Add(ReadFunction(r, version));
            evs.Add((evType, fl));
        }
        return evs;
    }

    static Func ReadFunction(BinaryReader r, int version)
    {
        var f = new Func();
        f.FunctionType = r.ReadInt32();
        f.Target = r.ReadInt32();
        r.ReadInt32(); r.ReadUInt32(); r.ReadBoolean(); // TickCount,TickInterval,dolocalstats
        ReadRequirements(r);
        int args = r.ReadInt32();
        for (int i = 0; i < args; i++)
        {
            byte kind = r.ReadByte();
            if (kind == 1) f.Args.Add(r.ReadInt32());
            else if (kind == 2) f.Args.Add(r.ReadSingle());
            else if (kind == 3) f.Args.Add(r.ReadString());
            else throw new InvalidDataException("bad arg kind " + kind);
        }
        if (version >= 2 && r.ReadBoolean())
        {
            r.ReadInt32(); r.ReadInt32(); r.ReadInt32(); r.ReadInt32();
            SkipIntList(r);
            ReadBytes(r);
        }
        return f;
    }

    static void ReadRecordData(BinaryReader r, int version)
    {
        if (!r.ReadBoolean()) return;
        r.ReadInt32(); r.ReadInt32(); r.ReadInt32();
        r.ReadString();
        SkipIntList(r);
        int groups = r.ReadInt32();
        for (int i = 0; i < groups; i++) { r.ReadInt32(); SkipIntList(r); }
        SkipIntList(r);
        int sets = r.ReadInt32();
        for (int i = 0; i < sets; i++)
        {
            r.ReadInt32(); r.ReadInt32();
            int entries = r.ReadInt32();
            for (int j = 0; j < entries; j++) { r.ReadInt32(); SkipIntList(r); }
        }
        int actions = r.ReadInt32();
        for (int i = 0; i < actions; i++) { r.ReadInt32(); SkipIntList(r); }
        int shops = r.ReadInt32();
        for (int i = 0; i < shops; i++)
        {
            r.ReadInt32();
            int entries = r.ReadInt32();
            for (int j = 0; j < entries; j++) ReadBytes(r);
        }
        if (version >= 3)
        {
            int fns = r.ReadInt32();
            for (int i = 0; i < fns; i++) ReadFunction(r, version);
        }
    }

    static Dictionary<int, (string name, string type)> LoadNames(string path)
    {
        var d = new Dictionary<int, (string, string)>(150000);
        string text = File.ReadAllText(path);
        var rx = new Regex(@"\(\s*(\d+)\s*,\s*'((?:[^']|'')*)'\s*,\s*'((?:[^']|'')*)'", RegexOptions.Compiled);
        foreach (Match m in rx.Matches(text))
        {
            int id = int.Parse(m.Groups[1].Value);
            d[id] = (m.Groups[2].Value.Replace("''", "'"), m.Groups[3].Value.Replace("''", "'"));
        }
        return d;
    }
}
