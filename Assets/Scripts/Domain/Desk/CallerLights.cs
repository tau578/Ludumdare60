using System.Collections.Generic;

namespace Switchboard.Domain
{
    /// <summary>What a caller's jack shows on the board (D58). Explicit values: views and tests compare them.</summary>
    public enum JackLight
    {
        Off = 0,
        /// <summary>A call from this jack waits: dark on the board (fix 3); hovering its card previews it.</summary>
        Waiting = 1,
        /// <summary>The call in hand comes from this jack: the only blinking jack.</summary>
        InHand = 2
    }

    /// <summary>
    /// D58: the caller's jack lights up, as on a real switchboard, so only the recipient needs looking up. Only the
    /// call in hand's jack blinks; a waiting call (ringing, or accepted but not the one in hand) stays dark, and only
    /// its card's hover flares it (fix 3). The light goes out once
    /// the call connects or ends, or while a loose plug (a free cord's only plug in) sits in the jack: the operator
    /// is patching it. View only: the rules never read it.
    /// </summary>
    public static class CallerLights
    {
        public static JackLight Of(Call call, Call inHand)
        {
            if (call == null) return JackLight.Off;
            switch (call.State)
            {
                case CallState.Incoming: return JackLight.Waiting;
                case CallState.Taken:
                case CallState.Connecting: return call == inHand ? JackLight.InHand : JackLight.Waiting;
                default: return JackLight.Off;
            }
        }

        /// <summary>The brightest light of the calls from this jack; several calls may share a caller's jack.</summary>
        /// <param name="calls">The calls to consider (the game passes the calls on cards).</param>
        public static JackLight AtJack(JackId jack, IReadOnlyList<Call> calls, Call inHand, OperatorDesk desk)
        {
            if (calls == null || (desk != null && desk.HasLoosePlugIn(jack))) return JackLight.Off;
            JackLight light = JackLight.Off;
            for (int i = 0; i < calls.Count; i++)
            {
                Call call = calls[i];
                if (call == null || call.Request.From != jack) continue;
                JackLight own = Of(call, inHand);
                if (own > light) light = own;
            }
            return light;
        }
    }
}
