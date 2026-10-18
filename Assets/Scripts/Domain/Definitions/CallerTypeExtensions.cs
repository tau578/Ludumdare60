using System;
using System.Collections.Generic;

namespace Switchboard.Domain
{
    public static class CallerTypeExtensions
    {
        public static bool CarriesFines(this ICallerType type) =>
            type != null && (type.FineMissedCents > 0 || type.FineEarlyDisconnectCents > 0);

        public static long FineFor(this ICallerType type, CallEndReason reason)
        {
            if (type == null) return 0;
            switch (reason)
            {
                case CallEndReason.Missed:
                case CallEndReason.WrongConnection:
                    return Math.Max(0, type.FineMissedCents);
                case CallEndReason.DisconnectedEarly:
                    return Math.Max(0, type.FineEarlyDisconnectCents);
                default:
                    return 0;
            }
        }

        public static bool CallsBackAfter(this ICallerType type, CallEndReason reason)
        {
            switch (reason)
            {
                case CallEndReason.WrongConnection: return true;
                case CallEndReason.Missed: return type != null && type.CallbackAfterMiss;
                case CallEndReason.DisconnectedEarly: return type != null && type.CallbackAfterDisconnect;
                default: return false;
            }
        }

        /// <summary>Whether this person may receive the type's calls: people without tags, or entries with its tag.</summary>
        public static bool Accepts(this ICallerType type, IPerson person)
        {
            if (person == null) return false;
            IReadOnlyList<string> tags = person.Tags;
            string wanted = type?.RecipientTag;
            if (string.IsNullOrEmpty(wanted)) return tags == null || tags.Count == 0;
            return person.HasTag(wanted);
        }
    }
}
