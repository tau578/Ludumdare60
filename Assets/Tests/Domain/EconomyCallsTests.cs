using System;
using System.Collections.Generic;
using NUnit.Framework;
using Switchboard.Content;
using UnityEditor;
using UnityEngine;

namespace Switchboard.Domain.Tests
{
    /// <summary>The calls, pacing and starting values of Economy.json (Step 5c, D47).</summary>
    public sealed class EconomyCallsTests
    {
        private const string EconomyFilePath = "Assets/Configs/Economy.json";
        private const string GameConfigPath = "Assets/Content/GameConfig.asset";

        private const string Pacing =
            "'pacing': { 'firstCallDelaySeconds': 2, 'baseIntervalSeconds': 8, 'intervalJitter': 0.3, 'loadFactor': 0.5, 'idleTimeoutSeconds': 60 }";

        private static readonly string[] RegularFields =
        {
            "'id': 'type.regular'", "'name': 'Regular'", "'spawnWeight': 5", "'rateCentsPerSecond': { 'min': 3, 'max': 8 }",
            "'feeCents': { 'min': 5, 'max': 15 }", "'durationSeconds': { 'min': 15, 'max': 30 }"
        };

        private static readonly string Regular = Entry(RegularFields);

        private static string Emergency(string fee = "{ 'min': 20, 'max': 40 }", long fineMissed = 200, string spawnWeight = "1") =>
            "{ 'id': 'type.emergency', 'name': 'Emergency', 'spawnWeight': " + spawnWeight + ", 'rateCentsPerSecond': { 'min': 5, 'max': 10 }, " +
            "'feeCents': " + fee + ", 'durationSeconds': { 'min': 10, 'max': 20 }, 'callbackDelaySeconds': { 'min': 20, 'max': 40 }, " +
            "'responseTimeSeconds': 30, 'fineMissedCents': " + fineMissed + ", 'fineEarlyDisconnectCents': 100, 'callbackAfterMiss': false, " +
            "'callbackAfterDisconnect': false, 'maxSimultaneous': 1, 'everyoneCalls': true, 'recipientTag': 'emergency-service' }";

        private const string Upgrades =
            "'upgrades': [ { 'id': 'wires_1', 'type': 'extra_wires', 'name': 'Extra cord', 'description': '4 cords', 'costCents': 500, 'wires': 4 }, " +
            "{ 'id': 'secret', 'type': 'secret', 'name': '???', 'description': 'Something big', 'costCents': 25000 } ]";

        private static string Entry(IEnumerable<string> fields) => "{ " + string.Join(", ", fields) + " }";

        private static string Calls(params string[] entries) => "'calls': [ " + string.Join(", ", entries) + " ]";

        private static string File(params string[] sections) => ("{ " + string.Join(", ", sections) + " }").Replace('\'', '"');

        private static string Sample(string start = "'startingCents': 0, 'startingCords': 3", string pacing = Pacing, string calls = null) =>
            File(start, pacing, calls ?? Calls(Regular, Emergency()), Upgrades);

        private static EconomyCatalog Load(string json) => EconomyFileLoader.Load(json);

        private static void AssertSingleError(EconomyCatalog catalog, params string[] fragments)
        {
            Assert.That(catalog.Errors.Count, Is.EqualTo(1), string.Join("\n", catalog.Errors));
            foreach (string fragment in fragments) Assert.That(catalog.Errors[0], Does.Contain(fragment));
        }

        private static ICallerType TypeOf(EconomyCatalog catalog, string id)
        {
            foreach (ICallerType type in catalog.CallTypes)
                if (type.Id == id) return type;
            return null;
        }

        private static void AssertRange(FloatRange range, float min, float max, string what)
        {
            Assert.That(range.min, Is.EqualTo(min), what + " min");
            Assert.That(range.max, Is.EqualTo(max), what + " max");
        }

        [Test]
        public void EconomyFileCallsMatchStep5bValues()
        {
            var file = AssetDatabase.LoadAssetAtPath<TextAsset>(EconomyFilePath);
            Assert.That(file, Is.Not.Null, EconomyFilePath);
            EconomyCatalog catalog = EconomyFileLoader.Load(file.text);
            Assert.That(catalog.Errors, Is.Empty);

            Assert.That(catalog.StartingCents, Is.EqualTo(0));
            Assert.That(catalog.StartingCords, Is.EqualTo(3));
            Assert.That(catalog.Pacing.FirstCallDelaySeconds, Is.EqualTo(2));
            Assert.That(catalog.Pacing.BaseIntervalSeconds, Is.EqualTo(8));
            Assert.That(catalog.Pacing.IntervalJitter, Is.EqualTo(0.3).Within(1e-6));
            Assert.That(catalog.Pacing.LoadFactor, Is.EqualTo(0.5));
            Assert.That(catalog.Pacing.IdleTimeoutSeconds, Is.EqualTo(60));
            Assert.That(catalog.CallTypes.Count, Is.EqualTo(2));

            ICallerType regular = catalog.CallTypes[0];
            Assert.That(regular.Id, Is.EqualTo("type.regular"));
            Assert.That(regular.DisplayName, Is.EqualTo("Regular"));
            Assert.That(regular.SpawnWeight, Is.EqualTo(5));
            AssertRange(regular.CentsPerSecond, 3, 8, "regular rate");
            AssertRange(regular.ConnectionFeeCents, 5, 15, "regular fee");
            AssertRange(regular.DurationSeconds, 15, 30, "regular duration");
            AssertRange(regular.CallbackDelaySeconds, 20, 40, "regular callback delay");
            Assert.That(regular.ResponseTimeSeconds, Is.EqualTo(0));
            Assert.That(regular.FineMissedCents, Is.EqualTo(0));
            Assert.That(regular.FineEarlyDisconnectCents, Is.EqualTo(0));
            Assert.That(regular.CallbackAfterMiss, Is.False);
            Assert.That(regular.CallbackAfterDisconnect, Is.False);
            Assert.That(regular.MaxSimultaneous, Is.EqualTo(0));
            Assert.That(regular.EveryoneCalls, Is.False);
            Assert.That(regular.RecipientTag, Is.EqualTo(string.Empty));

            ICallerType emergency = catalog.CallTypes[1];
            Assert.That(emergency.Id, Is.EqualTo("type.emergency"));
            Assert.That(emergency.DisplayName, Is.EqualTo("Emergency"));
            Assert.That(emergency.SpawnWeight, Is.EqualTo(1));
            AssertRange(emergency.CentsPerSecond, 5, 10, "emergency rate");
            AssertRange(emergency.ConnectionFeeCents, 20, 40, "emergency fee");
            AssertRange(emergency.DurationSeconds, 10, 20, "emergency duration");
            AssertRange(emergency.CallbackDelaySeconds, 20, 40, "emergency callback delay");
            Assert.That(emergency.ResponseTimeSeconds, Is.EqualTo(30));
            Assert.That(emergency.FineMissedCents, Is.EqualTo(200));
            Assert.That(emergency.FineEarlyDisconnectCents, Is.EqualTo(100));
            Assert.That(emergency.CallbackAfterMiss, Is.False);
            Assert.That(emergency.CallbackAfterDisconnect, Is.False);
            Assert.That(emergency.MaxSimultaneous, Is.EqualTo(1));
            Assert.That(emergency.EveryoneCalls, Is.True);
            Assert.That(emergency.RecipientTag, Is.EqualTo("emergency-service"));
        }

        [Test]
        public void JsonUtilityKeepsTheMarkersForFieldsTheFileLeavesOut()
        {
            var data = JsonUtility.FromJson<EconomyFileData>(File("'calls': [ { 'id': 'type.regular' } ]"));

            Assert.That(data.startingCords, Is.EqualTo(EconomyFileData.Unset));
            Assert.That(data.pacing, Is.Not.Null);
            Assert.That(float.IsNaN(data.pacing.baseIntervalSeconds), Is.True);
            Assert.That(data.calls.Length, Is.EqualTo(1));
            Assert.That(float.IsNaN(data.calls[0].spawnWeight), Is.True);
            Assert.That(data.calls[0].rateCentsPerSecond, Is.Not.Null);
            Assert.That(data.calls[0].rateCentsPerSecond.IsMissing, Is.True);
        }

        [Test]
        public void TheFullSampleParses()
        {
            EconomyCatalog catalog = Load(Sample());
            Assert.That(catalog.Errors, Is.Empty);
            Assert.That(catalog.CallTypes.Count, Is.EqualTo(2));
            Assert.That(TypeOf(catalog, "type.emergency").FineMissedCents, Is.EqualTo(200));
            Assert.That(catalog.Upgrades.Count, Is.EqualTo(2));
        }

        [TestCase("'id': 'type.regular'", "Call type 1:", "the id is empty")]
        [TestCase("'name': 'Regular'", "Call type 1 \"type.regular\"", "the name is empty")]
        [TestCase("'spawnWeight': 5", "Call type 1 \"type.regular\"", "spawnWeight is missing")]
        [TestCase("'rateCentsPerSecond': { 'min': 3, 'max': 8 }", "Call type 1 \"type.regular\"", "rateCentsPerSecond is missing")]
        [TestCase("'feeCents': { 'min': 5, 'max': 15 }", "Call type 1 \"type.regular\"", "feeCents is missing")]
        [TestCase("'durationSeconds': { 'min': 15, 'max': 30 }", "Call type 1 \"type.regular\"", "durationSeconds is missing")]
        public void EachRequiredFieldMissingGivesItsMessage(string field, string where, string message)
        {
            var fields = new List<string>(RegularFields);
            fields.Remove(field);

            EconomyCatalog catalog = Load(Sample(calls: Calls(Entry(fields), Emergency())));

            AssertSingleError(catalog, where, message);
            Assert.That(catalog.CallTypes.Count, Is.EqualTo(1), "the broken type is left out");
            Assert.That(catalog.CallTypes[0].Id, Is.EqualTo("type.emergency"));
        }

        [Test]
        public void ReportsARangeWithOnlyAMin() =>
            AssertSingleError(Load(Sample(calls: Calls(Regular, Emergency(fee: "{ 'min': 20 }")))), "feeCents needs both a min and a max");

        [Test]
        public void OptionalFieldsLeftOutMeanOff()
        {
            ICallerType regular = TypeOf(Load(Sample()), "type.regular");

            Assert.That(regular.ResponseTimeSeconds, Is.EqualTo(0));
            Assert.That(regular.FineMissedCents, Is.EqualTo(0));
            Assert.That(regular.FineEarlyDisconnectCents, Is.EqualTo(0));
            Assert.That(regular.CallbackAfterMiss, Is.False);
            Assert.That(regular.CallbackAfterDisconnect, Is.False);
            Assert.That(regular.MaxSimultaneous, Is.EqualTo(0));
            Assert.That(regular.EveryoneCalls, Is.False);
            Assert.That(regular.RecipientTag, Is.EqualTo(string.Empty));
            AssertRange(regular.CallbackDelaySeconds, 20, 40, "default callback delay");
        }

        [Test]
        public void ReportsAMinAboveItsMax()
        {
            EconomyCatalog catalog = Load(Sample(calls: Calls(Regular, Emergency(fee: "{ 'min': 40, 'max': 20 }"))));

            AssertSingleError(catalog, "Call type 2 \"type.emergency\": feeCents min 40 is above its max 20. Swap them.");
            Assert.That(TypeOf(catalog, "type.emergency"), Is.Null);
        }

        [Test]
        public void ReportsANegativeRangeAndADurationOfZero()
        {
            var fields = new List<string>(RegularFields)
            {
                [3] = "'rateCentsPerSecond': { 'min': -1, 'max': 8 }",
                [5] = "'durationSeconds': { 'min': 0, 'max': 30 }"
            };

            EconomyCatalog catalog = Load(Sample(calls: Calls(Entry(fields), Emergency())));

            Assert.That(catalog.Errors.Count, Is.EqualTo(2), string.Join("\n", catalog.Errors));
            Assert.That(catalog.Errors[0], Does.Contain("rateCentsPerSecond min -1 is negative"));
            Assert.That(catalog.Errors[1], Does.Contain("durationSeconds min 0 must be above 0"));
        }

        [Test]
        public void ReportsANegativeFine() =>
            AssertSingleError(Load(Sample(calls: Calls(Regular, Emergency(fineMissed: -5)))),
                "Call type 2 \"type.emergency\": fineMissedCents -5 is negative");

        [Test]
        public void ReportsNegativeLimitsAndTimes()
        {
            var fields = new List<string>(RegularFields)
            {
                "'responseTimeSeconds': -1", "'maxSimultaneous': -1", "'fineEarlyDisconnectCents': -1"
            };

            EconomyCatalog catalog = Load(Sample(calls: Calls(Entry(fields), Emergency())));

            Assert.That(catalog.Errors.Count, Is.EqualTo(3), string.Join("\n", catalog.Errors));
        }

        [Test]
        public void ReportsANegativeSpawnWeight() =>
            AssertSingleError(Load(Sample(calls: Calls(Regular, Emergency(spawnWeight: "-1")))), "spawnWeight -1 is negative");

        [Test]
        public void ReportsADuplicateId() =>
            AssertSingleError(Load(Sample(calls: Calls(Regular, Regular))), "Call type 2 \"type.regular\"", "already used by call type 1");

        [Test]
        public void ReportsWhenNoTypeCanRing()
        {
            var fields = new List<string>(RegularFields) { [2] = "'spawnWeight': 0" };

            AssertSingleError(Load(Sample(calls: Calls(Entry(fields), Emergency(spawnWeight: "0")))),
                "No call type has a spawnWeight above 0");
        }

        [Test]
        public void ReportsAJitterOutOfRangeAndUsesTheDefault()
        {
            EconomyCatalog catalog = Load(Sample(pacing:
                "'pacing': { 'firstCallDelaySeconds': 2, 'baseIntervalSeconds': 8, 'intervalJitter': 1.2, 'loadFactor': 0.5, 'idleTimeoutSeconds': 60 }"));

            AssertSingleError(catalog, "pacing: intervalJitter 1.2 is outside 0 to 0.95");
            Assert.That(catalog.Pacing.IntervalJitter, Is.EqualTo(PacingSettings.Default.IntervalJitter));
            Assert.That(catalog.Pacing.BaseIntervalSeconds, Is.EqualTo(8));
        }

        [Test]
        public void ReportsAnIntervalBelowATenthAndANegativeLoadFactor()
        {
            EconomyCatalog catalog = Load(Sample(pacing:
                "'pacing': { 'firstCallDelaySeconds': 2, 'baseIntervalSeconds': 0, 'intervalJitter': 0.3, 'loadFactor': -1, 'idleTimeoutSeconds': 60 }"));

            Assert.That(catalog.Errors.Count, Is.EqualTo(2), string.Join("\n", catalog.Errors));
            Assert.That(catalog.Errors[0], Does.Contain("baseIntervalSeconds 0 is below 0.1"));
            Assert.That(catalog.Errors[1], Does.Contain("loadFactor -1 is negative"));
            Assert.That(catalog.Pacing.BaseIntervalSeconds, Is.EqualTo(8));
            Assert.That(catalog.Pacing.LoadFactor, Is.EqualTo(0.5));
        }

        [Test]
        public void AMissingPacingSectionUsesTheDefaultsWithOneMessage()
        {
            EconomyCatalog catalog = Load(File("'startingCords': 3", Calls(Regular, Emergency()), Upgrades));

            AssertSingleError(catalog, "There is no pacing section", "baseIntervalSeconds 8");
            Assert.That(catalog.Pacing.BaseIntervalSeconds, Is.EqualTo(PacingSettings.Default.BaseIntervalSeconds));
            Assert.That(catalog.Pacing.IdleTimeoutSeconds, Is.EqualTo(PacingSettings.Default.IdleTimeoutSeconds));
        }

        [Test]
        public void AMissingPacingValueIsNamed()
        {
            EconomyCatalog catalog = Load(Sample(pacing:
                "'pacing': { 'firstCallDelaySeconds': 2, 'baseIntervalSeconds': 3, 'intervalJitter': 0.3, 'idleTimeoutSeconds': 60 }"));

            AssertSingleError(catalog, "pacing: loadFactor is missing", "0.5");
            Assert.That(catalog.Pacing.LoadFactor, Is.EqualTo(0.5));
            Assert.That(catalog.Pacing.BaseIntervalSeconds, Is.EqualTo(3));
        }

        [Test]
        public void StartingValuesLeftOutAreZeroCentsAndThreeCordsWithNoMessage()
        {
            EconomyCatalog catalog = Load(File(Pacing, Calls(Regular, Emergency()), Upgrades));

            Assert.That(catalog.Errors, Is.Empty);
            Assert.That(catalog.StartingCents, Is.EqualTo(0));
            Assert.That(catalog.StartingCords, Is.EqualTo(3));
        }

        [Test]
        public void StartingCentsAndCordsComeFromTheFile()
        {
            EconomyCatalog catalog = Load(Sample(start: "'startingCents': 500, 'startingCords': 6"));

            Assert.That(catalog.StartingCents, Is.EqualTo(500));
            Assert.That(catalog.StartingCords, Is.EqualTo(6));
        }

        [TestCase(2, "below the 3 cords the tray is built with")]
        [TestCase(7, "more than the tray holds")]
        public void StartingCordsOutsideThreeToSixFallBackToThree(int cords, string message)
        {
            EconomyCatalog catalog = Load(Sample(start: "'startingCords': " + cords));

            Assert.That(catalog.StartingCords, Is.EqualTo(3));
            Assert.That(catalog.Errors.Count, Is.GreaterThanOrEqualTo(1), string.Join("\n", catalog.Errors));
            Assert.That(catalog.Errors[0], Does.Contain("startingCords " + cords).And.Contain(message).And.Contain("Use 3 to 6"));
        }

        [Test]
        public void ReportsNegativeStartingCents()
        {
            EconomyCatalog catalog = Load(Sample(start: "'startingCents': -5"));

            AssertSingleError(catalog, "startingCents -5 is negative");
            Assert.That(catalog.StartingCents, Is.EqualTo(0));
        }

        [Test]
        public void WiresChecksUseTheFilesStartingCords() =>
            AssertSingleError(Load(Sample(start: "'startingCords': 4")),
                "Upgrade 1 \"wires_1\": wires 4 is not above the 4 cords the game starts with", "5 or more");

        [Test]
        public void AFileWithNoCallsSectionGivesOneMessageAndNoCallsRing()
        {
            EconomyCatalog catalog = Load(File("'startingCords': 3", Pacing, Upgrades));

            AssertSingleError(catalog, "calls section is missing or empty");
            Assert.That(catalog.CallTypes, Is.Empty);

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            PeopleCatalog people = config.LoadPeople(catalog, ContentValidator.BoardJacks());
            Assert.That(people.Callers, Is.Empty);
            var session = new GameSession(new GameContent(config, people, catalog), new SeededRandom(5), new FakeSwitchboard());
            int rung = 0;
            session.Calls.CallAdded += _ => rung++;
            for (int tick = 0; tick < 1200; tick++) session.Tick(0.1);
            Assert.That(rung, Is.EqualTo(0));
        }

        [Test]
        public void ReportsACardStyleWhoseIdIsNotInTheFile()
        {
            EconomyCatalog catalog = Load(Sample());

            List<string> errors = ContentValidator.ValidateCardStyles(new[] { "type.regular", "type.vip", null }, catalog.CallTypes);

            Assert.That(errors.Count, Is.EqualTo(2), string.Join("\n", errors));
            Assert.That(errors[0], Does.Contain("\"type.vip\"").And.Contain("Economy.json"));
            Assert.That(errors[1], Does.Contain("card style 3 is empty"));
        }

        [Test]
        public void ACallTypeWithNoCardStyleNeedsNoMessage() =>
            Assert.That(ContentValidator.ValidateCardStyles(new[] { "type.regular" }, Load(Sample()).CallTypes), Is.Empty);

        [Test]
        public void ReportsAPhoneBookTypeThatIsNotInTheFile()
        {
            EconomyCatalog catalog = Load(Sample());
            string people = ("{ 'contactsPerPage': 6, 'contacts': [ " +
                "{ 'name': 'Police', 'socket': 'B6', 'tags': ['emergency-service'] }, " +
                "{ 'name': 'Ada Moss', 'socket': 'A1', 'portrait': 'caller-01', 'type': 'type.regular' }, " +
                "{ 'name': 'Basil Thorne', 'socket': 'B2', 'portrait': 'caller-01', 'type': 'type.regular' }, " +
                "{ 'name': 'Clara Whitlock', 'socket': 'C3', 'portrait': 'caller-01', 'type': 'type.vip' } ] }").Replace('\'', '"');

            List<string> errors = ContentValidator.Validate(people, catalog.CallTypes,
                name => name != null && name.StartsWith("caller-", StringComparison.Ordinal), ContentValidator.BoardJacks());

            Assert.That(errors.Count, Is.EqualTo(1), string.Join("\n", errors));
            Assert.That(errors[0], Does.Contain("Contact 4 \"Clara Whitlock\"").And.Contain("'type.vip' is unknown")
                .And.Contain("type.regular, type.emergency"));
        }
    }
}
