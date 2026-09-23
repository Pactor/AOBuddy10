using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AOSharp.Clientless;

namespace AOBuddy
{
    /// <summary>
    /// The bot's help, as clickable in-game pages rather than one unreadable line of forty command names.
    ///
    /// Anarchy Online's chat renders a small subset of HTML, and two links do the work:
    ///   &lt;a href="text://CONTENT"&gt;label&lt;/a&gt;              opens CONTENT in a popup window
    ///   &lt;a href='chatcmd:///tell BOT command'&gt;label&lt;/a&gt; sends BOT that command when clicked
    ///
    /// EVERYTHING CLICKABLE LIVES INSIDE A POPUP. A chatcmd link written straight into a tell renders as
    /// plain text and does nothing when clicked - the first version of this did exactly that and the log
    /// proved it: 'help' arrived three times, the 'help pets' the link should have sent never arrived once.
    /// Tyrbot puts every chatcmd link inside a blob for the same reason.
    ///
    /// NESTING. A popup's content rides inside a double-quoted href, so it must not contain a raw double
    /// quote - single quotes for the links and fonts inside it. To put a popup INSIDE a popup, the inner
    /// href's quotes are written as &amp;quot; (Tyrbot does this in core/text.py paginate, which escapes every
    /// double quote in blob content). That is what lets one tell open a page index whose entries each open
    /// their own page. Two levels is the most we need, so no deeper escaping is required.
    ///
    /// Syntax checked against the Tyrbot source in E:\Funcom\Tyrbot (core/text.py format_page + paginate,
    /// and the chatcmd links in core/setting_types.py) rather than from memory - it is a working AO bot on
    /// the same chat protocol.
    /// </summary>
    public static class HelpPages
    {
        // Muted blue for descriptions, so the clickable command names stand out against them.
        private const string Dim = "#9CC6E7";
        private const string Head = "#FFFF00";

        private sealed class Cmd
        {
            public string Name;
            public string Args;
            public string What;
            public Cmd(string name, string what, string args = null) { Name = name; What = what; Args = args; }
        }

        private sealed class Page
        {
            public string Key;
            public string Title;
            public string Blurb;
            public Cmd[] Commands;
        }

        private static readonly Page[] Pages =
        {
            new Page
            {
                Key = "move", Title = "Movement", Blurb = "Following you, travel, and getting through zone lines.",
                Commands = new[]
                {
                    new Cmd("follow", "follow me again (and switch to Assist if idle)"),
                    new Cmd("stay", "stop following and hold position"),
                    new Cmd("come", "walk to where I am now"),
                    new Cmd("stop", "drop whatever you were walking toward"),
                    new Cmd("stand", "stand up"),
                    new Cmd("sit", "sit down"),
                    new Cmd("forward", "nudge forward a few metres", "[m]"),
                    new Cmd("zone", "work the nearest zone line until it takes", "[m]"),
                    new Cmd("nav", "the recorded route memory for this playfield", "[save|on|off|use]"),
                    new Cmd("record", "start/stop recording a named path I walk", "[stop]"),
                    new Cmd("savepath", "save what you just recorded", "<name>"),
                    new Cmd("path", "walk a saved path", "<name>|stop"),
                    new Cmd("paths", "list the saved paths"),
                },
            },
            new Page
            {
                Key = "fight", Title = "Fighting", Blurb = "What I attack, and how I pick it.",
                Commands = new[]
                {
                    new Cmd("assist", "assist you - attack what you attack"),
                    new Cmd("solo", "fight anything nearby that is already fighting"),
                    new Cmd("idle", "stop fighting and stand down"),
                    new Cmd("specials", "toggle weapon specials (Brawl, Fast Attack, Burst, ...)"),
                    new Cmd("status", "level, mode, health, current target"),
                    new Cmd("stat", "read any stat by name or number", "<name|number>"),
                    new Cmd("mission", "mission mode: where I am in the building, the next hop, run it, stop", "[status|route|blitz|stop]"),
                },
            },
            new Page
            {
                Key = "pets", Title = "Pets", Blurb = "Summoning, dismissing, and aiming them. Roles come from the server, not guesswork.",
                Commands = new[]
                {
                    new Cmd("petstatus", "what I own, what I can see, and who the heal pet is on"),
                    new Cmd("petsummon", "resummon the full set"),
                    new Cmd("petdismiss", "terminate every pet"),
                    new Cmd("petfollow", "call them all back to me now"),
                    new Cmd("petattack", "send them at your current target"),
                    new Cmd("pets", "turn the whole pet system on or off"),
                    new Cmd("resummon", "toggle automatic resummoning"),
                    new Cmd("petbuffs", "toggle keeping buffs up on the pets"),
                    new Cmd("pethealme", "heal pet onto YOU"),
                    new Cmd("pethealself", "heal pet onto ME"),
                    new Cmd("pethealpet", "heal pet onto my attack pet"),
                    new Cmd("pethealtarget", "heal pet onto anyone I can see - me/you = you, him/self = me, pet = my attack pet", "<name>"),
                    new Cmd("pethealmytarget", "heal pet onto what you are FIGHTING (a click alone is not on the wire)"),
                    new Cmd("pethealauto", "back to automatic: you when melee, attack pet when ranged"),
                    new Cmd("petdbg", "dump pet diagnostics to the log"),
                },
            },
            new Page
            {
                Key = "heal", Title = "Healing and buffs", Blurb = "Stims, rechargers, and keeping nanos up.",
                Commands = new[]
                {
                    new Cmd("heal", "heal you now if I can"),
                    new Cmd("buff", "cast my keep-up buffs", "[self|owner|team|all]"),
                    new Cmd("autobuff", "toggle automatic buffing"),
                    new Cmd("keepup", "list what I classified as a keep-up buff"),
                },
            },
            new Page
            {
                Key = "supply", Title = "Supplies", Blurb = "Restocking stims and rechargers from a terminal.",
                Commands = new[]
                {
                    new Cmd("resupply", "go and buy stims and rechargers", "[stop|status|machines|forget]"),
                    new Cmd("supplies", "what I am carrying"),
                    new Cmd("vendordebug", "open every terminal nearby and log what it sells"),
                },
            },
            new Page
            {
                Key = "info", Title = "What I know", Blurb = "Reference data about this character.",
                Commands = new[]
                {
                    new Cmd("class", "profession, breed and loadout"),
                    new Cmd("nanos", "nanos I know", "[filter]"),
                    new Cmd("active", "nanos currently running on me"),
                    new Cmd("learnable", "nanos I could upload", "[filter]"),
                    new Cmd("perks", "trained perks, read from the wire"),
                    new Cmd("catalog", "the item catalogue"),
                    new Cmd("nanodump", "dump every known nano to the log"),
                    new Cmd("missiondbg", "toggle mission debug logging"),
                },
            },
        };

        /// <summary>Who a chatcmd link should tell. The character's own name, falling back to the configured
        /// one if the local player has not been built yet - a link addressed to nobody silently does nothing.</summary>
        private static string BotName
        {
            get
            {
                string n = DynelManager.LocalPlayer?.Name;
                if (!string.IsNullOrEmpty(n)) return n;
                // The name we logged in with. Always known, and known before the first SimpleCharFullUpdate
                // for ourselves lands - a link addressed to nobody silently does nothing when clicked.
                return string.IsNullOrEmpty(Client.CharacterName) ? "me" : Client.CharacterName;
            }
        }

        /// <summary>A command the owner can click to run. Single quotes, so it nests inside a popup.</summary>
        private static string Click(string command, string label) =>
            $"<a href='chatcmd:///tell {BotName} {command}'>{label}</a>";

        /// <summary>A popup window, for use in an ordinary message.</summary>
        private static string Popup(string label, string content) =>
            $"<a href=\"text://{content}\">{label}</a>";

        /// <summary>A popup nested INSIDE another popup's content: its quotes are escaped so they don't end
        /// the outer href. See the class remarks.</summary>
        private static string NestedPopup(string label, string content) =>
            $"<a href=&quot;text://{content}&quot;>{label}</a>";

        /// <summary>
        /// The reply to plain 'help': ONE popup. Inside it, each page opens its own popup, and inside those
        /// every command is a link that runs it. Nothing clickable is left out in the tell itself, because a
        /// link there does not work.
        /// </summary>
        public static string Index() => Popup("[Help - click here]", Contents());

        /// <summary>One page, opened directly. Unknown page falls back to the index.</summary>
        public static string For(string page)
        {
            page = (page ?? "").Trim().ToLowerInvariant();
            if (page.Length == 0) return Index();

            Page p = Pages.FirstOrDefault(x => x.Key == page)
                     ?? Pages.FirstOrDefault(x => x.Key.StartsWith(page, StringComparison.OrdinalIgnoreCase));
            if (p == null) return $"No help page called '{page}'. {Index()}";

            return Popup($"[{p.Title} - {p.Commands.Length} commands]", Body(p));
        }

        /// <summary>The index popup: a line per page, each opening that page in its own popup.</summary>
        private static string Contents()
        {
            var sb = new StringBuilder();
            sb.Append($"<font color='{Head}'>{BotName} - help</font>\n\n");
            foreach (Page p in Pages)
            {
                sb.Append(NestedPopup($"<font color='{Head}'>{p.Title}</font>", Body(p)));
                sb.Append($"  <font color='{Dim}'>{p.Commands.Length} commands</font>\n");
                sb.Append($"   <font color='{Dim}'>{p.Blurb}</font>\n\n");
            }
            sb.Append($"<font color='{Dim}'>Open a page, then click a command to run it.</font>");
            return sb.ToString();
        }

        private static string Body(Page p)
        {
            var sb = new StringBuilder();
            sb.Append($"<font color='{Head}'>{p.Title}</font>\n");
            sb.Append($"<font color='{Dim}'>{p.Blurb}</font>\n\n");
            foreach (Cmd c in p.Commands)
            {
                sb.Append(Click(c.Name, c.Name));
                if (!string.IsNullOrEmpty(c.Args)) sb.Append($" <font color='{Dim}'>{c.Args}</font>");
                sb.Append($"\n   <font color='{Dim}'>{c.What}</font>\n");
            }
            sb.Append($"\n<font color='{Dim}'>Click a command to run it.</font>");
            return sb.ToString();
        }

    }
}
