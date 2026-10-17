using System.Collections.Generic;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Content
{
    [CreateAssetMenu(menuName = "Switchboard/PhoneDirectoryDefinition")]
    public sealed class PhoneDirectoryDefinition : ScriptableObject
    {
        [SerializeField] private DirectoryEntry[] entries = new DirectoryEntry[0];

        public IReadOnlyList<IDirectoryEntry> Entries => entries ?? new DirectoryEntry[0];
    }
}
