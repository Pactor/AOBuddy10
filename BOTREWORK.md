Outline for new Bot structure:

Main loop just code which receives data from the server and handles the hearbeat. This opens it up for fully asnyc code in the other classes.
- The other classes can register for packet types they wish to receive (multiple recipients possible, but with a optional 'end sequence' flag, just like windows event system with handled=true does). This makes different fighting brain classes possible for different professions, also with their special commands to change their behavior, for example pet professions though they are 1hb/2hb utilizing they're nano attacks more or fixers using their roots and hold distance or how to handle multiple mobs (lowest first to diminish their numbers first or highest first to get rid of the biggest threat)
- High level functions for "am i in a shop" (check via pf id), "am i in a mission building", "which are my pets" (including level and running nanos), "are my pets being attacked", each pet with its own state machine (following you, guarding you, location etc)

Then separate classes for travel (handling all movement stuff besides owner follow), called from for example the fighting brains, for mission handling (get mission, finish mission goal - for example repair machine/find item etc), for buffing, for twinking and other things.

Packet are received and sent by Smokelounge.Aotomation and AOSharp.clientless classes.