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
    /// Every live call has a card over one of the team's caller slots, in arrival order (Design 4.3, 5.4).
    /// When a card leaves, the cards below it slide up. Ended cards keep their slot while the stamp shows, so a call
    /// the queue fills in at once (D52) waits for that stamp before its card appears.
    /// Only the open slots take cards (D49); closed slot artwork is hidden without changing the layout.
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

        private readonly List<CallCardView> pool = new List<CallCardView>();
        private readonly List<CallCardView> shown = new List<CallCardView>();
        private readonly List<Call> waitingForSlot = new List<Call>();
        private readonly Vector3[] slotCorners = new Vector3[4];
        private CallService calls;
        private OperatorDesk desk;
        private EarningsService earnings;
        private PenaltyService penalties;
        private Func<CallScript, Sprite> portraitOf;
        private Func<bool, CallCardStyle> styleOf;
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
            Func<CordId, Color> cordColors, Func<bool, CallCardStyle> cardStyles, Action syncBoard = null,
            MoneyCounterView counter = null)
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
            cordColorOf = cordColors;
            slots = callerSlots ?? new RectTransform[0];
            slotCentres = new Vector2[slots.Length];
            slotSizes = new Vector2[slots.Length];
            BuildSlotVisibility();
            slotMin = new Vector3[slots.Length];
            slotMax = new Vector3[slots.Length];
            BuildPool(Mathf.Max(1, slots.Length));
            calls.CallAdded += OnCallAdded;
            calls.CallStateChanged += OnCallStateChanged;
            earnings.ConnectionFeePaid += OnConnectionFeePaid;
            if (penalties != null) penalties.FineApplied += OnFineApplied;
            foreach (CallCardView card in pool)
            {
                card.Clicked += OnCardClicked;
                card.Finished += OnCardFinished;
                card.Hide();
            }
            shown.Clear();
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
            CallCardView card = FreeCard();
            card.ShowDuration = showDurations;
            CallCardStyle style = styleOf != null ? styleOf(call.Request.Script.IsEmergency) : default;
            card.Show(call, portraitOf?.Invoke(call.Request.Script), cordColorOf, style, earnings?.CurrentMultiplier ?? 1);
            shown.Add(card);
            SnapToSlot(card, shown.Count - 1);
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
            CardOf(e.Call)?.Refresh();
        }

        private void OnConnectionFeePaid(ConnectionFeePaidArgs e)
        {
            CallCardView card = CardOf(e.Call);
            if (card == null) return;
            card.ShowConnectionFee(e.Cents);
            if (moneyCounter != null) moneyCounter.FlyIncomeFrom(card.PortraitRect);
        }

        // "Missed -$2.00" names the amount actually taken, which is known only once the fine lands.
        private void OnFineApplied(FineAppliedArgs e) => CardOf(e.Call)?.RefreshStamp();

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
            shown.Remove(card);
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

        private CallCardView CardOf(Call call)
        {
            for (int i = 0; i < shown.Count; i++)
                if (shown[i].Call == call) return shown[i];
            return null;
        }

        // The slots are laid out by the team's layout group, possibly a frame late, so keep following them.
        private void LateUpdate()
        {
            if (slots.Length == 0) return;
            bool moved = MeasureSlots();
            float blend = slideSpeed > 0 ? 1f - Mathf.Exp(-slideSpeed * Time.unscaledDeltaTime) : 1f;
            for (int i = 0; i < shown.Count; i++)
            {
                shown[i].SetIncomeMultiplier(earnings?.CurrentMultiplier ?? 1);
                var rect = (RectTransform)shown[i].transform;
                Vector2 target = slotCentres[Mathf.Min(i, slots.Length - 1)];
                if (moved) rect.sizeDelta = slotSize;
                Vector2 now = rect.localPosition;
                rect.localPosition = (now - target).sqrMagnitude < 0.25f ? target : Vector2.Lerp(now, target, blend);
            }
        }

        private void SnapToSlot(CallCardView card, int index)
        {
            MeasureSlots();
            var rect = (RectTransform)card.transform;
            rect.sizeDelta = slotSize;
            rect.localPosition = slotCentres[Mathf.Min(index, slots.Length - 1)];
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
