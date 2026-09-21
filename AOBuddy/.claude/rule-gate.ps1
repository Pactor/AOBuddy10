param([string]$Event = "PreToolUse")

# Emitted into Claude's context before it acts (PreToolUse) and at the start of every turn
# (UserPromptSubmit), so the do-it-right rules are always present as instructions, not memory.
$rules = @"
MANDATORY RULE CHECK (AOBuddy do-it-right - see CLAUDE.md):
1) CITE EVIDENCE FIRST. Before this action, name the exact aobuddy.log line, decoded sniff, or
   file:line that justifies it. If you cannot cite it, STOP and go look. No evidence = no action.
2) READ BEFORE CLAIMING. Read the log/sniff before stating what the bot does or fixing it.
3) DO NOT INVENT RULES. Only the user's actual rules count; never attribute a rule he did not give.
No guessing, no hardcoding, no pretending. Pronoun for the user/bot char: he/they, never she.
"@

$payload = @{
    hookSpecificOutput = @{
        hookEventName     = $Event
        additionalContext = $rules
    }
} | ConvertTo-Json -Compress

Write-Output $payload
exit 0
