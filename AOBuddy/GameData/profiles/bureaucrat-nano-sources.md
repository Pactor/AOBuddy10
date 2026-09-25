# Bureaucrat - Nano sources

Where each of the 343 Bureaucrat-castable nanos comes from. Derived by locating each nano's CRYSTAL item in `items.ocp` (the item whose UploadNano function argument is the nano id) and reading that crystal's own criteria.

## Method

```
{
 "expansionGate": "Read the Expansion(stat 389) BitAnd requirement from the nano program's ToUse action AND from its nano-crystal item (items.ocp) use/wield criteria. bit1(2)=ShadowLands, bit3(8)=AlienInvasion, bit5(32)=LostEden (AOSharp ExpansionFlags). Authoritative from client data.",
 "crystalLink": "nano id -> crystal item id via scanning items.ocp for an UploadNano(53019) function whose argument is the nano id.",
 "vendorData": "Shop presence = crystal item id present in OmniCell research-output vendors.csv. NOTE: local vendor data currently covers only researched playfields (Temple of Three Winds), so general nano-shop buyability is NOT separable from local data; such low/mid nanos fall to the mission-roll default below.",
 "defaultRule": "AO default (cited, not invented): a non-shop, non-expansion profession nano crystal is mission-rollable. Applied only when no Expansion gate and no local shop entry exist.",
 "noInvention": "No vendor, mob, garden, or tier is asserted unless it comes from local data. Unknown SL sub-sources and tiers are left null."
}
```

## How counts

```
{
 "mission-roll": 295,
 "shadowlands": 48
}
```

## Diagnostics

```
{
 "totalNanos": 343,
 "setFromExpansionFlag": 50,
 "expansionFromCrystalOnly": 0,
 "setFromLocalVendor": 0,
 "nanosWithNoCrystalLocated": 3
}
```

## Coverage, honestly

- 295 of 343 nanos are classified by the mission-roll DEFAULT RULE, not by a lookup.
- 48 carry an authoritative Shadowlands gate from the Expansion(389) criterion. No garden, vendor or tier is named for any of them - every `tier` field is null.
- 0 are vendor-sourced, because local vendor data covers only researched playfields and holds no nano crystals at all. That is a gap in the data, not evidence that Crat crystals are unbuyable.
- 3 nanos have no crystal item anywhere in `items.ocp`.

| id | Nano | how | where | expansion |
|---|---|---|---|---|
| 29199 | Tight Embrace | mission-roll | - | - |
| 29200 | Nano Growth | mission-roll | - | - |
| 29246 | Pistol Mastery | mission-roll | - | - |
| 29618 | Winter's Bite | mission-roll | - | - |
| 30056 | Anger Addlement | mission-roll | - | - |
| 30057 | Authority Figure | mission-roll | - | - |
| 30060 | Chains of Iron | mission-roll | - | - |
| 30064 | Cut Red Tape | mission-roll | - | - |
| 30065 | Distracted Gaze | mission-roll | - | - |
| 30068 | Drill Missile | mission-roll | - | - |
| 30071 | Droid Overhaul | mission-roll | - | - |
| 30072 | Droid Repair | mission-roll | - | - |
| 30074 | Face in the Crowd | mission-roll | - | - |
| 30077 | Gunslinger | mission-roll | - | - |
| 30079 | Living Embalming | mission-roll | - | - |
| 30082 | Monofilament Cat-O'Nine-Tails | mission-roll | - | - |
| 30083 | Monofilament Whip | mission-roll | - | - |
| 30086 | Pheromone Control | mission-roll | - | - |
| 30090 | Red Tape | mission-roll | - | - |
| 30091 | Remembered Pain | mission-roll | - | - |
| 30092 | Siren Call | mission-roll | - | - |
| 30093 | Sleep | mission-roll | - | - |
| 30094 | Sneaking Terror | mission-roll | - | - |
| 30095 | Nano Restoration Escalation | mission-roll | - | - |
| 30098 | Terror Blast | mission-roll | - | - |
| 32032 | False Profession: Bureaucrat | mission-roll | - | - |
| 43368 | Momentary Daze | mission-roll | - | - |
| 46350 | Supervisor-Grade Secretary-Droid | mission-roll | - | - |
| 46351 | Supervisor-Grade Worker-Droid | mission-roll | - | - |
| 46352 | Supervisor-Grade Administrator-Droid | mission-roll | - | - |
| 46353 | Supervisor-Grade Aide-Droid | mission-roll | - | - |
| 46354 | Supervisor-Grade Assistant-Droid | mission-roll | - | - |
| 46355 | Supervisor-Grade Attendant-Droid | mission-roll | - | - |
| 46356 | Supervisor-Grade Bodyguard | mission-roll | - | - |
| 46357 | Supervisor-Grade Helper-Droid | mission-roll | - | - |
| 46358 | Supervisor-Grade Minion | mission-roll | - | - |
| 46359 | Limited Helper-Bot | mission-roll | - | - |
| 46360 | Limited Minion | mission-roll | - | - |
| 46361 | Limited Secretary-Droid | mission-roll | - | - |
| 46362 | Limited Worker-Droid | mission-roll | - | - |
| 46363 | Faithful Worker-Droid | mission-roll | - | - |
| 46364 | Limited Administrator-Droid | mission-roll | - | - |
| 46365 | Limited Aide-Droid | mission-roll | - | - |
| 46366 | Limited Assistant-Droid | mission-roll | - | - |
| 46367 | Limited Attendant-Droid | mission-roll | - | - |
| 46368 | Limited Bodyguard | mission-roll | - | - |
| 46369 | Faithful Administrator-Droid | mission-roll | - | - |
| 46370 | Faithful Aide-Droid | mission-roll | - | - |
| 46371 | Faithful Assistant-Droid | mission-roll | - | - |
| 46372 | Faithful Attendant-Droid | mission-roll | - | - |
| 46373 | Faithful Bodyguard | mission-roll | - | - |
| 46374 | Faithful Helper-Droid | mission-roll | - | - |
| 46375 | Faithful Minion | mission-roll | - | - |
| 46376 | Faithful Secretary-Droid | mission-roll | - | - |
| 46377 | Executive-Grade Administrator-Droid | mission-roll | - | - |
| 46378 | Executive-Grade Aide-Droid | mission-roll | - | - |
| 46379 | Executive-Grade Assistant-Droid | mission-roll | - | - |
| 46380 | Executive-Grade Attendant-Droid | mission-roll | - | - |
| 46381 | Executive-Grade Bodyguard | mission-roll | - | - |
| 46382 | Executive-Grade Helper-Droid | mission-roll | - | - |
| 46383 | Executive-Grade Minion | mission-roll | - | - |
| 46384 | Executive-Grade Secretary-Droid | mission-roll | - | - |
| 46385 | Executive-Grade Worker-Droid | mission-roll | - | - |
| 46386 | Director-Grade Worker-Droid | mission-roll | - | - |
| 46387 | Director-Grade Administrator-Droid | mission-roll | - | - |
| 46388 | Director-Grade Aide-Droid | mission-roll | - | - |
| 46389 | Director-Grade Assistant-Droid | mission-roll | - | - |
| 46390 | Director-Grade Attendant-Droid | mission-roll | - | - |
| 46391 | Director-Grade Bodyguard | mission-roll | - | - |
| 46392 | Director-Grade Helper-Droid | mission-roll | - | - |
| 46393 | Director-Grade Minion | mission-roll | - | - |
| 46394 | Director-Grade Secretary-Droid | mission-roll | - | - |
| 46395 | Basic Minion | mission-roll | - | - |
| 46396 | Basic Secretary-Droid | mission-roll | - | - |
| 46397 | Basic Worker-Droid | mission-roll | - | - |
| 46398 | Advanced Secretary-Droid | mission-roll | - | - |
| 46399 | Advanced Worker | mission-roll | - | - |
| 46400 | Basic Administrator | mission-roll | - | - |
| 46401 | Basic Aide-Droid | mission-roll | - | - |
| 46402 | Basic Assistant-Droid | mission-roll | - | - |
| 46403 | Basic Attendant-Droid | mission-roll | - | - |
| 46404 | Basic Bodyguard | mission-roll | - | - |
| 46405 | Basic Helper-Droid | mission-roll | - | - |
| 46406 | Advanced Administrator-Droid | mission-roll | - | - |
| 46407 | Advanced Aide-Droid | mission-roll | - | - |
| 46408 | Advanced Assistant-Droid | mission-roll | - | - |
| 46409 | Advanced Attendant-Droid | mission-roll | - | - |
| 46410 | Advanced Bodyguard | mission-roll | - | - |
| 46411 | Advanced Helper | mission-roll | - | - |
| 46412 | Advanced Minion | mission-roll | - | - |
| 55980 | Total Musculature Command | mission-roll | - | - |
| 55981 | Greater Musculature Command | mission-roll | - | - |
| 55982 | Captivating Speech | mission-roll | - | - |
| 55983 | Greater Illusory Paralysis | mission-roll | - | - |
| 55984 | Inhibit Motion | mission-roll | - | - |
| 55985 | Musculature Command | mission-roll | - | - |
| 55986 | Greater Restrict Movement | mission-roll | - | - |
| 55987 | Enforce Rest Break | mission-roll | - | - |
| 55988 | Illusory Paralysis | mission-roll | - | - |
| 55989 | Revoke Movement License | mission-roll | - | - |
| 55990 | Lesser Musculature Command | mission-roll | - | - |
| 55991 | Restrict Movement | mission-roll | - | - |
| 55992 | Lesser Illusory Paralysis | mission-roll | - | - |
| 55993 | Void Inertia | mission-roll | - | - |
| 78394 | Erasing Ray | mission-roll | - | - |
| 78395 | Monofilament Scourger | mission-roll | - | - |
| 78396 | Erratic Laser | mission-roll | - | - |
| 78397 | Splinter Missile | mission-roll | - | - |
| 78398 | Punishing Blade | mission-roll | - | - |
| 78399 | Searing Bolt | mission-roll | - | - |
| 78400 | Rule of One | mission-roll | - | - |
| 81996 | Stinging Reminder | mission-roll | - | - |
| 81997 | Performance Review | mission-roll | - | - |
| 81998 | Subsonic Blast | mission-roll | - | - |
| 81999 | Energized Bolt | mission-roll | - | - |
| 82000 | Disciplinary Action | mission-roll | - | - |
| 82156 | Mass Daze | mission-roll | - | - |
| 82157 | Mass Illusory Paralysis | mission-roll | - | - |
| 82158 | Capture Attention | mission-roll | - | - |
| 82159 | Captivate Crowd | mission-roll | - | - |
| 82160 | Rigid Stance | mission-roll | - | - |
| 82161 | Greater Mass Illusory Paralysis | mission-roll | - | - |
| 82163 | Lesser Fear of Attention | mission-roll | - | - |
| 82164 | Fear of Attention | mission-roll | - | - |
| 82166 | Greater Fear of Attention | mission-roll | - | - |
| 82463 | Shackles of Obedience | mission-roll | - | - |
| 82466 | Oppressive Weight of the Guilty | mission-roll | - | - |
| 82469 | Powerful Blizzard of Red Tape | mission-roll | - | - |
| 82472 | Greater Stumbling Steps | mission-roll | - | - |
| 82474 | Great Weight of the Guilty | mission-roll | - | - |
| 82477 | Blizzard of Red Tape | mission-roll | - | - |
| 82480 | Stumbling Steps | mission-roll | - | - |
| 82482 | Weight of the Guilty | mission-roll | - | - |
| 82528 | Lesser Stumbling Steps | mission-roll | - | - |
| 82531 | Lesser Blizzard of Red Tape | mission-roll | - | - |
| 85313 | Lesser Enforced Sloth | mission-roll | - | - |
| 85314 | Enforced Sloth | mission-roll | - | - |
| 85315 | Lesser Weighty Announcement | mission-roll | - | - |
| 90448 | Baton of Command (Team) | mission-roll | - | - |
| 90449 | Baton of Leadership (Team) | mission-roll | - | - |
| 90450 | Baton of Authority (Team) | mission-roll | - | - |
| 95382 | Greater Enforced Sloth | mission-roll | - | - |
| 95383 | Weighty Announcement | mission-roll | - | - |
| 99192 | Total Mental Domination | mission-roll | - | - |
| 99193 | Charismatic Rapture | mission-roll | - | - |
| 99194 | Inveigle Support | mission-roll | - | - |
| 99195 | Insidious Beguilement | mission-roll | - | - |
| 99196 | Temporary Allegiance | mission-roll | - | - |
| 99197 | Enrapturing Bondage | mission-roll | - | - |
| 99198 | Allure of Servitude | mission-roll | - | - |
| 99199 | Captivated Thoughts | mission-roll | - | - |
| 99200 | Lesser Charismatic Rapture | mission-roll | - | - |
| 99201 | Impose Will | mission-roll | - | - |
| 99202 | Solicit Support | mission-roll | - | - |
| 99203 | Personal Magnetism | mission-roll | - | - |
| 99204 | Dominate Psyche | mission-roll | - | - |
| 99205 | Soft Siren Call | mission-roll | - | - |
| 99206 | Bend Will | mission-roll | - | - |
| 99207 | Lesser Pheromone Control | mission-roll | - | - |
| 99208 | Temporary Glamor | mission-roll | - | - |
| 99209 | Displace Thought Patterns | mission-roll | - | - |
| 100422 | Disrupted Psyche | mission-roll | - | - |
| 100424 | Disjointed Psyche | mission-roll | - | - |
| 100426 | Muddled Psyche | mission-roll | - | - |
| 100428 | Disjointed From Reality | mission-roll | - | - |
| 100429 | Wandering Mind | mission-roll | - | - |
| 100430 | Introspective Engagement | mission-roll | - | - |
| 100431 | Cubicle Dweller | mission-roll | - | - |
| 100432 | Demotivate | mission-roll | - | - |
| 116798 | Thorough Overhaul | mission-roll | - | - |
| 117209 | Mimic Profession: Bureaucrat | mission-roll | - | - |
| 117220 | Assume Profession: Bureaucrat | mission-roll | - | - |
| 121136 | Sudden Scare | mission-roll | - | - |
| 121137 | Horror From The Darkest Pit | mission-roll | - | - |
| 121138 | Primal Fear | mission-roll | - | - |
| 121139 | Visions of a Doomed Future | mission-roll | - | - |
| 121140 | Crush Bravery | mission-roll | - | - |
| 121141 | Prey On Fear | mission-roll | - | - |
| 155577 | Contemplation | mission-roll | - | - |
| 155615 | Mud Slinger | mission-roll | - | - |
| 155616 | Defamation 101 | mission-roll | - | - |
| 155617 | Character Assassin | mission-roll | - | - |
| 155805 | Motivational Speech: Triumphant Pose | mission-roll | - | - |
| 155806 | Motivational Speech: Glorious Leader | mission-roll | - | - |
| 155807 | Motivational Speech: Heroic Measures | mission-roll | - | - |
| 155808 | Motivational Speech: Lead From The Front | mission-roll | - | - |
| 155809 | Motivational Speech: Bravery Overcomes | mission-roll | - | - |
| 156255 | Emergency XP Loss Reducer: 30 | mission-roll | - | - |
| 157499 | Motivational Speech: Opportunity Knocks | mission-roll | - | - |
| 157500 | Motivational Speech: Implement Through Iteration | mission-roll | - | - |
| 157501 | Motivational Speech: Organizational Opportunities | mission-roll | - | - |
| 157502 | Motivational Speech: Only the Paranoid Will Survive! | mission-roll | - | - |
| 157503 | Motivational Speech: Assassin's Focus | mission-roll | - | - |
| 157504 | Motivational Speech: Improvise and Adapt | mission-roll | - | - |
| 157524 | Demotivational Speech: Retreat to Glory | mission-roll | - | - |
| 157525 | Demotivational Speech: That is not on the Agenda | mission-roll | - | - |
| 157526 | Demotivational Speech: Who Writes the Minutes? | mission-roll | - | - |
| 157527 | Demotivational Speech: Mourner's March | mission-roll | - | - |
| 157528 | Demotivational Speech: Fumble Fingers | mission-roll | - | - |
| 157529 | Demotivational Speech: 10 Thumbs | mission-roll | - | - |
| 157530 | Demotivational Speech: Certainty of Defeat | mission-roll | - | - |
| 157531 | Demotivational Speech: Factory Recall | mission-roll | - | - |
| 157532 | Demotivational Speech: Administrative Error | mission-roll | - | - |
| 157533 | Demotivational Speech: Surge in the System | mission-roll | - | - |
| 157534 | Demotivational Speech: Swapdisk Mayhem | mission-roll | - | - |
| 157535 | Demotivational Speech: Let's Make a Committee | mission-roll | - | - |
| 203657 | Sidestep the Blame | mission-roll | - | - |
| 203659 | Bypass Limitations | mission-roll | - | - |
| 203661 | Circumvent Restrictions | mission-roll | - | - |
| 203663 | Evade Responsibility | mission-roll | - | - |
| 203665 | Guile of the Snake Oil Seller | mission-roll | - | - |
| 203831 | Beg for Freedom | mission-roll | - | - |
| 203835 | Pursuade for Freedom | mission-roll | - | - |
| 203837 | Appeal for Freedom | mission-roll | - | - |
| 203839 | Negotiate for Freedom | mission-roll | - | - |
| 203842 | Bribe for Freedom | mission-roll | - | - |
| 203844 | Subpoena for Freedom | mission-roll | - | - |
| 203846 | Pay Bail | mission-roll | - | - |
| 203850 | Minor Exoskeleton Pulse | mission-roll | - | - |
| 203852 | Lesser Exoskeleton Pulse | mission-roll | - | - |
| 203855 | Exoskeleton Pulse | mission-roll | - | - |
| 203857 | Superior Exoskeleton Pulse | mission-roll | - | - |
| 203859 | Greater Exoskeleton Pulse | mission-roll | - | - |
| 203948 | Request Freedom | mission-roll | - | - |
| 203950 | Beseech Freedom | mission-roll | - | - |
| 203952 | Petition Freedom | mission-roll | - | - |
| 203954 | Demand Freedom | mission-roll | - | - |
| 203956 | Solicit Freedom | mission-roll | - | - |
| 203958 | The Claim to Freedom | mission-roll | - | - |
| 203960 | The Right to Movement | mission-roll | - | - |
| 203962 | The Prerogative of Mobility | mission-roll | - | - |
| 203964 | The Privilege of Speed | mission-roll | - | - |
| 205287 | Gallant Hero: The Enraged Drone | mission-roll | - | - |
| 205289 | Gallant Hero: The Infuriated Minion | mission-roll | - | - |
| 205291 | Gallant Hero: The Aggravated Servant | mission-roll | - | - |
| 205293 | Gallant Hero: The Indignant Flunky | mission-roll | - | - |
| 205295 | Gallant Hero: The Angry Servitor | mission-roll | - | - |
| 205297 | Gallant Hero: The Bitter Clerk | mission-roll | - | - |
| 205299 | Gallant Hero: The Irate Attache | mission-roll | - | - |
| 205301 | Gallant Hero: The Incensed Retainer | mission-roll | - | - |
| 205303 | Gallant Hero: The Vengeful Butler | mission-roll | - | - |
| 205433 | Corporate Leadership: Dispensation | mission-roll | - | - |
| 205435 | Corporate Leadership: Clemency | mission-roll | - | - |
| 205437 | Corporate Leadership: Impunity | mission-roll | - | - |
| 205439 | Corporate Leadership: Exoneration | mission-roll | - | - |
| 207284 | Improved Authority Figure | mission-roll | - | - |
| 219020 | Empowered Distracted Gaze | shadowlands | - | Shadowlands |
| 220345 | Neuronal Stimulator | shadowlands | - | Shadowlands |
| 222687 | Intensify Stress | shadowlands | - | Shadowlands |
| 222695 | Improved Cut Red Tape | mission-roll | - | - |
| 222853 | Soothing Calm | mission-roll | - | - |
| 222857 | Bot Reproduction | mission-roll | - | - |
| 224115 | Puissant Momentary Daze | shadowlands | - | Shadowlands |
| 224117 | Puissant Restrict Movement | shadowlands | - | Shadowlands |
| 224119 | Puissant Illusionary Paralysis | shadowlands | - | Shadowlands |
| 224121 | Puissant Musculature Command | shadowlands | - | Shadowlands |
| 224123 | Puissant Inhibit Motion | shadowlands | - | Shadowlands |
| 224125 | Puissant Captivating Speech | shadowlands | - | Shadowlands |
| 224127 | Puissant Total Muscular Command | shadowlands | - | Shadowlands |
| 224129 | Puissant Void Inertia | shadowlands | - | Shadowlands |
| 224131 | Empowered Anger Addlement | shadowlands | - | Shadowlands |
| 224133 | Empowered Sleep | shadowlands | - | Shadowlands |
| 224135 | Empowered Demotivate | shadowlands | - | Shadowlands |
| 224137 | Empowered Cubicle Dweller | shadowlands | - | Shadowlands |
| 224139 | Empowered Contemplation | shadowlands | - | Shadowlands |
| 224141 | Empowered Chaotic Mind | shadowlands | - | Shadowlands |
| 224143 | Empowered Divided Ego | shadowlands | - | Shadowlands |
| 224145 | Empowered Introspective Engagement | shadowlands | - | Shadowlands |
| 224147 | Empowered Wandering Mind | shadowlands | - | Shadowlands |
| 224149 | Empowered Disjointed From Reality | shadowlands | - | Shadowlands |
| 229961 | Team Empowered Allure of Servitude | shadowlands | - | Shadowlands |
| 229964 | Team Empowered Captivated Thoughts | shadowlands | - | Shadowlands |
| 230372 | Gallant Slave: The Enraged Slave | shadowlands | - | Shadowlands |
| 230374 | Gallant Slave: The Infuriated Thrall | shadowlands | - | Shadowlands |
| 230376 | Gallant Slave: The Aggravated Serf | shadowlands | - | Shadowlands |
| 230378 | Gallant Slave: The Indignant Peon | shadowlands | - | Shadowlands |
| 230380 | Gallant Slave: The Angry Drudge | shadowlands | - | Shadowlands |
| 230382 | Gallant Slave: The Bitter Vassal | shadowlands | - | Shadowlands |
| 230384 | Gallant Slave: The Irate Chattel | shadowlands | - | Shadowlands |
| 230386 | Gallant Slave: The Incensed Subordinate | shadowlands | - | Shadowlands |
| 230388 | Gallant Slave: The Vengeful Toiler | shadowlands | - | Shadowlands |
| 230391 | Team Empowered Bend Will | shadowlands | - | Shadowlands |
| 230392 | Team Empowered Dominate Psyche | shadowlands | - | Shadowlands |
| 230393 | Team Empowered Impose Will | shadowlands | - | Shadowlands |
| 230394 | Team Empowered Insidious Beguilement | shadowlands | - | Shadowlands |
| 230395 | Team Empowered Inveigle Support | shadowlands | - | Shadowlands |
| 230396 | Team Empowered Solicit Support | shadowlands | - | Shadowlands |
| 230397 | Team Empowered Temporary Glamor | shadowlands | - | Shadowlands |
| 230398 | Team Empowered Total Mental Domination | shadowlands | - | Shadowlands |
| 231008 | The Voice of One | mission-roll | - | - |
| 231009 | Peer Pressure | mission-roll | - | - |
| 231010 | The Voice of God | mission-roll | - | - |
| 231011 | Team Empowered Voice of Truth | mission-roll | - | - |
| 231012 | My Way | mission-roll | - | - |
| 231013 | The Voice of Truth | mission-roll | - | - |
| 235386 | Corporate Guardian | shadowlands | - | Shadowlands |
| 236506 | Malaise of Motivation | shadowlands | - | Shadowlands |
| 236508 | Malaise of Emotion | shadowlands | - | Shadowlands |
| 236514 | Malaise of Fervor | shadowlands | - | Shadowlands |
| 258222 | Means Test Pet | mission-roll | - | - |
| 258580 | Carlo Pinnetti | shadowlands | - | Shadowlands |
| 263250 | Greater Gunslinger | shadowlands | - | Shadowlands |
| 263251 | Skilled Gunslinger | shadowlands | - | Shadowlands |
| 266303 | Fill Inbox | mission-roll | - | Lost Eden |
| 266305 | Rabies | mission-roll | - | Lost Eden |
| 267013 | Please Hold | mission-roll | - | - |
| 267311 | Improved Red Tape | mission-roll | - | - |
| 267535 | Last Minute Negotiations | mission-roll | - | - |
| 267538 | Junior Last Minute Negotiations | mission-roll | - | - |
| 267603 | Lesser Corporate Insurance Policy | mission-roll | - | - |
| 267604 | Corporate Insurance Policy | mission-roll | - | - |
| 267605 | Greater Corporate Insurance Policy | mission-roll | - | - |
| 267611 | Corporate Strategy | mission-roll | - | - |
| 267612 | Nanite Robot Protection | mission-roll | - | - |
| 267615 | Lesser Nanite Robot Protection | mission-roll | - | - |
| 267616 | Basic Nanite Robot Protection | mission-roll | - | - |
| 267916 | Droid Damage Matrix | mission-roll | - | - |
| 267917 | Take the Bullet | mission-roll | - | - |
| 269447 | Brain Freeze | mission-roll | - | - |
| 269869 | Pet Attention | mission-roll | - | - |
| 269870 | Pet Cleanse | mission-roll | - | - |
| 269907 | Pet Steal Back | mission-roll | - | - |
| 269908 | Improved Pet Steal Back | mission-roll | - | - |
| 270250 | Improved Rule of One | mission-roll | - | - |
| 270347 | Improved Rule of One | mission-roll | - | - |
| 270783 | Motivational Speech: Improved Heroic Measures | mission-roll | - | - |
| 273300 | CEO Guardian | mission-roll | - | - |
| 273307 | Pink Slip | mission-roll | - | - |
| 273631 | Workplace Depression | mission-roll | - | - |
| 275009 | The Choir Fantastic | mission-roll | - | - |
| 275824 | Malaise of Zeal | mission-roll | - | - |
| 275826 | Demotivational Speech: Dead Man Walking | mission-roll | - | - |
| 279340 | Weekend Volunteer | mission-roll | - | - |
| 293899 | Carlita Desposito | shadowlands | - | Shadowlands |
| 302140 | Inefficient Marksmanship | mission-roll | - | - |
| 302142 | Inefficient Nanobot Control | mission-roll | - | - |
| 302143 | Inefficient Arm Movements | mission-roll | - | - |
| 302144 | Inefficient Close-Quarters Combat | mission-roll | - | - |
| 302148 | Wasteful Close-Quarters Combat | mission-roll | - | - |
| 302150 | Wasteful Arm Movements | mission-roll | - | - |
| 302152 | Wasteful Nanobot Control | mission-roll | - | - |
| 302154 | Wasteful Marksmanship | mission-roll | - | - |
| 302247 | Droid Pressure Matrix | mission-roll | - | - |

## Deviations from the template classes

- 48 Shadowlands-gated nanos out of 343 (14%). The gate is concentrated in exactly the parts of the kit that define the modern Crat: the Empowered calm line, the Puissant root line, the Corporate Leadership line, the SL bot guardians and the Team Empowered charms. A froob Bureaucrat keeps the bots and the classic calms and loses the upgrades, which is a sharper split than the Soldier or Doctor files show.
- 295 mission-roll entries is the highest count of any class file here, simply because the Crat has the largest nano kit. The RATE (86%) is ordinary; the volume is not.
- Zero vendor-sourced entries, same as every other class, and for the same reason - local vendor data does not cover nano shops. Recorded rather than filled in from a website.

## Unverified

- 'mission-roll' is a DEFAULT RULE, not a lookup: it is applied to a crystal only when the nano program and the crystal both carry no Expansion(389) gate and the crystal is absent from local vendor data. 295 of 343 entries fall to that rule.
- Local vendor data covers only researched playfields (Temple of Three Winds) and contains no nano crystals at all, so 'buyable in a shop' is NOT separable from the local data. 0 entries are marked vendor. Absence here is not evidence of absence in game.
- 48 entries are marked 'shadowlands' from the authoritative Expansion(389) BitAnd bit. That says the nano needs Shadowlands; it does NOT say which garden, vendor or tier, and no tier is asserted (every 'tier' field is null).
- 3 of the 343 nanos have no nano-crystal item anywhere in items.ocp (diagnostics.nanosWithNoCrystalLocated). The generator does not record the crystal id per entry, so this file cannot name which three; their 'how' therefore rests on the nano program's own gate only.
- No mob, vendor NPC, garden or quest is named anywhere in this file. Those live on the per-item databases that are blocked to automation (playbook section 4), so they are left out rather than guessed.
