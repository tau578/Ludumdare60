using System;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Content
{
    [Serializable]
    public sealed class DirectoryEntry : IDirectoryEntry
    {
        [SerializeField] private PersonDefinition person;
        [SerializeField, Tooltip("A SocketView address on the board, A1 to F6.")] private string jackLabel;

        public IPerson Person => person != null ? person : null;
        public string JackLabel => jackLabel;
    }
}
