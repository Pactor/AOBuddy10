# run-bot.ps1 - supervisor for one AOBuddy bot (Build\Test.exe).
#
# The plugin's dead-connection watchdog (AOBuddy\Watchdog.cs) exits the process with code 75 when the server has
# sent nothing for WatchdogSeconds while in play (2026-09-27: the zone link went silent for 25 minutes and only a
# restart brought him back). This script starts Test.exe, waits for it to exit and starts it again after a 75.
# The plugin resumes the mission run by itself after such a restart (watchdog.json), so nothing is sent here.
#
#   powershell -ExecutionPolicy Bypass -File tools\run-bot.ps1                    # Build\, API 5591
#   powershell -ExecutionPolicy Bypass -File tools\run-bot.ps1 -BuildDir E:\Funcom\AOBuddy10\Build-MA -Port 5595
#   ... -RestartOnCrash      also restart after any other non-zero exit, then send -ResumeCommands over the API
#
# A Test.exe already running from that folder is ATTACHED to, never doubled: start this next to a live bot and
# it just watches it. A bot killed by hand (restart.ps1 kills Test.exe and starts a new one) is left alone:
# after a non-75 exit the script first looks for a Test.exe that someone else started and attaches to that.

param(
    [string]$BuildDir = (Join-Path (Split-Path -Parent $PSScriptRoot) 'Build'),
    [int]$Port = 5591,
    [string]$Config = '',   # e.g. dadbod.json: Build\dadbod.json (+ Plugins\AOBuddy\dadbod.json if present) instead of config.json
    [switch]$RestartOnCrash,
    [string[]]$ResumeCommands = @('mission run shop on', 'mission run'),
    [int]$WatchdogExitCode = 75
)

$ErrorActionPreference = 'Stop'
$exe = Join-Path $BuildDir 'Test.exe'
$logFile = Join-Path $BuildDir 'run-bot.log'
if (-not (Test-Path $exe)) { throw "No Test.exe at $exe" }

function Say([string]$text) {
    $line = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') $text"
    Write-Host $line
    try { Add-Content -Path $logFile -Value $line -Encoding utf8 } catch {}
}

function Find-Running {
    Get-Process Test -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -and ($_.Path -ieq $exe) } | Select-Object -First 1
}

function Start-Bot {
    $p = if ($Config) { Start-Process -FilePath $exe -WorkingDirectory $BuildDir -WindowStyle Minimized -PassThru -ArgumentList '--config', $Config }
         else { Start-Process -FilePath $exe -WorkingDirectory $BuildDir -WindowStyle Minimized -PassThru }
    $null = $p.Handle   # keep a handle so ExitCode is readable after exit (PowerShell 5.1 quirk)
    Say "started Test.exe pid $($p.Id)"
    return $p
}

function Wait-InPlay {
    for ($i = 0; $i -lt 60; $i++) {
        Start-Sleep 2
        try {
            $s = (Invoke-WebRequest -UseBasicParsing "http://127.0.0.1:$Port/status" -TimeoutSec 3).Content
            if ($s -match '"playfield": [1-9]') { return $true }
        } catch {}
    }
    return $false
}

function Send-Command([string]$cmd) {
    try {
        $r = (Invoke-WebRequest -UseBasicParsing -Method Post -Body $cmd "http://127.0.0.1:$Port/command" -TimeoutSec 5).Content
        Say "sent '$cmd': $r"
    } catch { Say "sending '$cmd' failed: $($_.Exception.Message)" }
}

$proc = Find-Running
if ($proc) { $null = $proc.Handle; Say "attached to running Test.exe pid $($proc.Id)" }
else { $proc = Start-Bot }

while ($true) {
    $proc.WaitForExit()
    $code = $null
    try { $code = $proc.ExitCode } catch {}
    Say "Test.exe pid $($proc.Id) exited with code $code"

    if ($code -eq $WatchdogExitCode) {
        # The server link was dead. Give the server a moment to drop the old session, then log in again; the
        # plugin resumes the mission run on its own.
        Start-Sleep 10
        $other = Find-Running
        if ($other) { $null = $other.Handle; $proc = $other; Say "someone else started Test.exe pid $($proc.Id); attached"; continue }
        Say "watchdog exit: restarting"
        $proc = Start-Bot
        continue
    }

    # Not the watchdog. If the owner (or restart.ps1) killed it and started a new one, watch that one instead.
    $other = $null
    for ($i = 0; $i -lt 10 -and -not $other; $i++) { Start-Sleep 2; $other = Find-Running }
    if ($other) { $null = $other.Handle; $proc = $other; Say "a new Test.exe pid $($proc.Id) is running; attached"; continue }

    if ($code -eq 0) { Say "normal exit: supervisor stops"; break }
    if (-not $RestartOnCrash) { Say "exit code $code is not the watchdog's ($WatchdogExitCode) and -RestartOnCrash is off: supervisor stops"; break }

    Say "crash (code $code): restarting"
    $proc = Start-Bot
    if (Wait-InPlay) {
        Start-Sleep 3
        foreach ($c in $ResumeCommands) { Send-Command $c }
    } else { Say "no playfield on /status after 2 minutes; commands not sent" }
}
