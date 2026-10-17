using System;
using System.Collections.Generic;

namespace Switchboard.Domain.Tests
{
    internal sealed class FakePerson : IPerson
    {
        public FakePerson(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
            Id = $"person.{firstName.ToLowerInvariant()}-{lastName.ToLowerInvariant()}";
        }

        public string Id { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public IReadOnlyList<string> Tags => Array.Empty<string>();
    }

    internal sealed class FakeCallerType : ICallerType
    {
        public FakeCallerType(FloatRange centsPerSecond, FloatRange durationSeconds, FloatRange connectionFeeCents = default)
        {
            CentsPerSecond = centsPerSecond;
            DurationSeconds = durationSeconds;
            ConnectionFeeCents = connectionFeeCents;
        }

        public string Id => "type.regular";
        public string DisplayName => "Regular";
        public FloatRange CentsPerSecond { get; }
        public FloatRange ConnectionFeeCents { get; }
        public FloatRange DurationSeconds { get; }
    }

    internal sealed class FakeCaller : ICaller
    {
        public FakeCaller(IPerson person, ICallerType type, float weight = 1, float centsPerSecondOverride = 0,
            float connectionFeeCentsOverride = 0)
        {
            Person = person;
            Type = type;
            Weight = weight;
            CentsPerSecondOverride = centsPerSecondOverride;
            ConnectionFeeCentsOverride = connectionFeeCentsOverride;
        }

        public string Id => "caller." + Person.Id;
        public IPerson Person { get; }
        public ICallerType Type { get; }
        public float Weight { get; }
        public float CentsPerSecondOverride { get; }
        public float ConnectionFeeCentsOverride { get; }
    }

    internal sealed class FakeDirectoryEntry : IDirectoryEntry
    {
        public FakeDirectoryEntry(IPerson person, string jackLabel)
        {
            Person = person;
            JackLabel = jackLabel;
        }

        public IPerson Person { get; }
        public string JackLabel { get; }
    }

    internal sealed class FakeContent : IGameContent
    {
        public long StartingCents { get; set; }
        public IReadOnlyList<ICallerType> CallerTypes { get; set; } = Array.Empty<ICallerType>();
        public IReadOnlyList<IPerson> People { get; set; } = Array.Empty<IPerson>();
        public IReadOnlyList<ICaller> Callers { get; set; } = Array.Empty<ICaller>();
        public IReadOnlyList<IDirectoryEntry> Directory { get; set; } = Array.Empty<IDirectoryEntry>();
    }

    internal sealed class FakeSwitchboard : ISwitchboard
    {
        public List<CordId> Returned { get; } = new List<CordId>();
        public List<CordId> Refused { get; } = new List<CordId>();
        public int CordCount => 3;

        public void ReturnToTray(CordId cord) => Returned.Add(cord);
        public void Refuse(CordId cord) => Refused.Add(cord);
    }

    /// <summary>Three regular callers: Ada Moss on A1, Basil Thorne on B2 and Clara Whitlock on C3.</summary>
    internal sealed class TestWorld
    {
        public TestWorld()
        {
            Regular = new FakeCallerType(new FloatRange(3, 8), new FloatRange(15, 30), new FloatRange(5, 15));
            AdaCaller = new FakeCaller(Ada, Regular);
            Callers = new ICaller[] { AdaCaller, new FakeCaller(Basil, Regular), new FakeCaller(Clara, Regular) };
            Entries = new IDirectoryEntry[]
            {
                new FakeDirectoryEntry(Clara, "C3"),
                new FakeDirectoryEntry(Ada, "A1"),
                new FakeDirectoryEntry(Basil, "B2")
            };
            Directory = new PhoneDirectory(Entries);
            Content = new FakeContent
            {
                CallerTypes = new ICallerType[] { Regular },
                People = new IPerson[] { Ada, Basil, Clara },
                Callers = Callers,
                Directory = Entries
            };
        }

        public FakePerson Ada { get; } = new FakePerson("Ada", "Moss");
        public FakePerson Basil { get; } = new FakePerson("Basil", "Thorne");
        public FakePerson Clara { get; } = new FakePerson("Clara", "Whitlock");
        public FakeCallerType Regular { get; }
        public ICaller AdaCaller { get; }
        public IReadOnlyList<ICaller> Callers { get; }
        public IReadOnlyList<IDirectoryEntry> Entries { get; }
        public PhoneDirectory Directory { get; }
        public FakeContent Content { get; }

        public static readonly JackId A1 = new JackId("A1");
        public static readonly JackId B2 = new JackId("B2");
        public static readonly JackId C3 = new JackId("C3");

        public CallRequest AdaWantsBasil(double centsPerSecond = 5, double durationSeconds = 20, double connectionFeeCents = 0) =>
            new CallRequest(AdaCaller, Basil, centsPerSecond, durationSeconds, connectionFeeCents);
    }
}
