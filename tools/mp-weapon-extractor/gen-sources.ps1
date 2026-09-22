# Generates metaphysicist-weapon-sources.json.
# Rule: never invent a vendor/mob/mission. `how` is only set when a source is
# established from authoritative data. Local OmniCell drop/vendor tables
# (mobdroptable/vendortemplate/moblootprofiles) were checked and contain NONE of
# these ids; readable public AO DBs (auno.org=anti-bot, aoitems.com=parked,
# fandom=402) were unreachable in this environment -> how="unknown".
# `expansion` IS data-traceable: the weapon's ToWield Expansion(389) requirement
# (ExpansionFlags enum) is captured from items.ocp where present.
param(
  [string]$Raw = "$env:TEMP\mp_weapons_raw.json",
  [string]$Weapons = "E:\Funcom\AOBuddy10\AOBuddy\GameData\profiles\metaphysicist-weapons.json",
  [string]$Out = "E:\Funcom\AOBuddy10\AOBuddy\GameData\profiles\metaphysicist-weapon-sources.json"
)
$ErrorActionPreference = "Stop"
$all = Get-Content $Raw -Raw | ConvertFrom-Json
$wj  = Get-Content $Weapons -Raw | ConvertFrom-Json

# Collect every representativeWeapons id (guarantees full coverage).
$repIds = @()
foreach ($t in $wj.usableWeaponTypes) { foreach ($r in $t.representativeWeapons) { $repIds += [int]$r.id } }
$repIds = $repIds | Sort-Object -Unique

$sources = [ordered]@{}
$real = 0; $unknown = 0
foreach ($id in $repIds) {
  $w = $all | Where-Object { $_.Id -eq $id } | Select-Object -First 1
  $mpOnly = [bool]($w.ProfReqs -match 'Metaphysicist')
  $exp = if ($w.ExpansionReq) { $w.ExpansionReq } else { $null }

  # Build factual notes (data only, no source guesses).
  $notes = @()
  if ($mpOnly) { $notes += "MP-only: ToWield has Profession EqualTo Metaphysicist(12) in items.ocp." }
  if ($exp) { $notes += "Wield requires expansion '$exp' (ToWield Expansion stat 389, items.ocp) -> item belongs to that expansion's content." }
  if ($w.Name -match "Kyr'Ozch") { $notes += "Kyr'Ozch alien-tech naming (Alien Invasion line)." }
  $notes += "Acquisition (vendor/mob/mission) NOT verified: absent from OmniCell drop/vendor tables and public AO DBs unreachable in this environment."

  $sources["$id"] = [ordered]@{
    name      = $w.Name
    ql        = $w.Ql
    how       = "unknown"      # no verified vendor/mob/mission/quest/tradeskill source
    where     = $null
    expansion = $exp           # data-traceable (or null)
    notes     = ($notes -join " ")
  }
  if ($exp) { $real++ } else { $unknown++ }  # `real` here = has any data-traceable provenance (expansion)
}

$doc = [ordered]@{
  profile = "Acquisition sources for Metaphysicist representative weapons (AOBuddy10)"
  generatedUtc = (Get-Date).ToUniversalTime().ToString("s") + "Z"
  sources = $sources
  _sources = @(
    [ordered]@{
      coverage = [ordered]@{
        representativeIdsCovered = $repIds.Count
        withVerifiedHow          = 0
        withDataTraceableExpansion = $real
        howUnknown               = $repIds.Count
      }
      method = @(
        "Checked OmniCell local tables E:\Funcom\OmniCell\OmniCell\Built\Release\SqlTables\ (mobdroptable.sql=25 rows, vendortemplate.sql, moblootprofiles.sql): NONE of the representative ids appear (exact or range).",
        "expansion field derived from each weapon's ToWield Expansion(389) requirement in items.ocp, mapped via AOSharp.Common ExpansionFlags / OmniCell.Enums.Expansions (authoritative).",
        "Public AO item DBs were not machine-readable here: auno.org (Anubis anti-bot 403), aoitems.com (domain parked/repurposed), anarchyonline.fandom.com (HTTP 402). No per-item drop/vendor/mission was recorded rather than guess one."
      )
      honestFlag = "how is 'unknown' for every id by design: no vendor/mob/mission/quest could be verified against a readable authoritative source, and the no-invention rule forbids guessing a boss or vendor. Only data-traceable expansion provenance is asserted (5 of the representative ids carry an explicit Shadowlands/AlienInvasion wield requirement)."
      toImprove = "Re-run against a reachable AO DB (auno.org via a browser/allowed fetch, or a local aoitems dump) to fill how/where; or import OmniCell moblootprofiles once populated."
    }
  )
}

$json = $doc | ConvertTo-Json -Depth 10
[System.IO.File]::WriteAllText($Out, $json, (New-Object System.Text.UTF8Encoding($false)))  # UTF-8 no BOM
Write-Output ("wrote $Out")
Write-Output ("ids covered: {0}  data-traceable expansion: {1}  how-unknown: {2}" -f $repIds.Count,$real,$repIds.Count)
