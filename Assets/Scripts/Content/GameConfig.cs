using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Switchboard.Content
{
    /// <summary>
    /// The one asset GameInstaller references: the people and economy files, plus what stays in Unity because it is
    /// presentation or scene layout (card styles, portraits, cord colours, the caller panel's slot count).
    /// </summary>
    [CreateAssetMenu(menuName = "Switchboard/GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField, Min(1), Tooltip("Live calls never outnumber the caller panel's slots (D17).")]
        private int maxWaitingCalls = 6;
        [SerializeField, Tooltip("Colours for the cords that extra_wires upgrades add, in order (5b).")]
        private Color[] extraCordColors =
        {
            new Color32(92, 184, 92, 255),
            new Color32(156, 104, 214, 255),
            new Color32(240, 232, 206, 255)
        };
        [SerializeField, FormerlySerializedAs("callerTypes"), Tooltip("How each call type's cards look, matched by id to Economy.json's calls.")]
        private CallerTypeDefinition[] cardStyles = new CallerTypeDefinition[0];
        [SerializeField, Tooltip("Assets/Configs/PhoneBook.json, shared with the team's phone book (D31).")]
        private TextAsset peopleFile;
        [SerializeField, Tooltip("Assets/Configs/Economy.json, the designer's economy file (D39).")]
        private TextAsset economyFile;
        [SerializeField] private PortraitEntry[] portraits = new PortraitEntry[0];

        public int MaxWaitingCalls => maxWaitingCalls;
        public IReadOnlyList<Color> ExtraCordColors => extraCordColors ?? new Color[0];

        public string PeopleFileText => peopleFile != null ? peopleFile.text : null;

        public string EconomyFileText => economyFile != null ? economyFile.text : null;

        /// <param name="economy">People reference the economy file's call types by id, so load it first.</param>
        public PeopleCatalog LoadPeople(EconomyCatalog economy, IEnumerable<string> boardJacks) =>
            PeopleFileLoader.Load(PeopleFileText, economy.CallTypes, HasPortrait, boardJacks);

        public EconomyCatalog LoadEconomy() => EconomyFileLoader.Load(EconomyFileText);

        public List<string> Validate()
        {
            EconomyCatalog economy = LoadEconomy();
            var errors = new List<string>();
            foreach (string error in economy.Errors) errors.Add("Economy file: " + error);
            foreach (string error in ContentValidator.Validate(PeopleFileText, economy.CallTypes, HasPortrait, ContentValidator.BoardJacks()))
                errors.Add("People file: " + error);
            errors.AddRange(ContentValidator.ValidateCardStyles(CardStyleIds(), economy.CallTypes));
            return errors;
        }

        /// <summary>How cards of a call type look; a type with no style gets plain cards.</summary>
        public CallCardStyle CardStyleOf(string typeId)
        {
            if (cardStyles == null || string.IsNullOrEmpty(typeId)) return default;
            foreach (CallerTypeDefinition style in cardStyles)
                if (style != null && style.Id == typeId) return style.CardStyle;
            return default;
        }

        private List<string> CardStyleIds()
        {
            var ids = new List<string>();
            if (cardStyles != null)
                foreach (CallerTypeDefinition style in cardStyles) ids.Add(style != null ? style.Id : null);
            return ids;
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
