using System;
using System.Collections.Generic;
using Switchboard.Domain;
using Switchboard.UI;
using UnityEngine;

namespace Switchboard.Game
{
    /// <summary>
    /// Drives the team's 4 shop buttons (D41): the top 3 follow <see cref="ShopShelf"/>, the bottom one is the
    /// secret with its savings fill. Money and the fill are polled; a button is redrawn only when its state changes.
    /// </summary>
    public sealed class ShopView : MonoBehaviour
    {
        public const int SlotCount = ShopShelf.SlotCount + 1;
        private const int SecretSlot = ShopShelf.SlotCount;
        private const string OwnedText = "Owned";
        [SerializeField] private ShopAudio purchaseAudio;

        private enum SlotState { None, Empty, Affordable, TooExpensive, Owned }

        private readonly UpgradeSlotView[] slots = new UpgradeSlotView[SlotCount];
        private readonly Action[] clickHandlers = new Action[SlotCount];
        private readonly IUpgrade[] shownOffers = new IUpgrade[SlotCount];
        private readonly SlotState[] shownStates = new SlotState[SlotCount];
        private UpgradeService upgrades;
        private ShopShelf shelf;
        private GoalProgress goal;
        private Wallet wallet;
        private MoneyCounterView moneyCounter;
        private long shownCents = -1;
        private float shownFill = -1;
        private bool shownHighlight;
        private bool stale;

        /// <param name="found">The team's shop buttons in the scene; exactly 4 are expected.</param>
        public bool Bind(UpgradeService service, ShopShelf offers, GoalProgress savings, Wallet money, List<UpgradeSlotView> found,
            MoneyCounterView counter = null)
        {
            Unbind();
            if (found == null || found.Count != SlotCount)
            {
                Debug.LogError($"ShopView needs exactly {SlotCount} UpgradeSlotView buttons in the shop panel, but found " +
                    $"{found?.Count ?? 0}. Run Tools/Switchboard/Update Upgrade Shop Buttons.", this);
                return false;
            }
            // Order top to bottom on screen; the layout runs first so the positions are real.
            Canvas.ForceUpdateCanvases();
            found.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));
            upgrades = service;
            shelf = offers;
            goal = savings;
            wallet = money;
            moneyCounter = counter;
            for (int i = 0; i < SlotCount; i++)
            {
                int slot = i;
                slots[i] = found[i];
                clickHandlers[i] = () => OnSlotClicked(slot);
                slots[i].Clicked += clickHandlers[i];
            }
            shelf.Changed += OnShelfChanged;
            stale = true;
            return true;
        }

        private void OnDisable() => Unbind();

        private void Unbind()
        {
            if (shelf != null) shelf.Changed -= OnShelfChanged;
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i] != null && clickHandlers[i] != null) slots[i].Clicked -= clickHandlers[i];
                shownOffers[i] = null;
                shownStates[i] = SlotState.None;
                slots[i] = null;
                clickHandlers[i] = null;
            }
            shelf = null;
            moneyCounter = null;
        }

        /// <summary>The mystery upgrade on the pinned bottom button; null when the file has none.</summary>
        public IUpgrade Secret => shelf != null ? goal?.Goal : null;

        /// <summary>The button that sells this upgrade right now, or null (the tutorial's shop steps, D57).</summary>
        public RectTransform ButtonFor(IUpgrade upgrade)
        {
            if (shelf == null || upgrade == null) return null;
            for (int i = 0; i < SlotCount; i++)
                if (slots[i] != null && OfferAt(i) == upgrade) return (RectTransform)slots[i].transform;
            return null;
        }

        private void OnShelfChanged() => stale = true;

        private void OnSlotClicked(int slot)
        {
            IUpgrade offer = OfferAt(slot);
            if (offer == null) return;
            PurchaseCheck check = upgrades.CanBuy(offer.Id);
            if (check == PurchaseCheck.TooExpensive)
            {
                if (moneyCounter != null) moneyCounter.ShowInsufficientFunds();
                if (purchaseAudio != null) purchaseAudio.PlayInsufficientFunds();
            }
            if (check != PurchaseCheck.Ok) return;
            if (upgrades.TryBuy(offer.Id) && purchaseAudio != null)
                purchaseAudio.PlayPurchased(offer.Kind == UpgradeKind.Secret);
        }

        private IUpgrade OfferAt(int slot)
        {
            if (slot < SecretSlot) return shelf.OfferAt(slot);
            return goal.Goal;
        }

        private void LateUpdate()
        {
            if (shelf == null) return;
            if (stale || wallet.Cents != shownCents)
            {
                stale = false;
                shownCents = wallet.Cents;
                for (int i = 0; i < SlotCount; i++) Refresh(i);
            }
            if (goal.Goal == null) return;
            float progress = (float)goal.Progress;
            if (progress != shownFill)
            {
                shownFill = progress;
                slots[SecretSlot].SetFill(progress);
            }
            bool highlight = progress >= 1 && !goal.IsPurchased;
            if (highlight != shownHighlight)
            {
                shownHighlight = highlight;
                slots[SecretSlot].SetHighlight(highlight);
            }
        }

        private void Refresh(int slot)
        {
            IUpgrade offer = OfferAt(slot);
            SlotState state = offer == null ? SlotState.Empty
                : upgrades.IsOwned(offer.Id) ? SlotState.Owned
                : wallet.Cents >= offer.CostCents ? SlotState.Affordable
                : SlotState.TooExpensive;
            if (offer == shownOffers[slot] && state == shownStates[slot]) return;
            shownOffers[slot] = offer;
            shownStates[slot] = state;

            UpgradeSlotView view = slots[slot];
            if (offer != null)
                view.SetArtwork(offer.Kind == UpgradeKind.ExtraWires, offer.Kind == UpgradeKind.ExtraIncome, offer.Kind == UpgradeKind.Secret);
            string title = offer == null ? string.Empty : offer.Kind == UpgradeKind.ExtraWires ? "More Cables"
                : offer.Kind == UpgradeKind.ExtraClients ? "More Clients"
                : offer.Kind == UpgradeKind.ExtraIncome ? "More Profit"
                : offer.Kind == UpgradeKind.Secret ? "??????????" : offer.Name;
            if (state == SlotState.Empty) view.ShowEmpty();
            else if (state == SlotState.Owned) view.Show(title, OwnedText, string.Empty, true);
            else view.Show(title, offer.Description, MoneyFormat.Dollars(offer.CostCents), state == SlotState.Affordable);
        }
    }
}
