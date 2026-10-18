using System;
using System.Collections.Generic;

namespace Switchboard.Domain.Tests
{
    internal sealed class FakePerson : IPerson
    {
        public FakePerson(string firstName, string lastName, params string[] tags)
        {
            FirstName = firstName;
            LastName = lastName;
            Tags = tags ?? Array.Empty<string>();
            Id = string.IsNullOrEmpty(firstName)
                ? $"person.{lastName.ToLowerInvariant().Replace(' ', '-')}"
                : $"person.{firstName.ToLowerInvariant()}-{lastName.ToLowerInvariant()}";
        }

        public string Id { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public IReadOnlyList<string> Tags { get; }
    }

    internal sealed class FakeCallerType : ICallerType
    {
        public FakeCallerType(FloatRange centsPerSecond, FloatRange durationSeconds, FloatRange connectionFeeCents = default,
            FloatRange? callbackDelaySeconds = null)
        {
            CentsPerSecond = centsPerSecond;
            DurationSeconds = durationSeconds;
            ConnectionFeeCents = connectionFeeCents;
            CallbackDelaySeconds = callbackDelaySeconds ?? new FloatRange(20, 40);
        }

        public string Id { get; set; } = "type.regular";
        public string DisplayName { get; set; } = "Regular";
        public FloatRange CentsPerSecond { get; }
        public FloatRange ConnectionFeeCents { get; }
        public FloatRange DurationSeconds { get; }
        public FloatRange CallbackDelaySeconds { get; }
        public double ResponseTimeSeconds { get; set; }
        public long FineMissedCents { get; set; }
        public long FineEarlyDisconnectCents { get; set; }
        public bool CallbackAfterMiss { get; set; }
        public bool CallbackAfterDisconnect { get; set; }
        public string RecipientTag { get; set; } = string.Empty;
        public int MaxSimultaneous { get; set; }
        public double SpawnWeight { get; set; } = 1;
        public bool EveryoneCalls { get; set; }

        /// <summary>The Step 4 emergency numbers (D37): 30 s to connect, fines of 200¢ and 100¢, calls only to services.</summary>
        public static FakeCallerType Emergency(double spawnWeight = 1) =>
            new FakeCallerType(new FloatRange(5, 10), new FloatRange(10, 20), new FloatRange(20, 40), new FloatRange(20, 40))
            {
                Id = "type.emergency",
                DisplayName = "Emergency",
                ResponseTimeSeconds = 30,
                FineMissedCents = 200,
                FineEarlyDisconnectCents = 100,
                RecipientTag = ServiceTag,
                MaxSimultaneous = 1,
                SpawnWeight = spawnWeight,
                EveryoneCalls = true
            };

        public const string ServiceTag = "emergency-service";
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
        public int MaxWaitingCalls { get; set; } = 6;
        public IPacingProfile Pacing { get; set; } = PacingSettings.Default;
        public IReadOnlyList<ICallerType> CallerTypes { get; set; } = Array.Empty<ICallerType>();
        public IReadOnlyList<IPerson> People { get; set; } = Array.Empty<IPerson>();
        public IReadOnlyList<ICaller> Callers { get; set; } = Array.Empty<ICaller>();
        public IReadOnlyList<IDirectoryEntry> Directory { get; set; } = Array.Empty<IDirectoryEntry>();
        public IReadOnlyList<IUpgrade> Upgrades { get; set; } = Array.Empty<IUpgrade>();
    }

    internal sealed class FakeUpgrade : IUpgrade
    {
        public FakeUpgrade(string id, UpgradeKind kind, long costCents, double incomeMultiplier = 0)
        {
            Id = id;
            Kind = kind;
            CostCents = costCents;
            IncomeMultiplier = incomeMultiplier;
        }

        public string Id { get; }
        public UpgradeKind Kind { get; }
        public string Name => Id;
        public string Description => string.Empty;
        public long CostCents { get; }
        public double IncomeMultiplier { get; }

        public static FakeUpgrade Income(string id, long costCents, double multiplier) =>
            new FakeUpgrade(id, UpgradeKind.ExtraIncome, costCents, multiplier);

        /// <summary>The placeholder shop of D43: income x1.5, x2 and x3 for $8, $20 and $50, and the $250 secret.</summary>
        public static IUpgrade[] PlaceholderShop() => new IUpgrade[]
        {
            Income("income_1", 800, 1.5),
            Income("income_2", 2000, 2.0),
            Income("income_3", 5000, 3.0),
            new FakeUpgrade("secret", UpgradeKind.Secret, 25000)
        };
    }

    internal sealed class FakeSwitchboard : ISwitchboard
    {
        public List<CordId> Returned { get; } = new List<CordId>();
        public List<CordId> Refused { get; } = new List<CordId>();
        public int CordCount { get; set; } = 3;

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

    /// <summary>Any number of regular callers: Person N1 on jack J1, Person N2 on J2, and so on.</summary>
    internal sealed class Crowd
    {
        public Crowd(int count, FloatRange? callbackDelaySeconds = null)
        {
            Regular = new FakeCallerType(new FloatRange(3, 8), new FloatRange(15, 30), new FloatRange(5, 15), callbackDelaySeconds);
            var people = new IPerson[count];
            var callers = new ICaller[count];
            var entries = new IDirectoryEntry[count];
            for (int i = 0; i < count; i++)
            {
                people[i] = new FakePerson("Person", "N" + (i + 1));
                callers[i] = new FakeCaller(people[i], Regular);
                entries[i] = new FakeDirectoryEntry(people[i], "J" + (i + 1));
            }
            People = people;
            Callers = callers;
            Directory = new PhoneDirectory(entries);
            Content = new FakeContent { CallerTypes = new ICallerType[] { Regular }, People = people, Callers = callers, Directory = entries };
        }

        public FakeCallerType Regular { get; }
        public IReadOnlyList<IPerson> People { get; }
        public IReadOnlyList<ICaller> Callers { get; }
        public PhoneDirectory Directory { get; }
        public FakeContent Content { get; }

        public CallRequest Request(int caller, int recipient, double centsPerSecond = 5, double durationSeconds = 20) =>
            new CallRequest(Callers[caller], People[recipient], centsPerSecond, durationSeconds);
    }

    /// <summary>
    /// People N1..Nn on jacks J1..Jn, each with a regular and an emergency caller (as Content builds them for
    /// everyoneCalls), plus Police on S1, Fire Brigade on S2 and Doctor on S3, who never call (D36, D37).
    /// </summary>
    internal sealed class EmergencyWorld
    {
        public EmergencyWorld(int count = 8, double regularWeight = 5, double emergencyWeight = 1)
        {
            Regular = new FakeCallerType(new FloatRange(3, 8), new FloatRange(15, 30), new FloatRange(5, 15)) { SpawnWeight = regularWeight };
            Emergency = FakeCallerType.Emergency(emergencyWeight);
            var people = new List<IPerson>();
            var callers = new List<ICaller>();
            var entries = new List<IDirectoryEntry>();
            Services = new IPerson[]
            {
                new FakePerson("", "Police", FakeCallerType.ServiceTag),
                new FakePerson("", "Fire Brigade", FakeCallerType.ServiceTag),
                new FakePerson("", "Doctor", FakeCallerType.ServiceTag)
            };
            for (int i = 0; i < Services.Count; i++)
            {
                people.Add(Services[i]);
                entries.Add(new FakeDirectoryEntry(Services[i], "S" + (i + 1)));
            }
            var subscribers = new IPerson[count];
            for (int i = 0; i < count; i++)
            {
                subscribers[i] = new FakePerson("Person", "N" + (i + 1));
                people.Add(subscribers[i]);
                entries.Add(new FakeDirectoryEntry(subscribers[i], "J" + (i + 1)));
                callers.Add(new FakeCaller(subscribers[i], Regular));
            }
            foreach (IPerson person in subscribers) callers.Add(new FakeCaller(person, Emergency));
            People = subscribers;
            Callers = callers;
            Directory = new PhoneDirectory(entries);
            Content = new FakeContent
            {
                StartingCents = 10000,
                CallerTypes = new ICallerType[] { Regular, Emergency },
                People = people,
                Callers = callers,
                Directory = entries
            };
        }

        public FakeCallerType Regular { get; }
        public FakeCallerType Emergency { get; }
        public IReadOnlyList<IPerson> People { get; }
        public IReadOnlyList<IPerson> Services { get; }
        public IReadOnlyList<ICaller> Callers { get; }
        public PhoneDirectory Directory { get; }
        public FakeContent Content { get; }

        public ICaller EmergencyCallerOf(int person)
        {
            foreach (ICaller caller in Callers)
                if (caller.Type == Emergency && caller.Person == People[person]) return caller;
            return null;
        }

        public CallRequest EmergencyRequest(int caller, int service = 0) =>
            new CallRequest(EmergencyCallerOf(caller), Services[service], 7, 15, 30);
    }
}
