using System.Collections.Generic;
using NUnit.Framework;
using Switchboard.Content;
using UnityEditor;
using UnityEngine;

namespace Switchboard.Domain.Tests
{
    /// <summary>Economy.json's startingBoard, startingClientSlots and extra_clients entries (D49, D52).</summary>
    public sealed class EconomyBoardTests
    {
        private const string EconomyFilePath = "Assets/Configs/Economy.json";

        private const string Rest = "'startingCords': 3";

        private const string Start = "'startingBoard': { 'cols': 3, 'rows': 3 }, 'startingClientSlots': 3";

        private const string Secret = "{ 'id': 'secret', 'type': 'secret', 'name': '???', 'costCents': 25000 }";

        private static string Board(string id, int cols, int rows, int slots) =>
            $"{{ 'id': '{id}', 'type': 'extra_clients', 'name': 'Bigger board', 'description': '{cols}x{rows}', 'costCents': 1000, " +
            $"'boardSize': {{ 'cols': {cols}, 'rows': {rows} }}, 'clientSlots': {slots} }}";

        private static EconomyCatalog LoadWithStart(string start, params string[] upgrades) =>
            EconomyFileLoader.Load(("{ " + (start.Length > 0 ? start + ", " : "") + Rest +
                $", 'upgrades': [ {string.Join(", ", upgrades)}, {Secret} ] }}").Replace('\'', '"'));

        private static EconomyCatalog Load(params string[] upgrades) => LoadWithStart(Start, upgrades);

        private static void AssertSingleError(EconomyCatalog catalog, params string[] fragments)
        {
            Assert.That(catalog.Errors.Count, Is.EqualTo(1), string.Join("\n", catalog.Errors));
            foreach (string fragment in fragments) Assert.That(catalog.Errors[0], Does.Contain(fragment));
        }

        // The designer balances Economy.json at any time (lead, 2026-10-19): this checks the rules, not the sizes.
        [Test]
        public void TheRealFileStartsOnItsStartingBoardAndEveryStepGrows()
        {
            EconomyCatalog catalog = EconomyFileLoader.Load(AssetDatabase.LoadAssetAtPath<TextAsset>(EconomyFilePath).text);
            Assert.That(catalog.Errors, Is.Empty, string.Join("\n", catalog.Errors));
            Assert.That(catalog.StartingBoard.IsValid, Is.True, catalog.StartingBoard.ToString());
            Assert.That(catalog.StartingClientSlots, Is.InRange(1, StatSheet.MaxClientSlots));
            BoardSize board = catalog.StartingBoard;
            int slots = catalog.StartingClientSlots;
            foreach (IUpgrade upgrade in catalog.Upgrades)
            {
                if (upgrade.Kind != UpgradeKind.ExtraClients) continue;
                BoardSize size = upgrade.BoardSize;
                Assert.That(size.IsValid, Is.True, upgrade.Id);
                Assert.That(size.Cols >= board.Cols && size.Rows >= board.Rows && !size.Equals(board), Is.True,
                    $"{upgrade.Id}: {size} is bigger than {board}");
                Assert.That(upgrade.ClientSlots, Is.InRange(slots, StatSheet.MaxClientSlots), $"{upgrade.Id}: slots never shrink");
                board = size;
                slots = upgrade.ClientSlots;
            }
            Assert.That(catalog.MostClientSlots, Is.EqualTo(slots), "the last step opens the most slots");
        }

        [Test]
        public void JsonUtilityKeepsTheMarkersOfALeftOutBoard()
        {
            var data = JsonUtility.FromJson<EconomyFileData>("{ \"upgrades\": [ { \"id\": \"board_1\" } ] }");
            Assert.That(data.startingBoard.IsMissing, Is.True);
            Assert.That(data.startingClientSlots, Is.EqualTo(EconomyFileData.Unset));
            Assert.That(data.upgrades[0].boardSize.IsMissing, Is.True);
            Assert.That(data.upgrades[0].clientSlots, Is.EqualTo(EconomyFileData.Unset));
        }

        [Test]
        public void ABoardChainLoads()
        {
            EconomyCatalog catalog = Load(Board("board_1", 4, 3, 3), Board("board_2", 4, 4, 4), Board("board_3", 6, 6, 4));
            Assert.That(catalog.Errors, Is.Empty, "one side may grow alone, and slots may stay the same");
            Assert.That(catalog.Upgrades[0].BoardSize, Is.EqualTo(new BoardSize(4, 3)));
            Assert.That(catalog.Upgrades[0].ClientSlots, Is.EqualTo(3));
            Assert.That(catalog.Upgrades[2].Kind, Is.EqualTo(UpgradeKind.ExtraClients));
        }

        [Test]
        public void AMissingStartGivesPlainMessagesAndTheFullBoard()
        {
            EconomyCatalog catalog = LoadWithStart("", Board("board_1", 4, 4, 4));
            Assert.That(catalog.StartingBoard, Is.EqualTo(BoardSize.Full));
            Assert.That(catalog.StartingClientSlots, Is.EqualTo(StatSheet.MaxClientSlots));
            Assert.That(catalog.Errors[0], Does.Contain("startingBoard is missing").And.Contain("full 6x6 board"));
            Assert.That(catalog.Errors[1], Does.Contain("startingClientSlots is missing").And.Contain($"all {StatSheet.MaxClientSlots}"));
        }

        [TestCase("'startingBoard': { 'cols': 3 }, 'startingClientSlots': 3", "needs both cols and rows")]
        [TestCase("'startingBoard': { 'cols': 0, 'rows': 3 }, 'startingClientSlots': 3", "startingBoard 0x3 is outside the board")]
        [TestCase("'startingBoard': { 'cols': 3, 'rows': 7 }, 'startingClientSlots': 3", "startingBoard 3x7 is outside the board")]
        public void ABadStartingBoardFallsBackToTheFullBoard(string start, string message)
        {
            EconomyCatalog catalog = LoadWithStart(start, Board("board_1", 6, 6, 4));
            Assert.That(catalog.StartingBoard, Is.EqualTo(BoardSize.Full));
            Assert.That(catalog.Errors[0], Does.Contain(message).And.Contain("full 6x6 board"));
        }

        [TestCase(0)]
        [TestCase(7)]
        public void StartingClientSlotsOutsideThePanelOpenEverySlot(int slots)
        {
            EconomyCatalog catalog = LoadWithStart($"'startingBoard': {{ 'cols': 3, 'rows': 3 }}, 'startingClientSlots': {slots}", Board("board_1", 4, 4, 4));
            AssertSingleError(catalog, $"startingClientSlots {slots} is outside 1 to {StatSheet.MaxClientSlots}", "caller panel");
            Assert.That(catalog.StartingClientSlots, Is.EqualTo(StatSheet.MaxClientSlots));
        }

        [Test]
        public void ReportsAMissingBoardSize() =>
            AssertSingleError(Load("{ 'id': 'board_1', 'type': 'extra_clients', 'name': 'Bigger board', 'costCents': 1000, 'clientSlots': 4 }"),
                "Upgrade 1 \"board_1\": boardSize is missing", "\"cols\": 4");

        [Test]
        public void ReportsAHalfWrittenBoardSize() =>
            AssertSingleError(Load("{ 'id': 'board_1', 'type': 'extra_clients', 'name': 'Bigger board', 'costCents': 1000, " +
                "'boardSize': { 'rows': 4 }, 'clientSlots': 4 }"), "\"board_1\": boardSize needs both cols and rows");

        [Test]
        public void ReportsABoardSizeOutsideTheBoard() =>
            AssertSingleError(Load(Board("board_1", 7, 7, 4)), "\"board_1\": boardSize 7x7 is outside the board", "1 to 6");

        [Test]
        public void ReportsMissingClientSlots() =>
            AssertSingleError(Load("{ 'id': 'board_1', 'type': 'extra_clients', 'name': 'Bigger board', 'costCents': 1000, " +
                "'boardSize': { 'cols': 4, 'rows': 4 } }"), "\"board_1\": clientSlots is missing");

        [Test]
        public void ReportsClientSlotsAboveTheCallerPanel() =>
            AssertSingleError(Load(Board("board_1", 4, 4, 7)), $"\"board_1\": clientSlots 7 is outside 1 to {StatSheet.MaxClientSlots}");

        [Test]
        public void TheFirstEntryMustBeBiggerThanTheStart() =>
            AssertSingleError(Load(Board("board_1", 3, 3, 4)), "\"board_1\": boardSize 3x3 is the same as the startingBoard 3x3",
                "The board must grow");

        [Test]
        public void TheBoardMustGrowAlongTheChain() =>
            AssertSingleError(Load(Board("board_1", 4, 4, 4), Board("board_2", 4, 4, 4)),
                "Upgrade 2 \"board_2\": boardSize 4x4 is the same as board_1's 4x4", "The board must grow");

        [Test]
        public void NeitherSideMayShrink()
        {
            AssertSingleError(Load(Board("board_1", 5, 5, 4), Board("board_2", 6, 4, 4)),
                "\"board_2\": boardSize 6x4 is smaller than board_1's 5x5", "can only grow");
            AssertSingleError(Load(Board("board_1", 2, 4, 4)), "boardSize 2x4 is smaller than the startingBoard 3x3");
        }

        [Test]
        public void ClientSlotsNeverShrink()
        {
            AssertSingleError(Load(Board("board_1", 4, 4, 4), Board("board_2", 5, 5, 3)),
                "\"board_2\": clientSlots 3 is fewer than board_1's 4", "only grow");
            AssertSingleError(Load(Board("board_1", 4, 4, 2)), "clientSlots 2 is fewer than startingClientSlots 3");
        }

        [Test]
        public void ABrokenEntryIsLeftOutOfTheChain()
        {
            EconomyCatalog catalog = Load(Board("board_1", 4, 4, 4), Board("board_2", 4, 4, 4));
            Assert.That(catalog.Upgrades.Count, Is.EqualTo(2), "board_1 and the secret");
            Assert.That(catalog.Upgrades[0].Id, Is.EqualTo("board_1"));
        }
    }
}
