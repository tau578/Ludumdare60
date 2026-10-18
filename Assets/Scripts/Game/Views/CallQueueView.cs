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
    /// When a card leaves, the cards below it slide up. Ended cards keep their slot while the stamp shows.
    /// Only the open slots take cards (D49); closed ones get a dark cover that lets clicks through.
    /// </summary>
    public sealed class CallQueueView : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("card"), Tooltip("The first card; the others are copies made at startup.")]
        private CallCardView cardTemplate;
        [SerializeField] private RectTransform cardArea;
        [SerializeField, Min(0), Tooltip("How fast cards slide to a new slot; 0 snaps.")]
        private float slideSpeed = 14f;
        [SerializeField] private UiClickSound takeSound;
        [SerializeField, Tooltip("The cover over a closed caller slot (D49).")]
        private Color closedSlotCover = new Color(.06f, .05f, .04f, .72f);

        private readonly List<CallCardView> pool = new List<CallCardView>();
        private readonly List<CallCardView> shown = new List<CallCardView>();
        private readonly Vector3[] slotCorners = new Vector3[4];
        private CallService calls;
        private OperatorDesk desk;
        private EarningsService earnings;
        private PenaltyService penalties;
        private Notepad notepad;
        private Func<IPerson, Sprite> portraitOf;
        private Func<ICallerType, CallCardStyle> styleOf;
        private Func<CordId, Color> cordColorOf;
        private RectTransform[] slots = new RectTransform[0];
        private Vector2[] slotCentres = new Vector2[0];
        private Vector2[] slotSizes = new Vector2[0];
        private Graphic[] covers = new Graphic[0];
        private int openSlots = -1;
        private Vector3[] slotMin = new Vector3[0];
        private Vector3[] slotMax = new Vector3[0];
        private Vector2 slotSize;
        private bool showDurations;
        private bool warnedFull;
        private Action synchronizeBoard;

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
            PenaltyService penaltyService, RectTransform[] callerSlots, Func<IPerson, Sprite> portraits,
            Func<CordId, Color> cordColors, Func<ICallerType, CallCardStyle> cardStyles, Notepad notes = null,
            Action syncBoard = null)
        {
            Unsubscribe();
            calls = callService;
            desk = operatorDesk;
            synchronizeBoard = syncBoard;
            earnings = earningsService;
            penalties = penaltyService;
            notepad = notes;
            portraitOf = portraits;
            styleOf = cardStyles;
            cordColorOf = cordColors;
            slots = callerSlots ?? new RectTransform[0];
            slotCentres = new Vector2[slots.Length];
            slotSizes = new Vector2[slots.Length];
            BuildCovers();
            slotMin = new Vector3[slots.Length];
            slotMax = new Vector3[slots.Length];
            BuildPool(Mathf.Max(1, slots.Length));
            calls.CallAdded += OnCallAdded;
            calls.CallStateChanged += OnCallStateChanged;
            earnings.ConnectionFeePaid += OnConnectionFeePaid;
            if (penalties != null) penalties.FineApplied += OnFineApplied;
            if (notepad != null) notepad.Noted += OnNoted;
            foreach (CallCardView card in pool)
            {
                card.Clicked += OnCardClicked;
                card.Finished += OnCardFinished;
                card.Hide();
            }
            shown.Clear();
        }

        /// <summary>How many slots, from the top, take calls; the rest are covered (D49).</summary>
        public void SetOpenSlots(int count)
        {
            openSlots = Mathf.Clamp(count, 0, slots.Length);
            for (int i = 0; i < covers.Length; i++)
                if (covers[i] != null) covers[i].gameObject.SetActive(i >= openSlots);
        }

        private int OpenSlotCount => openSlots >= 0 ? openSlots : slots.Length;

        // Behind the cards, over the team's slot art; never a raycast target, so cords and cards still get clicks.
        private void BuildCovers()
        {
            foreach (Graphic cover in covers)
                if (cover != null) Destroy(cover.gameObject);
            covers = new Graphic[slots.Length];
            for (int i = 0; i < covers.Length; i++)
            {
                // A disc, like the slot's round portrait window.
                var cover = new GameObject($"ClosedSlotCover {i + 1}", typeof(RectTransform)).AddComponent<CircleGraphic>();
                cover.transform.SetParent(cardArea, false);
                cover.transform.SetSiblingIndex(0);
                cover.color = closedSlotCover;
                cover.raycastTarget = false;
                cover.gameObject.SetActive(false);
                covers[i] = cover;
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
            if (notepad != null) notepad.Noted -= OnNoted;
            foreach (CallCardView card in pool)
            {
                if (card == null) continue;
                card.Clicked -= OnCardClicked;
                card.Finished -= OnCardFinished;
            }
            calls = null;
            earnings = null;
            penalties = null;
            notepad = null;
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
            // A stamp still showing gives up its slot to a new call.
            if (shown.Count >= OpenSlotCount) RemoveOldestEnded();
            CallCardView card = FreeCard();
            if (card == null)
            {
                if (!warnedFull) Debug.LogWarning("CallQueueView: more live calls than open caller slots; check the slot values in Economy.json.", this);
                warnedFull = true;
                return;
            }
            card.ShowDuration = showDurations;
            CallCardStyle style = styleOf != null ? styleOf(e.Call.Request.Type) : default;
            card.Show(e.Call, portraitOf?.Invoke(e.Call.Request.CallerPerson), cordColorOf, style);
            shown.Add(card);
            SnapToSlot(card, shown.Count - 1);
        }

        private void OnCallStateChanged(CallStateChangedArgs e) => CardOf(e.Call)?.Refresh();

        private void OnConnectionFeePaid(ConnectionFeePaidArgs e) => CardOf(e.Call)?.ShowConnectionFee(e.Cents);

        // "Missed -$2.00" names the amount actually taken, which is known only once the fine lands.
        private void OnFineApplied(FineAppliedArgs e) => CardOf(e.Call)?.RefreshStamp();

        private void OnNoted(CallId id)
        {
            if (calls != null) CardOf(calls.Find(id))?.ShowNotes();
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
            if (!newOrder) return;
            clicked.PlayPress();
            if (takeSound != null) takeSound.Play();
        }

        private void OnCardFinished(CallCardView card)
        {
            shown.Remove(card);
            card.Hide();
        }

        private void RemoveOldestEnded()
        {
            for (int i = 0; i < shown.Count; i++)
            {
                if (shown[i].Call == null || shown[i].Call.State != CallState.Ended) continue;
                OnCardFinished(shown[i]);
                return;
            }
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
                if (i < covers.Length && covers[i] != null)
                {
                    var cover = (RectTransform)covers[i].transform;
                    cover.sizeDelta = slotSizes[i];
                    cover.localPosition = slotCentres[i];
                }
            }
            return moved;
        }
    }
}
