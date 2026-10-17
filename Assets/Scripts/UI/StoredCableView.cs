using UnityEngine;
using UnityEngine.UI;

namespace Switchboard.UI
{
    /// <summary>Tray preview uses the same wire and plug graphics as the connected cable.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Image))]
    public sealed class StoredCableView : MonoBehaviour
    {
        [SerializeField] private RectTransform preview;
        [SerializeField] private CableGraphic wire;
        [SerializeField] private PlugGraphic firstPlug;
        [SerializeField] private PlugGraphic secondPlug;
        private Image hitArea;
        public CableController Cable { get; private set; }
        public bool Available => HitArea.enabled && (Cable == null || Cable.IsInTray);
        private Image HitArea => hitArea != null ? hitArea : hitArea = GetComponent<Image>();

        public void Configure(RectTransform visual, CableGraphic line, PlugGraphic first, PlugGraphic second)
        {
            preview = visual;
            wire = line;
            firstPlug = first;
            secondPlug = second;
            UpdateWire();
        }

        public void Bind(CableController cable) => Cable = cable;
        private void OnEnable() => UpdateWire();

        private void UpdateWire()
        {
            if (wire != null && firstPlug != null && secondPlug != null)
                wire.SetEndpoints(firstPlug.rectTransform.anchoredPosition + firstPlug.TailOffset,
                    secondPlug.rectTransform.anchoredPosition + secondPlug.TailOffset);
        }

        public void SetCableColor(Color value)
        {
            if (wire != null) wire.InsulationColor = value;
            if (firstPlug != null) firstPlug.SleeveColor = value;
            if (secondPlug != null) secondPlug.SleeveColor = value;
        }

        public void SetAvailable(bool available)
        {
            HitArea.enabled = available;
            if (preview != null) preview.gameObject.SetActive(available);
        }
    }
}
