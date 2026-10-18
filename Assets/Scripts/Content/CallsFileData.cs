using System;

namespace Switchboard.Content
{
    /// <summary>
    /// The designer's Assets/Configs/Calls.json (D51). JsonUtility gives 0 for a number the file leaves out, so the
    /// numbers start as NaN, which the loader reports as missing instead of silently using 0.
    /// </summary>
    [Serializable]
    public sealed class CallsFileData
    {
        public CallFileEntry[] calls;
    }

    [Serializable]
    public sealed class CallFileEntry
    {
        public string id;
        public string icon;
        public string name;
        public string message;
        public string from;
        public string to;
        public double durationSeconds = double.NaN;
        public double connectCents = double.NaN;
        public double centsPerSecond = double.NaN;
        public bool emergency;
        public double responseTimeSeconds = double.NaN;
        public double penaltyCents = double.NaN;
    }
}
