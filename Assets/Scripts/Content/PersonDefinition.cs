using System;
using System.Collections.Generic;
using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Content
{
    [CreateAssetMenu(menuName = "Switchboard/PersonDefinition")]
    public sealed class PersonDefinition : ScriptableObject, IPerson
    {
        [SerializeField] private string id;
        [SerializeField] private string firstName;
        [SerializeField] private string lastName;
        [SerializeField] private Sprite portrait;
        [SerializeField] private string[] tags = new string[0];

        public string Id => id;
        public string FirstName => firstName;
        public string LastName => lastName;
        public Sprite Portrait => portrait;
        public IReadOnlyList<string> Tags => tags ?? Array.Empty<string>();
    }
}
