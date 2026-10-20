using Switchboard.Cables;
using UnityEngine;

namespace Switchboard.UI
{
    /// <summary>One cable: left-click a free socket to insert, or its occupied socket to remove.</summary>
    [DisallowMultipleComponent]
    public sealed class CableController : MonoBehaviour
    {
        [Header("Connection rules")]
        [SerializeField, Tooltip("Reject connecting both ends of this cable to the same socket.")]
        private bool preventSameSocketConnection = true;

        [Header("UI")]
        [SerializeField] private RectTransform interactionArea;
        [SerializeField] private CableGraphic wire;
        [SerializeField] private PlugGraphic firstPlug;
        [SerializeField] private PlugGraphic secondPlug;
        [SerializeField] private StoredCableView trayCableView;
        [SerializeField] private Color cableColor = new Color32(178, 99, 51, 255);
        [SerializeField] private CableArtwork artwork;

        public void SetArtwork(CableArtwork value) { artwork = value; ApplyColor(); }

        [Header("Sound")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip insertSound;
        [SerializeField] private AudioClip removeSound;
        [SerializeField] private AudioClip blockedSound;

        private CableConnection<SocketView> connection;
        private Vector2 pointer;
        private bool pointerInside;
        private Canvas firstPlugCanvas;
        private Canvas secondPlugCanvas;
        private Canvas wireCanvas;
        private int wireOrder = SwitchboardUISorting.Wire;
        public uint InsertionRevision { get; private set; }
        public SocketView LastInsertionAnchor { get; private set; }
        public bool LastInsertionWasManual { get; private set; }
        public event System.Action<CableController> PickedUp;

        public CablePhase Phase => Connection.Phase;
        public SocketView FirstSocket => Connection.FirstSocket;
        public SocketView SecondSocket => Connection.SecondSocket;
        public bool IsInTray => Connection.IsInTray;
        public Color CableColor => cableColor;
        public StoredCableView TrayView => trayCableView;
        public bool IsPointerVisible => pointerInside && Phase != CablePhase.Connected;
        public int WireSortingOrder => wireOrder;
        // Layers are canvases, so they are only added in scene objects, never in prefab assets.
        private bool Layered => gameObject.scene.IsValid();
        private CableConnection<SocketView> Connection => connection ??
            (connection = new CableConnection<SocketView>(preventSameSocketConnection));

        public void Configure(RectTransform area, CableGraphic cable,
            PlugGraphic plugA, PlugGraphic plugB, AudioSource sound,
            AudioClip insert, AudioClip remove, AudioClip blocked)
        {
            interactionArea = area;
            wire = cable;
            firstPlug = plugA;
            secondPlug = plugB;
            audioSource = sound;
            insertSound = insert;
            removeSound = remove;
            blockedSound = blocked;
            ApplyColor();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            connection = new CableConnection<SocketView>(preventSameSocketConnection);
            if (trayCableView != null) trayCableView.Bind(this);
            ApplyColor();
            RefreshVisuals();
        }

        public void SetCableColor(Color value)
        {
            cableColor = value;
            ApplyColor();
        }

        private void ApplyColor()
        {
            if (wire != null) wire.InsulationColor = cableColor;
            if (firstPlug != null) firstPlug.SleeveColor = cableColor;
            if (secondPlug != null) secondPlug.SleeveColor = cableColor;
            if (trayCableView != null) trayCableView.SetCableColor(cableColor);
            if (artwork != null)
            {
                var art = artwork.ForColor(cableColor);
                if (wire != null) wire.ThicknessScale = 1.5f;
                // Tint only additional colours which have no matching painted variant.
                Color.RGBToHSV(cableColor, out float hue, out _, out _);
                Color.RGBToHSV(art.color, out float artHue, out _, out _);
                Color tint = Mathf.Abs(Mathf.DeltaAngle(hue * 360, artHue * 360)) > 45 ? cableColor : Color.white;
                if (firstPlug != null) firstPlug.SetArtwork(art.held, art.inserted, tint);
                if (secondPlug != null) secondPlug.SetArtwork(art.held, art.inserted, tint);
                if (trayCableView != null) trayCableView.SetArtwork(art.stored, tint);
            }
        }

        public bool TryPickUp()
        {
            if (!Connection.TryPickUp()) return false;
            RefreshVisuals();
            PickedUp?.Invoke(this);
            return true;
        }

        public bool TryReturnToTray()
        {
            SocketView first = FirstSocket, second = SecondSocket;
            if (!Connection.TryReturnToTray()) return false;
            if (first != null) first.Release(this);
            if (second != null) second.Release(this);
            if (first != null || second != null) PlaySound(removeSound, 1f, 0.9f);
            pointerInside = false;
            RefreshVisuals();
            return true;
        }

        public void UpdatePointer(Vector2 screenPosition, bool selected = true)
        {
            if (interactionArea == null) return;
            Canvas canvas = interactionArea.GetComponentInParent<Canvas>();
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            pointerInside = selected && RectTransformUtility.RectangleContainsScreenPoint(interactionArea, screenPosition, camera);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(interactionArea, screenPosition, camera, out pointer);
            RefreshVisuals();
        }

        public void HidePointer()
        {
            pointerInside = false;
            RefreshVisuals();
        }

        public bool TryInsert(SocketView socket) => Insert(socket, true);

        private bool Insert(SocketView socket, bool manual)
        {
            if (socket == null || !socket.isActiveAndEnabled || socket.PlugAnchor == null) return false;
            if (!Connection.CanInsert(socket)) return Reject();
            if (!socket.TryOccupy(this)) return Reject();
            bool firstEnd = FirstSocket == null;
            SocketView anchor = firstEnd ? SecondSocket : FirstSocket;
            if (!Connection.TryInsert(socket)) { socket.Release(this); return false; }
            InsertionRevision++;
            LastInsertionAnchor = anchor;
            LastInsertionWasManual = manual;
            PlaySound(insertSound, Phase == CablePhase.Connected ? 1.07f : 1f, 1f);
            RefreshVisuals();
            return true;
        }

        public bool TryRemove(SocketView socket)
        {
            if (!Connection.TryRemove(socket)) return false;
            if (!Connection.Contains(socket)) socket.Release(this);
            PlaySound(removeSound, 1f, 0.9f);
            RefreshVisuals();
            return true;
        }

        public bool TryRestoreConnection()
        {
            return Phase == CablePhase.OneEndConnected && Connection.ReconnectSocket != null &&
                Insert(Connection.ReconnectSocket, false);
        }

        private bool Reject()
        {
            PlayBlockedFeedback();
            return false;
        }

        public void PlayBlockedFeedback() => PlaySound(blockedSound, 1f, 0.18f);

        /// <summary>Rank 0 is the oldest wire; every wire lies over every resting plug (lead, 2026-10-20).</summary>
        public void SetStackRank(int rank)
        {
            wireOrder = SwitchboardUISorting.Wire + Mathf.Clamp(rank, 0, SwitchboardUISorting.Book - SwitchboardUISorting.Wire - 1);
            ApplyWireLayer();
        }

        private void ApplyWireLayer()
        {
            if (wire == null || !Layered) return;
            if (wireCanvas == null) wireCanvas = SwitchboardUISorting.SetLayer(wire.gameObject, wireOrder);
            else if (wireCanvas.sortingOrder != wireOrder) wireCanvas.sortingOrder = wireOrder;
        }

        private void RefreshVisuals()
        {
            RefreshTrayImage();
            if (wire == null || firstPlug == null || secondPlug == null) return;
            bool firstAttached = FirstSocket != null;
            bool secondAttached = SecondSocket != null;
            firstPlug.SetInserted(firstAttached);
            secondPlug.SetInserted(secondAttached);
            bool showFirst = firstAttached || (pointerInside && (Phase == CablePhase.Held || secondAttached));
            bool showSecond = secondAttached || (firstAttached && !secondAttached && pointerInside);
            firstPlug.gameObject.SetActive(showFirst);
            secondPlug.gameObject.SetActive(showSecond);
            if (Layered)
            {
                if (wireCanvas == null) ApplyWireLayer();
                if (firstPlugCanvas == null) firstPlugCanvas = SwitchboardUISorting.SetLayer(firstPlug.gameObject, SwitchboardUISorting.Cable);
                if (secondPlugCanvas == null) secondPlugCanvas = SwitchboardUISorting.SetLayer(secondPlug.gameObject, SwitchboardUISorting.Cable);
                firstPlugCanvas.sortingOrder = firstAttached ? SwitchboardUISorting.Cable : SwitchboardUISorting.Cursor;
                secondPlugCanvas.sortingOrder = secondAttached ? SwitchboardUISorting.Cable : SwitchboardUISorting.Cursor;
                // Attached plugs stay in the main canvas, under every wire's own canvas.
                firstPlugCanvas.overrideSorting = !firstAttached;
                secondPlugCanvas.overrideSorting = !secondAttached;
            }
            firstPlug.rectTransform.anchoredPosition = firstAttached ? LocalAnchor(FirstSocket) : pointer;
            secondPlug.rectTransform.anchoredPosition = secondAttached ? LocalAnchor(SecondSocket) : pointer;

            firstPlug.color = Color.white;
            secondPlug.color = Color.white;

            wire.enabled = firstAttached || secondAttached;
            if (wire.enabled)
                wire.SetEndpoints(firstPlug.rectTransform.anchoredPosition + firstPlug.TailOffset,
                    secondPlug.rectTransform.anchoredPosition + secondPlug.TailOffset);
        }

        private Vector2 LocalAnchor(SocketView socket) => interactionArea.InverseTransformPoint(socket.PlugAnchor.position);

        public void SetTrayCableView(StoredCableView view)
        {
            trayCableView = view;
            if (view != null) { view.Bind(this); view.SetCableColor(cableColor); }
            RefreshTrayImage();
        }

        private void RefreshTrayImage()
        {
            if (trayCableView != null) trayCableView.SetAvailable(Connection.IsInTray);
        }

        private void PlaySound(AudioClip clip, float pitch, float volume)
        {
            if (!Application.isPlaying || audioSource == null || clip == null) return;
            audioSource.pitch = pitch * Random.Range(0.97f, 1.03f);
            audioSource.PlayOneShot(clip, volume);
        }

        private void OnDisable()
        {
            if (connection != null)
            {
                if (connection.FirstSocket != null) connection.FirstSocket.Release(this);
                if (connection.SecondSocket != null) connection.SecondSocket.Release(this);
                connection.Reset();
            }
            RefreshTrayImage();
            if (wire != null) wire.enabled = false;
            if (firstPlug != null) firstPlug.gameObject.SetActive(false);
            if (secondPlug != null) secondPlug.gameObject.SetActive(false);
            pointerInside = false;
        }
    }
}
