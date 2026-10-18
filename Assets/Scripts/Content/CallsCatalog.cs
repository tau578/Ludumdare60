using System;
using System.Collections.Generic;
using Switchboard.Domain;

namespace Switchboard.Content
{
    /// <summary>The usable calls of Calls.json, plus a plain message for every call left out or noted.</summary>
    public sealed class CallsCatalog
    {
        private readonly Dictionary<string, CallScript> byId = new Dictionary<string, CallScript>(StringComparer.Ordinal);
        private readonly HashSet<string> leftOut;

        public CallsCatalog(IReadOnlyList<CallScript> calls, IEnumerable<string> leftOutIds, IReadOnlyList<string> errors)
        {
            Calls = calls ?? Array.Empty<CallScript>();
            foreach (CallScript call in Calls)
                if (call != null && !byId.ContainsKey(call.Id)) byId.Add(call.Id, call);
            leftOut = new HashSet<string>(leftOutIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            Errors = errors ?? Array.Empty<string>();
        }

        public static CallsCatalog Empty { get; } = new CallsCatalog(null, null, null);

        public IReadOnlyList<CallScript> Calls { get; }
        public IReadOnlyList<string> Errors { get; }

        public CallScript Find(string id) => id != null && byId.TryGetValue(id, out CallScript call) ? call : null;

        /// <summary>Whether the file has a call with this id that was left out because of a problem.</summary>
        public bool IsLeftOut(string id) => id != null && leftOut.Contains(id);
    }
}
