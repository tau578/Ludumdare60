using System;
using System.Collections.Generic;
using Switchboard.Cables;
using Switchboard.Domain;
using Switchboard.UI;
using UnityEngine;

namespace Switchboard.Game
{
    /// <summary>
    /// The tutorial (D57) with the spotlight. A new game guides the first call: the caller, their message, the caller's
    /// blinking jack (10b fix 1), the book, the recipient's row in the book (clicking it marks the jack, D58), a cord,
    /// the caller's jack and then the found jack, one at a time (10b fix 2), the money. Then the shop hints, each once: buying the first extra cord, a look at the
    /// mystery upgrade, More Profit and More Clients (fix 2). Continue gets only the shop hints
    /// its save hasn't seen. Each step lets only its own target take clicks (fix 1).
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
        private FirstOrderGuideStage? renderedStage;
        private bool renderedTextRead;
        private int renderedSpread = -1;
        private int renderedTrayCords = -1;

        public FirstOrderGuideStage Stage => sequence.Stage;
        public TutorialSpotlight Spotlight => spotlight;

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
                spotlight.DimPressed += OnDimPressed;
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
            if (sequence.Stage != FirstOrderGuideStage.WaitingForOrder) return;
            firstCard = card; firstCall = card.Call;
            sequence.FirstOrderShown();
            RefreshHints();
        }

        private void OnCardOpened(CallCardView card)
        {
            if (card.Call == firstCall) sequence.FirstPortraitClicked();
            RefreshHints();
        }

        private void OnBookChanged(bool open)
        {
            AdvanceText(); // Also handles a click in the frame the last character appeared.
            sequence.BookChanged(open);
            RefreshHints();
        }

        private void OnCablePickedUp(CableController cable)
        {
            sequence.CablePickedUp();
            RefreshHints();
        }

        private void OnCallEnded(CallEndedArgs e)
        {
            if (firstCall == null || e.Call != firstCall) return;
            sequence.FirstOrderEnded();
            RefreshHints();
        }

        private void OnSkipClicked()
        {
            sequence.Skip();
            RefreshHints();
        }

        // Only a click the dark layer swallowed counts: not Skip, not the pause button above it, not a lit target.
        private void OnDimPressed()
        {
            if (Time.timeScale <= 0) return;
            sequence.AnyClick();
            RefreshHints();
        }

        private void LateUpdate()
        {
            if (queue == null) return;
            if (firstCard != null && firstCard.Call != firstCall && !sequence.IsFirstCallOver) sequence.FirstOrderEnded();
            AdvanceText();
            if (firstCall != null && firstCall.State == CallState.Connected) sequence.FirstOrderConnected();
            sequence.CordsOut(AnyCordOut());
            if (firstCall != null) sequence.CallerJackPlugged(IsOccupied(firstCall.Request.From));
            // Game time, so the pause menu holds the timed steps, the click guards and the gap between shop hints.
            sequence.Tick(Time.deltaTime);
            if (sequence.IsFirstCallOver && !sequence.IsOver) PollShop();
            RefreshHints();
        }

        private void PollShop()
        {
            // A hint comes up only in calm play: not paused, the book shut (its pages would sit dimmed between the
            // player and the shop) and no cord in hand.
            bool canStart = Time.timeScale > 0 && (book == null || !book.IsOpen) && !AnyCordInHand();
            sequence.Offer(ShopHints.Cord, OfferOf(firstCord), canStart);
            sequence.Offer(ShopHints.Mystery, MysteryOffer(), canStart);
            sequence.Offer(ShopHints.Profit, OfferOf(firstProfit), canStart);
            sequence.Offer(ShopHints.Clients, OfferOf(firstClients), canStart);
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

        private void AdvanceText()
        {
            if (firstCard != null && firstCard.Call == firstCall && firstCard.IsTaskTextComplete)
                sequence.TaskTextFinished();
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
                || (stage == FirstOrderGuideStage.TaskText && sequence.IsTaskTextRead != renderedTextRead)
                || (stage == FirstOrderGuideStage.ReadingBook && book != null && book.CurrentSpreadIndex != renderedSpread)
                || (stage == FirstOrderGuideStage.Cables && TrayCordCount() != renderedTrayCords);
            if (!changed) return;
            renderedStage = stage;
            renderedTextRead = sequence.IsTaskTextRead;
            if (book != null) renderedSpread = book.CurrentSpreadIndex;
            renderedTrayCords = TrayCordCount();
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
                case FirstOrderGuideStage.TaskText:
                    // Once the message has typed out, any click moves on (fix 2); until then there is nothing to do.
                    text = sequence.IsTaskTextRead ? texts.readMessageDone : texts.readMessage;
                    if (firstCard != null) Add(ref count, firstCard.SpeechBubbleRect, 10, clickable: false);
                    break;
                case FirstOrderGuideStage.CallerJack:
                    // D58: blinking means a caller's line. Like the message, any click moves on; the jack takes none.
                    text = texts.callerJack;
                    AddSocket(ref count, firstCall.Request.From, clickable: false);
                    break;
                case FirstOrderGuideStage.Book:
                    text = texts.openBook;
                    if (book != null && book.ClosedBookGraphic != null) Add(ref count, book.ClosedBookGraphic.rectTransform, 12);
                    break;
                case FirstOrderGuideStage.ReadingBook:
                    // D58: the board lights the caller's jack, so only the recipient is looked up. Clicking their row
                    // marks the jack and closes the book; the page corners still turn the pages.
                    text = texts.findJacks;
                    anchor = book != null ? book.OpenBookRect : null;
                    AddBookRow(ref count, firstCall.Request.To);
                    AddPageCorner(ref count, -1);
                    AddPageCorner(ref count, 1);
                    break;
                case FirstOrderGuideStage.Cables:
                    text = texts.takeCord;
                    foreach (CableController cable in cables)
                        if (cable != null && cable.gameObject.activeInHierarchy && cable.IsInTray && cable.TrayView != null)
                            Add(ref count, (RectTransform)cable.TrayView.transform, 10);
                    break;
                // One jack at a time (fix 2): first the caller's blinking jack, then the jack found in the book.
                case FirstOrderGuideStage.PluggingCaller:
                    text = texts.plugCaller;
                    AddSocket(ref count, firstCall.Request.From);
                    break;
                case FirstOrderGuideStage.PluggingRecipient:
                    text = texts.plugRecipient;
                    AddSocket(ref count, firstCall.Request.To);
                    break;
                case FirstOrderGuideStage.Connected:
                    text = texts.connected;
                    Add(ref count, moneySlot, 8, clickable: false);
                    break;
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
                default:
                    text = texts.mystery;
                    Add(ref count, shop != null ? shop.ButtonFor(shop.Secret) : null, 8, clickable: false);
                    break;
            }
            spotlight.Show(text, spots, count, anchor, clickArea);
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

        private void AddSocket(ref int count, JackId jack, bool clickable = true)
        {
            if (sockets.TryGetValue(jack.Label, out SocketView socket))
                Add(ref count, (RectTransform)socket.transform, 8, true, clickable);
        }

        private void Add(ref int count, RectTransform rect, float padding, bool round = false, bool clickable = true,
            bool lit = true)
        {
            if (rect == null || count >= spots.Length) return;
            spots[count++] = new SpotlightTarget(rect, padding, round, clickable, lit);
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
                spotlight.DimPressed -= OnDimPressed;
            }
            foreach (var cable in cables) if (cable != null) cable.PickedUp -= OnCablePickedUp;
            cables.Clear();
            sockets.Clear();
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
