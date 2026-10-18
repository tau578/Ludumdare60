using System.Collections.Generic;
using NUnit.Framework;
using Switchboard.Content;
using UnityEditor;

namespace Switchboard.Domain.Tests
{
    /// <summary>The real file, 33 people and 3 services on all 36 jacks (D35, D36), with a player who connects every call at once.</summary>
    public sealed class FullBoardTests
    {
        private const string GameConfigPath = "Assets/Content/GameConfig.asset";

        [Test]
        public void CallersComeFromTheWholeBoardAndNobodyIsInTwoCalls()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            Assert.That(config, Is.Not.Null, GameConfigPath);
            EconomyCatalog economy = config.LoadEconomy();
            PeopleCatalog people = config.LoadPeople(economy, ContentValidator.BoardJacks());
            var board = new FakeSwitchboard { CordCount = 6 };
            var session = new GameSession(new GameContent(config, people, economy), new SeededRandom(36), board);
            var callers = new HashSet<string>();
            var inCalls = new HashSet<string>();
            int emergencies = 0;
            session.Calls.CallAdded += e =>
            {
                Assert.That(e.Call.Request.CallerPerson.Tags, Is.Empty, "services never call");
                bool toService = e.Call.Request.Recipient.HasTag(PeopleFileLoader.ServiceTag);
                bool emergency = e.Call.Request.Type.Id == "type.emergency";
                Assert.That(toService, Is.EqualTo(emergency), e.Call.Request.Recipient.Id);
                if (emergency) emergencies++;
            };
            var live = new List<Call>();
            int nextCord = 0;

            for (int tick = 0; tick < 20 * 1200; tick++)
            {
                session.Idle.ReportInput();
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
            Assert.That(callers.Count, Is.GreaterThan(28), "distinct callers of 33 in 20 minutes");
            Assert.That(emergencies, Is.GreaterThan(10), "emergencies in 20 minutes");
        }
    }
}
