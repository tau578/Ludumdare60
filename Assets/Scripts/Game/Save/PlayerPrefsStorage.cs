using Switchboard.Domain;
using UnityEngine;

namespace Switchboard.Game
{
    /// <summary>Saves in PlayerPrefs, flushed after every change. Web builds move to the browser's storage in 9b.</summary>
    public sealed class PlayerPrefsStorage : ISaveStorage
    {
        public string Read(string key) => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;

        public void Write(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
