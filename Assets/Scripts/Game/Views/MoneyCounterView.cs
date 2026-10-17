using Switchboard.Domain;
using TMPro;
using UnityEngine;

namespace Switchboard.Game
{
    public sealed class MoneyCounterView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private Wallet wallet;
        private long shownCents = -1;

        public void Bind(Wallet source)
        {
            wallet = source;
            shownCents = -1;
        }

        // Money changes every frame while calls pay; the text changes only when the shown cents do.
        private void LateUpdate()
        {
            if (wallet == null || wallet.Cents == shownCents) return;
            shownCents = wallet.Cents;
            label.text = MoneyFormat.Dollars(shownCents);
        }
    }
}
