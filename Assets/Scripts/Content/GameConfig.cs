using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Switchboard.Content
{
    /// <summary>
    /// The one asset GameInstaller references: the calls, economy and book files, plus what stays in Unity because
    /// it is presentation (card styles, portraits, cord colours).
    /// </summary>
    [CreateAssetMenu(menuName = "Switchboard/GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField, Tooltip("Colours for the cords that extra_wires upgrades add, in order (5b).")]
        private Color[] extraCordColors =
        {
            new Color32(92, 184, 92, 255),
            new Color32(156, 104, 214, 255),
            new Color32(240, 232, 206, 255)
        };
        [SerializeField, Tooltip("How regular calls' cards look.")] private CallCardStyle regularCards;
        [SerializeField, Tooltip("How emergency calls' cards look (D09): a badge and an accent besides the countdown ring.")]
        private CallCardStyle emergencyCards = new CallCardStyle { badgeText = "EMERGENCY", accent = new Color(0.78f, 0.13f, 0.13f, 1) };
        [SerializeField, Tooltip("Assets/Configs/Calls.json, the designer's hand-written calls (D51).")]
        private TextAsset callsFile;
        [SerializeField, FormerlySerializedAs("peopleFile"), Tooltip("Assets/Configs/Book.json, the designer's phone book (D53).")]
        private TextAsset bookFile;
        [SerializeField, Tooltip("Assets/Configs/Economy.json, the designer's economy file (D39).")]
        private TextAsset economyFile;
        [SerializeField] private PortraitEntry[] portraits = new PortraitEntry[0];

        public IReadOnlyList<Color> ExtraCordColors => extraCordColors ?? new Color[0];

        public string CallsFileText => callsFile != null ? callsFile.text : null;

        public string BookFileText => bookFile != null ? bookFile.text : null;

        public string EconomyFileText => economyFile != null ? economyFile.text : null;

        public CallsCatalog LoadCalls() => CallsFileLoader.Load(CallsFileText, HasPortrait);

        /// <param name="calls">The pools refer to Calls.json; null loads it here.</param>
        public EconomyCatalog LoadEconomy(CallsCatalog calls = null) => EconomyFileLoader.Load(EconomyFileText, calls ?? LoadCalls());

        /// <param name="boardJacks">The jack labels on the board; null means the full 6x6 board.</param>
        public BookCatalog LoadBook(IEnumerable<string> boardJacks = null) => BookFileLoader.Load(BookFileText, boardJacks);

        /// <summary>Every file's messages, plus the checks across files, as Tools/Switchboard/Validate Content shows them.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            CallsCatalog calls = LoadCalls();
            foreach (string error in calls.Errors) errors.Add("Calls file: " + error);
            foreach (string error in LoadEconomy(calls).Errors) errors.Add("Economy file: " + error);
            BookCatalog book = LoadBook();
            foreach (string error in book.Errors) errors.Add("Book file: " + error);
            foreach (string error in ContentValidator.ValidateCallJacks(calls.Calls, book.Entries))
                errors.Add("Calls file: " + error);
            return errors;
        }

        public CallCardStyle CardStyleFor(bool emergency) => emergency ? emergencyCards : regularCards;

        public bool HasPortrait(string name) => Portrait(name) != null;

        public Sprite Portrait(string name)
        {
            if (portraits == null || string.IsNullOrEmpty(name)) return null;
            foreach (PortraitEntry entry in portraits)
                if (entry != null && entry.Name == name) return entry.Sprite;
            return null;
        }

        [Serializable]
        private sealed class PortraitEntry
        {
            [SerializeField, Tooltip("The name Calls.json's icon uses, e.g. caller-01.")] private string name;
            [SerializeField] private Sprite sprite;

            public string Name => name;
            public Sprite Sprite => sprite;
        }
    }
}
