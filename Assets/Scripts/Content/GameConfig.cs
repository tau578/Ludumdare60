using System;
using System.Collections.Generic;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Content
{
    /// <summary>The one asset GameInstaller references; it reaches every other definition and the people file.</summary>
    [CreateAssetMenu(menuName = "Switchboard/GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField] private long startingCents;
        [SerializeField, Min(1), Tooltip("Live calls never outnumber the caller panel's slots (D17).")]
        private int maxWaitingCalls = 6;
        [SerializeField] private PacingProfile pacing;
        [SerializeField] private CallerTypeDefinition[] callerTypes = new CallerTypeDefinition[0];
        [SerializeField, Tooltip("Assets/Configs/PhoneBook.json, shared with the team's phone book (D31).")]
        private TextAsset peopleFile;
        [SerializeField, Tooltip("Assets/Configs/Economy.json, the designer's economy file (D39).")]
        private TextAsset economyFile;
        [SerializeField] private PortraitEntry[] portraits = new PortraitEntry[0];

        public long StartingCents => startingCents;
        public int MaxWaitingCalls => maxWaitingCalls;
        public IPacingProfile Pacing => pacing != null ? pacing : null;
        public IReadOnlyList<ICallerType> CallerTypes => callerTypes ?? new CallerTypeDefinition[0];

        public string PeopleFileText => peopleFile != null ? peopleFile.text : null;

        public string EconomyFileText => economyFile != null ? economyFile.text : null;

        public PeopleCatalog LoadPeople(IEnumerable<string> boardJacks) =>
            PeopleFileLoader.Load(PeopleFileText, CallerTypes, HasPortrait, boardJacks);

        public EconomyCatalog LoadEconomy() => EconomyFileLoader.Load(EconomyFileText);

        public List<string> Validate()
        {
            List<string> errors = ContentValidator.Validate(PeopleFileText, CallerTypes, HasPortrait, ContentValidator.BoardJacks());
            foreach (string error in ContentValidator.ValidateEconomy(EconomyFileText)) errors.Add("Economy file: " + error);
            return errors;
        }

        /// <summary>How cards of a caller type look; a type with no style gets plain cards.</summary>
        public CallCardStyle CardStyleOf(string typeId)
        {
            if (callerTypes == null || string.IsNullOrEmpty(typeId)) return default;
            foreach (CallerTypeDefinition type in callerTypes)
                if (type != null && type.Id == typeId) return type.CardStyle;
            return default;
        }

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
            [SerializeField, Tooltip("The name the people file uses, e.g. caller-01.")] private string name;
            [SerializeField] private Sprite sprite;

            public string Name => name;
            public Sprite Sprite => sprite;
        }
    }
}
