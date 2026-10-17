using System;
using System.Collections.Generic;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Content
{
    /// <summary>The one asset GameInstaller references; it reaches every other definition.</summary>
    [CreateAssetMenu(menuName = "Switchboard/GameConfig")]
    public sealed class GameConfig : ScriptableObject, IGameContent
    {
        [SerializeField] private long startingCents;
        [SerializeField] private CallerTypeDefinition[] callerTypes = new CallerTypeDefinition[0];
        [SerializeField] private PersonDefinition[] people = new PersonDefinition[0];
        [SerializeField] private CallerDefinition[] callers = new CallerDefinition[0];
        [SerializeField] private PhoneDirectoryDefinition directory;

        public long StartingCents => startingCents;
        public IReadOnlyList<ICallerType> CallerTypes => callerTypes ?? new CallerTypeDefinition[0];
        public IReadOnlyList<IPerson> People => people ?? new PersonDefinition[0];
        public IReadOnlyList<ICaller> Callers => callers ?? new CallerDefinition[0];
        public IReadOnlyList<IDirectoryEntry> Directory =>
            directory != null ? directory.Entries : Array.Empty<IDirectoryEntry>();
    }
}
