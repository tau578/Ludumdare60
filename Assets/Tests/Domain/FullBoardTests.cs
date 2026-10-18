using System.Collections.Generic;
using NUnit.Framework;
using Switchboard.Content;
using UnityEditor;

namespace Switchboard.Domain.Tests
{
    /// <summary>The real 36-person file (D35) with a player who connects every call at once.</summary>
    public sealed class FullBoardTests
    {
        private const string GameConfigPath = "Assets/Content/GameConfig.asset";

        [Test]
        public void CallersComeFromTheWholeBoardAndNobodyIsInTwoCalls()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            Assert.That(config, Is.Not.Null, GameConfigPath);
            PeopleCatalog people = config.LoadPeople(ContentValidator.BoardJacks());
            var board = new FakeSwitchboard { CordCount = 6 };
            var session = new GameSession(new GameContent(config, people), new SeededRandom(36), board);
            var callers = new HashSet<string>();
            var inCalls = new HashSet<string>();
            var live = new List<Call>();
            int nextCord = 0;

            for (int tick = 0; tick < 20 * 1200; tick++)
            {
                session.Tick(0.05);
                live.Clear();
                live.AddRange(session.Calls.Live);
                inCalls.Clear();
                foreach (Call call in live)
                {
                    if (call.State == CallState.Ended) continue;
                    Assert.That(inCalls.Add(call.Request.CallerPerson.Id), Is.True, call.Request.CallerPerson.Id);
                    Assert.That(inCalls.Add(call.Request.Recipient.Id), Is.True, call.Request.Recipient.Id);
                    if (call.State != CallState.Incoming) continue;
                    callers.Add(call.Request.CallerPerson.Id);
                    session.Desk.TryTake(call.Id);
                    session.Desk.OnCircuitClosed(new CordId(nextCord++),
                        session.Directory.JackOf(call.Request.CallerPerson), session.Directory.JackOf(call.Request.Recipient));
                }
            }

            Assert.That(session.Directory.ListedEntries.Count, Is.EqualTo(36));
            Assert.That(callers.Count, Is.GreaterThan(30), "distinct callers in 20 minutes");
        }
    }
}
