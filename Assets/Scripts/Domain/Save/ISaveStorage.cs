namespace Switchboard.Domain
{
    /// <summary>Where saves live: PlayerPrefs in the editor, the browser's localStorage on the Web (9b).</summary>
    public interface ISaveStorage
    {
        /// <summary>The stored text, or null when the key holds nothing.</summary>
        string Read(string key);

        void Write(string key, string value);

        void Delete(string key);
    }
}
