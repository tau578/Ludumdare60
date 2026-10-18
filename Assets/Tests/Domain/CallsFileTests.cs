using System;
using System.Collections.Generic;
using NUnit.Framework;
using Switchboard.Content;
using UnityEditor;
using UnityEngine;

namespace Switchboard.Domain.Tests
{
    /// <summary>The designer's hand-written calls, Assets/Configs/Calls.json (D51).</summary>
    public sealed class CallsFileTests
    {
        private const string CallsFilePath = "Assets/Configs/Calls.json";

        private const string Sample = @"{
          'calls': [
            { 'id': 'call_001', 'icon': 'caller-01', 'name': 'Ada Moss', 'message': 'Connect me with Mabel Dunmore, please.', 'from': 'C3', 'to': 'A2', 'durationSeconds': 20, 'connectCents': 10, 'centsPerSecond': 5 },
            { 'id': 'call_002', 'icon': 'caller-08', 'name': 'Arthur Hollis', 'message': 'My neighbour is ill, get me the Doctor!', 'from': 'A1', 'to': 'D4', 'durationSeconds': 15, 'connectCents': 30, 'centsPerSecond': 7.5, 'emergency': true, 'responseTimeSeconds': 30, 'penaltyCents': 200 }
          ]
        }";

        private static readonly string[] Fields =
        {
            "'id': 'call_009'", "'icon': 'caller-02'", "'name': 'Basil Thorne'", "'message': 'Hello there.'", "'from': 'A1'",
            "'to': 'B2'", "'durationSeconds': 20", "'connectCents': 10", "'centsPerSecond': 5"
        };

        private static CallsCatalog Load(string json) =>
            CallsFileLoader.Load(json?.Replace('\'', '"'), name => name != null && name.StartsWith("caller-", StringComparison.Ordinal));

        private static string File(params string[] calls) => "{ 'calls': [ " + string.Join(", ", calls) + " ] }";

        private static string Call(params string[] fields) => "{ " + string.Join(", ", fields) + " }";

        /// <summary>The valid call with one field replaced (or removed when the replacement is null).</summary>
        private static string CallWith(string field, string replacement)
        {
            var fields = new List<string>();
            foreach (string each in Fields)
            {
                if (!each.StartsWith("'" + field + "'", StringComparison.Ordinal)) fields.Add(each);
                else if (replacement != null) fields.Add(replacement);
            }
            return Call(fields.ToArray());
        }

        private static void AssertSingleError(CallsCatalog catalog, params string[] fragments)
        {
            Assert.That(catalog.Errors.Count, Is.EqualTo(1), string.Join("\n", catalog.Errors));
            foreach (string fragment in fragments) Assert.That(catalog.Errors[0], Does.Contain(fragment));
        }

        [Test]
        public void TheSampleParses()
        {
            CallsCatalog catalog = Load(Sample);

            Assert.That(catalog.Errors, Is.Empty);
            Assert.That(catalog.Calls.Count, Is.EqualTo(2));
            CallScript ada = catalog.Calls[0];
            Assert.That(ada.Id, Is.EqualTo("call_001"));
            Assert.That(ada.Icon, Is.EqualTo("caller-01"));
            Assert.That(ada.Name, Is.EqualTo("Ada Moss"));
            Assert.That(ada.Message, Is.EqualTo("Connect me with Mabel Dunmore, please."));
            Assert.That(ada.From, Is.EqualTo(new JackId("C3")));
            Assert.That(ada.To, Is.EqualTo(new JackId("A2")));
            Assert.That(ada.DurationSeconds, Is.EqualTo(20));
            Assert.That(ada.ConnectCents, Is.EqualTo(10));
            Assert.That(ada.CentsPerSecond, Is.EqualTo(5));
            Assert.That(ada.IsEmergency, Is.False, "emergency defaults to false");
            Assert.That(ada.ResponseTimeSeconds, Is.Zero);

            CallScript doctor = catalog.Find("call_002");
            Assert.That(doctor.IsEmergency, Is.True);
            Assert.That(doctor.CentsPerSecond, Is.EqualTo(7.5), "cents per second may have decimals");
            Assert.That(doctor.ResponseTimeSeconds, Is.EqualTo(30));
            Assert.That(doctor.PenaltyCents, Is.EqualTo(200));
        }

        [Test]
        public void JsonUtilityKeepsTheMarkersForNumbersTheFileLeavesOut()
        {
            CallsFileData data = JsonUtility.FromJson<CallsFileData>("{ \"calls\": [ { \"id\": \"call_1\" } ] }");
            Assert.That(double.IsNaN(data.calls[0].durationSeconds), Is.True);
            Assert.That(double.IsNaN(data.calls[0].connectCents), Is.True);
            Assert.That(double.IsNaN(data.calls[0].centsPerSecond), Is.True);
            Assert.That(double.IsNaN(data.calls[0].responseTimeSeconds), Is.True);
            Assert.That(double.IsNaN(data.calls[0].penaltyCents), Is.True);
        }

        [Test]
        public void JackLabelsAreTidiedToTheBoardsSpelling()
        {
            CallsCatalog catalog = Load(File(CallWith("from", "'from': ' c3 '")));
            Assert.That(catalog.Errors, Is.Empty);
            Assert.That(catalog.Calls[0].From, Is.EqualTo(new JackId("C3")));
        }

        [TestCase(null, "The calls file is missing or empty")]
        [TestCase("  ", "The calls file is missing or empty")]
        [TestCase("{ 'calls': [ { 'id': } ]", "not valid JSON")]
        [TestCase("{ 'calls': [] }", "has no calls")]
        public void AMissingUnreadableOrEmptyFileGivesOneMessageAndNoCalls(string json, string message)
        {
            CallsCatalog catalog = Load(json);
            AssertSingleError(catalog, message);
            Assert.That(catalog.Calls, Is.Empty);
        }

        [Test]
        public void ReportsAnEmptyId() => AssertSingleError(Load(File(CallWith("id", "'id': ' '"))), "Call 1", "id is empty");

        [Test]
        public void ReportsADuplicateIdAndKeepsTheFirst()
        {
            CallsCatalog catalog = Load(File(Call(Fields), Call(Fields)));
            AssertSingleError(catalog, "Call 2 \"call_009\"", "already used by call 1");
            Assert.That(catalog.Calls.Count, Is.EqualTo(1));
        }

        [TestCase("name", "name is empty")]
        [TestCase("icon", "icon is empty")]
        [TestCase("message", "message is empty")]
        [TestCase("from", "from is empty")]
        [TestCase("to", "to is empty")]
        [TestCase("durationSeconds", "durationSeconds is missing")]
        [TestCase("connectCents", "connectCents is missing")]
        [TestCase("centsPerSecond", "centsPerSecond is missing")]
        public void EachRequiredFieldMissingIsNamedAndTheCallLeftOut(string field, string message)
        {
            CallsCatalog catalog = Load(File(CallWith(field, null)));
            AssertSingleError(catalog, "Call 1 \"call_009\"", message);
            Assert.That(catalog.Calls, Is.Empty);
            Assert.That(catalog.IsLeftOut("call_009"), Is.True);
        }

        [Test]
        public void ReportsAnUnknownPortrait() =>
            AssertSingleError(Load(File(CallWith("icon", "'icon': 'nobody'"))), "icon \"nobody\" is not in GameConfig's portrait list");

        [TestCase("'durationSeconds': 0", "durationSeconds 0 must be above 0")]
        [TestCase("'durationSeconds': -3", "durationSeconds -3 must be above 0")]
        public void ReportsADurationNotAboveZero(string field, string message) =>
            AssertSingleError(Load(File(CallWith("durationSeconds", field))), message);

        [TestCase("connectCents", "'connectCents': -1", "connectCents -1 is negative")]
        [TestCase("centsPerSecond", "'centsPerSecond': -0.5", "centsPerSecond -0.5 is negative")]
        [TestCase("connectCents", "'connectCents': 2.5", "connectCents 2.5 must be whole cents")]
        public void ReportsBadMoney(string field, string replacement, string message) =>
            AssertSingleError(Load(File(CallWith(field, replacement))), message);

        [Test]
        public void ZeroPayIsAllowed() =>
            Assert.That(Load(File(Call("'id': 'free'", "'icon': 'caller-01'", "'name': 'A'", "'message': 'Hi'", "'from': 'A1'", "'to': 'B1'",
                "'durationSeconds': 5", "'connectCents': 0", "'centsPerSecond': 0"))).Errors, Is.Empty);

        [TestCase("'from': 'G1'", "from \"G1\" is not a jack on the board")]
        [TestCase("'from': 'A7'", "from \"A7\" is not a jack on the board")]
        [TestCase("'from': 'A10'", "from \"A10\" is not a jack on the board")]
        [TestCase("'from': 'Moss'", "from \"Moss\" is not a jack on the board")]
        public void ReportsAJackOffTheBoard(string field, string message) =>
            AssertSingleError(Load(File(CallWith("from", field))), message);

        [Test]
        public void ReportsFromAndToTheSame() =>
            AssertSingleError(Load(File(CallWith("to", "'to': 'A1'"))), "from and to are both A1");

        [Test]
        public void AnEmergencyNeedsAResponseTimeAboveZero()
        {
            AssertSingleError(Load(File(Call(Fields) .Replace(" }", ", 'emergency': true, 'penaltyCents': 200 }"))),
                "an emergency needs responseTimeSeconds");
            AssertSingleError(Load(File(Call(Fields).Replace(" }", ", 'emergency': true, 'responseTimeSeconds': 0, 'penaltyCents': 200 }"))),
                "responseTimeSeconds 0 must be above 0");
        }

        [Test]
        public void AnEmergencyNeedsAPenaltyInWholeCents()
        {
            AssertSingleError(Load(File(Call(Fields).Replace(" }", ", 'emergency': true, 'responseTimeSeconds': 30 }"))),
                "penaltyCents is missing");
            AssertSingleError(Load(File(Call(Fields).Replace(" }", ", 'emergency': true, 'responseTimeSeconds': 30, 'penaltyCents': -5 }"))),
                "penaltyCents -5 is negative");
        }

        [Test]
        public void ARegularCallWithEmergencyFieldsGetsANoteAndStillPlays()
        {
            CallsCatalog catalog = Load(File(Call(Fields).Replace(" }", ", 'responseTimeSeconds': 30, 'penaltyCents': 200 }")));
            AssertSingleError(catalog, "only used for emergencies");
            Assert.That(catalog.Calls.Count, Is.EqualTo(1));
            Assert.That(catalog.Calls[0].IsEmergency, Is.False);
            Assert.That(catalog.Calls[0].ResponseTimeSeconds, Is.Zero);
            Assert.That(catalog.Calls[0].PenaltyCents, Is.Zero);
        }

        [Test]
        public void ABrokenCallIsLeftOutWhileTheOthersLoad()
        {
            CallsCatalog catalog = Load(File(Call(Fields), CallWith("id", "'id': 'call_010'").Replace("'from': 'A1'", "'from': 'Z9'"),
                CallWith("id", "'id': 'call_011'")));
            Assert.That(catalog.Errors.Count, Is.EqualTo(1));
            Assert.That(catalog.Find("call_009"), Is.Not.Null);
            Assert.That(catalog.Find("call_010"), Is.Null);
            Assert.That(catalog.IsLeftOut("call_010"), Is.True);
            Assert.That(catalog.Find("call_011"), Is.Not.Null);
        }

        [Test]
        public void TheRealFileLoadsWithNoProblemsAndOnlyStraightQuotes()
        {
            var file = AssetDatabase.LoadAssetAtPath<TextAsset>(CallsFilePath);
            Assert.That(file, Is.Not.Null, CallsFilePath);
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Content/GameConfig.asset");
            CallsCatalog catalog = config.LoadCalls();
            Assert.That(catalog.Errors, Is.Empty);
            Assert.That(catalog.Calls.Count, Is.GreaterThanOrEqualTo(40));
            foreach (char c in file.text)
                Assert.That(c < 128, Is.True, $"'{c}' is not plain ASCII; the card font may lack it (use straight quotes)");
            foreach (CallScript call in catalog.Calls)
            {
                if (!call.IsEmergency)
                {
                    Assert.That(call.ConnectCents, Is.InRange(5, 15), call.Id);
                    Assert.That(call.CentsPerSecond, Is.InRange(3, 8), call.Id);
                    Assert.That(call.DurationSeconds, Is.InRange(15, 30), call.Id);
                    continue;
                }
                Assert.That(call.ConnectCents, Is.InRange(20, 40), call.Id);
                Assert.That(call.CentsPerSecond, Is.InRange(5, 10), call.Id);
                Assert.That(call.DurationSeconds, Is.InRange(10, 20), call.Id);
                Assert.That(call.ResponseTimeSeconds, Is.EqualTo(30), call.Id);
                Assert.That(call.PenaltyCents, Is.EqualTo(200), call.Id);
            }
        }
    }
}
