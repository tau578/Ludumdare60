using System.Collections.Generic;
using Switchboard.Domain;
using Switchboard.UI;
using UnityEngine;

namespace Switchboard.Game
{
    /// <summary>
    /// Faster lookup (D58). The call in hand's caller's jack blinks on the board; waiting callers stay dark (fix 3,
    /// <see cref="CallerLights"/>). A row clicked in the open
    /// book marks its jack with a steady glow, the only steady light, one mark at a time; it clears when a plug goes
    /// into that jack, another row is clicked, or another call is taken. Any row can be marked: it never checks the
    /// call (D53). Pointing the mouse at a waiting call's card flares its dark caller's jack for a moment (fix 2), as
    /// a preview, without taking the card. Polled every LateUpdate, without allocations.
    /// </summary>
    public sealed class CallerJackLights : MonoBehaviour
    {
        private readonly Dictionary<string, SocketView> byJack = new Dictionary<string, SocketView>();
        private readonly List<Call> shownCalls = new List<Call>();
        private SocketView[] sockets = new SocketView[0];
        private JackId[] jacks = new JackId[0];
        private CallQueueView queue;
        private OperatorDesk desk;
        private PhoneBookView book;
        private CableInputController input;
        private GameObject hovered;
        private Call hoveredCall;
        private SocketView marked;
        private bool markedWasOccupied;
        private Call markedFor;

        /// <summary>The marked jack's address, or null.</summary>
        public string MarkedJack => marked != null ? marked.Address : null;

        /// <param name="cordInput">Tells which card the mouse points at, for the hover flare; null for none.</param>
        public void Bind(CallQueueView callQueue, OperatorDesk operatorDesk, PhoneBookView phoneBook,
            IReadOnlyList<SocketView> board, CableInputController cordInput = null)
        {
            Unbind();
            queue = callQueue;
            desk = operatorDesk;
            book = phoneBook;
            input = cordInput;
            var found = new List<SocketView>();
            if (board != null)
                foreach (SocketView socket in board)
                    if (socket != null) found.Add(socket);
            sockets = found.ToArray();
            jacks = new JackId[sockets.Length];
            for (int i = 0; i < sockets.Length; i++)
            {
                jacks[i] = new JackId(sockets[i].Address);
                byJack[jacks[i].Label] = sockets[i];
            }
            if (queue != null) queue.CardOpened += OnCardOpened;
            if (book != null)
            {
                book.CanPickJack = CanMark;
                book.JackPicked += OnJackPicked;
            }
            enabled = true;
            Refresh();
        }

        /// <summary>Only a jack on the open board can be marked (D42); the book lists everyone (D53).</summary>
        public bool CanMark(string jack) => jack != null && byJack.TryGetValue(jack, out SocketView socket) &&
            socket.IsOpen && socket.isActiveAndEnabled;

        /// <summary>Marks the jack, replacing any earlier mark; false (and no change) for a jack that can't be marked.</summary>
        public bool Mark(string jack)
        {
            if (!CanMark(jack)) return false;
            marked = byJack[jack];
            markedWasOccupied = marked.IsOccupied;
            markedFor = queue != null ? queue.CallInHand : null;
            Refresh();
            return true;
        }

        public void ClearMark()
        {
            marked = null;
            markedFor = null;
        }

        private void OnJackPicked(string jack) => Mark(jack);

        // Opening another card (taking a new call, or going back to another one) ends the old lookup.
        private void OnCardOpened(CallCardView card)
        {
            if (marked != null && card.Call != markedFor) ClearMark();
            Refresh();
        }

        private void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (marked != null)
            {
                bool occupied = marked.IsOccupied;
                // A plug going in clears the mark; a jack marked with a plug already in waits for the next one.
                if ((occupied && !markedWasOccupied) || !marked.IsOpen) ClearMark();
                else markedWasOccupied = occupied;
            }
            if (queue != null) queue.CollectShownCalls(shownCalls);
            else shownCalls.Clear();
            Call inHand = queue != null ? queue.CallInHand : null;
            for (int i = 0; i < sockets.Length; i++)
            {
                SocketView socket = sockets[i];
                if (socket == null) continue;
                JackLight light = CallerLights.AtJack(jacks[i], shownCalls, inHand, desk);
                // The mark wins: it is the player's latest choice, even when they picked the caller's own row.
                socket.SetLamp(socket == marked ? SocketLamp.Steady
                    : light == JackLight.InHand ? SocketLamp.Blinking
                    : light == JackLight.Waiting ? SocketLamp.Waiting : SocketLamp.Off);
            }
            FlareHoveredCaller(inHand);
        }

        // The cord input already finds the top hit every frame; a card under it needs no raycast target of its own.
        private void FlareHoveredCaller(Call inHand)
        {
            GameObject now = input != null ? input.HoverTarget : null;
            if (now == hovered) return;
            hovered = now;
            CallCardView card = now != null ? now.GetComponentInParent<CallCardView>() : null;
            Call call = card != null ? card.Call : null;
            if (call == hoveredCall) return;
            hoveredCall = call;
            // Only a waiting caller (ringing, or accepted but not in hand) whose jack is dark: no plug, no mark.
            if (call == null || CallerLights.Of(call, inHand) != JackLight.Waiting) return;
            if (byJack.TryGetValue(call.Request.From.Label, out SocketView socket) && socket.Lamp == SocketLamp.Waiting)
                socket.FlareLamp();
        }

        private void OnDisable() => Unbind();

        private void Unbind()
        {
            if (queue != null) queue.CardOpened -= OnCardOpened;
            if (book != null)
            {
                book.JackPicked -= OnJackPicked;
                if (book.CanPickJack == (System.Func<string, bool>)CanMark) book.CanPickJack = null;
            }
            foreach (SocketView socket in sockets)
                if (socket != null) socket.SetLamp(SocketLamp.Off);
            queue = null;
            desk = null;
            book = null;
            input = null;
            hovered = null;
            hoveredCall = null;
            marked = null;
            markedFor = null;
            sockets = new SocketView[0];
            jacks = new JackId[0];
            byJack.Clear();
            shownCalls.Clear();
        }
    }
}
