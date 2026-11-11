using Switchboard.UI;
using TMPro;
using UnityEngine;

namespace Switchboard.Game
{
    /// <summary>
    /// The paint box (D75, StyleGuide §3, §5 and §6): the named colours, the three font roles and the standard timings,
    /// in one asset. Views read a value when they apply it, so an edit made here in Play Mode shows at the next event
    /// and is kept afterwards. Nothing writes to it at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Switchboard/GameStyle")]
    public sealed class GameStyle : ScriptableObject, IBoardPalette
    {
        [Header("Surfaces")]
        [SerializeField, Tooltip("Paper: the speech bubble and the book's pages.")]
        private Color paper = new Color32(0xF3, 0xE9, 0xCF, 255);
        [SerializeField, Tooltip("Stamp paper: the cards' stamps.")]
        private Color stampPaper = new Color32(0xEA, 0xE0, 0xC7, 255);
        [SerializeField, Tooltip("Ink: text on paper, such as the speech bubble.")]
        private Color ink = new Color32(0x33, 0x24, 0x1A, 255);
        [SerializeField, Tooltip("Cream text on dark surfaces: the card badges.")]
        private Color creamText = new Color32(0xF3, 0xE9, 0xCF, 255);
        [SerializeField, Tooltip("Cream text on the menus' buttons.")]
        private Color menuText = new Color32(0xFF, 0xFA, 0xE8, 255);
        [SerializeField, Tooltip("Card teal: the cards' badges, and the plate under a talking call's tab (D83).")]
        private Color cardTeal = new Color32(0x17, 0x53, 0x51, 255);
        [SerializeField, Tooltip("Gold line: the thin curls on frames and panels.")]
        private Color goldLine = new Color32(0x96, 0x88, 0x1E, 255);
        [SerializeField, Tooltip("Brass plate, its lit top: the EMERGENCY plate.")]
        private Color brassPlateTop = new Color32(0xC6, 0xA9, 0x5E, 255);
        [SerializeField, Tooltip("Brass plate, its darker bottom.")]
        private Color brassPlateBottom = new Color32(0xA8, 0x8A, 0x42, 255);
        [SerializeField, Tooltip("Brass plate, its thin darker rim.")]
        private Color brassPlateRim = new Color32(0x6E, 0x5A, 0x1E, 255);

        [Header("Good: connected, paid")]
        [SerializeField, Tooltip("Good light: connected jacks.")]
        private Color goodLight = new Color32(0x7D, 0xFF, 0x9B, 255);
        [SerializeField, Tooltip("Good ring: the connection ring.")]
        private Color goodRing = new Color32(0x68, 0xF0, 0x86, 255);
        [SerializeField, Tooltip("Good ink, on paper: the \"Paid\" stamp.")]
        private Color goodInk = new Color32(0x26, 0x68, 0x3A, 255);

        [Header("Money gold: money and rewards only")]
        [SerializeField, Tooltip("Money gold text: the counter's amount, fee popups.")]
        private Color moneyText = new Color32(0xF2, 0xD2, 0x7A, 255);
        [SerializeField, Tooltip("Coin, its light tone.")]
        private Color coinLight = new Color32(0xFF, 0xDA, 0x70, 255);
        [SerializeField, Tooltip("Coin, its middle tone.")]
        private Color coinMiddle = new Color32(0xD4, 0x92, 0x31, 255);
        [SerializeField, Tooltip("Coin, its dark tone.")]
        private Color coinDark = new Color32(0x7E, 0x4B, 0x1B, 255);
        [SerializeField, Tooltip("Tip: the quick-connect tip.")]
        private Color tip = new Color32(0xFA, 0xE6, 0x8C, 255);

        [Header("Decoration: never money")]
        [SerializeField, Tooltip("Brass, muted: the frame's curls and rims.")]
        private Color brass = new Color32(0x96, 0x88, 0x1E, 255);
        [SerializeField, Tooltip("Light brass: decoration that should stay quiet. Titles use Title gold.")]
        private Color lightBrass = new Color32(0xD9, 0xBE, 0x72, 255);
        [SerializeField, Tooltip("Title gold: big titles (the pause title, the main menu's titles). Neither screen shows the money, so it competes with nothing there.")]
        private Color titleGold = new Color32(0xFF, 0xDB, 0x1F, 255);

        [Header("Error, crimson: wrong, fine, can't, missed")]
        [SerializeField, Tooltip("Error glow, light on dark: the wrong-number flash and the red ring over a live call's jack.")]
        private Color errorGlow = new Color32(0xEE, 0x44, 0x50, 255);
        [SerializeField, Tooltip("Error, text on dark: the fine and \"can't afford\" on the money counter.")]
        private Color error = new Color32(0xC4, 0x2F, 0x3A, 255);
        [SerializeField, Tooltip("Error ink, on paper: the \"Wrong number!\" stamp.")]
        private Color errorInk = new Color32(0xA4, 0x26, 0x26, 255);
        [SerializeField, Tooltip("Error deep, on paper, the worst case: the \"Missed\" stamp.")]
        private Color errorDeep = new Color32(0x74, 0x12, 0x1E, 255);

        [Header("Alarm, orange-red: an emergency waiting")]
        [SerializeField, Tooltip("Alarm glow, light on dark: the countdown ring, the lamp's glows, the light on the EMERGENCY plate.")]
        private Color alarmGlow = new Color32(0xFF, 0x6A, 0x3D, 255);
        [SerializeField, Tooltip("Alarm: the lit emergency lamp. The emergency card's badge and rate outline use the same colour, set as the accent in GameConfig.")]
        private Color alarm = new Color32(0xE8, 0x4A, 0x34, 255);
        [SerializeField, Tooltip("Alarm quiet: the unlit emergency lamp.")]
        private Color alarmQuiet = new Color32(0x8E, 0x34, 0x26, 255);

        [Header("Attention, amber: hover, guide")]
        [SerializeField, Tooltip("Attention hover: hovered jacks, book-row hover.")]
        private Color attentionHover = new Color32(0xF5, 0xD2, 0x78, 255);
        [SerializeField, Tooltip("Attention lamp: the caller's blinking jack.")]
        private Color attentionLamp = new Color32(0xFF, 0xD0, 0x6E, 255);

        [Header("Neutral: neither good nor bad")]
        [SerializeField, Tooltip("The \"New order!\" stamp.")]
        private Color newOrderInk = new Color32(0x7B, 0x52, 0x20, 255);
        [SerializeField, Tooltip("Neutral ink: the \"Cut off\" and \"Hung up\" stamps.")]
        private Color cutOffInk = new Color32(0x5C, 0x50, 0x44, 255);

        [Header("The EMERGENCY plate's engraved letters")]
        [SerializeField, Tooltip("Quiet: dark ink cut into the brass.")]
        private Color signQuiet = new Color32(0x3A, 0x14, 0x0C, 255);
        [SerializeField, Tooltip("Lit: a touch redder while the alarm's light is on the plate.")]
        private Color signLit = new Color32(0x48, 0x0B, 0x0D, 255);

        [Header("The board's glows")]
        [SerializeField, Range(.25f, .8f), Tooltip("The wrong-number ring's outer edge, as a share of the jack's size (it starts at 0.22).")]
        private float wrongNumberRingOuter = .54f;
        [SerializeField, Range(0, 1), Tooltip("The wrong-number ring's strength (1 is full).")]
        private float wrongNumberStrength = .85f;
        [SerializeField, Range(.5f, 2.5f), Tooltip("The red ring when the pointer is over a live call's jack, hand empty or holding a cord (a connected jack's green glow is 1.4).")]
        private float blockedGlowIntensity = 1.5f;

        [Header("Fonts")]
        [SerializeField, Tooltip("Display: big decorative titles only (Boecklins Universe SDF Static).")]
        private TMP_FontAsset displayFont;
        [SerializeField, Tooltip("Label: short period labels: stamps, signs, name plates, menus (Della Respira SDF Static).")]
        private TMP_FontAsset labelFont;
        [SerializeField, Tooltip("Plain: anything read fast or holding numbers, and the only font with ¢ (LiberationSans SDF).")]
        private TMP_FontAsset plainFont;

        [Header("Timings: stamps")]
        [SerializeField, Min(.05f), Tooltip("A stamp's pop, eased out with a small overshoot.")]
        private float stampPopSeconds = .38f;
        [SerializeField, Range(1, 1.4f)] private float stampOvershoot = 1.16f;
        [SerializeField, Min(.01f)] private float stampExitSeconds = .22f;
        [SerializeField, Min(0), Tooltip("How long an ended call's stamp stays.")]
        private float stampHoldSeconds = 1.5f;

        [Header("Timings: cards")]
        [SerializeField, Min(.01f), Tooltip("A card's squash when a click takes it.")]
        private float cardPressSeconds = .2f;
        [SerializeField, Min(.05f), Tooltip("A new card's slide in from the left.")]
        private float cardArrivalSeconds = .55f;
        [SerializeField, Min(.05f), Tooltip("The new card's ring shake, after it lands.")]
        private float ringShakeSeconds = .45f;
        [SerializeField, Min(0)] private float ringShakeDegrees = 5f;
        [SerializeField, Min(.05f), Tooltip("A wrong number's \"no\" shake: three swings, decaying.")]
        private float wrongShakeSeconds = .4f;
        [SerializeField, Min(0)] private float wrongShakePixels = 12f;

        [Header("Timings: emergencies (game time, frozen on pause)")]
        [SerializeField, Min(0), Tooltip("A waiting emergency card's gentle wobble.")]
        private float emergencyWobbleDegrees = 2f;
        [SerializeField, Min(0)] private float emergencyWobblesPerSecond = 1.2f;
        [SerializeField, Min(0), Tooltip("Seconds left when the countdown ring turns urgent and the wobble grows.")]
        private float urgentSeconds = 10f;
        [SerializeField, Min(0), Tooltip("The wobble in the urgent seconds, in step with the ring's pulse.")]
        private float urgentWobbleDegrees = 5f;
        [SerializeField, Min(0), Tooltip("The countdown ring's pulse in the urgent seconds.")]
        private float urgentPulsesPerSecond = 3f;
        [SerializeField, Min(0), Tooltip("The emergency lamp's flashes.")]
        private float lampFlashesPerSecond = 2f;

        [Header("Timings: patience (D63; game time, frozen on pause)")]
        [SerializeField, Min(0), Tooltip("Seconds of patience left when the amber ring starts its gentle pulse.")]
        private float patienceUrgentSeconds = 5f;
        [SerializeField, Min(0), Tooltip("The amber patience ring's pulse in its last seconds; slower and softer than an emergency's.")]
        private float patiencePulsesPerSecond = 1.5f;

        [Header("Timings: money")]
        [SerializeField, Min(.01f), Tooltip("One coin's flight to the counter.")]
        private float coinFlightSeconds = 1f;
        [SerializeField, Min(0)] private float coinSpacingSeconds = .055f;
        [SerializeField, Min(.01f), Tooltip("A money drum's roll to its new digit.")]
        private float drumRollSeconds = .15f;
        [SerializeField, Min(0), Tooltip("The money plate's glow when income lands.")]
        private float plateGlowSeconds = .6f;
        [SerializeField, Min(0), Tooltip("A fine's plaque shake, decaying.")]
        private float fineShakeSeconds = .3f;
        [SerializeField, Min(0)] private float fineShakePixels = 6f;
        [SerializeField, Min(.01f), Tooltip("Fee, tip and fine popups rise and fade.")]
        private float popupSeconds = 1f;

        [Header("Timings: the board")]
        [SerializeField, Min(.01f), Tooltip("The connection ring.")]
        private float connectRingSeconds = .5f;
        [SerializeField, Min(.01f), Tooltip("A connected cord's wave, pinned at both plugs.")]
        private float cordWaveSeconds = .5f;
        [SerializeField, Min(0)] private float cordWavePixels = 12f;
        [SerializeField, Min(.01f), Tooltip("A wrong number's sharp flicker on the jack.")]
        private float wrongFlashSeconds = .5f;
        [SerializeField, Min(.01f), Tooltip("A wrong number's short circuit at the plug.")]
        private float shortCircuitSeconds = .6f;
        [SerializeField, Min(.01f), Tooltip("A wrong number's cord jolt.")]
        private float cordJoltSeconds = .35f;
        [SerializeField, Min(0)] private float cordJoltPixels = 7f;
        [SerializeField, Min(1), Tooltip("The speech bubble's typing.")]
        private float typingCharactersPerSecond = 40f;

        public Color Paper => paper;
        public Color StampPaper => stampPaper;
        public Color Ink => ink;
        public Color CreamText => creamText;
        public Color MenuText => menuText;
        public Color CardTeal => cardTeal;
        public Color GoldLine => goldLine;
        public Color BrassPlateTop => brassPlateTop;
        public Color BrassPlateBottom => brassPlateBottom;
        public Color BrassPlateRim => brassPlateRim;
        public Color GoodLight => goodLight;
        public Color GoodRing => goodRing;
        public Color GoodInk => goodInk;
        public Color MoneyText => moneyText;
        public Color CoinLight => coinLight;
        public Color CoinMiddle => coinMiddle;
        public Color CoinDark => coinDark;
        public Color Tip => tip;
        public Color Brass => brass;
        public Color LightBrass => lightBrass;
        public Color TitleGold => titleGold;
        public Color ErrorGlow => errorGlow;
        public Color Error => error;
        public Color ErrorInk => errorInk;
        public Color ErrorDeep => errorDeep;
        public Color AlarmGlow => alarmGlow;
        public Color Alarm => alarm;
        public Color AlarmQuiet => alarmQuiet;
        public Color AttentionHover => attentionHover;
        public Color AttentionLamp => attentionLamp;
        public Color NewOrderInk => newOrderInk;
        public Color CutOffInk => cutOffInk;
        public Color SignQuiet => signQuiet;
        public Color SignLit => signLit;

        public float WrongNumberRingOuter => wrongNumberRingOuter;
        public float WrongNumberStrength => wrongNumberStrength;
        public float BlockedGlowIntensity => blockedGlowIntensity;

        public TMP_FontAsset DisplayFont => displayFont;
        public TMP_FontAsset LabelFont => labelFont;
        public TMP_FontAsset PlainFont => plainFont;

        public float StampPopSeconds => stampPopSeconds;
        public float StampOvershoot => stampOvershoot;
        public float StampExitSeconds => stampExitSeconds;
        public float StampHoldSeconds => stampHoldSeconds;
        public float CardPressSeconds => cardPressSeconds;
        public float CardArrivalSeconds => cardArrivalSeconds;
        public float RingShakeSeconds => ringShakeSeconds;
        public float RingShakeDegrees => ringShakeDegrees;
        public float WrongShakeSeconds => wrongShakeSeconds;
        public float WrongShakePixels => wrongShakePixels;
        public float EmergencyWobbleDegrees => emergencyWobbleDegrees;
        public float EmergencyWobblesPerSecond => emergencyWobblesPerSecond;
        public float UrgentSeconds => urgentSeconds;
        public float UrgentWobbleDegrees => urgentWobbleDegrees;
        public float UrgentPulsesPerSecond => urgentPulsesPerSecond;
        public float LampFlashesPerSecond => lampFlashesPerSecond;
        public float PatienceUrgentSeconds => patienceUrgentSeconds;
        public float PatiencePulsesPerSecond => patiencePulsesPerSecond;
        public float CoinFlightSeconds => coinFlightSeconds;
        public float CoinSpacingSeconds => coinSpacingSeconds;
        public float DrumRollSeconds => drumRollSeconds;
        public float PlateGlowSeconds => plateGlowSeconds;
        public float FineShakeSeconds => fineShakeSeconds;
        public float FineShakePixels => fineShakePixels;
        public float PopupSeconds => popupSeconds;
        public float ConnectRingSeconds => connectRingSeconds;
        public float CordWaveSeconds => cordWaveSeconds;
        public float CordWavePixels => cordWavePixels;
        public float WrongFlashSeconds => wrongFlashSeconds;
        public float ShortCircuitSeconds => shortCircuitSeconds;
        public float CordJoltSeconds => cordJoltSeconds;
        public float CordJoltPixels => cordJoltPixels;
        public float TypingCharactersPerSecond => typingCharactersPerSecond;
    }
}
