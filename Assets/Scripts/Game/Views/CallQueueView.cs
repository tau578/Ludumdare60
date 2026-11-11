using System;
using System.Collections.Generic;
using Switchboard.Content;
using Switchboard.Domain;
using Switchboard.UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>
    /// Every live call has a card over one of the caller slots (Design 4.3, 5.4). The newest card is the top one (D80): the
    /// cards rest on the lowest open slots, oldest at the bottom, so a new card lands on top of them, and when a card
    /// leaves, the cards above it slide down into its place. Ended cards keep their slot while the stamp shows, so a call
    /// the queue fills in at once (D52) waits for that stamp before its card appears.
    /// Only the open slots take cards (D49); closed slot artwork is hidden without changing the layout.
    /// D83 (13a pass 2b): the windows hold waiting calls only. A talking call's card shrinks into a tab in a 2x2 grid in the
    /// space of the first closed window (the 4th), and its window takes the next caller; an ended tab shows its stamp there.
    /// </summary>
    public sealed class CallQueueView : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("card"), Tooltip("The first card; the others are copies made at startup.")]
        private CallCardView cardTemplate;
        [SerializeField] private RectTransform cardArea;
        [SerializeField, Min(0), Tooltip("How fast cards slide to a new slot; 0 snaps.")]
        private float slideSpeed = 14f;
        [SerializeField] private Sprite callerFrame;
        [SerializeField] private UiClickSound takeSound;
        [SerializeField, Min(0), Tooltip("How far left of its slot a new card starts its slide, in card widths (D60, V4).")]
        private float arrivalWidths = .75f;

        [Header("Talking-call tabs (D83), in canvas units")]
        [SerializeField, Min(10), Tooltip("A tab's portrait width.")]
        private float tabSize = 80f;
        [SerializeField, Tooltip("The gaps between the tabs' portraits: across, and down (room for the money plate).")]
        private Vector2 tabSpacing = new Vector2(30f, 40f);
        [SerializeField, Tooltip("Moves the whole grid from the centre of the 4th window's space.")]
        private Vector2 tabGridOffset = new Vector2(-10f, 6f);

        /// <summary>The most tabs the grid holds: the most cords today (D83).</summary>
        public const int MaxTabs = 4;

        private readonly List<CallCardView> pool = new List<CallCardView>();
        private readonly List<CallCardView> shown = new List<CallCardView>();
        // Talking calls (and their end stamps), in the order they connected: the grid fills left to right, top to bottom.
        private readonly List<CallCardView> tabs = new List<CallCardView>();
        private readonly List<Call> waitingForSlot = new List<Call>();
        private readonly Vector3[] slotCorners = new Vector3[4];
        private CallService calls;
        private OperatorDesk desk;
        private EarningsService earnings;
        private PenaltyService penalties;
        private Func<CallScript, Sprite> portraitOf;
        private Func<bool, CallCardStyle> styleOf;
        private GameStyle paint;
        private Func<CordId, Color> cordColorOf;
        private RectTransform[] slots = new RectTransform[0];
        private Vector2[] slotCentres = new Vector2[0];
        private Vector2[] slotSizes = new Vector2[0];
        private CanvasGroup[] slotVisibility = new CanvasGroup[0];
        private Vector2[] preferredSlotSizes = new Vector2[0];
        private int openSlots = -1;
        private Vector3[] slotMin = new Vector3[0];
        private Vector3[] slotMax = new Vector3[0];
        private Vector2 slotSize;
        private bool showDurations;
        private bool warnedFull;
        private bool warnedTabs;
        private Action synchronizeBoard;
        private MoneyCounterView moneyCounter;
        public event Action<CallCardView> OrderShown;
        public event Action<CallCardView> CardOpened;

        /// <summary>Dev panel: show each call's hidden duration on its card.</summary>
        public bool HasSpeakingCaller
        {
            get
            {
                foreach (CallCardView card in shown)
                    if (card != null && card.IsGivingTask) return true;
                return false;
            }
        }

        /// <summary>The call whose card is open and still waits for its connection: the call in hand (D58).</summary>
        public Call CallInHand
        {
            get
            {
                foreach (CallCardView card in shown)
                    if (card != null && card.IsGivingTask) return card.Call;
                return null;
            }
        }

        /// <summary>The talking calls' tabs, in grid order (D83).</summary>
        public IReadOnlyList<CallCardView> Tabs => tabs;

        /// <summary>The tab's resting centre in the grid, in the card area's units.</summary>
        public Vector2 TabCentre(int index)
        {
            int zone = Mathf.Clamp(OpenSlotCount, 0, Mathf.Max(0, slots.Length - 1));
            Vector2 centre = zone < slotCentres.Length ? slotCentres[zone] : Vector2.zero;
            int cell = Mathf.Min(index, MaxTabs - 1);
            float across = tabSize + tabSpacing.x, down = tabSize + tabSpacing.y;
            return centre + tabGridOffset + new Vector2((cell % 2 - .5f) * across, (.5f - cell / 2) * down);
        }

        /// <summary>A tab's <see cref="CallCardView.Scale"/>: its portrait art, at the windows' card size, shrunk to <c>tabSize</c>.</summary>
        public float TabScaleOf(CallCardView card)
        {
            float art = card != null ? card.PortraitRect.rect.width : 0;
            return art > 0 ? tabSize / art : .5f;
        }

        /// <summary>The waiting card showing the call; null for a call still waiting for a slot, or talking on a tab.</summary>
        public CallCardView WaitingCardOf(Call call)
        {
            if (call == null) return null;
            foreach (CallCardView card in shown)
                if (card != null && card.Call == call) return card;
            return null;
        }

        /// <summary>Fills the list with the calls on cards, in slot order; a call still waiting for a slot has none.</summary>
        public void CollectShownCalls(List<Call> into)
        {
            into.Clear();
            foreach (CallCardView card in shown)
                if (card != null && card.Call != null) into.Add(card.Call);
        }

        public bool ShowDurations
        {
            get => showDurations;
            set
            {
                showDurations = value;
                foreach (CallCardView card in pool) card.ShowDuration = value;
            }
        }

        public void Bind(CallService callService, OperatorDesk operatorDesk, EarningsService earningsService,
            PenaltyService penaltyService, RectTransform[] callerSlots, Func<CallScript, Sprite> portraits,
            Func<CordId, Color> cordColors, Func<bool, CallCardStyle> cardStyles, GameStyle paintBox,
            Action syncBoard = null, MoneyCounterView counter = null)
        {
            Unsubscribe();
            calls = callService;
            desk = operatorDesk;
            synchronizeBoard = syncBoard;
            moneyCounter = counter;
            earnings = earningsService;
            penalties = penaltyService;
            portraitOf = portraits;
            styleOf = cardStyles;
            paint = paintBox;
            cordColorOf = cordColors;
            slots = callerSlots ?? new RectTransform[0];
            slotCentres = new Vector2[slots.Length];
            slotSizes = new Vector2[slots.Length];
            BuildSlotVisibility();
            slotMin = new Vector3[slots.Length];
            slotMax = new Vector3[slots.Length];
            BuildPool(Mathf.Max(1, slots.Length) + MaxTabs);
            calls.CallAdded += OnCallAdded;
            calls.CallStateChanged += OnCallStateChanged;
            earnings.ConnectionFeePaid += OnConnectionFeePaid;
            earnings.QuickConnectTipPaid += OnQuickConnectTipPaid;
            if (penalties != null) penalties.FineApplied += OnFineApplied;
            foreach (CallCardView card in pool)
            {
                card.Clicked += OnCardClicked;
                card.Finished += OnCardFinished;
                card.Hide();
            }
            shown.Clear();
            tabs.Clear();
            waitingForSlot.Clear();
        }

        /// <summary>How many slots, from the top, take calls; the rest are hidden (D49).</summary>
        public void SetOpenSlots(int count)
        {
            openSlots = Mathf.Clamp(count, 0, slots.Length);
            float scale = openSlots == 6 ? .84f : 1f;
            for (int i = 0; i < slotVisibility.Length; i++)
            {
                if (slotVisibility[i] == null) continue;
                var layout = slots[i].GetComponent<LayoutElement>();
                if (layout != null)
                {
                    layout.minWidth = layout.preferredWidth = preferredSlotSizes[i].x * scale;
                    layout.minHeight = layout.preferredHeight = preferredSlotSizes[i].y * scale;
                }
                slotVisibility[i].alpha = i < openSlots ? 1 : 0;
                slotVisibility[i].blocksRaycasts = i < openSlots;
                slotVisibility[i].interactable = i < openSlots;
            }
            ShowWaitingCalls();
        }

        private int OpenSlotCount => openSlots >= 0 ? openSlots : slots.Length;

        // Keep each layout cell in place while hiding all artwork of locked slots.
        private void BuildSlotVisibility()
        {
            slotVisibility = new CanvasGroup[slots.Length];
            preferredSlotSizes = new Vector2[slots.Length];
            if (slots.Length > 0 && slots[0] != null &&
                slots[0].parent.TryGetComponent<VerticalLayoutGroup>(out var layoutGroup))
                layoutGroup.spacing = Mathf.Max(layoutGroup.spacing, 40f);
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                var layout = slots[i].GetComponent<LayoutElement>();
                if (layout != null) preferredSlotSizes[i] = new Vector2(layout.preferredWidth, layout.preferredHeight);
                var group = slots[i].GetComponent<CanvasGroup>();
                if (group == null) group = slots[i].gameObject.AddComponent<CanvasGroup>();
                slotVisibility[i] = group;
                if (callerFrame != null && slots[i].Find("Portrait frame") == null)
                {
                    foreach (var old in slots[i].GetComponentsInChildren<Graphic>(true)) old.enabled = false;
                    var frame = new GameObject("Portrait frame", typeof(RectTransform)).AddComponent<Image>();
                    frame.transform.SetParent(slots[i], false);
                    frame.sprite = callerFrame;
                    frame.raycastTarget = false;
                    var rect = frame.rectTransform;
                    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                    rect.offsetMin = new Vector2(-45, -8); rect.offsetMax = new Vector2(10, 8);
                    frame.preserveAspect = true;
                }
            }
            openSlots = -1;
        }
        private void OnDisable() => Unsubscribe();

        private void Unsubscribe()
        {
            if (calls != null)
            {
                calls.CallAdded -= OnCallAdded;
                calls.CallStateChanged -= OnCallStateChanged;
            }
            if (earnings != null) earnings.ConnectionFeePaid -= OnConnectionFeePaid;
            if (earnings != null) earnings.QuickConnectTipPaid -= OnQuickConnectTipPaid;
            if (penalties != null) penalties.FineApplied -= OnFineApplied;
            foreach (CallCardView card in pool)
            {
                if (card == null) continue;
                card.Clicked -= OnCardClicked;
                card.Finished -= OnCardFinished;
            }
            calls = null;
            earnings = null;
            penalties = null;
        }

        private void BuildPool(int size)
        {
            if (pool.Count == 0) pool.Add(cardTemplate);
            while (pool.Count < size)
            {
                CallCardView copy = Instantiate(cardTemplate, cardTemplate.transform.parent);
                copy.name = $"{cardTemplate.name} {pool.Count + 1}";
                pool.Add(copy);
            }
        }

        private void OnCallAdded(CallAddedArgs e)
        {
            // A stamp still showing keeps its slot; the new call's card appears when the stamp leaves.
            if (waitingForSlot.Count > 0 || FreeCard() == null)
            {
                if (!warnedFull && !AnyEnded())
                {
                    Debug.LogWarning("CallQueueView: more live calls than open caller slots; check the slot values in Economy.json.", this);
                    warnedFull = true;
                }
                waitingForSlot.Add(e.Call);
                return;
            }
            ShowCard(e.Call);
        }

        private void ShowCard(Call call)
        {
            CallCardView card = call.IsTalking ? FreeTabCard() : FreeCard();
            if (card == null) return;
            card.ShowDuration = showDurations;
            CallCardStyle style = styleOf != null ? styleOf(call.Request.Script.IsEmergency) : default;
            card.Show(call, portraitOf?.Invoke(call.Request.Script), cordColorOf, style, paint, earnings?.CurrentMultiplier ?? 1);
            if (call.IsTalking)
            {
                // Already talking (nothing restores one today, but a tab is where it belongs): straight into the grid.
                AddTab(card);
                card.Scale = TabScaleOf(card);
                card.RestPosition = TabCentre(tabs.Count - 1);
                ((RectTransform)card.transform).localPosition = card.RestPosition;
                return;
            }
            shown.Insert(0, card);
            SnapToSlot(card, SlotOf(0));
            // V4: it slides in from the caller panel's edge; it is clickable at once.
            card.PlayArrival(slotSize.x * arrivalWidths);
            ((RectTransform)card.transform).localPosition = card.RestPosition + card.MotionOffset;
            DrawUpperCardsOnTop();
            OrderShown?.Invoke(card);
        }

        // Each card's name plate hangs below it into the gap between slots, so a card draws over the one below it.
        private void DrawUpperCardsOnTop()
        {
            for (int i = 0; i < shown.Count; i++) shown[i].transform.SetAsFirstSibling();
        }

        private void ShowWaitingCalls()
        {
            while (waitingForSlot.Count > 0 && FreeCard() != null)
            {
                Call call = waitingForSlot[0];
                waitingForSlot.RemoveAt(0);
                if (call.State != CallState.Ended) ShowCard(call);
            }
        }

        private bool AnyEnded()
        {
            foreach (CallCardView card in shown)
                if (card.Call != null && card.Call.State == CallState.Ended) return true;
            return false;
        }

        private void OnCallStateChanged(CallStateChangedArgs e)
        {
            // A call that ends before its card could show (the dev panel's clear) never shows.
            if (e.To == CallState.Ended) waitingForSlot.Remove(e.Call);
            CallCardView card = CardOf(e.Call);
            if (card == null) return;
            if (e.Call.IsTalking && shown.Remove(card))
            {
                // D83: the card shrinks into its tab and its window takes the next caller; the cards above slide down.
                AddTab(card);
                DrawUpperCardsOnTop();
                ShowWaitingCalls();
                return;
            }
            card.Refresh();
        }

        private void AddTab(CallCardView card)
        {
            // A full grid cuts the oldest end stamp short, as a full column does (D52).
            if (tabs.Count >= MaxTabs)
            {
                CallCardView stamped = null;
                foreach (CallCardView tab in tabs)
                    if (tab.Call == null || tab.Call.State == CallState.Ended) { stamped = tab; break; }
                if (stamped != null)
                {
                    tabs.Remove(stamped);
                    stamped.Hide();
                }
                else if (!warnedTabs)
                {
                    Debug.LogWarning($"CallQueueView: more than {MaxTabs} talking calls; the tab grid holds {MaxTabs}.", this);
                    warnedTabs = true;
                }
            }
            tabs.Add(card);
            // The same card size as in a window, so the tab's scale alone sets its size.
            if (slotSize.x > 0) ((RectTransform)card.transform).sizeDelta = slotSize;
            card.SetTab(true, TabScaleOf(card));
        }

        private void OnConnectionFeePaid(ConnectionFeePaidArgs e)
        {
            CallCardView card = CardOf(e.Call);
            if (card == null) return;
            card.ShowConnectionFee(e.Cents);
            if (moneyCounter != null) moneyCounter.FlyIncomeFrom(card.PortraitRect);
        }

        private void OnQuickConnectTipPaid(QuickConnectTipPaidArgs e) => CardOf(e.Call)?.ShowQuickConnectTip(e.Cents);

        // "Missed -$2.00" names the amount actually taken, which is known only once the fine lands; the coins taken fly
        // from the counter to the missed card (D60, V2).
        private void OnFineApplied(FineAppliedArgs e)
        {
            CallCardView card = CardOf(e.Call);
            if (card == null) return;
            card.RefreshStamp();
            if (moneyCounter != null) moneyCounter.FlyFineTo(card.PortraitRect, e.TakenCents);
        }

        private void OnCardClicked(CallCardView clicked)
        {
            if (desk == null || clicked.Call == null || clicked.Call.State == CallState.Ended) return;
            bool newOrder = clicked.Call.State == CallState.Incoming;
            if (newOrder)
            {
                synchronizeBoard?.Invoke();
                if (!desk.TryTake(clicked.Call.Id)) return;
            }
            foreach (CallCardView card in shown) card.SetDetailsVisible(card == clicked);
            CardOpened?.Invoke(clicked);
            if (!newOrder) return;
            clicked.PlayPress();
            if (takeSound != null) takeSound.Play();
        }

        private void OnCardFinished(CallCardView card)
        {
            if (!shown.Remove(card)) tabs.Remove(card);
            card.Hide();
            DrawUpperCardsOnTop();
            ShowWaitingCalls();
        }

        private CallCardView FreeCard()
        {
            if (shown.Count >= OpenSlotCount && slots.Length > 0) return null;
            foreach (CallCardView card in pool)
                if (card.Call == null) return card;
            return null;
        }

        private CallCardView FreeTabCard()
        {
            foreach (CallCardView card in pool)
                if (card.Call == null) return card;
            return null;
        }

        private CallCardView CardOf(Call call)
        {
            for (int i = 0; i < shown.Count; i++)
                if (shown[i].Call == call) return shown[i];
            for (int i = 0; i < tabs.Count; i++)
                if (tabs[i].Call == call) return tabs[i];
            return null;
        }

        // The slots are laid out by the caller panel's layout group, possibly a frame late, so keep following them.
        private void LateUpdate()
        {
            if (slots.Length == 0) return;
            bool moved = MeasureSlots();
            float blend = slideSpeed > 0 ? 1f - Mathf.Exp(-slideSpeed * Time.unscaledDeltaTime) : 1f;
            for (int i = 0; i < shown.Count; i++)
            {
                shown[i].SetIncomeMultiplier(earnings?.CurrentMultiplier ?? 1);
                var rect = (RectTransform)shown[i].transform;
                Vector2 target = slotCentres[SlotOf(i)];
                if (moved) rect.sizeDelta = slotSize;
                // The slide between slots moves the resting place; the card's own motion rides on top (D60).
                Vector2 now = shown[i].RestPosition;
                Vector2 rest = (now - target).sqrMagnitude < 0.25f ? target : Vector2.Lerp(now, target, blend);
                shown[i].RestPosition = rest;
                rect.localPosition = rest + shown[i].MotionOffset;
            }
            // Tabs slide into their cell and shrink on the same curve, so the card visibly shrinks into its tab (D83).
            for (int i = 0; i < tabs.Count; i++)
            {
                CallCardView tab = tabs[i];
                float tabScale = TabScaleOf(tab);
                tab.SetIncomeMultiplier(earnings?.CurrentMultiplier ?? 1);
                var rect = (RectTransform)tab.transform;
                if (moved) rect.sizeDelta = slotSize;
                Vector2 target = TabCentre(i);
                Vector2 now = tab.RestPosition;
                tab.RestPosition = (now - target).sqrMagnitude < 0.25f ? target : Vector2.Lerp(now, target, blend);
                rect.localPosition = tab.RestPosition + tab.MotionOffset;
                if (tab.Scale != tabScale)
                    tab.Scale = Mathf.Abs(tab.Scale - tabScale) < .002f ? tabScale : Mathf.Lerp(tab.Scale, tabScale, blend);
            }
        }

        // Cards rest on the lowest open slots, the first (newest) card on top, so a gap always closes from above (D80).
        private int SlotOf(int index) =>
            Mathf.Clamp(Mathf.Min(OpenSlotCount, slots.Length) - shown.Count + index, 0, Mathf.Max(0, slots.Length - 1));

        private void SnapToSlot(CallCardView card, int slot)
        {
            MeasureSlots();
            var rect = (RectTransform)card.transform;
            rect.sizeDelta = slotSize;
            card.RestPosition = slotCentres[Mathf.Clamp(slot, 0, slots.Length - 1)];
            rect.localPosition = card.RestPosition;
        }

        private bool MeasureSlots()
        {
            bool moved = false;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                slots[i].GetWorldCorners(slotCorners);
                if (slotCorners[0] == slotMin[i] && slotCorners[2] == slotMax[i]) continue;
                slotMin[i] = slotCorners[0];
                slotMax[i] = slotCorners[2];
                moved = true;

                // Both canvases are Screen Space Overlay, so world corners are screen pixels.
                RectTransformUtility.ScreenPointToLocalPointInRectangle(cardArea, slotMin[i], null, out Vector2 min);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(cardArea, slotMax[i], null, out Vector2 max);
                slotCentres[i] = (min + max) * 0.5f;
                slotSizes[i] = max - min;
                if (i == 0) slotSize = slotSizes[i];

            }
            return moved;
        }
    }
}
