using System.Collections.Generic;
using NUnit.Framework;
using Switchboard.Content;

namespace Switchboard.Domain.Tests
{
    /// <summary>The save codec (5.12, 9a): JSON round trip, versions and migrations, and the backup key.</summary>
    public sealed class SaveFileTests
    {
        private MemoryStorage storage;
        private SaveFile file;

        [SetUp]
        public void SetUp()
        {
            storage = new MemoryStorage();
            file = new SaveFile(storage);
        }

        private static SaveData Sample(long cents = 12345) => new SaveData
        {
            version = SaveData.CurrentVersion,
            cents = cents,
            fraction = 0.4375,
            upgrades = new List<string> { "income_1", "board_1" },
            queuePosition = 3,
            career = new CareerData { earnedCents = 98765, callsConnected = 42, finesPaidCents = 600, secondsPlayed = 1234.5 }
        };

        [Test]
        public void TheKeysNameTheGame()
        {
            Assert.That(SaveFile.MainKey, Is.EqualTo("operator.save"));
            Assert.That(SaveFile.BackupKey, Is.EqualTo("operator.save.backup"));
        }

        [Test]
        public void ASaveComesBackTheSame()
        {
            file.Write(Sample());

            SaveLoad load = file.Load();

            Assert.That(load.Source, Is.EqualTo(SaveSource.Main));
            Assert.That(load.Messages, Is.Empty);
            SaveData data = load.Data;
            Assert.That(data.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(data.cents, Is.EqualTo(12345));
            Assert.That(data.fraction, Is.EqualTo(0.4375));
            Assert.That(data.upgrades, Is.EqualTo(new[] { "income_1", "board_1" }));
            Assert.That(data.queuePosition, Is.EqualTo(3));
            Assert.That(data.career.earnedCents, Is.EqualTo(98765));
            Assert.That(data.career.callsConnected, Is.EqualTo(42));
            Assert.That(data.career.finesPaidCents, Is.EqualTo(600));
            Assert.That(data.career.secondsPlayed, Is.EqualTo(1234.5));
        }

        [Test]
        public void AnAwkwardFractionAndABigBalanceSurviveTheJson()
        {
            SaveData sample = Sample(9_000_000_000_123L);
            sample.fraction = 1.0 / 3.0;
            file.Write(sample);
            SaveData data = file.Load().Data;
            Assert.That(data.cents, Is.EqualTo(9_000_000_000_123L));
            Assert.That(data.fraction, Is.EqualTo(1.0 / 3.0));
        }

        [Test]
        public void EmptyStorageStartsANewGameQuietly()
        {
            SaveLoad load = file.Load();
            Assert.That(load.Data, Is.Null);
            Assert.That(load.Source, Is.EqualTo(SaveSource.New));
            Assert.That(load.Messages, Is.Empty);
            Assert.That(file.HasSave, Is.False);
        }

        [Test]
        public void AVersionOneFileLoads()
        {
            storage.Write(SaveFile.MainKey,
                "{\"version\":1,\"cents\":500,\"fraction\":0.25,\"upgrades\":[\"wires_1\"],\"queuePosition\":2," +
                "\"career\":{\"earnedCents\":700,\"callsConnected\":3,\"finesPaidCents\":0,\"secondsPlayed\":61.5}}");

            SaveData data = file.Load().Data;

            Assert.That(data.cents, Is.EqualTo(500));
            Assert.That(data.fraction, Is.EqualTo(0.25));
            Assert.That(data.upgrades, Is.EqualTo(new[] { "wires_1" }));
            Assert.That(data.queuePosition, Is.EqualTo(2));
            Assert.That(data.career.callsConnected, Is.EqualTo(3));
        }

        [Test]
        public void FieldsAFileLeavesOutKeepTheirDefaults()
        {
            storage.Write(SaveFile.MainKey, "{\"version\":1,\"cents\":500}");
            SaveData data = file.Load().Data;
            Assert.That(data.cents, Is.EqualTo(500));
            Assert.That(data.upgrades, Is.Empty);
            Assert.That(data.queuePosition, Is.EqualTo(0));
            Assert.That(data.career, Is.Not.Null);
            Assert.That(data.career.earnedCents, Is.EqualTo(0));
        }

        [Test]
        public void AnInjectedStepRunsOnceInOrder()
        {
            var ran = new List<string>();
            var steps = new[]
            {
                new SaveMigration(2, data => { ran.Add("2to3"); data.cents += 1; }),
                new SaveMigration(1, data => { ran.Add("1to2"); data.cents *= 10; })
            };
            var data1 = new SaveData { version = 1, cents = 5 };

            int count = SaveMigrations.Run(data1, steps, 3);

            Assert.That(ran, Is.EqualTo(new[] { "1to2", "2to3" }));
            Assert.That(count, Is.EqualTo(2));
            Assert.That(data1.cents, Is.EqualTo(51));
            Assert.That(data1.version, Is.EqualTo(3));
            Assert.That(SaveMigrations.Run(data1, steps, 3), Is.EqualTo(0), "an up-to-date save runs nothing");
        }

        [Test]
        public void AVersionOneSaveNeedsNoSteps()
        {
            int runs = 0;
            var withStep = new SaveFile(storage, new[] { new SaveMigration(0, _ => runs++) });
            storage.Write(SaveFile.MainKey, "{\"version\":1,\"cents\":5}");
            withStep.Load();
            Assert.That(runs, Is.EqualTo(0), "a v1 save has nothing to migrate in a v1 game");
            Assert.That(SaveMigrations.All, Is.Empty, "version 1 is the first format");
        }

        [Test]
        public void ANewerSaveLoadsWhatItCanWithOneWarning()
        {
            storage.Write(SaveFile.MainKey,
                "{\"version\":7,\"cents\":800,\"upgrades\":[\"income_1\"],\"futureThing\":{\"a\":1},\"career\":{\"callsConnected\":4,\"medals\":3}}");

            SaveLoad load = file.Load();

            Assert.That(load.Source, Is.EqualTo(SaveSource.Main));
            Assert.That(load.Data.cents, Is.EqualTo(800));
            Assert.That(load.Data.upgrades, Is.EqualTo(new[] { "income_1" }));
            Assert.That(load.Data.career.callsConnected, Is.EqualTo(4));
            Assert.That(load.Messages.Count, Is.EqualTo(1));
            StringAssert.Contains("newer version", load.Messages[0]);
        }

        [Test]
        public void TheBackupHoldsThePreviousGoodSave()
        {
            file.Write(Sample(100));
            Assert.That(storage.Read(SaveFile.BackupKey), Is.Null, "the first save has nothing to back up");
            file.Write(Sample(200));
            file.Write(Sample(300));

            Assert.That(file.Load().Data.cents, Is.EqualTo(300));
            SaveFile.TryParse(storage.Read(SaveFile.BackupKey), out SaveData backup, out _);
            Assert.That(backup.cents, Is.EqualTo(200));
        }

        [Test]
        public void TheSameSaveTwiceKeepsTheOlderBackup()
        {
            file.Write(Sample(100));
            file.Write(Sample(200));
            int writes = storage.Writes;
            file.Write(Sample(200));
            Assert.That(storage.Writes, Is.EqualTo(writes));
            SaveFile.TryParse(storage.Read(SaveFile.BackupKey), out SaveData backup, out _);
            Assert.That(backup.cents, Is.EqualTo(100));
        }

        [TestCase("{ this is not json")]
        [TestCase("")]
        [TestCase("{}")]
        [TestCase("[1,2,3]")]
        public void ACorruptMainSaveFallsBackToTheBackup(string broken)
        {
            file.Write(Sample(100));
            file.Write(Sample(200));
            storage.Write(SaveFile.MainKey, broken);

            SaveLoad load = file.Load();

            Assert.That(load.Source, Is.EqualTo(SaveSource.Backup));
            Assert.That(load.Data.cents, Is.EqualTo(100));
            Assert.That(load.Messages.Count, Is.EqualTo(1));
            StringAssert.Contains("backup", load.Messages[0]);
            Assert.That(storage.Read(SaveFile.MainKey), Is.EqualTo(broken), "broken data isn't deleted on load");
        }

        [Test]
        public void ACorruptMainAndBackupStartANewGame()
        {
            storage.Write(SaveFile.MainKey, "{ broken");
            storage.Write(SaveFile.BackupKey, "also broken");

            SaveLoad load = file.Load();

            Assert.That(load.Data, Is.Null);
            Assert.That(load.Source, Is.EqualTo(SaveSource.New));
            Assert.That(load.Messages.Count, Is.EqualTo(1));
            StringAssert.Contains("new game", load.Messages[0]);
            Assert.That(storage.Read(SaveFile.MainKey), Is.EqualTo("{ broken"));
            Assert.That(storage.Read(SaveFile.BackupKey), Is.EqualTo("also broken"));
        }

        [Test]
        public void ABrokenMainSaveIsNotCopiedOverTheBackup()
        {
            file.Write(Sample(100));
            file.Write(Sample(200));
            storage.Write(SaveFile.MainKey, "{ broken");
            file.Write(Sample(300));
            SaveFile.TryParse(storage.Read(SaveFile.BackupKey), out SaveData backup, out _);
            Assert.That(backup.cents, Is.EqualTo(100));
        }

        [Test]
        public void DeleteRemovesTheSaveAndItsBackup()
        {
            file.Write(Sample(100));
            file.Write(Sample(200));
            storage.Write("Switchboard.AudioPatch.v1", "keep me");
            Assert.That(file.HasSave, Is.True);

            file.Delete();

            Assert.That(file.HasSave, Is.False);
            Assert.That(storage.Read(SaveFile.MainKey), Is.Null);
            Assert.That(storage.Read(SaveFile.BackupKey), Is.Null);
            Assert.That(storage.Read("Switchboard.AudioPatch.v1"), Is.EqualTo("keep me"), "other keys stay");
        }
    }
}
