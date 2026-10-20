using System;
using System.Collections.Generic;
using Switchboard.Domain;
using Switchboard.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Switchboard.Game
{
    /// <summary>
    /// The tutorial of a new game (D57) with the spotlight: the caller, their message, the book, both jacks in the book,
    /// a cord, both jacks on the board, the money; later, buying the first extra cord and a look at the mystery upgrade.
    /// It is mandatory (fix 1): each step lets only its own target take clicks. Bound only when no save was loaded.
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
        private Action firstCallOver;
        private readonly List<CableController> cables = new List<CableController>();
        private readonly Dictionary<string, SocketView> sockets = new Dictionary<string, SocketView>();
        private readonly SpotlightTarget[] spots = new SpotlightTarget[SpotlightGraphic.MaxHoles];
        private CordBridge cordBridge;
        private Call firstCall;
        private CallCardView firstCard;
        private FirstOrderGuideStage? renderedStage;
        private int renderedSpread = -1;
        private int renderedTrayCords = -1;

        public FirstOrderGuideStage Stage => sequence.Stage;
        public TutorialSpotlight Spotlight => spotlight;

        /// <param name="releaseCall">Called once the first call's steps are over: its caller may hang up again.</param>
        public void Bind(CallQueueView callQueue, CallService service, PhoneBookView phoneBook, CableController[] wires,
            CordBridge bridge, TutorialSpotlight view, IReadOnlyList<SocketView> board, RectTransform money,
            Action releaseCall = null)
        {
            Unbind();
            sequence = new FirstOrderGuideSequence();
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
            if (spotlight != null) spotlight.SkipClicked += OnSkipClicked;
            queue.OrderShown += OnOrderShown;
            queue.CardOpened += OnCardOpened;
            calls.CallEnded += OnCallEnded;
            if (book != null) book.OpenChanged += OnBookChanged;
            foreach (var cable in wires) RegisterCable(cable);
            RefreshHints();
        }

        /// <summary>Steps 8 and 9: buy the first extra cord once affordable, then look at the mystery upgrade.</summary>
        public void BindShop(ShopView shopView, UpgradeService service)
        {
            shop = shopView;
            upgrades = service;
            IReadOnlyList<IUpgrade> chain = upgrades != null ? upgrades.ChainOf(UpgradeKind.ExtraWires) : null;
            firstCord = chain != null && chain.Count > 0 ? chain[0] : null;
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

        private void LateUpdate()
        {
            if (queue == null) return;
            if (firstCard != null && firstCard.Call != firstCall && !sequence.IsFirstCallOver) sequence.FirstOrderEnded();
            AdvanceText();
            if (firstCall != null && firstCall.State == CallState.Connected) sequence.FirstOrderConnected();
            sequence.CordsOut(AnyCordOut());
            FirstOrderGuideStage stage = sequence.Stage;
            if (stage == FirstOrderGuideStage.WaitingForShop || stage == FirstOrderGuideStage.BuyCord) PollShop();
            if (stage == FirstOrderGuideStage.Connected || stage == FirstOrderGuideStage.Mystery)
            {
                // Game time, so the pause menu holds these steps too; a click on the pause menu doesn't dismiss them.
                sequence.Tick(Time.deltaTime);
                Pointer pointer = Pointer.current;
                if (Time.timeScale > 0 && pointer != null && pointer.press.wasPressedThisFrame) sequence.AnyClick();
            }
            RefreshHints();
        }

        private void PollShop()
        {
            if (shop == null || upgrades == null || firstCord == null) { sequence.ShopUnavailable(); return; }
            if (upgrades.IsOwned(firstCord.Id))
            {
                sequence.CordBought(shop.Secret != null && shop.ButtonFor(shop.Secret) != null);
                return;
            }
            // The first cord isn't for sale on the shelf (another chain holds every slot): no shop steps.
            if (shop.ButtonFor(firstCord) == null) { sequence.ShopUnavailable(); return; }
            // Not while the book is open: its pages would sit dimmed between the player and the shop.
            sequence.CordAffordable(upgrades.CanBuy(firstCord.Id) == PurchaseCheck.Ok && (book == null || !book.IsOpen));
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

        private int TrayCordCount()
        {
            int count = 0;
            foreach (CableController cable in cables)
                if (cable != null && cable.gameObject.activeInHierarchy && cable.IsInTray && cable.TrayView != null) count++;
            return count;
        }

        private void RefreshHints()
        {
            FirstOrderGuideStage stage = Stage;
            bool changed = renderedStage != stage
                || (stage == FirstOrderGuideStage.ReadingBook && book != null && book.CurrentSpreadIndex != renderedSpread)
                || (stage == FirstOrderGuideStage.Cables && TrayCordCount() != renderedTrayCords);
            if (!changed) return;
            renderedStage = stage;
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
                    text = texts.readMessage;
                    if (firstCard != null) Add(ref count, firstCard.SpeechBubbleRect, 10, clickable: false);
                    break;
                case FirstOrderGuideStage.Book:
                    text = texts.openBook;
                    if (book != null && book.ClosedBookGraphic != null) Add(ref count, book.ClosedBookGraphic.rectTransform, 12);
                    break;
                case FirstOrderGuideStage.ReadingBook:
                    text = texts.findJacks;
                    // The whole open book takes clicks: page corners turn the pages, the rest closes it.
                    anchor = clickArea = book != null ? book.OpenBookRect : null;
                    AddBookRow(ref count, firstCall.Request.From);
                    AddBookRow(ref count, firstCall.Request.To);
                    break;
                case FirstOrderGuideStage.Cables:
                    text = texts.takeCord;
                    foreach (CableController cable in cables)
                        if (cable != null && cable.gameObject.activeInHierarchy && cable.IsInTray && cable.TrayView != null)
                            Add(ref count, (RectTransform)cable.TrayView.transform, 10);
                    break;
                case FirstOrderGuideStage.Plugging:
                    text = texts.plugJacks;
                    AddSocket(ref count, firstCall.Request.From);
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
            if (row != null) { Add(ref count, row, 4, clickable: false); return; }
            RectTransform corner = book.PageCorner(book.SpreadDirectionTo(jack.Label));
            for (int i = 0; i < count; i++) if (spots[i].Rect == corner) return;
            Add(ref count, corner, 6);
        }

        private void AddSocket(ref int count, JackId jack)
        {
            if (sockets.TryGetValue(jack.Label, out SocketView socket)) Add(ref count, (RectTransform)socket.transform, 8, true);
        }

        private void Add(ref int count, RectTransform rect, float padding, bool round = false, bool clickable = true)
        {
            if (rect == null || count >= spots.Length) return;
            spots[count++] = new SpotlightTarget(rect, padding, round, clickable);
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
            if (spotlight != null) spotlight.SkipClicked -= OnSkipClicked;
            foreach (var cable in cables) if (cable != null) cable.PickedUp -= OnCablePickedUp;
            cables.Clear();
            sockets.Clear();
            cordBridge = null;
            queue = null; calls = null; book = null;
            shop = null; upgrades = null; firstCord = null;
            // A guide that stops early (skipped, or the scene closing) never leaves the call held.
            Action release = firstCallOver;
            firstCallOver = null;
            release?.Invoke();
        }
    }
}
