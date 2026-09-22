# Generates metaphysicist-weapons.json from the extractor's raw dump.
# Every weapon value is pulled straight from mp_weapons_raw.json (which came from items.ocp),
# so nothing is transcribed by hand.
param(
  [string]$Raw = "$env:TEMP\mp_weapons_raw.json",
  [string]$OutDir = "E:\Funcom\AOBuddy10\AOBuddy\GameData\profiles"
)
$ErrorActionPreference = "Stop"
$all = Get-Content $Raw -Raw | ConvertFrom-Json

function Pick($id) {
  $w = $all | Where-Object { $_.Id -eq $id } | Select-Object -First 1
  if ($null -eq $w) { throw "id $id not found in raw dump" }
  [ordered]@{
    id        = $w.Id
    name      = $w.Name
    ql        = $w.Ql
    primaryWield = $w.PrimaryWield
    wield     = @($w.Wield | ForEach-Object { [ordered]@{ skill = $_.skill; requiredValue = $_.req } })
    specials  = @($w.Specials | ForEach-Object { [ordered]@{ special = $_.special; requiredValue = $_.req } })
    professionLock = @($w.ProfReqs)
    levelReq  = $w.LevelReq
  }
}

# Practical MP weapon types. cost: wiki says MP spends less IP on 1HB, 2HE, Bow, Pistol.
$typeMeta = @(
  @{ skill="Bow";         statId=111; cost="reduced (MP green)"; role="Primary MP ranged weapon line (signature). MP-only Shadowlands bows + generic bows."; reps=@(214333,214338,122719,212368,271438,122785,288286) },
  @{ skill="Pistol";      statId=112; cost="reduced (MP green)"; role="Common MP ranged option; single or dual-wield.";                 reps=@(122990,123786,122282,288293) },
  @{ skill="1hBlunt";     statId=102; cost="reduced (MP green)"; role="Melee / energy shields (often paired with Melee Energy).";       reps=@(122354,245669,288292) },
  @{ skill="2hEdged";     statId=105; cost="reduced (MP green)"; role="Cheap melee skill; MP-only 2he weapons exist.";                  reps=@(246715) },
  @{ skill="MeleeEnergy"; statId=104; cost="standard (guide-recommended)"; role="MP-only energy weapons/shields (Jupiter Aegis, The Argument).";  reps=@(208069,223467,226329) },
  @{ skill="1hEdged";     statId=103; cost="standard"; role="MP-only Shank of Maze line.";                                              reps=@(238956) },
  @{ skill="2hBlunt";     statId=107; cost="standard"; role="MP-only 2hb weapons (Howlet).";                                            reps=@(246705) }
)

$types = foreach ($t in $typeMeta) {
  $ofType = $all | Where-Object { $_.PrimaryWield -eq $t.skill }
  $mpUsable = $ofType | Where-Object { $_.MpUsable }
  $specUnion = $mpUsable | ForEach-Object { $_.Specials } | ForEach-Object { $_.special } | Sort-Object -Unique
  [ordered]@{
    weaponSkill          = $t.skill
    wieldSkillStatId     = $t.statId
    mpSkillCost          = $t.cost
    role                 = $t.role
    weaponsTotalInData   = @($ofType).Count
    weaponsMpUsable      = @($mpUsable).Count
    allowedSpecialsObservedForType = @($specUnion)
    representativeWeapons = @($t.reps | ForEach-Object { Pick $_ })
  }
}

# MP-only weapons summary
$mponly = $all | Where-Object { $_.ProfReqs -match 'EqualTo Metaphysicist' }
$mponlyByType = $mponly | Group-Object PrimaryWield | Sort-Object Count -Descending | ForEach-Object {
  [ordered]@{ weaponSkill = $_.Name; count = $_.Count }
}

$doc = [ordered]@{
  profile = "Metaphysicist weapons + allowed special attacks (for AOBuddy10 bot decision-making)"
  profession = [ordered]@{ id = 12; name = "Metaphysicist" }
  generatedUtc = (Get-Date).ToUniversalTime().ToString("s") + "Z"
  dataSource = [ordered]@{
    itemData   = "E:\Funcom\OmniCell\OmniCell\Datafiles\items.ocp (OMNICELL-CONTENT v3, client 18.8.50_EP1) - full converted item pack"
    itemNames  = "E:\Funcom\attic\extracted-client-data\itemnames.sql"
    extractor  = "E:\Funcom\AOBuddy10\tools\mp-weapon-extractor (net10)"
    weaponsInData = @($all).Count
    note = "A weapon's wield requirements live in its ToWield action (OmniCell.Enums.ActionType.ToWield=8). Operator.GreaterThan(2) means 'stat >= value'. Special-attack stats present in those requirements = the specials that weapon allows. Requirement VALUES scale with the template's QL (low/high endpoints); values shown are for the listed QL."
  }
  howToReadSpecialsAtRuntime = "For the equipped weapon, read its ToWield criteria: any requirement whose stat is in specialAttackStats is a special that weapon allows (the value is the skill needed at that QL). Wield-skill stat(s) give the weapon type. This mirrors the item data exactly."
  specialAttackStats = [ordered]@{
    Brawl=142; Dimach=144; SneakAttack=146; FastAttack=147; Burst=148; FlingShot=150; AimedShot=151; FullAuto=167; Backstab=489
    passiveNotOnDemand = @("Riposte(143)","Parry(145)")
    note = "BowSpecialAttack(121) is a support SKILL for bow Aimed Shot, not an on-demand special; a bow's on-demand special still shows as AimedShot(151)/FlingShot(150) in criteria."
  }
  professionRestrictionEncoding = [ordered]@{
    stat = "Profession (stat 60 / 0x3C)"
    form = "A ToWield Requirement with Statnumber=60. Operator EqualTo(0) + Value=<professionId> locks the weapon to that one profession."
    mpUsableRule = "Weapon is MP-equippable unless it has a Profession EqualTo lock to a profession other than 12 (or Unequal/Not 12). Weapons with no profession requirement are usable by any profession if the skill is met. 59 weapons are locked EqualTo Metaphysicist(12)."
    example = "id 271438 'Bow-Blaster - 402' ql300: ToWield = Bow>=1500, AimedShot>=750, no profession lock -> MP-usable."
  }
  dualWieldAndMultiSkills = [ordered]@{
    note = "wield now lists EVERY trainable-skill requirement in the weapon's ToWield action (weapon skill(s) plus any Multi Melee/Multi Ranged or other skill), each as {skill, requiredValue}. specials lists the on-demand special-attack skills."
    multiMeleeStatId = 101
    multiRangedStatId = 134
    finding = ("In this data (client 18.8.50_EP1) MultiMelee(101)/MultiRanged(134) appear as a ToWield criterion on ONLY " + (@($all | Where-Object { $_.Wield.skill -contains 'MultiRanged' }).Count) + " weapons - all 'Small Ebony Figurine' variants locked to Soldier/Engineer/Fixer/Agent/Adventurer/Enforcer (NOT Metaphysicist). No MP-usable weapon carries a Multi requirement.")
    dualWieldMechanic = "Dual-wielding is gated by the CHARACTER's Multi Melee / Multi Ranged skill for the off-hand slot, not by a per-weapon ToWield criterion. An MP dual-wields by training Multi Ranged/Melee and equipping two normal weapons (e.g. two Pistols); each weapon still only requires its own wield skill in criteria."
  }
  usableWeaponTypes = @($types)
  metaphysicistOnlyWeapons = [ordered]@{
    count = @($mponly).Count
    byWeaponSkill = @($mponlyByType)
    note = "These have a Profession EqualTo Metaphysicist(12) lock in ToWield - designed for MPs. Names include Sprite Bow, Bow of Sympathy, Shank of Maze, Howlet, Panther, Jupiter Aegis, The Argument."
  }
  technicallyEquippableButAtypical = [ordered]@{
    note = "Any weapon whose only gate is a wield skill can be equipped by an MP IF that skill is raised (implants/buffs), but these types are not part of normal MP play (expensive weapon IP for a nano profession). Counts are MP-usable weapons present in the data."
    types = @(
      @{ skill="AssaultRifle"; mpUsableInData=($all|?{$_.PrimaryWield -eq 'AssaultRifle' -and $_.MpUsable}).Count },
      @{ skill="Rifle";        mpUsableInData=($all|?{$_.PrimaryWield -eq 'Rifle' -and $_.MpUsable}).Count },
      @{ skill="Piercing";     mpUsableInData=($all|?{$_.PrimaryWield -eq 'Piercing' -and $_.MpUsable}).Count },
      @{ skill="Shotgun";      mpUsableInData=($all|?{$_.PrimaryWield -eq 'Shotgun' -and $_.MpUsable}).Count },
      @{ skill="MGSMG";        mpUsableInData=($all|?{$_.PrimaryWield -eq 'MGSMG' -and $_.MpUsable}).Count },
      @{ skill="RangedEnergy"; mpUsableInData=($all|?{$_.PrimaryWield -eq 'RangedEnergy' -and $_.MpUsable}).Count },
      @{ skill="Grenade";      mpUsableInData=($all|?{$_.PrimaryWield -eq 'Grenade' -and $_.MpUsable}).Count },
      @{ skill="MartialArts";  mpUsableInData=($all|?{$_.PrimaryWield -eq 'MartialArts' -and $_.MpUsable}).Count },
      @{ skill="HeavyWeapons"; mpUsableInData=($all|?{$_.PrimaryWield -eq 'HeavyWeapons' -and $_.MpUsable}).Count }
    )
  }
  unverifiedOrCaveats = @(
    "MP skill-cost color (which weapon skills are 'green'/cheap) comes from AOWiki (wiki.aodb.us Meta-Physicist / Meta-Physicist:Weapons), not from items.ocp. items.ocp gates weapons by skill value, not by an MP-specific cost.",
    "Requirement values scale with QL; a single named weapon spans a QL range (low/high templates). Values shown are for the exact template QL listed.",
    "EquipSlot (Weap_RightHand 0x06 / Weap_LeftHand 0x08) is not stored in items.ocp; weapon identity here = 'has a ToWield action requiring a weapon wield skill'."
  )
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$json = $doc | ConvertTo-Json -Depth 12
$target = Join-Path $OutDir "metaphysicist-weapons.json"
[System.IO.File]::WriteAllText($target, $json, (New-Object System.Text.UTF8Encoding($false)))  # UTF-8, no BOM
Write-Output ("wrote " + $target + " (" + $json.Length + " chars)")
