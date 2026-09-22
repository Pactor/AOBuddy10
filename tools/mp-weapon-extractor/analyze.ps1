$raw = Get-Content "$env:TEMP\mp_weapons_raw.json" -Raw | ConvertFrom-Json
Write-Host "total weapons:" $raw.Count
# MP-only (locked EqualTo Metaphysicist)
$mponly = $raw | Where-Object { $_.ProfReqs -match 'EqualTo Metaphysicist' }
Write-Host "`n=== MP-only weapons (Profession EqualTo Metaphysicist):" $mponly.Count "==="
$mponly | Group-Object PrimaryWield | Sort-Object Count -Descending | ForEach-Object { Write-Host ("  {0}: {1}" -f $_.Name,$_.Count) }
Write-Host "`n--- MP-only sample names (clean, non-Equip_) ---"
$mponly | Where-Object { $_.Name -notmatch '^Equip_|^Gfx|- \d+$' } | Select-Object -First 30 Id,Name,Ql,PrimaryWield | ForEach-Object { Write-Host ("  {0} [{1}] ql{2} {3}" -f $_.Id,$_.Name,$_.Ql,$_.PrimaryWield) }
