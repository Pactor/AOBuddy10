# Builds metaphysicist-weapons-endgame.json: EXHAUSTIVE MP-usable endgame weapon
# models (deduped by name), grouped by MP weapon skill. Data from the extractor's
# raw dump (items.ocp). No invented ids/names; sources are line-level known
# acquisition or "unknown".
param(
  [string]$Raw = "$env:TEMP\mp_weapons_raw.json",
  [string]$Out = "E:\Funcom\AOBuddy10\AOBuddy\GameData\profiles\metaphysicist-weapons-endgame.json",
  [switch]$AnalyzeOnly
)
$ErrorActionPreference = "Stop"
$all = Get-Content $Raw -Raw | ConvertFrom-Json

$mpTypes = @('Bow','Pistol','1hBlunt','2hEdged','MeleeEnergy','1hEdged','2hBlunt')

# MP-usable weapons of an MP weapon type.
$pool = $all | Where-Object { $_.MpUsable -and ($mpTypes -contains $_.PrimaryWield) }

function Classify($name, $exp) {
  if ($name -match 'Ofab') { return 'Ofab' }
  if ($name -match "Kyr'Ozch" -or $name -match '\bAlien' -or $exp -match 'AlienInvasion') { return 'Alien/Kyrozch' }
  if ($name -match '\bBeast\b|of the Beast|Beastmunching|Predator|Nightflicker') { return 'Beast' }
  if ($name -match 'Perennium') { return 'Perennium' }
  if ($name -match '\bXan\b|Xan ' -or $exp -match 'LegacyOfTheXan') { return 'Xan/LegacyOfTheXan' }
  if ($exp -match 'LostEden') { return 'LostEden' }
  if ($exp -match 'Shadowlands') { return 'Shadowlands' }
  return 'generic'
}

# Line-level known acquisition (cited from ao-universe / AOWiki; general, not a per-item boss guess).
$lineSource = @{
  'Ofab'              = @{ how='battlestation'; where='Battlestation (Sector 7) reward terminal - bought/upgraded with Victory Points + tokens'; ref='ao-universe/AOWiki Battlestation' }
  'Alien/Kyrozch'     = @{ how='drop|tradeskill'; where='Alien Invasion: alien city raids drop Kyr''Ozch weapons; "typed"/Special Edition versions are tradeskilled from Kyr''Ozch weapon + bio-material'; ref='ao-universe Alien Invasion guides' }
  'Beast'             = @{ how='drop'; where='The Beast, Pandemonium (Shadowlands raid)'; ref='AOWiki The Beast' }
  'Perennium'         = @{ how='tradeskill'; where='Tradeskilled (Perennium Bar + base weapon)'; ref='ao-universe tradeskill guides' }
  'Xan/LegacyOfTheXan'= @{ how='drop|quest'; where='Legacy of the Xan endgame content (requires the LoX/SL expansion to wield)'; ref='AOWiki Legacy of the Xan' }
}

$rows = @{}   # weaponSkill -> list
$models = $pool | Group-Object Name
foreach ($g in $models) {
  $top = $g.Group | Sort-Object Ql -Descending | Select-Object -First 1
  $minQl = ($g.Group | Measure-Object Ql -Minimum).Minimum
  $maxQl = ($g.Group | Measure-Object Ql -Maximum).Maximum
  $line = Classify $top.Name $top.ExpansionReq
  $namedLines = @('Ofab','Alien/Kyrozch','Beast','Perennium','Xan/LegacyOfTheXan')
  $isEndgame = ($namedLines -contains $line) -or ($maxQl -ge 200)
  if (-not $isEndgame) { continue }

  $src = if ($lineSource.ContainsKey($line)) {
    [ordered]@{ how=$lineSource[$line].how; where=$lineSource[$line].where; ref=$lineSource[$line].ref }
  } else { [ordered]@{ how='unknown'; where=$null; ref=$null } }

  $model = [ordered]@{
    id            = $top.Id
    name          = $top.Name
    qlRange       = "$minQl-$maxQl"
    weaponSkill   = $top.PrimaryWield
    wield         = @($top.Wield | ForEach-Object { [ordered]@{ skill=$_.skill; requiredValue=$_.req } })
    specials      = @($top.Specials | ForEach-Object { [ordered]@{ special=$_.special; requiredValue=$_.req } })
    expansion     = if ($top.ExpansionReq) { $top.ExpansionReq } else { $null }
    professionLock = if ($top.ProfReqs -match 'Metaphysicist') { 'Metaphysicist' } elseif ($top.ProfReqs.Count -gt 0) { ($top.ProfReqs -join ';') } else { $null }
    line          = $line
    source        = $src
    modelIdCount  = $g.Count
  }
  if (-not $rows.ContainsKey($top.PrimaryWield)) { $rows[$top.PrimaryWield] = New-Object System.Collections.ArrayList }
  [void]$rows[$top.PrimaryWield].Add($model)
}

$byType = foreach ($skill in $mpTypes) {
  if (-not $rows.ContainsKey($skill)) { continue }
  $list = $rows[$skill] | Sort-Object { $_.line } , { -$_.id }
  [ordered]@{ weaponSkill=$skill; count=@($list).Count; weapons=@($list) }
}
$total = ($byType | ForEach-Object { $_.count } | Measure-Object -Sum).Sum

if ($AnalyzeOnly) {
  Write-Output ("total endgame models: " + $total)
  $byType | ForEach-Object { Write-Output ("  {0}: {1}" -f $_.weaponSkill,$_.count) }
  Write-Output "-- by line --"
  $rows.Values | ForEach-Object { $_ } | Group-Object line | Sort-Object Count -Descending | ForEach-Object { Write-Output ("  {0}: {1}" -f $_.Name,$_.Count) }
  return
}

$lineCounts = $rows.Values | ForEach-Object { $_ } | Group-Object line | ForEach-Object { [ordered]@{ line=$_.Name; models=$_.Count } }

$doc = [ordered]@{
  profession = "Metaphysicist"
  generatedUtc = (Get-Date).ToUniversalTime().ToString("s") + "Z"
  definition = "Exhaustive MP-usable (not locked to a non-Metaphysicist profession) weapons of MP weapon types (Bow, Pistol, 1hBlunt, 2hEdged, MeleeEnergy, 1hEdged, 2hBlunt) that are endgame-relevant: a named endgame line (Xan/LegacyOfTheXan, Ofab, Alien/Kyr'Ozch, Beast, Perennium) OR top QL >= 200. Deduped by model (grouped by name); one row per model with its QL range; wield/specials/expansion are read at the model's TOP QL id (values scale with QL)."
  dataSource = "items.ocp (OMNICELL-CONTENT v3, client 18.8.50_EP1) via tools/mp-weapon-extractor; names verified in itemnames.sql"
  byType = @($byType)
  totalModels = $total
  lineBreakdown = @($lineCounts)
  _unverified = @(
    "source is line-level known acquisition (cited ref column) for Ofab/Alien/Beast/Perennium/Xan lines; NOT a per-item drop confirmation. Public per-item drop DBs (auno/aoitems) were unreachable, so exact mob/QL drop tables are not asserted.",
    "Weapons with how='unknown' are high-QL (>=200) generic/Shadowlands/LostEden models with no named-line acquisition established.",
    "wield/specials values are at the model TOP QL and scale down with QL; qlRange gives the model's span in items.ocp."
  )
  _sources = @(
    [ordered]@{
      method = @(
        "Enumerated every MP-usable weapon of an MP weapon type from items.ocp, grouped by name (model), kept models that are a named endgame line or reach QL>=200.",
        "Line classification from name + the ToWield Expansion(389) bit (ExpansionFlags): AlienInvasion, LegacyOfTheXan, LostEden, Shadowlands.",
        "Line-level acquisition cited from ao-universe.com and wiki.aodb.us (Battlestation/Ofab, The Beast, Alien Invasion, Legacy of the Xan). No per-item boss guessed."
      )
      coverage = "See totalModels and lineBreakdown. Named-line models carry a source; generic high-QL models are how='unknown'."
    }
  )
}

$json = $doc | ConvertTo-Json -Depth 12
[System.IO.File]::WriteAllText($Out, $json, (New-Object System.Text.UTF8Encoding($false)))
Write-Output ("wrote $Out  totalModels=$total")
