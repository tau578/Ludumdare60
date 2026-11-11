using System;
using System.Collections.Generic;
using Switchboard.Cables;
using Switchboard.Domain;
using Switchboard.UI;
using UnityEngine;

namespace Switchboard.Game
{
    /// <summary>
    /// The tutorial (D57) with the spotlight. A new game guides the first call, the caller's side first and then the
    /// recipient's (13a pass 6, D81): the caller's card, a tray cord (with the caller's now blinking jack lit beside it),
    /// the caller's jack, the book (the hint names the recipient as the book shows them), the recipient's row (clicking
    /// it marks the jack, D58), the marked jack, the money. Then the shop hints, each once: buying the first extra cord,
    /// a look at the mystery upgrade, More Profit and More Clients (fix 2). Once the first call is over, a click on a
    /// plug of its cord frees it (13a pass 3, D79, D81), and the first emergency's card gets its own step (13a pass 4,
    /// D81). While any step shows, the game's clock holds (<see cref="HoldsClock"/>), input stays live. Continue gets
    /// only the hints its save hasn't seen. Each step lets only its own target take clicks (fix 1); a step that only
    /// informs lets only its OK button take them (13a pass 5, D81). After the free-cord step, the second call repeats
    /// the caller's side with the freed cord in hand: the next caller's card, then its blinking jack (13a pass 6).
    /// </summary>
    public sealed class FirstOrderGuide : MonoBehaviour
    {
        private FirstOrderGuideSequence sequence = new FirstOrderGuideSequence();
        private CallQueueView queue;
        private CallService calls;
        private PhoneBookView book;
        private TutorialSpotlight spotlight;
        private RectTransform moneySlot;
        private ShopView shop;
        private UpgradeService upgrades;
        private IUpgrade firstCord;
        private IUpgrade firstProfit;
        private IUpgrade firstClients;
        private Action<ShopHints> shopHintsDone;
        private ShopHints recordedHints;
        private Action firstCallOver;
        private readonly List<CableController> cables = new List<CableController>();
        private readonly Dictionary<string, SocketView> sockets = new Dictionary<string, SocketView>();
        private readonly SpotlightTarget[] spots = new SpotlightTarget[SpotlightGraphic.MaxHoles];
        private CordBridge cordBridge;
        private Call firstCall;
        private CallCardView firstCard;
        // The cord that carried the first call, still on the board when the call ended (D81).
        private CableController finishedCord;
        // The emergency whose card rings for the emergency step (D81), and its card.
        private Call emergencyCall;
        private CallCardView emergencyCard;
        // The second call's caller (D81): the oldest waiting regular call on a card, its jack free.
        private Call nextCall;
        private Call renderedNextCall;
        private FirstOrderGuideStage? renderedStage;
        private int renderedSpread = -1;
        private int renderedTrayCords = -1;

        public FirstOrderGuideStage Stage => sequence.Stage;
        public TutorialSpotlight Spotlight => spotlight;

        /// <summary>
        /// A step is on screen: the game's clock holds (D81, lead 2026-11-12), so no caller loses patience, no call
        /// times out, talks or pays and nothing new rings while the player reads. Input stays live.
        /// </summary>
        public bool HoldsClock => sequence.IsGuiding;

        /// <param name="releaseCall">Called once the first call's steps are over: its caller may hang up again.</param>
        /// <param name="guideFirstCall">False on Continue: only the shop hints, from <see cref="BindShop"/>.</param>
        public void Bind(CallQueueView callQueue, CallService service, PhoneBookView phoneBook, CableController[] wires,
            CordBridge bridge, TutorialSpotlight view, IReadOnlyList<SocketView> board, RectTransform money,
            Action releaseCall = null, bool guideFirstCall = true)
        {
            Unbind();
            sequence = new FirstOrderGuideSequence(ShopHints.None, guideFirstCall);
            recordedHints = ShopHints.None;
            renderedStage = null;
            firstCall = null;
            firstCard = null;
            finishedCord = null;
            emergencyCall = null;
            emergencyCard = null;
            nextCall = null;
            renderedNextCall = null;
            queue = callQueue; calls = service; book = phoneBook; cordBridge = bridge;
            spotlight = view; moneySlot = money; firstCallOver = releaseCall;
            enabled = true;
            if (board != null)
                foreach (SocketView socket in board)
                    if (socket != null) sockets[socket.Address] = socket;
            if (cordBridge != null) cordBridge.CableAdded += RegisterCable;
            if (spotlight != null)
            {
                spotlight.SkipClicked += OnSkipClicked;
                spotlight.OkClicked += OnOkClicked;
            }
            queue.OrderShown += OnOrderShown;
            queue.CardOpened += OnCardOpened;
            calls.CallEnded += OnCallEnded;
            if (book != null) book.OpenChanged += OnBookChanged;
            foreach (var cable in wires) RegisterCable(cable);
            RefreshHints();
        }

        /// <summary>
        /// The shop hints: the first extra cord (step 9) and the mystery upgrade (10), then the first More Profit and
        /// More Clients, each once it is affordable. Each chain's first entry comes from Economy.json.
        /// </summary>
        /// <param name="done">The hints the save has already shown; they never come back.</param>
        /// <param name="recordDone">Told whenever another hint is done, so the save remembers it.</param>
        public void BindShop(ShopView shopView, UpgradeService service, ShopHints done = ShopHints.None,
            Action<ShopHints> recordDone = null)
        {
            shop = shopView;
            upgrades = service;
            firstCord = FirstOf(UpgradeKind.ExtraWires);
            firstProfit = FirstOf(UpgradeKind.ExtraIncome);
            firstClients = FirstOf(UpgradeKind.ExtraClients);
            sequence.MarkShopHintsDone(done);
            recordedHints = sequence.ShopHintsDone;
            shopHintsDone = recordDone;
            RefreshHints();
        }

        private IUpgrade FirstOf(UpgradeKind kind)
        {
            IReadOnlyList<IUpgrade> chain = upgrades != null ? upgrades.ChainOf(kind) : null;
            return chain != null && chain.Count > 0 ? chain[0] : null;
        }

        private void RegisterCable(CableController cable)
        {
            if (cable == null || !cable.gameObject.activeInHierarchy || cables.Contains(cable)) return;
            cables.Add(cable);
            cable.PickedUp += OnCablePickedUp;
            // A bought cord joins the current step without restarting the tutorial.
            renderedTrayCords = -1;
            RefreshHints();
        }

        private void OnOrderShown(CallCardView card)
        {
            if (card.Call != null && card.Call.Request.Script.IsEmergency && !EmergencyRinging())
            {
                emergencyCall = card.Call;
                emergencyCard = card;
            }
            if (sequence.Stage != FirstOrderGuideStage.WaitingForOrder) return;
            firstCard = card; firstCall = card.Call;
            sequence.FirstOrderShown();
            RefreshHints();
        }

        private void OnCardOpened(CallCardView card)
        {
            if (card.Call == firstCall) sequence.FirstPortraitClicked();
            // The second call's card taken: the light moves to its jack in the same frame.
            PollNextCaller();
            RefreshHints();
        }

        private void OnBookChanged(bool open)
        {
            sequence.BookChanged(open);
            RefreshHints();
        }

        private void OnCablePickedUp(CableController cable)
        {
            sequence.CablePickedUp();
            // The freed cord's next step comes in the same frame, so the light moves rather than blinks off; a ringing
            // first emergency still comes first, as in LateUpdate.
            if (sequence.IsFirstCallOver && !sequence.IsOver) sequence.EmergencyRinging(EmergencyRinging(), Time.timeScale > 0);
            PollNextCaller();
            RefreshHints();
        }

        private void OnCallEnded(CallEndedArgs e)
        {
            if (firstCall == null || e.Call != firstCall) return;
            finishedCord = CordBetween(firstCall.Request.From, firstCall.Request.To);
            sequence.FirstOrderEnded(finishedCord != null);
            RefreshHints();
        }

        private void OnSkipClicked()
        {
            sequence.Skip();
            RefreshHints();
        }

        // The hint's OK button: the one way to end an information step (D81).
        private void OnOkClicked()
        {
            if (Time.timeScale <= 0) return;
            sequence.OkClicked();
            RefreshHints();
        }

        private void LateUpdate()
        {
            if (queue == null) return;
            if (firstCard != null && firstCard.Call != firstCall && !sequence.IsFirstCallOver) sequence.FirstOrderEnded();
            if (firstCall != null && firstCall.State == CallState.Connected) sequence.FirstOrderConnected();
            sequence.CordsOut(AnyCordOut());
            if (firstCall != null) sequence.CallerJackPlugged(IsOccupied(firstCall.Request.From));
            // Game time, so the pause menu holds the timed steps, the click guards and the gap between shop hints.
            sequence.Tick(Time.deltaTime);
            if (sequence.IsFirstCallOver && !sequence.IsOver)
            {
                // First: the emergency's own time runs until its step shows.
                sequence.EmergencyRinging(EmergencyRinging(), Time.timeScale > 0);
                sequence.FinishedCordPlugged(finishedCord != null && finishedCord.Phase == CablePhase.Connected, IsCalm());
                PollNextCaller();
                PollShop();
            }
            RefreshHints();
        }

        private void PollShop()
        {
            bool canStart = IsCalm();
            sequence.Offer(ShopHints.Cord, OfferOf(firstCord), canStart);
            sequence.Offer(ShopHints.Mystery, MysteryOffer(), canStart);
            sequence.Offer(ShopHints.Profit, OfferOf(firstProfit), canStart);
            sequence.Offer(ShopHints.Clients, OfferOf(firstClients), canStart);
        }

        private void PollNextCaller()
        {
            if (!sequence.IsFirstCallOver || sequence.IsOver) return;
            // Keep the lit caller while it still waits, so the light doesn't jump between callers.
            if (nextCall == null || !IsNextCallerCandidate(nextCall)) nextCall = FindNextCaller();
            sequence.NextCallerPolled(AnyCordHeld(), nextCall != null, nextCall != null && nextCall.State != CallState.Incoming,
                Time.timeScale > 0 && (book == null || !book.IsOpen));
        }

        // The oldest waiting regular call on a card whose caller's jack is free; calls are kept in arrival order.
        private Call FindNextCaller()
        {
            IReadOnlyList<Call> live = calls != null ? calls.Live : null;
            if (live == null) return null;
            for (int i = 0; i < live.Count; i++)
                if (IsNextCallerCandidate(live[i])) return live[i];
            return null;
        }

        private bool IsNextCallerCandidate(Call call) =>
            (call.State == CallState.Incoming || call.State == CallState.Taken) && !call.Request.Script.IsEmergency &&
            sockets.TryGetValue(call.Request.From.Label, out SocketView socket) && !socket.IsOccupied &&
            queue != null && queue.WaitingCardOf(call) != null;

        private bool AnyCordHeld()
        {
            foreach (CableController cable in cables)
                if (cable != null && cable.gameObject.activeInHierarchy && cable.Phase == CablePhase.Held) return true;
            return false;
        }

        // The tracked emergency's card is still on show, waiting to be taken.
        private bool EmergencyRinging() => emergencyCard != null && emergencyCall != null &&
            emergencyCard.Call == emergencyCall && emergencyCall.State == CallState.Incoming;

        // A step after the first call comes up only in calm play: not paused, the book shut (its pages would sit dimmed
        // between the player and the target) and no cord in hand.
        private bool IsCalm() => Time.timeScale > 0 && (book == null || !book.IsOpen) && !AnyCordInHand();

        // The cord plugged from one jack to the other, if one is.
        private CableController CordBetween(JackId a, JackId b)
        {
            if (!sockets.TryGetValue(a.Label, out SocketView from) || !sockets.TryGetValue(b.Label, out SocketView to)) return null;
            CableController cable = from.OccupyingCable;
            return cable != null && cable == to.OccupyingCable && cable.Phase == CablePhase.Connected ? cable : null;
        }

        private ShopOffer OfferOf(IUpgrade upgrade)
        {
            if (shop == null || upgrades == null || upgrade == null) return ShopOffer.NotForSale;
            if (upgrades.IsOwned(upgrade.Id)) return ShopOffer.Owned;
            // Not on a shop button (another chain holds every slot): its hint never shows.
            if (shop.ButtonFor(upgrade) == null) return ShopOffer.NotForSale;
            return upgrades.CanBuy(upgrade.Id) == PurchaseCheck.Ok ? ShopOffer.Affordable : ShopOffer.TooExpensive;
        }

        private ShopOffer MysteryOffer()
        {
            IUpgrade secret = shop != null && upgrades != null ? shop.Secret : null;
            if (secret == null) return ShopOffer.NotForSale;
            if (upgrades.IsOwned(secret.Id)) return ShopOffer.Owned;
            return shop.ButtonFor(secret) != null ? ShopOffer.Affordable : ShopOffer.NotForSale;
        }

        private bool AnyCordInHand()
        {
            foreach (CableController cable in cables)
                if (cable != null && cable.gameObject.activeInHierarchy &&
                    (cable.Phase == CablePhase.Held || cable.Phase == CablePhase.OneEndConnected)) return true;
            return false;
        }

        private bool AnyCordOut()
        {
            foreach (CableController cable in cables)
                if (cable != null && cable.gameObject.activeInHierarchy && !cable.IsInTray) return true;
            return false;
        }

        private bool IsOccupied(JackId jack) => sockets.TryGetValue(jack.Label, out SocketView socket) && socket.IsOccupied;

        private int TrayCordCount()
        {
            int count = 0;
            foreach (CableController cable in cables)
                if (cable != null && cable.gameObject.activeInHierarchy && cable.IsInTray && cable.TrayView != null) count++;
            return count;
        }

        private void RefreshHints()
        {
            RecordShopHints();
            FirstOrderGuideStage stage = Stage;
            bool changed = renderedStage != stage
                || (stage == FirstOrderGuideStage.ReadingBook && book != null && book.CurrentSpreadIndex != renderedSpread)
                || (stage == FirstOrderGuideStage.Cables && TrayCordCount() != renderedTrayCords)
                || ((stage == FirstOrderGuideStage.NextCard || stage == FirstOrderGuideStage.NextCaller) &&
                    nextCall != renderedNextCall);
            if (!changed) return;
            renderedStage = stage;
            if (book != null) renderedSpread = book.CurrentSpreadIndex;
            renderedTrayCords = TrayCordCount();
            renderedNextCall = nextCall;
            if (sequence.IsFirstCallOver && firstCallOver != null)
            {
                Action release = firstCallOver;
                firstCallOver = null;
                release();
            }
            // Only the book step may open the book (Tab or a click); the open book itself can always be closed.
            if (book != null)
                book.OpenLocked = (sequence.IsGuiding || stage == FirstOrderGuideStage.WaitingForOrder) && stage != FirstOrderGuideStage.Book;
            if (sequence.IsOver)
            {
                if (spotlight != null) spotlight.Hide();
                // Nothing runs once the guide is done.
                Unbind();
                enabled = false;
                return;
            }
            if (spotlight == null) return;
            if (!sequence.IsGuiding)
            {
                // Normal play between the first call and the shop steps: no light and no block.
                if (spotlight.IsShowing) spotlight.Hide();
                return;
            }
            TutorialTexts texts = spotlight.Texts;
            int count = 0;
            string text;
            RectTransform anchor = null;
            RectTransform clickArea = null;
            switch (stage)
            {
                case FirstOrderGuideStage.Portrait:
                    text = texts.clickCaller;
                    if (firstCard != null) Add(ref count, (RectTransform)firstCard.transform, 10);
                    break;
                // 13a pass 6 (D81): the caller's side first. Pass 7: one lit thing per step, so only the tray's cords; the
                // caller's blinking jack (D58) is explained at its own step.
                case FirstOrderGuideStage.Cables:
                    text = texts.takeCord;
                    foreach (CableController cable in cables)
                        if (cable != null && cable.gameObject.activeInHierarchy && cable.IsInTray && cable.TrayView != null)
                            Add(ref count, (RectTransform)cable.TrayView.transform, 10);
                    break;
                // One jack at a time (10b fix 2): the caller's blinking jack now, the marked one after the book.
                case FirstOrderGuideStage.PluggingCaller:
                    text = texts.plugCaller;
                    AddSocket(ref count, firstCall.Request.From);
                    break;
                case FirstOrderGuideStage.Book:
                    // Then the recipient's side: who they want, named as the book shows them.
                    text = texts.openBook.Replace(TutorialTexts.NamePlaceholder, RecipientName(firstCall));
                    if (book != null && book.ClosedBookGraphic != null) Add(ref count, book.ClosedBookGraphic.rectTransform, 12);
                    break;
                case FirstOrderGuideStage.ReadingBook:
                    // D58: clicking the recipient's row marks the jack and closes the book; the page corners still turn
                    // the pages. A held cord stays in hand through it all (the cord input keeps it on book clicks).
                    text = texts.findJacks;
                    anchor = book != null ? book.OpenBookRect : null;
                    AddBookRow(ref count, firstCall.Request.To);
                    AddPageCorner(ref count, -1);
                    AddPageCorner(ref count, 1);
                    break;
                case FirstOrderGuideStage.PluggingRecipient:
                    text = texts.plugRecipient;
                    AddSocket(ref count, firstCall.Request.To);
                    break;
                // 13a pass 8: one focus per step. First the tab the talking call shrank to (D83), then the money.
                case FirstOrderGuideStage.Connected:
                    text = texts.connected;
                    if (firstCard != null && firstCard.Call == firstCall) Add(ref count, firstCard.PortraitRect, 6, clickable: false);
                    break;
                case FirstOrderGuideStage.Earning:
                    text = texts.earning;
                    Add(ref count, moneySlot, 8, clickable: false);
                    break;
                case FirstOrderGuideStage.FreeCord:
                    // D81: the hole on one plug of the finished cord, the caller's end (the cord joins the call's two
                    // jacks, and the step blocks every click that could move it); the pickup ends the step.
                    text = texts.freeCord;
                    AddSocket(ref count, firstCall.Request.From);
                    break;
                // 13a pass 6 (D81): the second call, the caller's side again: the card, then its jack, which blinks once
                // the card is taken (10b fix 3). The player finishes the call alone.
                case FirstOrderGuideStage.NextCard:
                    text = texts.nextCard;
                    CallCardView nextCard = nextCall != null && queue != null ? queue.WaitingCardOf(nextCall) : null;
                    if (nextCard != null) Add(ref count, (RectTransform)nextCard.transform, 10);
                    break;
                case FirstOrderGuideStage.NextCaller:
                    text = texts.nextCaller;
                    if (nextCall != null) AddSocket(ref count, nextCall.Request.From);
                    break;
                case FirstOrderGuideStage.Emergency:
                    // D81: the hole on the ringing card; taking it ends the step. The clock holds meanwhile.
                    text = texts.emergency;
                    if (emergencyCard != null) Add(ref count, (RectTransform)emergencyCard.transform, 10);
                    break;
                // Purchase steps (13a pass 8): the hole is the shop button, clicked where it is; the purchase ends the step.
                case FirstOrderGuideStage.BuyCord:
                    text = texts.buyCord;
                    Add(ref count, shop != null ? shop.ButtonFor(firstCord) : null, 8);
                    break;
                case FirstOrderGuideStage.Profit:
                    text = texts.buyProfit;
                    Add(ref count, shop != null ? shop.ButtonFor(firstProfit) : null, 8);
                    break;
                case FirstOrderGuideStage.Clients:
                    text = texts.buyClients;
                    Add(ref count, shop != null ? shop.ButtonFor(firstClients) : null, 8);
                    break;
                // The mystery hint only informs (D81): its button is lit but takes no click.
                default:
                    text = texts.mystery;
                    Add(ref count, shop != null ? shop.ButtonFor(shop.Secret) : null, 8, clickable: false);
                    break;
            }
            spotlight.Show(text, spots, count, anchor, clickArea, sequence.IsInformation);
        }

        private void AddBookRow(ref int count, JackId jack)
        {
            if (book == null) return;
            RectTransform row = book.RowOnSpread(jack.Label);
            if (row != null) { Add(ref count, row, 4); return; }
            RectTransform corner = book.PageCorner(book.SpreadDirectionTo(jack.Label));
            for (int i = 0; i < count; i++) if (spots[i].Rect == corner) return;
            Add(ref count, corner, 6);
        }

        // Takes clicks without a light, unless the corner is already lit (it turns to the row).
        private void AddPageCorner(ref int count, int direction)
        {
            RectTransform corner = book != null ? book.PageCorner(direction) : null;
            if (corner == null) return;
            for (int i = 0; i < count; i++) if (spots[i].Rect == corner) return;
            Add(ref count, corner, 0, lit: false);
        }

        private void AddSocket(ref int count, JackId jack, bool clickable = true, bool aside = false)
        {
            if (sockets.TryGetValue(jack.Label, out SocketView socket))
                Add(ref count, (RectTransform)socket.transform, 8, true, clickable, aside: aside);
        }

        private void Add(ref int count, RectTransform rect, float padding, bool round = false, bool clickable = true,
            bool lit = true, bool aside = false)
        {
            if (rect == null || count >= spots.Length) return;
            spots[count++] = new SpotlightTarget(rect, padding, round, clickable, lit, aside);
        }

        // The recipient as the book lists them (its first entry for the jack, the row the book step lights).
        private string RecipientName(Call call)
        {
            string name = book != null ? book.EntryNameFor(call.Request.To.Label) : null;
            return string.IsNullOrEmpty(name) ? call.Request.To.Label : name;
        }

        private void RecordShopHints()
        {
            ShopHints done = sequence.ShopHintsDone;
            if (done == recordedHints) return;
            recordedHints = done;
            shopHintsDone?.Invoke(done);
        }

        private void OnDisable() => Unbind();

        private void Unbind()
        {
            if (queue != null) { queue.OrderShown -= OnOrderShown; queue.CardOpened -= OnCardOpened; }
            if (calls != null) calls.CallEnded -= OnCallEnded;
            if (book != null)
            {
                book.OpenChanged -= OnBookChanged;
                book.OpenLocked = false;
            }
            if (cordBridge != null) cordBridge.CableAdded -= RegisterCable;
            if (spotlight != null)
            {
                spotlight.SkipClicked -= OnSkipClicked;
                spotlight.OkClicked -= OnOkClicked;
            }
            foreach (var cable in cables) if (cable != null) cable.PickedUp -= OnCablePickedUp;
            cables.Clear();
            sockets.Clear();
            finishedCord = null;
            emergencyCall = null;
            emergencyCard = null;
            nextCall = null;
            renderedNextCall = null;
            cordBridge = null;
            queue = null; calls = null; book = null;
            shop = null; upgrades = null; firstCord = null; firstProfit = null; firstClients = null;
            shopHintsDone = null;
            // A guide that stops early (skipped, or the scene closing) never leaves the call held.
            Action release = firstCallOver;
            firstCallOver = null;
            release?.Invoke();
        }
    }
}
