using System;
using Switchboard.Content;
using Switchboard.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Switchboard.Game
{
    /// <summary>
    /// A view of one hand-written call (Design 5.4, D51): the portrait with the caller's name under it, ringing while
    /// it waits, its own message in a speech bubble while it is in hand, the cord's colour and the money earned while
    /// connected or on hold, then a stamp. Emergencies also get their badge, their accent and a countdown ring.
    /// </summary>
    public sealed class CallCardView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image portrait;
        [SerializeField, Tooltip("The caller's name under the portrait.")] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text rateLabel;
        [SerializeField] private GameObject stamp;
        [SerializeField] private TMP_Text stampLabel;
        [SerializeField] private Outline stampOutline;
        [SerializeField] private Color paidInk = new Color32(38, 104, 58, 255);
        [SerializeField] private Color wrongInk = new Color32(164, 38, 38, 255);
        [SerializeField] private Color cutOffInk = new Color32(92, 80, 68, 255);
        [SerializeField] private Color missedInk = new Color32(116, 18, 30, 255);
        [SerializeField] private GameObject speechBubble;
        [SerializeField] private TMP_Text speechLabel;
        [SerializeField, Min(1), Tooltip("How fast the bubble's text types out.")]
        private float charactersPerSecond = 40f;
        [SerializeField] private FloatingTextView feePopup;
        [SerializeField, Tooltip("A ring around the portrait in the colour of the call's cord.")]
        private Graphic cordRing;
        [SerializeField, Tooltip("The caller type's badge, e.g. EMERGENCY.")] private GameObject typeBadge;
        [SerializeField] private TMP_Text typeBadgeLabel;
        [SerializeField] private Graphic typeBadgeBackground;
        [SerializeField, Tooltip("Takes the type's accent on cards of a type that has one.")] private Outline rateOutline;
        [SerializeField, Tooltip("The countdown ring's dark track; shown while a timed call waits.")]
        private GameObject responseRingRoot;
        [SerializeField, Tooltip("A radial-filled ring that empties as the response time runs out.")]
        private Image responseRing;
        [SerializeField, Min(0), Tooltip("Seconds left when the ring turns urgent.")] private float urgentSeconds = 10f;
        [SerializeField] private Color urgentRingColor = new Color32(255, 92, 48, 255);
        [SerializeField, Min(0)] private float urgentPulsesPerSecond = 3f;
        [SerializeField] private GameObject earningsBadge;
        [SerializeField] private TMP_Text earningsLabel;
        [SerializeField, Range(0, 1), Tooltip("How strongly the cord ring and earnings show while the call is on hold.")]
        private float onHoldAlpha = 0.45f;
        [SerializeField, Tooltip("Dev only: the hidden duration, shown when the dev panel asks.")]
        private GameObject durationBadge;
        [SerializeField] private TMP_Text durationLabel;
        [SerializeField, Min(0)] private float stampSeconds = 1.5f;
        [SerializeField, Tooltip("Squashes and bounces when a click takes the call.")]
        private RectTransform pressTarget;
        [SerializeField, Tooltip("A white copy of the portrait that flashes on the press.")]
        private Image pressFlash;
        [SerializeField, Min(0.01f)] private float pressSeconds = 0.2f;
        [SerializeField, Range(0, 1)] private float pressFlashAlpha = 0.45f;

        [SerializeField] private Color newOrderInk = new Color32(123, 82, 32, 255);
        [SerializeField, FormerlySerializedAs("newOrderPopSeconds"), Min(.05f)] private float stampPopSeconds = .38f;
        [SerializeField, FormerlySerializedAs("newOrderOvershoot"), Range(1, 1.4f)] private float stampOvershoot = 1.16f;
        [SerializeField, Min(.01f)] private float stampExitSeconds = .22f;
        private RectTransform stampRect;
        private CanvasGroup stampGroup;
        private StampExitAnimation stampExit;
        private bool endingStampExit;
        private bool finishedRaised;
        private Vector2 stampSize;
        private Vector3 stampScale;
        private bool showingStamp;
        private CallState shownStampState;
        private float stampPopTime = -1;
        private Sprite defaultPortrait;
        private Color earningsColor;
        private Color rateOutlineColor;
        private Color typeBadgeColor;
        private Color ringColor;
        private float shownRingFill = -1;
        private bool ringWasUrgent;
        private bool initialized;
        private float stampTimeLeft;
        private Func<CordId, Color> cordColors;
        private long shownCents = -1;
        private int shownSeconds = -1;
        private readonly TypingReveal typing = new TypingReveal();
        private string typedText;
        private float pressTime = -1;
        private bool detailsVisible;

        public event Action<CallCardView> Clicked;

        /// <summary>Raised after the status stamp's display time and exit animation finish.</summary>
        public event Action<CallCardView> Finished;

        public Call Call { get; private set; }
        public RectTransform PortraitRect => portrait != null ? portrait.rectTransform : (RectTransform)transform;
        public bool ShowDuration { get; set; }
        public bool IsSpeechBubbleVisible => Call != null && speechBubble != null && speechBubble.activeInHierarchy;
        public bool IsGivingTask => IsSpeechBubbleVisible && (Call.State == CallState.Taken || Call.State == CallState.Connecting);
        public bool IsTaskTextComplete => IsGivingTask && !typing.IsTyping;

        public void Show(Call call, Sprite callerPortrait, Func<CordId, Color> cordColorOf, CallCardStyle style)
        {
            gameObject.SetActive(true);
            Initialize();
            Call = call;
            cordColors = cordColorOf;
            portrait.sprite = callerPortrait != null ? callerPortrait : defaultPortrait;
            if (nameLabel != null) nameLabel.text = call.Request.Script.Name;
            rateLabel.text = MoneyFormat.CardRate(call.Request.ConnectionFeeCents, call.Request.CentsPerSecond);
            ApplyStyle(style);
            shownRingFill = -1;
            ringWasUrgent = false;
            feePopup.Hide();
            shownCents = -1;
            shownSeconds = -1;
            typedText = null;
            detailsVisible = false;
            ResetStamp();
            finishedRaised = false;
            StopPress();
            Refresh();
        }

        public void Refresh()
        {
            if (Call == null) return;
            CallState state = Call.State;
            bool inHand = state == CallState.Taken || state == CallState.Connecting;
            bool onHold = state == CallState.OnHold;
            // An on-hold call keeps its cord ring and its earnings, frozen until it resumes (D07).
            bool onCord = state == CallState.Connected || onHold;

            bool waiting = state == CallState.Incoming || inHand;
            // The type badge sits on top while the call waits; once connected the earnings take that place.
            bool showTypeBadge = waiting && typeBadge != null && !string.IsNullOrEmpty(typeBadgeLabel.text);
            if (typeBadge != null) typeBadge.SetActive(showTypeBadge);
            // The response timer stops for good on a correct connection (D09), so the ring goes with it.
            if (responseRingRoot != null) responseRingRoot.SetActive(Call.HasResponseTime && waiting);
            earningsBadge.SetActive(onCord);
            cordRing.gameObject.SetActive(onCord);
            if (onCord && Call.Cord is CordId cord && cordColors != null) cordRing.color = Faded(cordColors(cord), onHold);
            earningsLabel.color = Faded(earningsColor, onHold);

            bool showSpeech = detailsVisible && (inHand || onCord);
            speechBubble.SetActive(showSpeech);
            if (showSpeech)
                Say(onHold ? "On hold"
                    : onCord ? "On the line"
                    : Call.Request.Script.Message);
            else typedText = null;

            if (state == CallState.Incoming)
            {
                ShowStamp(state);
                stampLabel.text = "New order!";
                stampLabel.color = stampOutline.effectColor = newOrderInk;
                return;
            }
            if (state != CallState.Ended)
            {
                DismissStamp();
                return;
            }
            durationBadge.SetActive(false);
            feePopup.Hide();
            // Cleared calls leave at once; the others show what happened for a moment.
            if (Call.EndReason == CallEndReason.Cleared || stampSeconds <= 0)
            {
                ResetStamp();
                stampTimeLeft = 0;
                return;
            }
            if (!endingStampExit && ShowStamp(state)) stampTimeLeft = stampSeconds;
            RefreshStamp();
        }

        /// <summary>The fine is known only after the call has ended, so the stamp is written again when it lands.</summary>
        public void RefreshStamp()
        {
            if (Call == null || Call.State != CallState.Ended) return;
            stampLabel.text = StampText(Call);
            stampLabel.color = stampOutline.effectColor = Ink(Call.EndReason);
        }

        public void ShowConnectionFee(double cents) => feePopup.Show("+" + MoneyFormat.Cents(cents));

        public void SetDetailsVisible(bool visible)
        {
            if (detailsVisible == visible) return;
            detailsVisible = visible;
            Refresh();
        }

        /// <summary>A quick squash, bounce and brighten for a click that took the call.</summary>
        public void PlayPress()
        {
            if (pressTarget == null) return;
            pressTime = 0;
            if (pressFlash == null) return;
            pressFlash.sprite = portrait.sprite;
            pressFlash.gameObject.SetActive(true);
        }

        public void Hide()
        {
            Call = null;
            cordColors = null;
            speechBubble.SetActive(false);
            typedText = null;
            // A full queue may reuse this card before its normal lifetime ends.
            // Let its stamp finish independently, without delaying the new call.
            DismissStamp();
            StopPress();
            feePopup.Hide();
            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && Call != null && Call.State != CallState.Ended)
                Clicked?.Invoke(this);
        }

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            defaultPortrait = portrait.sprite;
            stampRect = (RectTransform)stamp.transform;
            stampSize = stampRect.rect.size;
            stampScale = stamp.transform.localScale;
            stampGroup = stamp.GetComponent<CanvasGroup>();
            if (stampGroup == null) stampGroup = stamp.AddComponent<CanvasGroup>();
            stampGroup.blocksRaycasts = false;
            stampGroup.interactable = false;
            stampExit = stamp.GetComponent<StampExitAnimation>();
            if (stampExit == null) stampExit = stamp.AddComponent<StampExitAnimation>();
            earningsColor = earningsLabel.color;
            if (rateOutline != null) rateOutlineColor = rateOutline.effectColor;
            if (typeBadgeBackground != null) typeBadgeColor = typeBadgeBackground.color;
            if (responseRing != null) ringColor = responseRing.color;
        }

        // Not colour alone (Design 5.4): the badge text and the ring carry the meaning, the accent only adds to it.
        private void ApplyStyle(CallCardStyle style)
        {
            if (typeBadgeLabel != null) typeBadgeLabel.text = style.HasBadge ? style.badgeText : string.Empty;
            if (typeBadgeBackground != null) typeBadgeBackground.color = style.HasAccent ? Opaque(style.accent, typeBadgeColor.a) : typeBadgeColor;
            if (rateOutline != null) rateOutline.effectColor = style.HasAccent ? Opaque(style.accent, rateOutlineColor.a) : rateOutlineColor;
            if (responseRing == null) return;
            responseRing.color = ringColor;
            responseRing.rectTransform.localScale = Vector3.one;
        }

        private static Color Opaque(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        private void Update()
        {
            if (Call == null) return;
            if (pressTime >= 0) AnimatePress();
            if (typing.IsTyping) TypeOn();
            if (stampPopTime >= 0) AnimateStamp();
            if (Call.State == CallState.Ended && !finishedRaised && !endingStampExit && (stampTimeLeft -= Time.deltaTime) <= 0)
                FinishWithStampExit();
        }

        // Earnings and the dev duration change every frame; the text changes only when the shown number does.
        private void LateUpdate()
        {
            if (Call == null || Call.State == CallState.Ended) return;
            if (responseRing != null && responseRingRoot != null && responseRingRoot.activeSelf) UpdateResponseRing();
            if (Call.State == CallState.Connected || Call.State == CallState.OnHold)
            {
                long cents = (long)Math.Floor(Call.CentsEarned + 1e-9);
                if (cents != shownCents)
                {
                    shownCents = cents;
                    earningsLabel.text = MoneyFormat.Dollars(cents);
                }
            }

            durationBadge.SetActive(ShowDuration);
            if (!ShowDuration) return;
            // While an emergency waits, the dev badge also shows the seconds left to connect it: "25s / 14s".
            bool responding = Call.HasResponseTime && Call.State != CallState.Connected && Call.State != CallState.OnHold;
            int response = responding ? (int)Math.Ceiling(Call.ResponseSecondsLeft) : -1;
            int seconds = (int)Math.Ceiling(Call.SecondsLeft);
            int key = seconds * 1000 + response + 1;
            if (key == shownSeconds) return;
            shownSeconds = key;
            durationLabel.text = responding ? response + "s / " + seconds + "s" : seconds + "s";
        }

        // The fill changes every frame, so it is only written when it moves by a visible step.
        private void UpdateResponseRing()
        {
            double total = Call.Request.Script.ResponseTimeSeconds;
            float fill = total > 0 ? Mathf.Clamp01((float)(Call.ResponseSecondsLeft / total)) : 0f;
            fill = Mathf.Ceil(fill * 360f) / 360f;
            bool urgent = Call.ResponseSecondsLeft <= urgentSeconds;
            if (fill != shownRingFill)
            {
                shownRingFill = fill;
                responseRing.fillAmount = fill;
            }
            if (urgent)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * urgentPulsesPerSecond * Mathf.PI * 2f);
                Color hot = urgentRingColor;
                responseRing.color = new Color(hot.r, hot.g, hot.b, 0.55f + 0.45f * wave);
                responseRing.rectTransform.localScale = Vector3.one * (1f + 0.04f * wave);
                ringWasUrgent = true;
            }
            else if (ringWasUrgent)
            {
                ringWasUrgent = false;
                responseRing.color = ringColor;
                responseRing.rectTransform.localScale = Vector3.one;
            }
        }

        private void AnimateStamp()
        {
            // Bound loading hitches so the first visible frames still show the pop.
            stampPopTime += Mathf.Min(Time.unscaledDeltaTime, .05f);
            float t = Mathf.Clamp01(stampPopTime / Mathf.Max(.05f, stampPopSeconds));
            float scale = t < .55f
                ? Mathf.Lerp(.05f, stampOvershoot, 1f - Mathf.Pow(1f - t / .55f, 3))
                : t < .82f ? Mathf.Lerp(stampOvershoot, .97f, Mathf.SmoothStep(0, 1, (t - .55f) / .27f))
                : Mathf.Lerp(.97f, 1, Mathf.SmoothStep(0, 1, (t - .82f) / .18f));
            stamp.transform.localScale = stampScale * scale;
            if (t >= 1) { stamp.transform.localScale = stampScale; stampPopTime = -1; }
        }

        // All statuses share the same rectangle and animation. Text/fine refreshes do not replay it.
        private bool ShowStamp(CallState state)
        {
            if (showingStamp && shownStampState == state) return false;
            if (stamp.activeSelf) LeaveExitingCopy();
            stampExit.Cancel();
            endingStampExit = false;
            StopPress();
            showingStamp = true;
            shownStampState = state;
            stampRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, stampSize.x);
            stampRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, stampSize.y);
            stamp.SetActive(true);
            stampGroup.alpha = 1;
            stampPopTime = 0;
            stamp.transform.localScale = stampScale * .05f;
            return true;
        }

        private void DismissStamp()
        {
            if (initialized && stamp.activeSelf) LeaveExitingCopy();
            ResetStamp();
        }

        private void LeaveExitingCopy()
        {
            // Keep world geometry, but leave the card's press animation and pool lifecycle behind.
            var departing = Instantiate(stamp, stamp.transform.parent, false);
            departing.name = "DepartingStamp";
            departing.transform.SetParent(transform.parent, true);
            departing.GetComponent<StampExitAnimation>().Play(stampExitSeconds, () => Destroy(departing));
        }

        private void FinishWithStampExit()
        {
            if (!stamp.activeSelf)
            {
                FinishCard();
                return;
            }
            endingStampExit = true;
            stampPopTime = -1;
            StopPress();
            stampExit.Play(stampExitSeconds, () =>
            {
                ResetStamp();
                FinishCard();
            });
        }

        private void FinishCard()
        {
            if (finishedRaised) return;
            finishedRaised = true;
            Finished?.Invoke(this);
        }

        private void ResetStamp()
        {
            showingStamp = false;
            endingStampExit = false;
            stampPopTime = -1;
            if (stampExit != null) stampExit.Cancel();
            if (stampGroup != null) stampGroup.alpha = 1;
            if (initialized) stamp.transform.localScale = stampScale;
            stamp.SetActive(false);
        }

        // Restarts only when the words change, so Taken to Connecting keeps typing where it was.
        private void Say(string text)
        {
            if (text == typedText) return;
            typedText = text;
            speechLabel.text = text;
            speechLabel.maxVisibleCharacters = 0;
            speechLabel.ForceMeshUpdate();
            typing.Restart(speechLabel.textInfo.characterCount);
            speechLabel.maxVisibleCharacters = typing.Visible;
            if (typing.Visible > 0) OnCharacterShown(0);
        }

        private void TypeOn()
        {
            int before = typing.Visible;
            int now = typing.Advance(Time.unscaledDeltaTime, charactersPerSecond);
            for (int i = before; i < now; i++) OnCharacterShown(i);
            speechLabel.maxVisibleCharacters = now;
        }

        // Reserved for character-specific effects; connection babble is independent of typing.
        private void OnCharacterShown(int index)
        {
        }

        private void AnimatePress()
        {
            pressTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(pressTime / pressSeconds);
            float y = PressScale(t);
            pressTarget.localScale = new Vector3(1f + (1f - y) * 0.5f, y, 1f);
            if (pressFlash != null)
                pressFlash.color = new Color(1f, 1f, 1f, pressFlashAlpha * (1f - t) * (1f - t));
            if (t >= 1f) StopPress();
        }

        // Down to 86%, up past full size, then settle.
        private static float PressScale(float t)
        {
            if (t < 0.3f) return Mathf.Lerp(1f, 0.86f, Mathf.SmoothStep(0, 1, t / 0.3f));
            if (t < 0.65f) return Mathf.Lerp(0.86f, 1.06f, Mathf.SmoothStep(0, 1, (t - 0.3f) / 0.35f));
            return Mathf.Lerp(1.06f, 1f, Mathf.SmoothStep(0, 1, (t - 0.65f) / 0.35f));
        }

        private void StopPress()
        {
            pressTime = -1;
            if (pressTarget != null) pressTarget.localScale = Vector3.one;
            if (pressFlash != null) pressFlash.gameObject.SetActive(false);
        }

        private Color Faded(Color color, bool onHold) => onHold ? new Color(color.r, color.g, color.b, color.a * onHoldAlpha) : color;

        private Color Ink(CallEndReason reason)
        {
            switch (reason)
            {
                case CallEndReason.Finished: return paidInk;
                case CallEndReason.DisconnectedEarly: return cutOffInk;
                case CallEndReason.Missed: return missedInk;
                default: return wrongInk;
            }
        }

        private static string StampText(Call call)
        {
            switch (call.EndReason)
            {
                case CallEndReason.Finished: return "Paid " + MoneyFormat.Dollars((long)Math.Round(call.CentsEarned));
                case CallEndReason.WrongConnection: return "Wrong number!";
                case CallEndReason.DisconnectedEarly: return "Cut off";
                // The amount actually taken; nothing could be taken from an empty wallet.
                case CallEndReason.Missed: return call.FineCents > 0 ? "Missed " + MoneyFormat.Dollars(-call.FineCents) : "Missed";
                default: return string.Empty;
            }
        }
    }
}
