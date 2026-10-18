using System.Reflection;
using NUnit.Framework;
using Switchboard.Domain;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Switchboard.Game.Tests
{
    public sealed class IncomeCoinsTests
    {
        [Test]
        public void IncomeWaitsForAllCoinsButSpendingAndDisableDoNotLeaveStaleBalance()
        {
            var root = new GameObject("Reward test", typeof(RectTransform), typeof(Canvas));
            try
            {
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var counterObject = new GameObject("Counter", typeof(RectTransform));
                counterObject.transform.SetParent(root.transform, false);
                var label = counterObject.AddComponent<TextMeshProUGUI>();
                var counter = counterObject.AddComponent<MoneyCounterView>();
                var serialized = new SerializedObject(counter);
                serialized.FindProperty("label").objectReferenceValue = label;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var portrait = new GameObject("Portrait", typeof(RectTransform));
                portrait.transform.SetParent(root.transform, false);
                var wallet = new Wallet(100);
                counter.Bind(wallet, null, null, null);
                Assert.That(label.text, Is.EqualTo(MoneyFormat.Dollars(100)));
                wallet.Earn(200);
                counter.FlyIncomeFrom((RectTransform)portrait.transform);
                var coins = root.GetComponentInChildren<IncomeCoinsView>();
                Assert.That(coins.GetComponentsInChildren<IncomeCoinGraphic>().Length, Is.EqualTo(4));
                foreach (var coin in coins.GetComponentsInChildren<IncomeCoinGraphic>())
                    Assert.That(coin.GetComponent<CanvasRenderer>(), Is.Not.Null);
                coins.Tick(.3f);
                Invoke(counter, "RefreshBalance");
                Assert.That(label.text, Is.EqualTo(MoneyFormat.Dollars(100)));
                wallet.Earn(50);
                counter.FlyIncomeFrom((RectTransform)portrait.transform);
                coins.Tick(.53f);
                Assert.That(coins.IsFlying, Is.True, "The second reward must finish its own flight.");
                coins.Tick(.4f);
                Invoke(counter, "RefreshBalance");
                Assert.That(coins.IsFlying, Is.False);
                Assert.That(label.text, Is.EqualTo(MoneyFormat.Dollars(350)));
                counter.FlyIncomeFrom((RectTransform)portrait.transform);
                wallet.ApplyFine(100);
                Invoke(counter, "RefreshBalance");
                Assert.That(label.text, Is.EqualTo(MoneyFormat.Dollars(250)));
                Invoke(counter, "OnDisable");
                Assert.That(coins.IsFlying, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void Invoke(object target, string method) => target.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }
}
