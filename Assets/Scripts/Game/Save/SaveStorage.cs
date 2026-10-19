using Switchboard.Content;
using Switchboard.Domain;

namespace Switchboard.Game
{
    /// <summary>Picks where saves live on this platform. Stateless: every caller gets its own instance.</summary>
    public static class SaveStorage
    {
        public static ISaveStorage Create() => new PlayerPrefsStorage();

        public static SaveFile OpenFile() => new SaveFile(Create(), SaveMigrations.All);
    }
}
