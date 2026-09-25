using System;
using AOSharp.Clientless;
using SmokeLounge.AOtomation.Messaging.Messages;

namespace AOBuddy
{
    /// <summary>
    /// Message-subscription helper (R3.6). Main used to hand-roll the same pattern per feed — a
    /// try/catch around each forward in one big lambda, and a second handler whose one outer
    /// catch silently swallowed everything. One line per feed now, each handler ISOLATED: a
    /// throwing handler is caught, logged with its tag, and every other handler still runs for
    /// that message (one bad feed can no longer starve the rest of the frame). The tag is the
    /// exact old log-line prefix ("VITALS feed error", "MISSIONROLL", ...), so existing log
    /// greps keep matching; new feeds got fresh tags (DCMOVE / ZONEIN / WIRECAPTURE) where the
    /// old code was silent.
    /// </summary>
    public static class ClientEvents
    {
        public static void SafeSubscribe(Action<Message> handler, string tag, Action<string> log)
        {
            Client.MessageReceived += (s, m) =>
            {
                try { handler(m); }
                catch (Exception ex) { log(tag + ": " + ex.Message); }
            };
        }
    }
}
