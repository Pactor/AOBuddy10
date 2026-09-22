# HANDOFF — nano recovery (FACTS, not theories)

Rewritten 2026-09-22 by a fresh session. The prior 10,746-message session had drifted and guessed
repeatedly. Everything below is evidence from the log/source, with the source cited. Do not re-theorize
past these facts; get new evidence instead.

## HARD FACTS ESTABLISHED (with evidence)

1. **`Item.Use()` sends one `GenericCmdAction.Use` targeting the item's inventory `Slot`.**
   Source: `AOSharp.Clientless/Inventory/Item.cs:111`. It is the SAME packet for stims and rechargers.
   => The bot's call is correct. This is not a packet/targeting bug.

2. **THE SERVER ONLY SENDS A `CurrentNano` UPDATE ON SIT/STAND.** This is the single most important fact
   and it invalidated two earlier wrong conclusions. Log 05:22:
   ```
   05:22:23.7  stim used     rawNano=38      <- stands / re-sits
   05:22:35.1  REST sitting  rawNano=72   (+34)
   05:22:36-45 stim x8       rawNano=72   (FROZEN, 9.5s continuous sitting)
   05:22:47.7  REST sitting  rawNano=76   (+4)
   05:22:48-53 stim x4       rawNano=76   (FROZEN again)
   ```
   While seated continuously the value NEVER moves, whatever the item does. It only refreshes on a posture
   change. **Do NOT conclude an item is dead because rawNano is flat during a sit** — that is a missing
   packet, not a missing effect. The earlier "the Recharger restores zero nano" claim was exactly this
   mistake: those 4 recharger uses spanned 5.7s of unbroken sitting, so no update could have arrived.
   The recharger has healed HP for the user for days; treat it as working.

3. **The Recharger has no Use modifiers at all** — `UseMods[(none)]`, while `Item.cs` does populate
   `Modifiers[SpellListType.Use]` from templates for items that have them. Worth comparing against a stim.

4. **The HP healing that "worked for days" is the STIM path, not the recharger.**
   Heal path uses `Config.StimKeyword` (`SupportController.cs:685`, `stim.Use()`); the rest path preferred
   the Recharger — a DIFFERENT item. There is proof the stim's Use works and no evidence the recharger's does.

5. **Earlier "sits and does nothing" was a deadlock** (now fixed): an unaffordable queued pet summon set
   `castsActive`, which made `continueRest` false on the next tick, freezing `_rechargeAccum` at 0.3s so the
   1.5s recharge trigger never fired. Can't summon (no nano) -> can't recharge (queued summon blocks rest).

## USER'S GROUND TRUTH (treat as fact; do NOT re-derive or argue)
- Stims and rechargers restore BOTH HP and nano. Treat them the same.
- Rule: HP low OR nano low -> use a heal item. "The simplest thing there can be."
- It is NOT a cooldown. It is NOT a heal-over-time. The recharger is NOT out of uses.
- He has healed HP with this mechanic for days.

## CHANGES MADE THIS SESSION (all built clean, 0 errors)
- `castsActive` is now **active casting only** (`me.IsCasting`), not "something queued" — a queued but
  unaffordable summon no longer blocks recharging. Fixes the deadlock in fact #5.
- `continueRest` also requires `stillNeedsRecovery` (below `RestUntilPercent` / `RestNanoUntilPercent`),
  so he stands when topped instead of sitting forever.
- Recharge use reverted to `recharger.Use()` (no arg = self, per `Item.cs:111`), matching the proven stim
  path; removed the prior session's speculative `SetTarget(me.Identity); Use(me)`.
- **Rest path now tries the STIM FIRST**, recharger as fallback — use the item with the proven-working Use.
- `RECHARGE:` log line now prints `rawNano=<cur>/<max>` and `UseMods[...]`. Keep this; it is what produced
  every fact above.

Thresholds (`Config.cs:86-92`): RestBelow=80 RestUntil=96 / RestNanoBelow=60 RestNanoUntil=90.

## NEXT STEP — read the log, do not guess
Relaunch and read the `RECHARGE:` lines. They now name the item used:
- **Item is a Stim and `rawNano` climbs** -> solved; keep stim-first.
- **Item is a Stim and `rawNano` still flat** -> item use is not the nano mechanism at all for this char.
  Next evidence: wire-capture a REAL client recharging nano (`OmniCell\Tools\Capture\capture-marked.bat`)
  and diff the packets against what the bot sends. Do not change code before that capture exists.
- **Still the Recharger** -> no stim matched `Config.StimKeyword` ('Stim'); check inventory naming.

One variable never tested: every failing sample had **HP = 100%**. The working HP samples had HP low.
If a combined Health-and-Nano item is server-rejected at full HP, that alone explains the whole thing.
Cheap test: get HP below 80 with nano also low, then watch whether BOTH climb.

## RESOLVED 2026-09-22 06:20 — FullCharacter (0x29304349) drop

**ROOT CAUSE: it was never a parse failure.** `NetworkSession.cs:241` is `callback.Invoke(n3Msg)` — the
catch there also swallows exceptions thrown by HANDLERS, and reported them as "Dropping unparseable packet".
The packet always parsed fine.

Chain:
1. `FullCharacterReader` deliberately SKIPS the InventorySlots section (`SkipX3F1(r, 32)`) and never
   assigns `fc.InventorySlots` -> it is null.
2. `LocalPlayerProxy.ApplyChanges` passes it straight to `Inventory.OnFullCharacterMessage(null)`.
3. That method wiped `_items` and `_containers`, THEN walked the null -> `NullReferenceException`.

So every occurrence left the bot with an EMPTY INVENTORY and aborted the rest of the FullCharacter handler
(no team join; very likely also why heal items went missing). `Perks` already had a null guard for exactly
this reason — `InventorySlots` never did.

**FIX:** `Inventory.OnFullCharacterMessage` returns early when `inventorySlots == null`, keeping the existing
inventory instead of destroying it. Verified: spam-bot tell arrives, no drop line, no crash, bot zones and
greets owner.

**WHAT FOUND IT:** logging the real exception instead of discarding it (`catch (Exception)` with no variable).
Every previous "fix" guessed at a field because the failure was invisible. KEEP THAT LOGGING.

**LESSON — four theories were wrong, all mine, all disproven by the user's testing:** sender-is-someone-else,
unknown/remote sender, colour/HTML payload, chatcmd character reference. None mattered; there was no parse
bug to explain. Do not theorise about a failure you have not made visible first.

## (historical, all disproven) the old investigation

**Trigger (user-identified, from the log sequence):** a tell from a STRANGER.
```
05:50:07  [ignored tell] <Unknown>: ~&!!!#?!284ksCondemned Subway (dng)~
05:50:08  Dropping unparseable packet: n3type=0x29304349 len=2759
```
A stranger tells him -> the server sends THAT PLAYER'S FullCharacter so the client can resolve the name ->
`FullCharacterReader` throws -> packet dropped -> the name never reaches `ChatClient.IdToNameMap`, which is
why the tell rendered as `<Unknown>`. User reports it also stops him joining a team (mechanism NOT yet
confirmed — do not assert it).

**Why it has been "fixed" many times and keeps returning:** the failing packet describes ANOTHER CHARACTER,
not the local player. Past fixes were validated against the local player's FullCharacter (and the "a pet is
up" case), which has a different shape. It works until any stranger tells him.

**REPRO — READ THIS BEFORE "VERIFYING" ANY FIX. Two theories are already DISPROVEN by the user; do not
retry them:**
- Tell from his OWN ALT (same zone) -> works fine. NOT a "sender is someone else" bug.
- Tell from a BRAND NEW character in the start area (never seen, different zone) -> works fine.
  So it is NOT "an unknown/remote character forces a FullCharacter lookup". That theory was mine and it is wrong.

**STRONGEST LEAD (user observation): the spam bot sends its tells IN COLORS** — AO formatted/link-encoded
text (`~&!!!#?!284ksCondemned Subway (dng)~`). A plain tell from an alt or a fresh char does NOT break it.
**!! ALL THREE THEORIES BELOW ARE DISPROVEN BY TESTING. DO NOT RETRY THEM. !!**
1. "Sender is another character" -> WRONG (own alt tell: fine).
2. "Sender is unknown/remote, forcing a name lookup" -> WRONG (brand new char, start area, different zone: fine).
3. "Colour / HTML / chatcmd link payload" -> WRONG (user sent the exact anchor below himself: tell sent fine).

**THEREFORE: the tell is NOT established as the cause at all.** The only evidence linking them is that one
drop happened ~1s after one spam tell in the log. That is correlation from a single sample, and three
attempts to find the mechanism have all failed. Treat "a tell triggers it" as UNPROVEN. Do not build another
theory on it — get the exception first (see below), then work backwards from the real failing read.

The payload that did NOT reproduce it:
```html
<a href="chatcmd:///tell CharacterName Hello!">Click here to message me</a>
```
HYPOTHESIS (fits every result so far, still unproven): the embedded `chatcmd:///tell <CharacterName>`
names a CHARACTER, which forces a name->character resolution that a PLAIN tell never triggers. The server
answers with that character's data — the FullCharacter that then fails to parse. His alt and the brand-new
char both sent PLAIN tells, so nothing had to be resolved and nothing broke. This is the first theory that
explains ALL the observations (alt ok, fresh char ok, spam bot breaks) rather than just some of them.

AO clickable links (`text://...`) embed RAW HTML as their payload and can be LARGE — that is a far better
explanation for the 2759-byte size than "a developed character has more stats". Size may be driven by the
link's HTML blob, not by the character. Check whether the reader assumes a bounded string/section length.
DECISIVE TEST: send a tell WITH colour/link formatting from his own alt. If that reproduces the drop, the
trigger is the formatted-text handling, not the sender's character data at all — and every past fix was
tested with plain tells, which would explain the whole "fixed many times" history.
NOTE: the bytes confirm the dropped packet genuinely IS FullCharacter (offset 16 = 29 30 43 49), so it is
not a misidentified chat message — but a colour-encoded tell may be what makes the server send it.

Only the Condemned Subway spam bot triggers it. UNVERIFIED remaining difference: that packet is 2759 bytes,
i.e. a DEVELOPED character (buffs/perks/pets/items) vs a fresh char's tiny one — so the failing field is
likely in a section that is empty on a new char. The spam tell also carries AO link encoding
(`~&!!!#?!284ksCondemned Subway (dng)~`), which may or may not be related. TEST BOTH, ASSUME NEITHER.
Old (wrong) repro said: have a second
character (or a spam bot) send a tell.

**Why nobody could ever see the cause:** `NetworkSession` caught the exception as `catch (Exception)` with NO
variable and DISCARDED it, logging only n3type+len+hex. Every reader fix was therefore a guess at which field
broke. FIXED this session — the drop now logs `EX=<type>: <message> AT <deepest AOSharp frame>`.

**Next step:** get one `Dropping unparseable packet:` line with the new `EX=... AT ...` detail. It names the
exact read that walks off the end. Fix that field, then verify with a stranger's tell, NOT with self.
The fallback chain is `NetworkSession.cs:350` -> `FullCharacterReader.Read(bodyReader, packet.Length)`.

## STILL OPEN
- Sit/stand flapping: the two pet summons (43733, 125738) are attempted while unaffordable; each attempt
  stands him, then he re-sits. Proper fix is to hold a summon until nano can afford it.

## PROCESS RULE (the prior session violated this repeatedly)
STOP GUESSING. Get the log/item/wire fact FIRST, cite it, then change code. One change at a time.
